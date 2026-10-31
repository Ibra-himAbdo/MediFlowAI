using Microsoft.Extensions.AI;

namespace MediFlowAI.Services;

public interface IChatClientFactory
{
    IChatClient Create();
}
