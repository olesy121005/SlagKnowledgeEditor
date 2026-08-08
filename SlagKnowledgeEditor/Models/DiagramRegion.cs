namespace SlagKnowledgeEditor.Models
{
    public class DiagramRegion
    {
        public int Id { get; set; }

        public double Al2O3 { get; set; }

        public int Temperature { get; set; }

        public string ImagePath { get; set; } = string.Empty;

        public double TopLeftX { get; set; }
        public double TopLeftY { get; set; }

        public double TopRightX { get; set; }
        public double TopRightY { get; set; }

        public double BottomLeftX { get; set; }
        public double BottomLeftY { get; set; }

        public double? BottomRightX { get; set; }
        public double? BottomRightY { get; set; }

        public double? FifthX { get; set; }
        public double? FifthY { get; set; }
    }
}