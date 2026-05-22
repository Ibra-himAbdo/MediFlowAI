namespace MediFlowAI.Domain;

public class DoctorSchedule : BaseEntity
{
    public Guid DoctorId { get; set; }
    public Doctor? Doctor { get; set; }

    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }

    public Appointment? Appointment { get; set; }
}