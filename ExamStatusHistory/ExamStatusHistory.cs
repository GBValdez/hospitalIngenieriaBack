using fletesProyect.catalogues;
using project.users;
using project.utils;
using ExamModel = fletesProyect.Exam.Exam;

namespace fletesProyect.ExamStatusHistory
{
    public class ExamStatusHistory : CommonsModel<long>
    {
        public long examId { get; set; }
        public ExamModel exam { get; set; }
        public long? previousStatusId { get; set; }
        public AppointmentStatus? previousStatus { get; set; }
        public long statusId { get; set; }
        public AppointmentStatus status { get; set; }
        public string? comment { get; set; }
        public DateTime changedAt { get; set; }
        public string? changedByUserId { get; set; }
        public userEntity? changedByUser { get; set; }
    }
}
