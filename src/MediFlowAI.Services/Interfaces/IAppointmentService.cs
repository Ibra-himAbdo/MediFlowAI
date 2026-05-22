namespace MediFlowAI.Services;

public interface IAppointmentService
{
    Task<BookingResultDto> BookAppointmentAsync(
        Guid patientId,
        Guid doctorScheduleId,
        CancellationToken cancellationToken = default);

    Task CancelAppointmentAsync(Guid appointmentId, CancellationToken cancellationToken = default);

    Task CompleteAppointmentAsync(Guid appointmentId, CancellationToken cancellationToken = default);

    Task<AppointmentDto?> GetByIdAsync(Guid appointmentId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AppointmentDto>> GetPatientAppointmentsAsync(
        Guid patientId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AppointmentDto>> GetDoctorAppointmentsAsync(
        Guid doctorId,
        CancellationToken cancellationToken = default);
}
