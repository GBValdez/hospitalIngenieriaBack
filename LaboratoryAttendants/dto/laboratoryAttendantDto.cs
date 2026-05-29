using System;
using System.Collections.Generic;

namespace project.LaboratoryAttendants.dto
{
    public class laboratoryAttendantDto
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
        public List<long> examTypeIds { get; set; } = new List<long>();
        public List<string> examTypeNames { get; set; } = new List<string>();
        public string userId { get; set; }
        public string userName { get; set; }
        public string email { get; set; }
        public string phoneNumber { get; set; }
        public bool isActive { get; set; }
    }
}
