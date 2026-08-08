using Microsoft.Win32;
using SlagKnowledgeEditor.Database;
using SlagKnowledgeEditor.Models;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using SlagKnowledgeEditor.Services;

namespace SlagKnowledgeEditor
{
    public partial class MainWindow : Window
    {
        private readonly DatabaseService databaseService;

        private readonly CompositionCalculator compositionCalculator;

        private string currentImagePath = string.Empty;

        private bool selectingVertices = false;

        private bool calibrating = false;

        private int calibrationPointNumber = 0;

        private List<CompositionCalibrationPoint>
            calibrationPoints = new();

        private int vertexCount = 0;

        private int requiredVertexCount = 4;

        private List<Point> vertices = new();

        // Режим определения состава
        private bool determiningComposition = false;

        // Последняя выбранная точка
        private Point selectedCompositionPoint;

        private bool selectingCompositionPoint = false;

        private const int RequiredCalibrationPoints = 9;

        public MainWindow()
        {
            InitializeComponent();

            databaseService = new DatabaseService();
            compositionCalculator = new CompositionCalculator();
        }

        private void DetermineCompositionButton_Click(
    object sender,
    RoutedEventArgs e)
        {
            if (DiagramImage.Source == null)
            {
                MessageBox.Show(
                    "Сначала загрузите диаграмму.");
                return;
            }

            determiningComposition = true;

            StatusText.Text =
                "Кликните по нужной точке диаграммы для определения состава.";
        }

        // ---------------------------------------------------------
        // ОТКРЫТЬ ДИАГРАММУ
        // ---------------------------------------------------------

        private void StartCalibrationButton_Click(
    object sender,
    RoutedEventArgs e)
        {
            if (DiagramImage.Source == null)
            {
                MessageBox.Show(
                    "Сначала загрузите диаграмму.");

                return;
            }

            if (Al2O3ComboBox.SelectedItem == null ||
                TemperatureComboBox.SelectedItem == null)
            {
                MessageBox.Show(
                    "Выберите Al₂O₃ и температуру.");

                return;
            }

            string al2o3Text =
                ((ComboBoxItem)Al2O3ComboBox.SelectedItem)
                .Content.ToString()!;

            string temperatureText =
                ((ComboBoxItem)TemperatureComboBox.SelectedItem)
                .Content.ToString()!;

            double al2o3 =
                double.Parse(
                    al2o3Text.Replace("%", ""));

            int temperature =
                int.Parse(temperatureText);

            databaseService.DeleteCalibrationPoints(
    al2o3,
    temperature);

            calibrationPoints.Clear();

            calibrationPointNumber = 0;

            determiningComposition = false;
            selectingCompositionPoint = false;
            calibrating = true;

            ClearSelectionVisuals();

            StatusText.Text =
                $"Калибровка {al2o3}% / {temperature}°C. " +
                "Выберите первую точку.";
        }

        private void OpenImageButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();

            openFileDialog.Title = "Выберите диаграмму";

            openFileDialog.Filter =
                "Изображения (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp";

            if (openFileDialog.ShowDialog() == true)
            {
                currentImagePath = openFileDialog.FileName;

                BitmapImage bitmap = new BitmapImage();

                bitmap.BeginInit();
                bitmap.UriSource = new Uri(openFileDialog.FileName);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();

                DiagramImage.Source = bitmap;

                ClearSelection();

                StatusText.Text =
                    $"Статус: Загружено {System.IO.Path.GetFileName(openFileDialog.FileName)}";
            }
        }

        // ---------------------------------------------------------
        // ПОКАЗАТЬ ДИАГРАММУ ИЗ БАЗЫ
        // ---------------------------------------------------------

        private void ShowDiagramButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (Al2O3ComboBox.SelectedItem == null ||
                TemperatureComboBox.SelectedItem == null)
            {
                MessageBox.Show("Выберите Al₂O₃ и температуру.");
                return;
            }

            string al2o3Text =
                ((ComboBoxItem)Al2O3ComboBox.SelectedItem)
                .Content.ToString()!;

            string temperatureText =
                ((ComboBoxItem)TemperatureComboBox.SelectedItem)
                .Content.ToString()!;

            double al2o3 =
                double.Parse(al2o3Text.Replace("%", ""));

