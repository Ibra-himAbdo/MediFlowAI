namespace MediFlowAI.Services;

public interface IPatientService
{
    Task<IReadOnlyList<PatientDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<PatientDto?> GetByIdAsync(Guid patientId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PatientDto>> SearchAsync(
        string query,
        CancellationToken cancellationToken = default);
}
