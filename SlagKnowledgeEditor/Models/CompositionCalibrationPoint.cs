using System.Windows;

namespace SlagKnowledgeEditor.Models
{
    public class CompositionCalibrationPoint
    {
        public int Id { get; set; }

        public double Al2O3 { get; set; }

        public int Temperature { get; set; }

        // Координаты точки относительно самого изображения диаграммы
        public Point ImagePoint { get; set; }

        public double CaO { get; set; }

        public double MgO { get; set; }

        public double SiO2 { get; set; }
    }
}