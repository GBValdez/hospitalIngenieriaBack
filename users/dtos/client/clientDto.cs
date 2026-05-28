
using fletesProyect.catalogues;

namespace project.users.dto
{
    public class clientDto : clientDtoBase
    {
        public Sex sex { get; set; }
        public Nationality nationality { get; set; }
        public userEntity user { get; set; }
    }
}