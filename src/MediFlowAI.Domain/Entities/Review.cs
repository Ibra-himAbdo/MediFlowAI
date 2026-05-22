namespace MediFlowAI.Domain;

public class Review : BaseEntity
{
    public Guid DoctorId { get; set; }
    public Doctor? Doctor { get; set; }

    public Guid PatientId { get; set; }
    public Patient? Patient { get; set; }

    public Guid AppointmentId { get; set; }
    public Appointment? Appointment { get; set; }

    public int Rate { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
}