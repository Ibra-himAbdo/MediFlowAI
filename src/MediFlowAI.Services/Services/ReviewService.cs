namespace MediFlowAI.Services;

public class ReviewService(AppDbContext db) : IReviewService
{
    public async Task AddReviewAsync(
        Guid appointmentId,
        int rate,
        string? comment,
        CancellationToken cancellationToken = default)
    {
        if (rate is < 1 or > 5)
        {
            throw new ArgumentOutOfRangeException(nameof(rate), "Rate must be between 1 and 5.");
        }

        Appointment appointment = await db.Appointments
            .Include(a => a.Review)
            .Include(a => a.Doctor)
            .FirstOrDefaultAsync(a => a.Id == appointmentId, cancellationToken)
            ?? throw new InvalidOperationException("Appointment not found.");

        if (appointment.Status != AppointmentStatus.Completed)
        {
            throw new InvalidOperationException("Reviews are only allowed for completed appointments.");
        }

        if (appointment.Review is not null)
        {
            throw new InvalidOperationException("This appointment already has a review.");
        }

        Review review = new()
        {
            Id = Guid.CreateVersion7(),
            DoctorId = appointment.DoctorId,
            PatientId = appointment.PatientId,
            AppointmentId = appointmentId,
            Rate = rate,
            Comment = comment,
            CreatedAt = DateTime.UtcNow
        };

        db.Reviews.Add(review);

        List<int> rates = await db.Reviews
            .Where(r => r.DoctorId == appointment.DoctorId)
            .Select(r => r.Rate)
            .ToListAsync(cancellationToken);

        rates.Add(rate);

        Doctor doctor = appointment.Doctor!;
        doctor.TotalReviews = rates.Count;
        doctor.AverageRate = Math.Round(rates.Average(), 2);

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ReviewDto>> GetDoctorReviewsAsync(
        Guid doctorId,
        CancellationToken cancellationToken = default) =>
        await db.Reviews
            .AsNoTracking()
            .Where(r => r.DoctorId == doctorId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new ReviewDto(
                r.Id,
                r.DoctorId,
                r.PatientId,
                r.Patient!.FullName ?? string.Empty,
                r.AppointmentId,
                r.Rate,
                r.Comment,
                r.CreatedAt))
            .ToListAsync(cancellationToken);

    public async Task<double> GetDoctorAverageRateAsync(Guid doctorId, CancellationToken cancellationToken = default)
    {
        Doctor? doctor = await db.Doctors
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == doctorId, cancellationToken);

        return doctor?.AverageRate ?? 0;
    }
}
