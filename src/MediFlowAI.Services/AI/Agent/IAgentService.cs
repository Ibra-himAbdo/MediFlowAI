namespace MediFlowAI.Services;

public interface IAgentService
{
    Task<string> ProcessAsync(ChatRequest request, CancellationToken cancellationToken = default);
}