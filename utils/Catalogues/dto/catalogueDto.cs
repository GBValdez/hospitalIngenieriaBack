



using project.utils.Catalogues.dto;

namespace project.utils.catalogues.dto
{
    public class catalogueDto : catalogueDtoBse
    {
        public long Id { get; set; }
        public catalogueDto? catalogueParent { get; set; }
    }
}