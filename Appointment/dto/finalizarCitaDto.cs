using System;
using System.Collections.Generic;

namespace project.Appointment.dto
{
    public class finalizarCitaDto
    {
        public long appointmentId { get; set; }
        public string diagnosis { get; set; }
        public string? observations { get; set; }
        public string treatment { get; set; }
        public bool requiresRecipe { get; set; }
        public List<finalizarCitaRecipeDto> recipes { get; set; } = new List<finalizarCitaRecipeDto>();
        public bool requiresLabExams { get; set; }
        public List<finalizarCitaExamDto> labExams { get; set; } = new List<finalizarCitaExamDto>();
        public bool requiresReschedule { get; set; }
        public string? rescheduleReason { get; set; }
        public DateTime? newStartDate { get; set; }
    }

    public class finalizarCitaRecipeDto
    {
        public long medicineId { get; set; }
        public int days { get; set; }
        public int timeLimit { get; set; }
    }

    public class finalizarCitaExamDto
    {
        public long examTypeId { get; set; }
        public string indications { get; set; }
    }
}
