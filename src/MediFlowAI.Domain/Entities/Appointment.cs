namespace MediFlowAI.Domain;

public class Appointment : BaseEntity
{
    public Guid DoctorId { get; set; }
    public Doctor? Doctor { get; set; }

    public Guid PatientId { get; set; }
    public Patient? Patient { get; set; }

    public Guid DoctorScheduleId { get; set; }
    public DoctorSchedule? DoctorSchedule { get; set; }

    public AppointmentStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }

    public Review? Review { get; set; }
}