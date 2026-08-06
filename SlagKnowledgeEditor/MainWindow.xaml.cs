using Microsoft.Win32;
using System.Windows.Media.Imaging;
using System.Text;
using SlagKnowledgeEditor.Services;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Collections.Generic;

namespace SlagKnowledgeEditor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private class DiagramRegion
        {
            public string Al2O3 { get; set; }
            public string Temperature { get; set; }

            public Rect Region { get; set; }
        }

        private List<DiagramRegion> diagramRegions = new();

        private bool selectingVertices = false;

        private int vertexCount = 0;

        private List<Point> vertices = new();


        private void CreateRegionButton_Click(object sender, RoutedEventArgs e)
        {
            if (DiagramImage.Source == null)
            {
                MessageBox.Show("Сначала откройте страницу с диаграммами.");
                return;
            }

            vertices.Clear();
            vertexCount = 0;
            selectingVertices = true;

            StatusText.Text =
                "Выберите 3 вершины: верхнюю левую, верхнюю правую и нижнюю левую.";
        }
        public MainWindow()
        {
            InitializeComponent();
        }
        private void OpenImageButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();

            openFileDialog.Title = "Выберите диаграмму";

            openFileDialog.Filter =
                "Изображения (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp";

            if (openFileDialog.ShowDialog() == true)
            {
                BitmapImage bitmap = new BitmapImage(
                    new Uri(openFileDialog.FileName));

                DiagramImage.Source = bitmap;

                StatusText.Text =
                    $"Статус: Загружено {System.IO.Path.GetFileName(openFileDialog.FileName)}";
            }
        }
        
        private void DiagramImage_MouseMove(object sender, MouseEventArgs e)
        {
            Point position = e.GetPosition(DiagramImage);

            MousePositionText.Text =
                $"X: {position.X:F0}   Y: {position.Y:F0}";
        }
        private void DiagramImage_MouseLeftButtonDown(
    object sender,
    MouseButtonEventArgs e)
        {
            if (!selectingVertices)
                return;

            Point position = e.GetPosition(DiagramImage);

            vertices.Add(position);

            Ellipse point = new Ellipse
            {
                Width = 10,
                Height = 10,
                Fill = Brushes.Red,
                IsHitTestVisible = false
            };

            Canvas.SetLeft(point, position.X - 5);
            Canvas.SetTop(point, position.Y - 5);

            DiagramCanvas.Children.Add(point);

            vertexCount++;

            if (vertexCount == 1)
            {
                StatusText.Text =
                    "Верхняя левая выбрана. Теперь выберите верхнюю правую.";
            }
            else if (vertexCount == 2)
            {
                StatusText.Text =
                    "Верхняя правая выбрана. Теперь выберите нижнюю левую.";
            }
            else if (vertexCount == 3)
            {
                selectingVertices = false;

                DrawDiagramBorder();

                StatusText.Text =
                    "Границы диаграммы заданы. Нажмите «Сохранить область».";
            }
        }

        private void DrawDiagramBorder()
        {
            if (vertices.Count != 3)
                return;

            Point topLeft = vertices[0];
            Point topRight = vertices[1];
            Point bottomLeft = vertices[2];

            Point bottomRight = new Point(
                topRight.X + bottomLeft.X - topLeft.X,
                topRight.Y + bottomLeft.Y - topLeft.Y
            );

            Polygon polygon = new Polygon
            {
                Stroke = Brushes.Red,
                StrokeThickness = 2,
                Fill = Brushes.Transparent,
                IsHitTestVisible = false
            };

            polygon.Points.Add(topLeft);
            polygon.Points.Add(topRight);
            polygon.Points.Add(bottomRight);
            polygon.Points.Add(bottomLeft);

            DiagramCanvas.Children.Add(polygon);
        }

        private void SaveRegionButton_Click(object sender, RoutedEventArgs e)
        {
            if (vertices.Count != 3)
            {
                MessageBox.Show(
                    "Сначала настройте диаграмму и выберите три вершины.");

                return;
            }

            string al2o3 =
                ((ComboBoxItem)Al2O3ComboBox.SelectedItem)
                .Content.ToString();

            string temperature =
                ((ComboBoxItem)TemperatureComboBox.SelectedItem)
                .Content.ToString();

            Point topLeft = vertices[0];
            Point topRight = vertices[1];
            Point bottomLeft = vertices[2];

            Point bottomRight = new Point(
                topRight.X + bottomLeft.X - topLeft.X,
                topRight.Y + bottomLeft.Y - topLeft.Y
            );

            StatusText.Text =
                $"Сохранена диаграмма: Al₂O₃ {al2o3}, {temperature}°C";

            MessageBox.Show(
                $"Диаграмма сохранена.\n\n" +
                $"Al₂O₃: {al2o3}\n" +
                $"Температура: {temperature}°C\n\n" +
                $"Верхняя левая: X={topLeft.X:F0}, Y={topLeft.Y:F0}\n" +
                $"Верхняя правая: X={topRight.X:F0}, Y={topRight.Y:F0}\n" +
                $"Нижняя левая: X={bottomLeft.X:F0}, Y={bottomLeft.Y:F0}\n" +
                $"Нижняя правая: X={bottomRight.X:F0}, Y={bottomRight.Y:F0}"
            );
        }

        private void ShowDiagramButton_Click(object sender, RoutedEventArgs e)
        {
            string al2o3 =
                ((ComboBoxItem)Al2O3ComboBox.SelectedItem).Content.ToString();

            string temperature =
                ((ComboBoxItem)TemperatureComboBox.SelectedItem).Content.ToString();

            DiagramRegion region = diagramRegions.Find(r =>
                r.Al2O3 == al2o3 &&
                r.Temperature == temperature);

            if (region == null)
            {
                MessageBox.Show(
                    $"Для Al₂O₃ {al2o3} и температуры {temperature}°C область ещё не настроена.");

                return;
            }

            StatusText.Text =
                $"Найдена диаграмма: Al₂O₃ {al2o3}, {temperature}°C";
        }

    }

}