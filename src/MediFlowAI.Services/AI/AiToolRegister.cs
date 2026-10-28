using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace MediFlowAI.Services;

/// <summary>Explicit allow-list of service operations exposed to the booking assistant.</summary>
public sealed class AiToolRegister(
    ISpecializationService specializations,
    IDoctorService doctors,
    IScheduleService schedules,
    IAppointmentService appointments,
    IReviewService reviews,
    ILogger<AiToolRegister> logger)
{
    public IReadOnlyList<AITool> GetAllTools(Guid? patientId = null)
    {
        // This context is captured per request, never supplied by the model or stored on a singleton.
        PatientAppointmentTools patient = new(appointments, reviews, patientId);
        return
        [
            Create(GetDate, "get_date", "Current UTC date and time. Call before interpreting relative dates."),
            Create(specializations.GetAllAsync, "get_all_specializations", "List specializations and their IDs."),
            Create(doctors.GetAllAsync, "get_all_doctors", "List active doctors. Optional slotsDate: yyyy-MM-dd in UTC; omit or use null for no slots."),
            Create(doctors.SearchDoctorsAsync, "search_doctors", "Find doctors by name or specialty. Results include fee, rating and availableSlots when slotsDate (yyyy-MM-dd UTC) is supplied. Ask the user to choose when multiple doctors match."),
            Create(schedules.GetAvailableSlotsAsync, "get_available_slots", "Available slots for doctorId and date (yyyy-MM-dd UTC). Copy the slot ID to book."),
            Create(reviews.GetDoctorReviewsAsync, "get_doctor_reviews", "List published reviews for doctorId."),
            Create(patient.GetAppointmentsAsync, "get_patient_appointments", "List the current patient's appointments. Patient identity comes from request context."),
            Create(patient.BookAsync, "book_appointment", "Book doctorScheduleId for the current patient after they choose the doctor, date and time."),
            Create(patient.CancelAsync, "cancel_appointment", "Cancel an appointment belonging to the current patient when requested."),
            Create(patient.AddReviewAsync, "add_review", "Review the current patient's completed appointment. rate: 1-5; comment is optional. Cannot complete appointments.")
        ];
    }

    private AIFunction Create(Delegate method, string name, string description) =>
        new ToolErrorHandler(AIFunctionFactory.Create(method, new AIFunctionFactoryOptions
        {
            Name = name,
            Description = description
        }), logger);

    private static CurrentDate GetDate() => new(DateTime.UtcNow);
    private sealed record CurrentDate(DateTime UtcNow)
    {
        public DateOnly UtcToday => DateOnly.FromDateTime(UtcNow);
        public DateOnly UtcTomorrow => UtcToday.AddDays(1);
    }
}