            int temperature =
                int.Parse(temperatureText);

            DiagramRegion? region =
                databaseService.GetDiagramRegion(
                    al2o3,
                    temperature);

            if (region == null)
            {
                MessageBox.Show(
                    $"Диаграмма Al₂O₃ {al2o3}% / {temperature}°C ещё не настроена.");

                return;
            }

            if (!System.IO.File.Exists(region.ImagePath))
            {
                MessageBox.Show(
                    "Исходное изображение диаграммы не найдено.");

                return;
            }

            currentImagePath = region.ImagePath;

            BitmapImage bitmap = new BitmapImage();

            bitmap.BeginInit();
            bitmap.UriSource = new Uri(region.ImagePath);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            bitmap.Freeze();

            DiagramImage.Source = bitmap;

            ClearSelection();

            // Восстанавливаем точки
            vertices.Clear();

            vertices.Add(new Point(
                region.TopLeftX,
                region.TopLeftY));

            vertices.Add(new Point(
                region.TopRightX,
                region.TopRightY));

            vertices.Add(new Point(
                region.BottomLeftX,
                region.BottomLeftY));

            if (region.BottomRightX.HasValue &&
                region.BottomRightY.HasValue)
            {
                vertices.Add(new Point(
                    region.BottomRightX.Value,
                    region.BottomRightY.Value));
            }

            if (region.FifthX.HasValue &&
                region.FifthY.HasValue)
            {
                vertices.Add(new Point(
                    region.FifthX.Value,
                    region.FifthY.Value));
            }

            requiredVertexCount = vertices.Count;
            vertexCount = vertices.Count;

            ClearSelectionVisuals();

