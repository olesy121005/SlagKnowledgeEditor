using System.Windows;

namespace SlagKnowledgeEditor.Models
{
    public class ViscosityCalibrationPoint
    {
        public int Id { get; set; }

        public double Al2O3 { get; set; }

        public int Temperature { get; set; }

        // Координаты точки на изображении диаграммы
        public Point ImagePoint { get; set; }

        // Значение изолинии вязкости
        public double Viscosity { get; set; }
    }
}