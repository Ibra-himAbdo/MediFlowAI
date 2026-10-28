using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace MediFlowAI.Services;

/// <summary>One error boundary for SDK argument binding and service failures; no custom binding or serialization.</summary>
internal sealed class ToolErrorHandler(AIFunction inner, ILogger logger) : DelegatingAIFunction(inner)
{
    protected override async ValueTask<object?> InvokeCoreAsync(AIFunctionArguments arguments, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            return await base.InvokeCoreAsync(arguments, cancellationToken);
        }
        catch (Exception ex) when (ex is ArgumentException or JsonException or FormatException)
        {
            return new ToolError(true, "Invalid arguments. Use the schema's required parameters and types; dates are yyyy-MM-dd and IDs are UUIDs.");
        }
        catch (UnauthorizedAccessException)
        {
            return new ToolError(true, "Patient context is missing or the appointment is not accessible to this patient.");
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Tool {ToolName} could not complete", Name);
            return new ToolError(true, "The operation could not be completed. Check the record and its status. Do not report success or retry a mutation blindly.");
        }
    }

    private sealed record ToolError(
        [property: JsonPropertyName("error")] bool Error,
        [property: JsonPropertyName("message")] string Message);
}
