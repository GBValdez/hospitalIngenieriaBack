using System;
using System.Collections.Generic;

namespace project.Appointment.dto
{
    public class citaResultadoDto
    {
        public long appointmentId { get; set; }
        public string reason { get; set; }
        public DateTime startDate { get; set; }
        public DateTime? endDate { get; set; }
        public string? observations { get; set; }
        public string? doctorName { get; set; }
        public string? patientName { get; set; }
        public List<string> diseasesOrInjuries { get; set; } = new List<string>();
        public List<citaResultadoRecipeDto> recipes { get; set; } = new List<citaResultadoRecipeDto>();
        public List<citaResultadoExamDto> exams { get; set; } = new List<citaResultadoExamDto>();
    }

    public class citaResultadoRecipeDto
    {
        public long id { get; set; }
        public long medicineId { get; set; }
        public string medicineName { get; set; }
        public int days { get; set; }
        public int timeLimit { get; set; }
        public int totalAmount { get; set; }
    }

    public class citaResultadoExamDto
    {
        public long id { get; set; }
        public string examTypeName { get; set; }
        public DateTime startDate { get; set; }
        public DateTime endDate { get; set; }
        public string indications { get; set; }
        public string results { get; set; }
        public string? attendantName { get; set; }
        public List<string> diseasesOrInjuries { get; set; } = new List<string>();
    }
}
