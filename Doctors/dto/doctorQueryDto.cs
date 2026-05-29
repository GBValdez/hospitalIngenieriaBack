namespace project.Doctors.dto
{
    public class doctorQueryDto
    {
        public string? name { get; set; }
        public string? email { get; set; }
        public long? specialtyId { get; set; }
        public bool? isActive { get; set; }
    }
}
