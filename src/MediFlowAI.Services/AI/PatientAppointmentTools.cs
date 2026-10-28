namespace MediFlowAI.Services;

/// <summary>
/// Patient-specific operations. The request supplies identity; the model cannot select another patient.
/// The demo request ID is not authentication. Production must supply an authenticated patient ID.
/// </summary>
public sealed class PatientAppointmentTools(
    IAppointmentService appointments,
    IReviewService reviews,
    Guid? patientId)
{
    private Guid PatientId => patientId is { } id && id != Guid.Empty
        ? id : throw new UnauthorizedAccessException("Patient context is required.");

    public Task<IReadOnlyList<AppointmentDto>> GetAppointmentsAsync(CancellationToken cancellationToken = default) =>
        appointments.GetPatientAppointmentsAsync(PatientId, cancellationToken);

    private async Task EnsureAppointmentOwnerAsync(Guid appointmentId, CancellationToken cancellationToken)
    {
        Guid currentPatient = PatientId;
        RequireId(appointmentId);
        AppointmentDto? appointment = await appointments.GetByIdAsync(appointmentId, cancellationToken);
        if (appointment is null || appointment.PatientId != currentPatient)
            throw new UnauthorizedAccessException("Appointment is not accessible.");
    }

    public Task<BookingResultDto> BookAsync(Guid doctorScheduleId, CancellationToken cancellationToken = default)
    {
        RequireId(doctorScheduleId);
        return appointments.BookAppointmentAsync(PatientId, doctorScheduleId, cancellationToken);
    }

    public async Task<AppointmentActionResult> CancelAsync(Guid appointmentId, CancellationToken cancellationToken = default)
    {
        await EnsureAppointmentOwnerAsync(appointmentId, cancellationToken);
        await appointments.CancelAppointmentAsync(appointmentId, cancellationToken);
        return new(appointmentId, "Cancelled");
    }

    public async Task<AppointmentActionResult> AddReviewAsync(
        Guid appointmentId, int rate, string? comment = null, CancellationToken cancellationToken = default)
    {
        if (rate is < 1 or > 5) throw new ArgumentOutOfRangeException(nameof(rate));
        await EnsureAppointmentOwnerAsync(appointmentId, cancellationToken);
        // The service enforces completed status and prevents duplicate reviews.
        await reviews.AddReviewAsync(appointmentId, rate, comment, cancellationToken);
        return new(appointmentId, "Review added");
    }

    private static void RequireId(Guid id)
    {
        if (id == Guid.Empty) throw new ArgumentException("A non-empty UUID is required.");
    }
}

public sealed record AppointmentActionResult(Guid AppointmentId, string Message);
