namespace MediFlowAI.Services;

public class AgentOptions
{
    public Provider? Provider { get; set; }
    public string? Endpoint { get; set; }
    // Legacy configuration remains supported while deployments migrate.
    public bool IsOpenAI { get; set; }
    public string? BaseUri { get; set; }
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string SystemPrompt { get; set; } = string.Empty;

    /// <summary>
    /// Optional file name or path under the API content root (e.g. system-prompt.md).
    /// When the file exists, its contents replace <see cref="SystemPrompt"/> from configuration.
    /// </summary>
    public string? SystemPromptFile { get; set; }

    public double TimeoutSeconds { get; set; } = 90;
    public int MaximumToolIterations { get; set; } = 12;
}
