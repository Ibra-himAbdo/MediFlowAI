namespace MediFlowAI.Services;

public class DoctorService(AppDbContext db) : IDoctorService
{
    public async Task<IReadOnlyList<DoctorDto>> GetAllAsync(
        DateOnly? slotsDate = null,
        CancellationToken cancellationToken = default) =>
        await AttachAvailableSlotsAsync(
            await ProjectDoctors(
                    db.Doctors.AsNoTracking()
                        .Where(d => d.IsActive)
                        .OrderBy(d => d.Name))
                .ToListAsync(cancellationToken),
            slotsDate,
            cancellationToken);

    public async Task<DoctorDto?> GetByIdAsync(
        Guid doctorId,
        DateOnly? slotsDate = null,
        CancellationToken cancellationToken = default)
    {
        DoctorDto? doctor = await ProjectDoctors(db.Doctors.AsNoTracking().Where(d => d.Id == doctorId))
            .FirstOrDefaultAsync(cancellationToken);

        if (doctor is null)
        {
            return null;
        }

        return (await AttachAvailableSlotsAsync([doctor], slotsDate, cancellationToken))[0];
    }

    public async Task<IReadOnlyList<DoctorDto>> GetTopRatedDoctorsAsync(
        Guid specializationId,
        int take = 5,
        DateOnly? slotsDate = null,
        CancellationToken cancellationToken = default) =>
        await AttachAvailableSlotsAsync(
            await ProjectDoctors(
                    db.Doctors.AsNoTracking()
                        .Where(d => d.IsActive && d.SpecializationId == specializationId)
                        .OrderByDescending(d => d.AverageRate)
                        .ThenByDescending(d => d.TotalReviews)
                        .Take(take))
                .ToListAsync(cancellationToken),
            slotsDate,
            cancellationToken);

    public async Task<IReadOnlyList<DoctorDto>> SearchDoctorsAsync(
        string query,
        DateOnly? slotsDate = null,
        CancellationToken cancellationToken = default)
    {
        string normalizedQuery = query.Trim().ToLower();

        return await AttachAvailableSlotsAsync(
            await ProjectDoctors(
                    db.Doctors.AsNoTracking()
                        .Where(d => d.IsActive &&
                            ((d.Name != null && d.Name.ToLower().Contains(normalizedQuery)) ||
                             (d.Specialization != null && d.Specialization.Name != null &&
                              d.Specialization.Name.ToLower().Contains(normalizedQuery))))
                        .OrderBy(d => d.Name))
                .ToListAsync(cancellationToken),
            slotsDate,
            cancellationToken);
    }

    public async Task<IReadOnlyList<DoctorDto>> GetBySpecializationAsync(
        Guid specializationId,
        DateOnly? slotsDate = null,
        CancellationToken cancellationToken = default) =>
        await AttachAvailableSlotsAsync(
            await ProjectDoctors(
                    db.Doctors.AsNoTracking()
                        .Where(d => d.IsActive && d.SpecializationId == specializationId)
                        .OrderByDescending(d => d.AverageRate)
                        .ThenByDescending(d => d.TotalReviews))
                .ToListAsync(cancellationToken),
            slotsDate,
            cancellationToken);

    public async Task<IReadOnlyList<DoctorDto>> GetBySpecializationNameAsync(
        string specializationName,
        DateOnly? slotsDate = null,
        CancellationToken cancellationToken = default)
    {
        string normalizedName = specializationName.Trim().ToLower();

        return await AttachAvailableSlotsAsync(
            await ProjectDoctors(
                    db.Doctors.AsNoTracking()
                        .Where(d => d.IsActive &&
                            d.Specialization != null &&
                            d.Specialization.Name != null &&
                            d.Specialization.Name.ToLower().Contains(normalizedName))
                        .OrderByDescending(d => d.AverageRate)
                        .ThenByDescending(d => d.TotalReviews))
                .ToListAsync(cancellationToken),
            slotsDate,
            cancellationToken);
    }

    private static IQueryable<DoctorDto> ProjectDoctors(IQueryable<Doctor> query) =>
        query.Select(d => new DoctorDto(
            d.Id,
            d.Name ?? string.Empty,
            d.SpecializationId,
            d.Specialization!.Name ?? string.Empty,
            d.ConsultationFee,
            d.AverageRate,
            d.TotalReviews,
            d.IsActive,
            Array.Empty<DoctorSlotDto>()));

    private async Task<IReadOnlyList<DoctorDto>> AttachAvailableSlotsAsync(
        IReadOnlyList<DoctorDto> doctors,
        DateOnly? slotsDate,
        CancellationToken cancellationToken)
    {
        if (slotsDate is null || doctors.Count == 0)
        {
            return doctors;
        }

        DateTime rangeStart = slotsDate.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        DateTime rangeEnd = rangeStart.AddDays(1);
        DateTime now = DateTime.UtcNow;
        Guid[] doctorIds = doctors.Select(d => d.Id).ToArray();

        List<DoctorSlotDto> slots = await db.DoctorSchedules
            .AsNoTracking()
            .Where(s => doctorIds.Contains(s.DoctorId) &&
                        s.StartTime >= rangeStart &&
                        s.StartTime < rangeEnd &&
                        s.StartTime >= now &&
                        s.Appointment == null)
            .OrderBy(s => s.DoctorId)
            .ThenBy(s => s.StartTime)
            .Select(s => new DoctorSlotDto(s.Id, s.DoctorId, s.StartTime, s.EndTime, true))
            .ToListAsync(cancellationToken);

        Dictionary<Guid, List<DoctorSlotDto>> slotsByDoctor = slots
            .GroupBy(s => s.DoctorId)
            .ToDictionary(g => g.Key, g => g.ToList());

        return doctors
            .Select(d => d with
            {
                AvailableSlots = slotsByDoctor.TryGetValue(d.Id, out List<DoctorSlotDto>? doctorSlots)
                    ? doctorSlots
                    : []
            })
            .ToList();
    }
}
