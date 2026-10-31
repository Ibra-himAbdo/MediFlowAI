using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace MediFlowAI.Services;

/// <summary>
/// Defines the small, explicit set of application operations available to the model.
/// </summary>
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
        PatientAppointmentTools patientTools = new(appointments, reviews, patientId);

        return
        [
            Create(
                GetDate,
                "get_date",
                "Get the current UTC date and time. Use it before interpreting relative dates."),

            Create(
                specializations.GetAllAsync,
                "get_all_specializations",
                "List the available medical specializations and their IDs."),

            Create(
                doctors.GetAllAsync,
                "get_all_doctors",
                "List active doctors. Set slotsDate to yyyy-MM-dd UTC to include available slots."),

            Create(
                doctors.SearchDoctorsAsync,
                "search_doctors",
                "Find doctors by doctor or specialty name. Set slotsDate to include fees, ratings, and available slots."),

            Create(
                schedules.GetAvailableSlotsAsync,
                "get_available_slots",
                "List available slots for a doctor and UTC date. Copy a slot ID before booking."),

            Create(
                reviews.GetDoctorReviewsAsync,
                "get_doctor_reviews",
                "List published reviews for a doctor."),

            Create(
                patientTools.GetAppointmentsAsync,
                "get_patient_appointments",
                "List appointments for the patient bound to the current request."),

            Create(
                patientTools.BookAsync,
                "book_appointment",
                "Book a selected doctorScheduleId for the current patient."),

            Create(
                patientTools.CancelAsync,
                "cancel_appointment",
                "Cancel an appointment owned by the current patient."),

            Create(
                patientTools.AddReviewAsync,
                "add_review",
                "Review an owned, completed appointment. Rate must be 1-5; comment is optional.")
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
