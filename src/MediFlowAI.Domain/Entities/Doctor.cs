namespace MediFlowAI.Domain;

public class Doctor : BaseEntity
{
    public string? Name { get; set; }

    public Guid SpecializationId { get; set; }
    public Specialization? Specialization { get; set; }

    public decimal ConsultationFee { get; set; }
    public bool IsActive { get; set; }
    public double AverageRate { get; set; }
    public int TotalReviews { get; set; }

    public ICollection<DoctorSchedule>? Schedules { get; set; }
    public ICollection<Appointment>? Appointments { get; set; }
    public ICollection<Review>? Reviews { get; set; }
}