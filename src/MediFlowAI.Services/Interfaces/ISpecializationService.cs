namespace MediFlowAI.Services;

public interface ISpecializationService
{
    Task<IReadOnlyList<SpecializationDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<SpecializationDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SpecializationDto>> SearchByNameAsync(
        string name,
        CancellationToken cancellationToken = default);
}
