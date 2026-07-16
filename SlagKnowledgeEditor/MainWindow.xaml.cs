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
        private bool isSelectingRegion = false;

        private Point startPoint;

        private Rectangle selectionRectangle;

        private Rect selectedRegion;

        private bool selectingVertices = false;

        private int vertexCount = 0;

        private List<Point> vertices = new();
        private void DiagramCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!isSelectingRegion)
                return;


            startPoint = e.GetPosition(DiagramCanvas);


            selectionRectangle = new Rectangle();

            selectionRectangle.Stroke = Brushes.Red;
            selectionRectangle.StrokeThickness = 2;


            Canvas.SetLeft(selectionRectangle, startPoint.X);
            Canvas.SetTop(selectionRectangle, startPoint.Y);


            DiagramCanvas.Children.Add(selectionRectangle);
        }


        private void DiagramCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (!isSelectingRegion)
                return;

            if (selectionRectangle == null)
                return;


            Point currentPoint = e.GetPosition(DiagramCanvas);


            double width = Math.Abs(currentPoint.X - startPoint.X);
            double height = Math.Abs(currentPoint.Y - startPoint.Y);


            selectionRectangle.Width = width;
            selectionRectangle.Height = height;


            Canvas.SetLeft(
                selectionRectangle,
                Math.Min(currentPoint.X, startPoint.X));


            Canvas.SetTop(
                selectionRectangle,
                Math.Min(currentPoint.Y, startPoint.Y));
        }


        private void DiagramCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!isSelectingRegion)
                return;


            isSelectingRegion = false;


            Point endPoint = e.GetPosition(DiagramCanvas);


            double x = Math.Min(startPoint.X, endPoint.X);
            double y = Math.Min(startPoint.Y, endPoint.Y);


            double width = Math.Abs(endPoint.X - startPoint.X);
            double height = Math.Abs(endPoint.Y - startPoint.Y);

            selectedRegion = new Rect(
                x,
                y,
                width,
                height
            );

            StatusText.Text =
                $"Область: X={x:F0}; Y={y:F0}; W={width:F0}; H={height:F0}";
        }

        private void CreateRegionButton_Click(object sender, RoutedEventArgs e)
        {
            isSelectingRegion = true;

            StatusText.Text =
                "Режим настройки: выделите область диаграммы мышью";
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
        private void SelectVerticesButton_Click(object sender, RoutedEventArgs e)
        {
            selectingVertices = true;

            vertexCount = 0;

            StatusText.Text =
                "Статус: Выберите три вершины диаграммы";
        }
        private void DiagramImage_MouseMove(object sender, MouseEventArgs e)
        {
            Point position = e.GetPosition(DiagramImage);

            MousePositionText.Text =
                $"X: {position.X:F0}   Y: {position.Y:F0}";
        }
        private void DiagramImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!selectingVertices)
                return;

            Point position = e.GetPosition(DiagramImage);

            vertices.Add(position);
            System.Windows.Shapes.Ellipse ellipse =
    new System.Windows.Shapes.Ellipse();

            ellipse.Width = 10;
            ellipse.Height = 10;

            ellipse.Fill = Brushes.Red;

            Canvas.SetLeft(ellipse, position.X - 5);
            Canvas.SetTop(ellipse, position.Y - 5);

            DiagramCanvas.Children.Add(ellipse);

            vertexCount++;

            StatusText.Text =
                $"Выбрано вершин: {vertexCount}/3";

            if (vertexCount == 3)
            {
                selectingVertices = false;

                StatusText.Text =
                    "Три вершины успешно выбраны.";
            }
        }

    }

}