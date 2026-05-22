namespace MediFlowAI.Services;

public interface IReviewService
{
    Task AddReviewAsync(
        Guid appointmentId,
        int rate,
        string? comment,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ReviewDto>> GetDoctorReviewsAsync(
        Guid doctorId,
        CancellationToken cancellationToken = default);

    Task<double> GetDoctorAverageRateAsync(Guid doctorId, CancellationToken cancellationToken = default);
}
