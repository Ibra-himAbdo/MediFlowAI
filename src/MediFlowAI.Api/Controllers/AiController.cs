namespace MediFlowAI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AiController(IAgentService agentService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> AskAgentAsync(ChatRequest request, CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest("Request message cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(request.UserId))
        {
            request = request with
            {
                UserId = DemoPatient.IdString
            };
        }

        try
        {
            string response = await agentService.ProcessAsync(request, cancellationToken);
            return Ok(response);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Problem(
                statusCode: StatusCodes.Status504GatewayTimeout,
                title: "The AI provider timed out.",
                detail: "Please try the request again.");
        }
    }
}
