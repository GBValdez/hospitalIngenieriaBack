using back.catalogues;
using project.utils.catalogue;

namespace fletesProyect.Medicine
{
    public class Medicine:Catalogue
    {
        public float price { get; set; }
        public long brandId { get; set; }
        public Brand brand { get; set; }
    }
}
