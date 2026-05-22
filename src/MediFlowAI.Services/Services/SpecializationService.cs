namespace MediFlowAI.Services;

public class SpecializationService(AppDbContext db) : ISpecializationService
{
    public async Task<IReadOnlyList<SpecializationDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await db.Specializations
            .AsNoTracking()
            .OrderBy(s => s.Name)
            .Select(s => new SpecializationDto(s.Id, s.Name ?? string.Empty))
            .ToListAsync(cancellationToken);
    }

    public async Task<SpecializationDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await db.Specializations
            .AsNoTracking()
            .Where(s => s.Id == id)
            .Select(s => new SpecializationDto(s.Id, s.Name ?? string.Empty))
            .FirstOrDefaultAsync(cancellationToken);
    }
}