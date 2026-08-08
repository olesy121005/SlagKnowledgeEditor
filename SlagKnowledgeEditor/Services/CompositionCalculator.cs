using SlagKnowledgeEditor.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace SlagKnowledgeEditor.Services
{
    public class CompositionCalculator
    {
        private const double Epsilon = 1e-8;

        public CompositionResult Calculate(
            Point point,
            List<CompositionCalibrationPoint> calibrationPoints)
        {
            if (calibrationPoints == null ||
                calibrationPoints.Count < 4)
            {
                throw new InvalidOperationException(
                    "Для определения состава необходимо минимум 4 калибровочные точки.");
            }

            double al2o3 = calibrationPoints[0].Al2O3;

            // ---------------------------------------------------------
            // 1. Если кликнули практически точно в калибровочную точку,
            //    возвращаем именно то значение, которое туда ввели.
            // ---------------------------------------------------------

            CompositionCalibrationPoint? exactPoint = null;

            double minDistance = double.MaxValue;

            foreach (CompositionCalibrationPoint calibrationPoint
                     in calibrationPoints)
            {
                double dx =
                    point.X - calibrationPoint.ImagePoint.X;

                double dy =
                    point.Y - calibrationPoint.ImagePoint.Y;

                double distanceSquared =
                    dx * dx + dy * dy;

                if (distanceSquared < minDistance)
                {
                    minDistance = distanceSquared;
                    exactPoint = calibrationPoint;
                }
            }

            // 3 пикселя — достаточно, чтобы калибровочная точка
            // определялась абсолютно точно.
            if (exactPoint != null &&
                minDistance <= 9.0)
            {
                return CreateResult(
                    al2o3,
                    exactPoint.CaO,
                    exactPoint.MgO);
            }

            // ---------------------------------------------------------
            // 2. Строим локальную триангуляцию калибровочных точек.
            //
            // В отличие от старой гомографии здесь НЕ существует
            // одной формулы для всей диаграммы.
            //
            // Каждая часть диаграммы рассчитывается по ближайшему
            // треугольнику калибровочных точек.
            // ---------------------------------------------------------

            List<Triangle> triangles =
                BuildDelaunayTriangulation(calibrationPoints);

            foreach (Triangle triangle in triangles)
            {
                Point a =
                    calibrationPoints[triangle.A].ImagePoint;

                Point b =
                    calibrationPoints[triangle.B].ImagePoint;

                Point c =
                    calibrationPoints[triangle.C].ImagePoint;

                if (!TryGetBarycentricCoordinates(
                        point,
                        a,
                        b,
                        c,
                        out double wa,
                        out double wb,
                        out double wc))
                {
                    continue;
                }

                // -----------------------------------------------------
                // Локальная интерполяция.
                //
                // Значение внутри треугольника определяется только
                // тремя ближайшими калибровочными вершинами.
                // -----------------------------------------------------

                CompositionCalibrationPoint pa =
                    calibrationPoints[triangle.A];

                CompositionCalibrationPoint pb =
                    calibrationPoints[triangle.B];

                CompositionCalibrationPoint pc =
                    calibrationPoints[triangle.C];

                double caO =
                    wa * pa.CaO +
                    wb * pb.CaO +
                    wc * pc.CaO;

                double mgO =
                    wa * pa.MgO +
                    wb * pb.MgO +
                    wc * pc.MgO;

                return CreateResult(
                    al2o3,
                    caO,
                    mgO);
            }

            // ---------------------------------------------------------
            // Если точка чуть-чуть вышла за границу из-за клика мышью,
            // используем ближайшие 3 точки.
            // ---------------------------------------------------------

            List<CompositionCalibrationPoint> nearest =
                calibrationPoints
                    .OrderBy(p =>
                    {
                        double dx =
                            point.X - p.ImagePoint.X;

                        double dy =
                            point.Y - p.ImagePoint.Y;

                        return dx * dx + dy * dy;
                    })
                    .Take(3)
                    .ToList();

            if (nearest.Count == 3)
            {
                if (TryGetBarycentricCoordinates(
                        point,
                        nearest[0].ImagePoint,
                        nearest[1].ImagePoint,
                        nearest[2].ImagePoint,
                        out double wa,
                        out double wb,
                        out double wc))
                {
                    double caO =
                        wa * nearest[0].CaO +
                        wb * nearest[1].CaO +
                        wc * nearest[2].CaO;

                    double mgO =
                        wa * nearest[0].MgO +
                        wb * nearest[1].MgO +
                        wc * nearest[2].MgO;

                    return CreateResult(
                        al2o3,
                        caO,
                        mgO);
                }
            }

            throw new InvalidOperationException(
                "Выбранная точка находится за пределами калиброванной области.");
        }

        // =============================================================
        // СОЗДАНИЕ РЕЗУЛЬТАТА
        // =============================================================

        private static CompositionResult CreateResult(
            double al2o3,
            double caO,
            double mgO)
        {
            // Небольшая защита от численных погрешностей.
            if (Math.Abs(caO) < 1e-8)
                caO = 0;

            if (Math.Abs(mgO) < 1e-8)
                mgO = 0;

            double siO2 =
                100.0 -
                al2o3 -
                caO -
                mgO;

            // Допускаем небольшую погрешность вычислений.
            if (caO < -0.01 ||
                mgO < -0.01 ||
                siO2 < -0.01)
            {
                throw new InvalidOperationException(
                    "Выбранная точка находится за пределами диаграммы.");
            }

            caO = Math.Max(0, caO);
            mgO = Math.Max(0, mgO);
            siO2 = Math.Max(0, siO2);

            // Последняя коррекция суммы.
            double sum =
                al2o3 +
                caO +
                mgO +
                siO2;

            siO2 += 100.0 - sum;

            return new CompositionResult
            {
                Al2O3 = al2o3,
                CaO = caO,
                MgO = mgO,
                SiO2 = siO2
            };
        }

        // =============================================================
        // БАРИЦЕНТРИЧЕСКИЕ КООРДИНАТЫ
        // =============================================================

        private static bool TryGetBarycentricCoordinates(
            Point p,
            Point a,
            Point b,
            Point c,
            out double wa,
            out double wb,
            out double wc)
        {
            wa = 0;
            wb = 0;
            wc = 0;

            double denominator =
                (b.Y - c.Y) * (a.X - c.X) +
                (c.X - b.X) * (a.Y - c.Y);

            if (Math.Abs(denominator) < Epsilon)
                return false;

            wa =
                ((b.Y - c.Y) * (p.X - c.X) +
                 (c.X - b.X) * (p.Y - c.Y))
                / denominator;

            wb =
                ((c.Y - a.Y) * (p.X - c.X) +
                 (a.X - c.X) * (p.Y - c.Y))
                / denominator;

            wc =
                1.0 - wa - wb;

            const double tolerance = 1e-7;

            return
                wa >= -tolerance &&
                wb >= -tolerance &&
                wc >= -tolerance;
        }

        // =============================================================
        // ТРЕУГОЛЬНИК
        // =============================================================

        private sealed class Triangle
        {
            public int A;
            public int B;
            public int C;

            public Triangle(int a, int b, int c)
            {
                A = a;
                B = b;
                C = c;
            }
        }

        private readonly struct Edge :
            IEquatable<Edge>
        {
            public readonly int A;
            public readonly int B;

            public Edge(int a, int b)
            {
                if (a < b)
                {
                    A = a;
                    B = b;
                }
                else
                {
                    A = b;
                    B = a;
                }
            }

            public bool Equals(Edge other)
            {
                return A == other.A &&
                       B == other.B;
            }

            public override bool Equals(object? obj)
            {
                return obj is Edge other &&
                       Equals(other);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(A, B);
            }
        }

        // =============================================================
        // ТРИАНГУЛЯЦИЯ ДЕЛОНЕ
        // =============================================================

        private static List<Triangle>
            BuildDelaunayTriangulation(
                List<CompositionCalibrationPoint> points)
        {
            int n = points.Count;

            List<Point> allPoints =
                points
                    .Select(p => p.ImagePoint)
                    .ToList();

            // ---------------------------------------------------------
            // Создаём большой внешний треугольник.
            // ---------------------------------------------------------

            double minX =
                allPoints.Min(p => p.X);

            double maxX =
                allPoints.Max(p => p.X);

            double minY =
                allPoints.Min(p => p.Y);

            double maxY =
                allPoints.Max(p => p.Y);

            double dx = maxX - minX;
            double dy = maxY - minY;

            double delta =
                Math.Max(dx, dy) * 20.0;

            double centerX =
                (minX + maxX) / 2.0;

            double centerY =
                (minY + maxY) / 2.0;

            allPoints.Add(
                new Point(
                    centerX - 2.0 * delta,
                    centerY + delta));

            allPoints.Add(
                new Point(
                    centerX,
                    centerY - 2.0 * delta));

            allPoints.Add(
                new Point(
                    centerX + 2.0 * delta,
                    centerY + delta));

            int superA = n;
            int superB = n + 1;
            int superC = n + 2;

            List<Triangle> triangles =
                new List<Triangle>
                {
                    new Triangle(
                        superA,
                        superB,
                        superC)
                };

            // ---------------------------------------------------------
            // Добавляем реальные точки по одной.
            // ---------------------------------------------------------

            for (int pointIndex = 0;
                 pointIndex < n;
                 pointIndex++)
            {
                Point p =
                    allPoints[pointIndex];

                List<Triangle> badTriangles =
                    new List<Triangle>();

                foreach (Triangle triangle in triangles)
                {
                    if (PointInsideCircumcircle(
                            p,
                            allPoints[triangle.A],
                            allPoints[triangle.B],
                            allPoints[triangle.C]))
                    {
                        badTriangles.Add(triangle);
                    }
                }

                // -----------------------------------------------------
                // Находим границу удаляемой области.
                // -----------------------------------------------------

                Dictionary<Edge, int> edgeCount =
                    new Dictionary<Edge, int>();

                foreach (Triangle triangle in badTriangles)
                {
                    AddEdge(
                        edgeCount,
                        new Edge(
                            triangle.A,
                            triangle.B));

                    AddEdge(
                        edgeCount,
                        new Edge(
                            triangle.B,
                            triangle.C));

                    AddEdge(
                        edgeCount,
                        new Edge(
                            triangle.C,
                            triangle.A));
                }

                foreach (Triangle triangle in badTriangles)
                    triangles.Remove(triangle);

                // -----------------------------------------------------
                // Создаём новые треугольники.
                // -----------------------------------------------------

                foreach (KeyValuePair<Edge, int> edge
                         in edgeCount)
                {
                    if (edge.Value != 1)
                        continue;

                    triangles.Add(
                        new Triangle(
                            edge.Key.A,
                            edge.Key.B,
                            pointIndex));
                }
            }

            // ---------------------------------------------------------
            // Убираем треугольники, содержащие вершины
            // большого внешнего треугольника.
            // ---------------------------------------------------------

            triangles =
                triangles
                    .Where(t =>
                        t.A < n &&
                        t.B < n &&
                        t.C < n)
                    .ToList();

            return triangles;
        }

        private static void AddEdge(
            Dictionary<Edge, int> edgeCount,
            Edge edge)
        {
            if (edgeCount.ContainsKey(edge))
                edgeCount[edge]++;
            else
                edgeCount[edge] = 1;
        }

        // =============================================================
        // ПРОВЕРКА ОКРУЖНОСТИ
        // =============================================================

        private static bool PointInsideCircumcircle(
            Point p,
            Point a,
            Point b,
            Point c)
        {
            double ax = a.X;
            double ay = a.Y;

            double bx = b.X;
            double by = b.Y;

            double cx = c.X;
            double cy = c.Y;

            double d =
                2.0 *
                (ax * (by - cy) +
                 bx * (cy - ay) +
                 cx * (ay - by));

            if (Math.Abs(d) < Epsilon)
                return false;

            double ax2ay2 =
                ax * ax + ay * ay;

            double bx2by2 =
                bx * bx + by * by;

            double cx2cy2 =
                cx * cx + cy * cy;

            double ux =
                (ax2ay2 * (by - cy) +
                 bx2by2 * (cy - ay) +
                 cx2cy2 * (ay - by))
                / d;

            double uy =
                (ax2ay2 * (cx - bx) +
                 bx2by2 * (ax - cx) +
                 cx2cy2 * (bx - ax))
                / d;

            double dx =
                p.X - ux;

            double dy =
                p.Y - uy;

            double distanceSquared =
                dx * dx + dy * dy;

            double radiusX =
                ax - ux;

            double radiusY =
                ay - uy;

            double radiusSquared =
                radiusX * radiusX +
                radiusY * radiusY;

            return distanceSquared <=
                   radiusSquared + 1e-7;
        }
    }
}