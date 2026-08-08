namespace SlagKnowledgeEditor.Models
{
    public class CompositionResult
    {
        public double Al2O3 { get; set; }

        public double CaO { get; set; }

        public double MgO { get; set; }

        public double SiO2 { get; set; }

        public double Sum =>
            Al2O3 + CaO + MgO + SiO2;
    }
}