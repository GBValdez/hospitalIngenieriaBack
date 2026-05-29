namespace project.LaboratoryAttendants.dto
{
    public class laboratoryAttendantQueryDto
    {
        public string? name { get; set; }
        public string? email { get; set; }
        public long? examTypeId { get; set; }
        public bool? isActive { get; set; }
    }
}
