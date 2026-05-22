namespace MediFlowAI.Services;

public class ScheduleService(AppDbContext db) : IScheduleService
{
    public async Task<IReadOnlyList<DoctorSlotDto>> GetAvailableSlotsAsync(
        Guid doctorId,
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        (DateTime start, DateTime end) = GetDateRange(date);
        DateTime now = DateTime.UtcNow;

        return await db.DoctorSchedules
            .AsNoTracking()
            .Where(s => s.DoctorId == doctorId &&
                        s.StartTime >= start &&
                        s.StartTime < end &&
                        s.StartTime >= now &&
                        s.Appointment == null)
            .OrderBy(s => s.StartTime)
            .Select(s => new DoctorSlotDto(s.Id, s.DoctorId, s.StartTime, s.EndTime, true))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DoctorSlotDto>> GetDoctorScheduleAsync(
        Guid doctorId,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken = default)
    {
        DateTime rangeStart = fromDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        DateTime rangeEnd = toDate.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        return await db.DoctorSchedules
            .AsNoTracking()
            .Where(s => s.DoctorId == doctorId &&
                        s.StartTime >= rangeStart &&
                        s.StartTime < rangeEnd)
            .OrderBy(s => s.StartTime)
            .Select(s => new DoctorSlotDto(
                s.Id,
                s.DoctorId,
                s.StartTime,
                s.EndTime,
                s.Appointment == null))
            .ToListAsync(cancellationToken);
    }

    public Task<bool> IsSlotAvailableAsync(Guid scheduleId, CancellationToken cancellationToken = default) =>
        db.DoctorSchedules
            .AsNoTracking()
            .AnyAsync(s => s.Id == scheduleId && s.Appointment == null, cancellationToken);

    private static (DateTime Start, DateTime End) GetDateRange(DateOnly date)
    {
        DateTime start = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        return (start, start.AddDays(1));
    }
}
