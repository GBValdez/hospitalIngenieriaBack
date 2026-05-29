using System;

namespace project.Exams.dto
{
    public class examStatusHistoryDto
    {
        public long id { get; set; }
        public long examId { get; set; }
        public string? previousStatus { get; set; }
        public string status { get; set; }
        public string? comment { get; set; }
        public DateTime changedAt { get; set; }
        public string? changedByUserId { get; set; }
        public string? changedByUserName { get; set; }
    }
}
