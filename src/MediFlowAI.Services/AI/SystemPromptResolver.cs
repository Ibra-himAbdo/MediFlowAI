namespace MediFlowAI.Services;

public static class SystemPromptResolver
{
    public static string Resolve(AgentOptions options, IHostEnvironment environment)
    {
        if (string.IsNullOrWhiteSpace(options.SystemPromptFile))
        {
            return options.SystemPrompt;
        }

        string path = Path.IsPathRooted(options.SystemPromptFile)
            ? options.SystemPromptFile
            : Path.Combine(environment.ContentRootPath, options.SystemPromptFile);

        if (!File.Exists(path))
        {
            return options.SystemPrompt;
        }

        return File.ReadAllText(path).Trim();
    }
}
