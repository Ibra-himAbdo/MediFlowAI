namespace MediFlowAI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AiController : ControllerBase
{
    private readonly IAgentService _agentService;

    public AiController(IAgentService agentService)
    {
        _agentService = agentService;
    }

    [HttpPost]
    public async Task<IActionResult> AskAgentAsync(ChatRequest request, CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest("Request message cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(request.UserId))
            request = request with
            {
                UserId = DemoPatient.IdString
            };

        string response = await _agentService.ProcessAsync(request, cancellationToken);
        return Ok(response);
    }
}