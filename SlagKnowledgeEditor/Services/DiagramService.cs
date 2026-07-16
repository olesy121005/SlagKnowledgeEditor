using SlagKnowledgeEditor.Models;

namespace SlagKnowledgeEditor.Services
{
    public class DiagramService
    {
        public DiagramRegion GetRegion(
            string al2o3,
            string temperature)
        {

            if (al2o3 == "5%" && temperature == "1500")
            {
                return new DiagramRegion
                {
                    Name = "Al2O3_5_T1500",
                    X = 0,
                    Y = 0,
                    Width = 400,
                    Height = 300
                };
            }


            return new DiagramRegion();
        }
    }
}