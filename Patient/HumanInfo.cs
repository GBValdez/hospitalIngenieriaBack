using fletesProyect.catalogues;
using project.users;
using project.utils;

namespace fletesProyect.Patient
{
    public class HumanInfo : CommonsModel<long>
    {
        public string name { get; set; }
        public string dpi { get; set; }
        public string direction { get; set; }
        public DateOnly birthday { get; set; }
        public long sexId { get; set; }
        public Sex sex { get; set; }
        public long nationalityId { get; set; }
        public Nationality nationality { get; set; }
        public string userId {  get; set; }
        public userEntity user { get; set; }
    }
}
