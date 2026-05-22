namespace MediFlowAI.Services;

public class PatientService(AppDbContext db) : IPatientService
{
    public async Task<IReadOnlyList<PatientDto>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await ProjectPatients(db.Patients.AsNoTracking().OrderBy(p => p.FullName))
            .ToListAsync(cancellationToken);

    public Task<PatientDto?> GetByIdAsync(Guid patientId, CancellationToken cancellationToken = default) =>
        ProjectPatients(db.Patients.AsNoTracking().Where(p => p.Id == patientId))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<PatientDto>> SearchAsync(
        string query,
        CancellationToken cancellationToken = default)
    {
        string normalizedQuery = query.Trim().ToLower();

        return await ProjectPatients(
                db.Patients.AsNoTracking()
                    .Where(p =>
                        (p.FullName != null && p.FullName.ToLower().Contains(normalizedQuery)) ||
                        (p.PhoneNumber != null && p.PhoneNumber.Contains(normalizedQuery)))
                    .OrderBy(p => p.FullName))
            .ToListAsync(cancellationToken);
    }

    private static IQueryable<PatientDto> ProjectPatients(IQueryable<Patient> query) =>
        query.Select(p => new PatientDto(
            p.Id,
            p.FullName ?? string.Empty,
            p.PhoneNumber ?? string.Empty,
            p.BirthDate));
}
