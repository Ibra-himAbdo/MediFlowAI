namespace MediFlowAI.Domain;

public class Specialization : BaseEntity
{
    public string? Name { get; set; }

    public ICollection<Doctor>? Doctors { get; set; }
}