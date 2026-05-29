using fletesProyect.Appointment;
using fletesProyect.Recipe;
using Microsoft.EntityFrameworkCore;
using project.Models;
using project.utils.dto;
using AppointmentModel = fletesProyect.Appointment.Appointment;
using ExamModel = fletesProyect.Exam.Exam;

namespace project.utils.services
{
    public class clinicalNotificationService
    {
        private readonly DBProyContext context;
        private readonly emailService emailService;
        private readonly simplePdfService pdfService;

        public clinicalNotificationService(
            DBProyContext context,
            emailService emailService,
            simplePdfService pdfService)
        {
            this.context = context;
            this.emailService = emailService;
            this.pdfService = pdfService;
        }

        public async Task SendAppointmentScheduled(long appointmentId)
        {
            AppointmentModel appointment = await GetAppointment(appointmentId);
            await SendAppointmentEmail(
                appointment,
                "Cita agendada",
                "Tu cita fue agendada correctamente.",
                BuildAppointmentLines(appointment, "Agendada"),
                $"cita-{appointment.Id}.pdf");
        }

        public async Task SendAppointmentRescheduled(long appointmentId)
        {
            AppointmentModel appointment = await GetAppointment(appointmentId);
            await SendAppointmentEmail(
                appointment,
                "Cita reagendada",
                "Tu cita fue reagendada correctamente.",
                BuildAppointmentLines(appointment, "Reagendada"),
                $"cita-reagendada-{appointment.Id}.pdf");
        }

        public async Task SendAppointmentFinalized(long appointmentId)
        {
            AppointmentModel appointment = await GetAppointmentWithClinicalData(appointmentId);
            List<string> lines = BuildAppointmentLines(appointment, "Finalizada");
            lines.Add("# Diagnostico");
            List<string> diagnoses = await context.AppointmentDiseaseOrInjuries
                .Include(x => x.diseaseOrInjury)
                .Where(x => x.appointmentId == appointment.Id && x.deleteAt == null)
                .OrderBy(x => x.diseaseOrInjury.name)
                .Select(x => x.diseaseOrInjury.name)
                .ToListAsync();
            if (diagnoses.Count > 0)
                lines.Add($"Diagnosticos: {string.Join(", ", diagnoses)}");
            else
                lines.Add("Diagnosticos: Sin diagnosticos registrados");
            if (!string.IsNullOrWhiteSpace(appointment.observations))
                lines.Add($"Observaciones: {appointment.observations}");

            List<Recipe> recipes = await context.Recipes
                .Include(x => x.medicine)
                .Where(x => x.appointmentId == appointment.Id && x.deleteAt == null)
                .ToListAsync();
            if (recipes.Count > 0)
            {
                lines.Add("# Receta medica");
                lines.Add($"No. cita para despacho: {appointment.Id}");
                foreach (Recipe recipe in recipes)
                    lines.Add($"Medicamento: {recipe.medicine.name} - {recipe.days} dias, cada {recipe.timeLimit} horas");
            }

            List<ExamModel> exams = await context.Exams
                .Include(x => x.examType)
                .Include(x => x.attendant)
                .Where(x => x.appointmentId == appointment.Id && x.deleteAt == null)
                .OrderBy(x => x.startDate)
                .ToListAsync();
            if (exams.Count > 0)
            {
                lines.Add("# Examenes programados");
                foreach (ExamModel exam in exams)
                {
                    lines.Add($"Examen No.: {exam.Id}");
                    lines.Add($"Tipo de examen: {exam.examType?.name}");
                    lines.Add($"Encargado: {exam.attendant?.name}");
                    lines.Add($"Horario de examen: {exam.startDate:yyyy-MM-dd HH:mm} UTC");
                    if (!string.IsNullOrWhiteSpace(exam.observations))
                        lines.Add($"Indicaciones de examen: {exam.observations}");
                }
            }

            await SendAppointmentEmail(
                appointment,
                "Cita finalizada",
                "Tu cita fue finalizada. Adjuntamos el resumen en PDF.",
                lines,
                $"resumen-cita-{appointment.Id}.pdf");
        }

        public async Task SendExamScheduled(long examId)
        {
            ExamModel exam = await GetExam(examId);
            await SendExamEmail(
                exam,
                "Examen programado",
                "Tu examen de laboratorio fue programado correctamente.",
                BuildExamLines(exam, "Programado"),
                $"examen-{exam.Id}.pdf");
        }

