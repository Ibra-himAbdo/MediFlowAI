namespace MediFlowAI.Domain;

public class Patient : BaseEntity
{
    public string? FullName { get; set; }
    public string? PhoneNumber { get; set; }
    public DateTime BirthDate { get; set; }

    public ICollection<Appointment>? Appointments { get; set; }
    public ICollection<Review>? Reviews { get; set; }
}