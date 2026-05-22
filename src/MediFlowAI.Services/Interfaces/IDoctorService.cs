namespace MediFlowAI.Services;

public interface IDoctorService
{
    Task<IReadOnlyList<DoctorDto>> GetAllAsync(
        DateOnly? slotsDate = null,
        CancellationToken cancellationToken = default);

    Task<DoctorDto?> GetByIdAsync(
        Guid doctorId,
        DateOnly? slotsDate = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DoctorDto>> GetTopRatedDoctorsAsync(
        Guid specializationId,
        int take = 5,
        DateOnly? slotsDate = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DoctorDto>> SearchDoctorsAsync(
        string query,
        DateOnly? slotsDate = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DoctorDto>> GetBySpecializationAsync(
        Guid specializationId,
        DateOnly? slotsDate = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DoctorDto>> GetBySpecializationNameAsync(
        string specializationName,
        DateOnly? slotsDate = null,
        CancellationToken cancellationToken = default);
}
