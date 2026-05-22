namespace MediFlowAI.Services;

public class DoctorService(AppDbContext db) : IDoctorService
{
    public async Task<IReadOnlyList<DoctorDto>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await ProjectDoctors(
                db.Doctors.AsNoTracking()
                    .Where(d => d.IsActive)
                    .OrderBy(d => d.Name))
            .ToListAsync(cancellationToken);

    public Task<DoctorDto?> GetByIdAsync(Guid doctorId, CancellationToken cancellationToken = default) =>
        ProjectDoctors(db.Doctors.AsNoTracking().Where(d => d.Id == doctorId))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<DoctorDto>> GetTopRatedDoctorsAsync(
        Guid specializationId,
        int take = 5,
        CancellationToken cancellationToken = default) =>
        await ProjectDoctors(
                db.Doctors.AsNoTracking()
                    .Where(d => d.IsActive && d.SpecializationId == specializationId)
                    .OrderByDescending(d => d.AverageRate)
                    .ThenByDescending(d => d.TotalReviews)
                    .Take(take))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<DoctorDto>> SearchDoctorsAsync(
        string query,
        CancellationToken cancellationToken = default)
    {
        string normalizedQuery = query.Trim().ToLower();

        return await ProjectDoctors(
                db.Doctors.AsNoTracking()
                    .Where(d => d.IsActive &&
                        ((d.Name != null && d.Name.ToLower().Contains(normalizedQuery)) ||
                         (d.Specialization != null && d.Specialization.Name != null &&
                          d.Specialization.Name.ToLower().Contains(normalizedQuery))))
                    .OrderBy(d => d.Name))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DoctorDto>> GetBySpecializationAsync(
        Guid specializationId,
        CancellationToken cancellationToken = default) =>
        await ProjectDoctors(
                db.Doctors.AsNoTracking()
                    .Where(d => d.IsActive && d.SpecializationId == specializationId)
                    .OrderByDescending(d => d.AverageRate)
                    .ThenByDescending(d => d.TotalReviews))
            .ToListAsync(cancellationToken);

    private static IQueryable<DoctorDto> ProjectDoctors(IQueryable<Doctor> query) =>
        query.Select(d => new DoctorDto(
            d.Id,
            d.Name ?? string.Empty,
            d.SpecializationId,
            d.Specialization!.Name ?? string.Empty,
            d.ConsultationFee,
            d.AverageRate,
            d.TotalReviews,
            d.IsActive));
}
