using System;

namespace project.Nurses.dto
{
    public class nurseDto
    {
        public long id { get; set; }
        public string name { get; set; }
        public string dpi { get; set; }
        public string direction { get; set; }
        public DateOnly birthday { get; set; }
        public long sexId { get; set; }
        public string sexName { get; set; }
        public long nationalityId { get; set; }
        public string nationalityName { get; set; }
        public DateTime hiringDate { get; set; }
        public string userId { get; set; }
        public string userName { get; set; }
        public string email { get; set; }
        public string phoneNumber { get; set; }
        public bool isActive { get; set; }
    }
}
