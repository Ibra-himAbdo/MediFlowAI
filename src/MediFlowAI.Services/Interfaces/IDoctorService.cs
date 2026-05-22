namespace MediFlowAI.Services;

public interface IDoctorService
{
    Task<IReadOnlyList<DoctorDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<DoctorDto?> GetByIdAsync(Guid doctorId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DoctorDto>> GetTopRatedDoctorsAsync(
        Guid specializationId,
        int take = 5,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DoctorDto>> SearchDoctorsAsync(
        string query,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DoctorDto>> GetBySpecializationAsync(
        Guid specializationId,
        CancellationToken cancellationToken = default);
}
