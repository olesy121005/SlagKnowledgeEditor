using SlagKnowledgeEditor.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace SlagKnowledgeEditor.Services
{
    public class CompositionCalculator
    {
        private const double SnapDistance = 12.0;

        public CompositionResult Calculate(
            Point point,
            List<CompositionCalibrationPoint> calibrationPoints)
        {
            if (calibrationPoints == null ||
                calibrationPoints.Count < 3)
            {
                throw new InvalidOperationException(
                    "Недостаточно калибровочных точек.");
            }

            // =========================================================
            // 1. Если попали практически точно в калибровочную точку,
            //    возвращаем её состав.
            // =========================================================

            CompositionCalibrationPoint nearest =
                calibrationPoints
                    .OrderBy(p => DistanceSquared(
                        point,
                        p.ImagePoint))
                    .First();

            if (DistanceSquared(
                    point,
                    nearest.ImagePoint)
                <= SnapDistance * SnapDistance)
            {
                return CreateResult(
                    nearest.Al2O3,
                    nearest.CaO,
                    nearest.MgO,
                    nearest.SiO2);
            }


            // =========================================================
            // 2. Al2O3
            //
            //    Al2O3 для одной диаграммы фиксирован.
            //    Поэтому берём его из калибровки.
            // =========================================================

            double al2o3 =
                calibrationPoints.Average(p => p.Al2O3);


            // =========================================================
            // 3. CaO
            //
            //    CaO читается ТОЛЬКО по наклонным линиям CaO.
            //
            //    Значение CaO является линейной функцией координат
            //    точки на изображении:
            //
            //        CaO = A*x + B*y + C
            //
            //    Коэффициенты определяем по калибровочным точкам.
            // =========================================================

            double caO =
                CalculateComponent(
                    point,
                    calibrationPoints,
                    p => p.CaO);


            // =========================================================
            // 4. MgO
            //
            //    MgO читается по своему направлению линий,
            //    связанному с нижней шкалой MgO.
            //
            //    Это также линейная функция координат:
            //
            //        MgO = A*x + B*y + C
            // =========================================================

            double mgO =
                CalculateComponent(
                    point,
                    calibrationPoints,
                    p => p.MgO);


            // =========================================================
            // 5. SiO2
            //
            //    SiO2 читается ГОРИЗОНТАЛЬНО.
            //
            //    Поэтому его координатная зависимость также
            //    восстанавливается по калибровочным точкам.
            //
            //    При этом используем именно сохранённые значения
            //    SiO2, а не получаем их из CaO/MgO.
            // =========================================================

            double siO2 =
                CalculateComponent(
                    point,
                    calibrationPoints,
                    p => p.SiO2);


            // =========================================================
            // 6. Небольшая коррекция погрешности калибровки.
            //
            //    Идеально:
            //
            //    Al2O3 + CaO + MgO + SiO2 = 100
            //
            //    Из-за округления координат и ручного выбора точек
            //    может получиться небольшое отклонение.
            //
            //    Поэтому корректируем SiO2 на остаток.
            // =========================================================

            double calculatedSum =
                al2o3 +
                caO +
                mgO +
                siO2;

            double correction =
                100.0 - calculatedSum;

            if (Math.Abs(correction) <= 2.0)
            {
                siO2 += correction;
            }


            // =========================================================
            // 7. Ограничиваем значения диапазоном 0...100.
            // =========================================================

            caO = Clamp(caO, 0.0, 100.0);
            mgO = Clamp(mgO, 0.0, 100.0);
            siO2 = Clamp(siO2, 0.0, 100.0);


            // =========================================================
            // 8. Возвращаем результат.
            // =========================================================

            return CreateResult(
                al2o3,
                caO,
                mgO,
                siO2);
        }


        // =============================================================
        // ВОССТАНОВЛЕНИЕ ЛИНЕЙНОЙ ЗАВИСИМОСТИ КОМПОНЕНТА
        //
        // component = A*x + B*y + C
        //
        // Используем ВСЕ калибровочные точки.
        // =============================================================

        private static double CalculateComponent(
            Point point,
            List<CompositionCalibrationPoint> calibrationPoints,
            Func<CompositionCalibrationPoint, double> selector)
        {
            if (calibrationPoints.Count < 3)
            {
                throw new InvalidOperationException(
                    "Для расчёта необходимо минимум 3 калибровочные точки.");
            }


            // ---------------------------------------------------------
            // Формируем систему нормальных уравнений для
            //
            // z = A*x + B*y + C
            //
            // ---------------------------------------------------------

            double sXX = 0.0;
            double sXY = 0.0;
            double sX = 0.0;

            double sYY = 0.0;
            double sY = 0.0;

            double sZx = 0.0;
            double sZy = 0.0;
            double sZ = 0.0;


            foreach (CompositionCalibrationPoint calibrationPoint
                     in calibrationPoints)
            {
                double x =
                    calibrationPoint.ImagePoint.X;

                double y =
                    calibrationPoint.ImagePoint.Y;

                double z =
                    selector(calibrationPoint);


                sXX += x * x;
                sXY += x * y;
                sX += x;

                sYY += y * y;
                sY += y;

                sZx += z * x;
                sZy += z * y;
                sZ += z;
            }


            // =========================================================
            // Решаем систему:
            //
            // [Σx²  Σxy  Σx] [A]   [Σxz]
            // [Σxy  Σy²  Σy] [B] = [Σyz]
            // [Σx   Σy   n ] [C]   [Σz ]
            // =========================================================

            double n =
                calibrationPoints.Count;


            double[,] matrix =
            {
                { sXX, sXY, sX, sZx },
                { sXY, sYY, sY, sZy },
                { sX,  sY,  n,  sZ  }
            };


            Solve3x3(
                matrix,
                out double A,
                out double B,
                out double C);


            // =========================================================
            // Получаем значение компонента для выбранной точки.
            // =========================================================

            double result =
                A * point.X +
                B * point.Y +
                C;


            return result;
        }


        // =============================================================
        // РЕШЕНИЕ СИСТЕМЫ 3x3
        // =============================================================

        private static void Solve3x3(
            double[,] m,
            out double x,
            out double y,
            out double z)
        {
            double determinant =
                m[0, 0] *
                (m[1, 1] * m[2, 2] -
                 m[1, 2] * m[2, 1])

                -

                m[0, 1] *
                (m[1, 0] * m[2, 2] -
                 m[1, 2] * m[2, 0])

                +

                m[0, 2] *
                (m[1, 0] * m[2, 1] -
                 m[1, 1] * m[2, 0]);


            if (Math.Abs(determinant) < 0.000000001)
            {
                throw new InvalidOperationException(
                    "Калибровочные точки расположены неудачно. " +
                    "Невозможно определить координатную зависимость.");
            }


            // ---------------------------------------------------------
            // Определитель для X
            // ---------------------------------------------------------

            double determinantX =
                m[0, 3] *
                (m[1, 1] * m[2, 2] -
                 m[1, 2] * m[2, 1])

                -

                m[0, 1] *
                (m[1, 3] * m[2, 2] -
                 m[1, 2] * m[2, 3])

                +

                m[0, 2] *
                (m[1, 3] * m[2, 1] -
                 m[1, 1] * m[2, 3]);


            // ---------------------------------------------------------
            // Определитель для Y
            // ---------------------------------------------------------

            double determinantY =
                m[0, 0] *
                (m[1, 3] * m[2, 2] -
                 m[1, 2] * m[2, 3])

                -

                m[0, 3] *
                (m[1, 0] * m[2, 2] -
                 m[1, 2] * m[2, 0])

                +

                m[0, 2] *
                (m[1, 0] * m[2, 3] -
                 m[1, 3] * m[2, 0]);


            // ---------------------------------------------------------
            // Определитель для Z
            // ---------------------------------------------------------

            double determinantZ =
                m[0, 0] *
                (m[1, 1] * m[2, 3] -
                 m[1, 3] * m[2, 1])

                -

                m[0, 1] *
                (m[1, 0] * m[2, 3] -
                 m[1, 3] * m[2, 0])

                +

                m[0, 3] *
                (m[1, 0] * m[2, 1] -
                 m[1, 1] * m[2, 0]);


            x =
                determinantX /
                determinant;

            y =
                determinantY /
                determinant;

            z =
                determinantZ /
                determinant;
        }


        // =============================================================
        // РАССТОЯНИЕ МЕЖДУ ТОЧКАМИ
        // =============================================================

        private static double DistanceSquared(
            Point a,
            Point b)
        {
            double dx =
                a.X - b.X;

            double dy =
                a.Y - b.Y;

            return
                dx * dx +
                dy * dy;
        }


        // =============================================================
        // ОГРАНИЧЕНИЕ ЗНАЧЕНИЯ
        // =============================================================

        private static double Clamp(
            double value,
            double min,
            double max)
        {
            if (value < min)
                return min;

            if (value > max)
                return max;

            return value;
        }


        // =============================================================
        // ФОРМИРОВАНИЕ РЕЗУЛЬТАТА
        // =============================================================

        private static CompositionResult CreateResult(
            double al2o3,
            double caO,
            double mgO,
            double siO2)
        {
            return new CompositionResult
            {
                Al2O3 = al2o3,
                CaO = caO,
                MgO = mgO,
                SiO2 = siO2
            };
        }
    }
}