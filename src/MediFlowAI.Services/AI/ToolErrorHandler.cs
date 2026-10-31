using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace MediFlowAI.Services;

/// <summary>
/// Converts expected tool and business-rule failures into structured model-readable errors.
/// </summary>
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
            return ToolError.From(
                "Invalid arguments. Use the schema types; dates are yyyy-MM-dd and IDs are UUIDs.");
        }
        catch (UnauthorizedAccessException)
        {
            return ToolError.From(
                "Patient context is missing or this appointment is not accessible to the patient.");
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Tool {ToolName} could not complete", Name);
            return ToolError.From(
                "The operation could not be completed. Check the record and its status. " +
                "Do not report success or retry a mutation blindly.");
        }
    }

    private sealed record ToolError(
        [property: JsonPropertyName("error")] bool Error,
        [property: JsonPropertyName("message")] string Message)
    {
        public static ToolError From(string message) => new(true, message);
    }
}
