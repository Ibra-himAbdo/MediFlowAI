namespace MediFlowAI.Domain;

public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
}