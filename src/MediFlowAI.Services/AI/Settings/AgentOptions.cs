namespace MediFlowAI.Services;

public class AgentOptions
{
    public required bool IsOpenAI { get; set; }
    public required string BaseUri { get; set; }
    public required string ApiKey { get; set; }
    public required string Model { get; set; }
    public required string SystemPrompt { get; set; }

    /// <summary>
    /// Optional file name or path under the API content root (e.g. system-prompt.md).
    /// When the file exists, its contents replace <see cref="SystemPrompt"/> from configuration.
    /// </summary>
    public string? SystemPromptFile { get; set; }

    public double TimeoutSeconds { get; set; } = 15;
}