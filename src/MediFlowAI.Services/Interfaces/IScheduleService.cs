namespace MediFlowAI.Services;

public interface IScheduleService
{
    Task<IReadOnlyList<DoctorSlotDto>> GetAvailableSlotsAsync(
        Guid doctorId,
        DateOnly date,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DoctorSlotDto>> GetDoctorScheduleAsync(
        Guid doctorId,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken = default);

    Task<bool> IsSlotAvailableAsync(Guid scheduleId, CancellationToken cancellationToken = default);
}