            StatusText.Text =
                $"Показана диаграмма: Al₂O₃ {al2o3}% / {temperature}°C";
        }

        // ---------------------------------------------------------
        // НАСТРОИТЬ ОБЛАСТЬ
        // ---------------------------------------------------------

        private void CreateRegionButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (DiagramImage.Source == null)
            {
                MessageBox.Show(
                    "Сначала откройте диаграмму.");

                return;
            }

            if (Al2O3ComboBox.SelectedItem == null)
            {
                MessageBox.Show(
                    "Выберите содержание Al₂O₃.");

                return;
            }

            string al2o3Text =
                ((ComboBoxItem)Al2O3ComboBox.SelectedItem)
                .Content.ToString()!;

            double al2o3 =
                double.Parse(al2o3Text.Replace("%", ""));

            // 5% = 4 вершины
            // 10% = 5 вершин
            // 15% = 5 вершин
            requiredVertexCount =
                al2o3 == 5 ? 4 : 5;

            vertices.Clear();

            vertexCount = 0;

            selectingVertices = true;

            ClearSelectionVisuals();

            StatusText.Text =
                $"Выберите {requiredVertexCount} точки области " +
                $"(Al₂O₃ {al2o3}%).";
        }

        private Point GetImagePoint(MouseButtonEventArgs e)
        {
            Point screenPoint =
                e.GetPosition(DiagramImage);

            return screenPoint;
        }

        // ---------------------------------------------------------
        // ДВИЖЕНИЕ МЫШИ
        // ---------------------------------------------------------

        private void DiagramImage_MouseMove(
            object sender,
            MouseEventArgs e)
        {
            Point position =
                e.GetPosition(DiagramImage);

            MousePositionText.Text =
                $"X: {position.X:F0}   Y: {position.Y:F0}";
        }

        // ---------------------------------------------------------
        // КЛИК ПО ДИАГРАММЕ
        // ---------------------------------------------------------

        private void DiagramImage_MouseLeftButtonDown(
    object sender,
    MouseButtonEventArgs e)
        {
            if (calibrating)
            {
                HandleCalibrationClick(e);
                return;
            }

            // Во всех режимах используем координаты относительно самого Image.
            Point position = GetImagePoint(e);

            // Режим определения состава
            if (determiningComposition)
            {
                selectedCompositionPoint = position;

                DrawCompositionPoint(position);

                DetermineComposition(position);

                determiningComposition = false;

                e.Handled = true;

                return;
            }

            // Режим настройки области
            if (!selectingVertices)
                return;

            vertices.Add(position);

            vertexCount++;

            DrawPoint(position);

            if (vertexCount < requiredVertexCount)
            {
                StatusText.Text =
                    $"Выбрано {vertexCount} из {requiredVertexCount}. " +
                    $"Выберите следующую точку.";
            }
            else
            {
                selectingVertices = false;

                DrawDiagramBorder();

                StatusText.Text =
                    "Границы диаграммы заданы. " +
                    "Нажмите «Сохранить область».";
            }
        }

        private void HandleCalibrationClick(
    MouseButtonEventArgs e)
        {
            Point position =
                e.GetPosition(DiagramImage);

            if (Al2O3ComboBox.SelectedItem == null ||
                TemperatureComboBox.SelectedItem == null)
            {
                calibrating = false;

                MessageBox.Show(
                    "Не выбраны Al₂O₃ и температура.");

                return;
            }

            string al2o3Text =
                ((ComboBoxItem)Al2O3ComboBox.SelectedItem)
                .Content.ToString()!;

            string temperatureText =
                ((ComboBoxItem)TemperatureComboBox.SelectedItem)
                .Content.ToString()!;

            double al2o3 =
                double.Parse(
                    al2o3Text.Replace("%", ""));

            int temperature =
                int.Parse(temperatureText);

            double? caO = AskCompositionValue(
                "Введите содержание CaO, %:");

            if (!caO.HasValue)
                return;

            double? mgO = AskCompositionValue(
                "Введите содержание MgO, %:");

            if (!mgO.HasValue)
                return;

            double? siO2 = AskCompositionValue(
                "Введите содержание SiO₂, %:");

            if (!siO2.HasValue)
                return;

            double sum =
                al2o3 +
                caO.Value +
                mgO.Value +
                siO2.Value;

            if (Math.Abs(sum - 100.0) > 0.01)
            {
                MessageBox.Show(
                    $"Сумма компонентов должна быть 100 %.\n\n" +
                    $"Сейчас: {sum:F2} %");

                return;
            }

            CompositionCalibrationPoint point =
                new CompositionCalibrationPoint
                {
                    Al2O3 = al2o3,

                    Temperature = temperature,

                    ImagePoint = position,

                    CaO = caO.Value,

                    MgO = mgO.Value,

                    SiO2 = siO2.Value
                };

            calibrationPoints.Add(point);

            databaseService.SaveCalibrationPoint(point);

            DrawCalibrationPoint(position);

            calibrationPointNumber++;

            if (calibrationPointNumber < RequiredCalibrationPoints)
            {
                StatusText.Text =
                    $"Калибровочная точка {calibrationPointNumber} сохранена. " +
                    $"Выберите точку №{calibrationPointNumber + 1}.";
            }
            else
            {
                calibrating = false;

                StatusText.Text =
                    $"Калибровка {al2o3}% / {temperature}°C завершена.";

                MessageBox.Show(
                    "Калибровка диаграммы завершена.\n\n" +
                    $"Сохранено {RequiredCalibrationPoints} калибровочных точек.");
            }
        }

        private double? AskCompositionValue(
    string message)
        {
            string input =
                Microsoft.VisualBasic.Interaction.InputBox(
                    message,
                    "Калибровка диаграммы",
                    "");

            if (string.IsNullOrWhiteSpace(input))
                return null;

            if (!double.TryParse(
                input.Replace(',', '.'),
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture,
                out double value))
            {
                MessageBox.Show(
                    "Введите корректное число.");

                return null;
            }

            if (value < 0 || value > 100)
            {
                MessageBox.Show(
                    "Значение должно находиться от 0 до 100 %.");

                return null;
            }

            return value;
        }



        private void DrawCalibrationPoint(Point position)
        {
            const double ImageMargin = 20;

            Ellipse point = new Ellipse
            {
                Width = 12,
                Height = 12,
                Fill = Brushes.Blue,
                Stroke = Brushes.White,
                StrokeThickness = 2,
                IsHitTestVisible = false
            };

            Canvas.SetLeft(
                point,
                position.X + ImageMargin - 6);

            Canvas.SetTop(
                point,
                position.Y + ImageMargin - 6);

            DiagramCanvas.Children.Add(point);
        }

        // ---------------------------------------------------------
        // РИСУЕМ ТОЧКУ
        // ---------------------------------------------------------

        private void DrawPoint(Point position)
        {
            Ellipse point = new Ellipse
            {
                Width = 10,
                Height = 10,
                Fill = Brushes.Red,
                IsHitTestVisible = false
            };

            // Координаты клика были получены относительно Image.
            // Переводим их в координаты Canvas.
            Point canvasPosition =
                DiagramImage.TranslatePoint(position, DiagramCanvas);

            Canvas.SetLeft(point, canvasPosition.X - 5);
            Canvas.SetTop(point, canvasPosition.Y - 5);

            DiagramCanvas.Children.Add(point);
        }

        // ---------------------------------------------------------
        // ВОССТАНОВИТЬ ТОЧКИ ИЗ БД
        // ---------------------------------------------------------

        private void DrawSelectedPoints()
        {
            foreach (Point position in vertices)
            {
                DrawPoint(position);
            }
        }

        private Ellipse? compositionPointMarker;

        private void DrawCompositionPoint(Point position)
        {
            // Удаляем предыдущую точку определения состава
            if (compositionPointMarker != null)
            {
                DiagramCanvas.Children.Remove(compositionPointMarker);
                compositionPointMarker = null;
            }

            compositionPointMarker = new Ellipse
            {
                Width = 10,
                Height = 10,
                Fill = Brushes.Red,
                IsHitTestVisible = false
            };

            Point canvasPosition =
                DiagramImage.TranslatePoint(position, DiagramCanvas);

            Canvas.SetLeft(
                compositionPointMarker,
                canvasPosition.X - 5);

            Canvas.SetTop(
                compositionPointMarker,
                canvasPosition.Y - 5);

            DiagramCanvas.Children.Add(compositionPointMarker);
        }
        // ---------------------------------------------------------
        // РИСУЕМ ГРАНИЦУ
        // ---------------------------------------------------------

        private void DrawDiagramBorder()
        {
            if (vertices.Count < 3)
                return;

            Polygon polygon = new Polygon
            {
                Stroke = Brushes.Red,
                StrokeThickness = 2,
                Fill = Brushes.Transparent,
                IsHitTestVisible = false
            };

            foreach (Point point in vertices)
            {
                polygon.Points.Add(point);
            }

            DiagramCanvas.Children.Add(polygon);
        }

        // ---------------------------------------------------------
        // СОХРАНИТЬ ОБЛАСТЬ
        // ---------------------------------------------------------

        private void SaveRegionButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(currentImagePath))
            {
                MessageBox.Show(
                    "Сначала откройте диаграмму.");

                return;
            }

            if (vertices.Count != requiredVertexCount)
            {
                MessageBox.Show(
                    $"Нужно выбрать {requiredVertexCount} точки.");

                return;
            }

            string al2o3Text =
                ((ComboBoxItem)Al2O3ComboBox.SelectedItem)
                .Content.ToString()!;

            string temperatureText =
                ((ComboBoxItem)TemperatureComboBox.SelectedItem)
                .Content.ToString()!;

            double al2o3 =
                double.Parse(
                    al2o3Text.Replace("%", ""));

            int temperature =
                int.Parse(temperatureText);

            DiagramRegion region = new DiagramRegion
            {
                Al2O3 = al2o3,

                Temperature = temperature,

                ImagePath = currentImagePath,

                TopLeftX = vertices[0].X,
                TopLeftY = vertices[0].Y,

                TopRightX = vertices[1].X,
                TopRightY = vertices[1].Y,

                BottomLeftX = vertices[2].X,
                BottomLeftY = vertices[2].Y
            };

            // Для 4-й точки
            if (vertices.Count >= 4)
            {
                region.BottomRightX = vertices[3].X;
                region.BottomRightY = vertices[3].Y;
            }

            // Для 5-й точки
            if (vertices.Count >= 5)
            {
                region.FifthX = vertices[4].X;
                region.FifthY = vertices[4].Y;
            }

            databaseService.SaveDiagramRegion(region);

            StatusText.Text =
                $"Сохранено в БД: Al₂O₃ {al2o3}% / {temperature}°C";

            MessageBox.Show(
                $"Диаграмма Al₂O₃ {al2o3}% / {temperature}°C " +
                $"успешно сохранена в базе данных.");
        }

        // ---------------------------------------------------------
        // ОЧИСТКА
        // ---------------------------------------------------------

        private void DetermineComposition(Point position)
        {
            if (Al2O3ComboBox.SelectedItem == null ||
                TemperatureComboBox.SelectedItem == null)
            {
                MessageBox.Show(
                    "Выберите Al₂O₃ и температуру.");

                return;
            }


            string al2o3Text =
                ((ComboBoxItem)Al2O3ComboBox.SelectedItem)
                .Content.ToString()!;

            string temperatureText =
                ((ComboBoxItem)TemperatureComboBox.SelectedItem)
                .Content.ToString()!;


            double al2o3 =
                double.Parse(
                    al2o3Text.Replace("%", ""));

            int temperature =
                int.Parse(temperatureText);


            List<CompositionCalibrationPoint>
                calibrationPoints =
                    databaseService.GetCalibrationPoints(
                        al2o3,
                        temperature);


            if (calibrationPoints.Count < 4)
            {
                MessageBox.Show(
                    $"Для этой диаграммы сохранено " +
                    $"{calibrationPoints.Count} калибровочных точек.\n\n" +
                    "Необходимо 4 точки.");

                return;
            }

            try
            {
                CompositionResult result =
                    compositionCalculator.Calculate(
                        position,
                        calibrationPoints);


                MessageBox.Show(
                    $"Выбрана точка:\n\n" +
                    $"X = {position.X:F0}\n" +
                    $"Y = {position.Y:F0}\n\n" +

                    $"Al₂O₃ = {result.Al2O3:F1} %\n" +
                    $"CaO = {result.CaO:F1} %\n" +
                    $"MgO = {result.MgO:F1} %\n" +
                    $"SiO₂ = {result.SiO2:F1} %\n\n" +

                    $"Сумма = {result.Sum:F1} %",
                    "Состав шлака");
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Ошибка определения состава:\n\n{ex.Message}",
                    "Ошибка");
            }
        }

        private void ClearSelection()
        {
            vertices.Clear();

            vertexCount = 0;

            selectingVertices = false;
            selectingCompositionPoint = false;
            determiningComposition = false;
            calibrating = false;

            ClearSelectionVisuals();
        }


        // ---------------------------------------------------------
        // ОПРЕДЕЛЕНИЕ СОСТАВА
        // ---------------------------------------------------------

        private void SelectCompositionButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (DiagramImage.Source == null)
            {
                MessageBox.Show(
                    "Сначала загрузите диаграмму.");

                return;
            }

            determiningComposition = true;
            selectingCompositionPoint = true;

            StatusText.Text =
                "Кликните по нужной точке диаграммы.";
        }

        private void ClearCalibrationButton_Click(
    object sender,
    RoutedEventArgs e)
        {
            if (Al2O3ComboBox.SelectedItem == null ||
                TemperatureComboBox.SelectedItem == null)
            {
                MessageBox.Show(
                    "Выберите Al₂O₃ и температуру.");

                return;
            }

            string al2o3Text =
                ((ComboBoxItem)Al2O3ComboBox.SelectedItem)
                .Content.ToString()!;

            string temperatureText =
                ((ComboBoxItem)TemperatureComboBox.SelectedItem)
                .Content.ToString()!;

            double al2o3 =
                double.Parse(
                    al2o3Text.Replace("%", ""));

            int temperature =
                int.Parse(temperatureText);

            databaseService.DeleteCalibrationPoints(
                al2o3,
                temperature);

            calibrationPoints.Clear();

            calibrationPointNumber = 0;

            calibrating = false;

            ClearSelectionVisuals();

            StatusText.Text =
                "Калибровка очищена.";

            MessageBox.Show(
                $"Калибровка Al₂O₃ {al2o3}% / {temperature}°C удалена.");
        }

        private void ClearSelectionVisuals()
        {
            for (int i = DiagramCanvas.Children.Count - 1;
                 i >= 0;
                 i--)
            {
                UIElement element =
                    DiagramCanvas.Children[i];

                if (element != DiagramImage)
                {
                    DiagramCanvas.Children.RemoveAt(i);
                }
            }
        }
    }
}