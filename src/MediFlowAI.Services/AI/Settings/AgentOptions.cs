namespace MediFlowAI.Services;

public class AgentOptions
{
    public Provider Provider { get; set; } = Provider.OpenAILike;
    public string? Endpoint { get; set; }
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string SystemPrompt { get; set; } = string.Empty;

    /// <summary>
    /// Optional file under the API content root, for example system-prompt.md.
    /// Its contents replace <see cref="SystemPrompt"/> when the file exists.
    /// </summary>
    public string? SystemPromptFile { get; set; }

    public double TimeoutSeconds { get; set; } = 90;
    public int MaximumToolIterations { get; set; } = 12;
}