        public async Task SendExamFinalized(long examId)
        {
            ExamModel exam = await GetExam(examId);
            List<string> lines = BuildExamLines(exam, "Finalizado");
            List<string> diagnoses = await context.ExamDiseaseOrInjuries
                .Include(x => x.diseaseOrInjury)
                .Where(x => x.examId == exam.Id && x.deleteAt == null)
                .OrderBy(x => x.diseaseOrInjury.name)
                .Select(x => x.diseaseOrInjury.name)
                .ToListAsync();
            lines.Add("# Resultado");
            if (diagnoses.Count > 0)
                lines.Add($"Diagnosticos: {string.Join(", ", diagnoses)}");
            lines.Add($"Resultado: {exam.results}");
            if (!string.IsNullOrWhiteSpace(exam.observations))
                lines.Add($"Observaciones: {exam.observations}");

            await SendExamEmail(
                exam,
                "Examen finalizado",
                "Tu examen fue finalizado. Adjuntamos el resultado en PDF.",
                lines,
                $"resultado-examen-{exam.Id}.pdf");
        }

        private async Task<AppointmentModel> GetAppointment(long appointmentId)
        {
            return await context.Appointments
                .Include(x => x.patient)
                    .ThenInclude(x => x.user)
                .Include(x => x.doctor)
                .FirstOrDefaultAsync(x => x.Id == appointmentId);
        }

        private async Task<AppointmentModel> GetAppointmentWithClinicalData(long appointmentId)
        {
            return await context.Appointments
                .Include(x => x.patient)
                    .ThenInclude(x => x.user)
                .Include(x => x.doctor)
                .FirstOrDefaultAsync(x => x.Id == appointmentId);
        }

        private async Task<ExamModel> GetExam(long examId)
        {
            return await context.Exams
                .Include(x => x.examType)
                .Include(x => x.attendant)
                .Include(x => x.appointment)
                    .ThenInclude(x => x.patient)
                        .ThenInclude(x => x.user)
                .Include(x => x.appointment)
                    .ThenInclude(x => x.doctor)
                .FirstOrDefaultAsync(x => x.Id == examId);
        }

        private async Task SendAppointmentEmail(
            AppointmentModel appointment,
            string subject,
            string message,
            List<string> lines,
            string fileName)
        {
            string email = appointment?.patient?.user?.Email;
            if (string.IsNullOrWhiteSpace(email))
                return;

            await SendEmail(email, subject, message, lines, fileName);
        }

        private async Task SendExamEmail(
            ExamModel exam,
            string subject,
            string message,
            List<string> lines,
            string fileName)
        {
            string email = exam?.appointment?.patient?.user?.Email;
            if (string.IsNullOrWhiteSpace(email))
                return;

            await SendEmail(email, subject, message, lines, fileName);
        }

        private async Task SendEmail(
            string email,
            string subject,
            string message,
            List<string> lines,
            string fileName)
        {
            byte[] pdf = pdfService.CreateDocument(subject, lines);
            await Task.Run(() => emailService.SendEmail(new emailSendDto
            {
                email = email,
                subject = subject,
                message = $"<p>{message}</p>",
                attachments = new List<emailAttachmentDto>
                {
                    new emailAttachmentDto
                    {
                        fileName = fileName,
                        contentType = "application/pdf",
                        content = pdf
                    }
                }
            }));
        }

        private static List<string> BuildAppointmentLines(AppointmentModel appointment, string status)
        {
            return new List<string>
            {
                "# Datos de la cita",
                $"No. cita: {appointment.Id}",
                $"Estado: {status}",
                $"Paciente: {appointment.patient?.name}",
                $"DPI paciente: {appointment.patient?.dpi}",
                $"Doctor: {appointment.doctor?.name}",
                $"Motivo: {appointment.reason}",
                $"Fecha programada: {appointment.startDate:yyyy-MM-dd HH:mm} UTC"
            };
        }

        private static List<string> BuildExamLines(ExamModel exam, string status)
        {
            return new List<string>
            {
                "# Datos del examen",
                $"No. examen: {exam.Id}",
                $"No. cita: {exam.appointmentId}",
                $"Estado: {status}",
                $"Paciente: {exam.appointment?.patient?.name}",
                $"DPI paciente: {exam.appointment?.patient?.dpi}",
                $"Doctor: {exam.appointment?.doctor?.name}",
                $"Tipo de examen: {exam.examType?.name}",
                $"Encargado: {exam.attendant?.name}",
                $"Fecha programada: {exam.startDate:yyyy-MM-dd HH:mm} UTC"
            };
        }
    }
}
