using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Microsoft.Extensions.AI;

namespace MediFlowAI.Services;

public sealed class AgentService(
    IOptions<AgentOptions> options,
    AiToolRegister toolRegister,
    IChatClient client,
    IHostEnvironment environment) : IAgentService
{
    // Demo-only in-memory history. Use a durable, authenticated conversation store in production.
    private static readonly ConcurrentDictionary<string, Conversation> Conversations = new();
    private sealed class Conversation
    {
        public SemaphoreSlim Gate { get; } = new(1, 1);
        public List<ChatMessage> Messages { get; } = [];
    }

    public async Task<string> ProcessAsync(ChatRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Message);
        AgentOptions settings = options.Value;
        using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(settings.TimeoutSeconds));
        CancellationToken token = timeout.Token;
        // Requests without a conversation id must not retain unreachable histories.
        Conversation conversation = string.IsNullOrWhiteSpace(request.UserId)
            ? new() : Conversations.GetOrAdd(request.UserId, _ => new());
        await conversation.Gate.WaitAsync(token);
        try
        {
            List<ChatMessage> history = [new(ChatRole.System, SystemPromptResolver.Resolve(settings, environment))];
            if (Guid.TryParse(request.UserId, out Guid patientId) && patientId != Guid.Empty)
                history.Add(new(ChatRole.System, $"Demo request patientId: {patientId:D}. Patient tools are bound to this request context; do not supply a patientId argument. This is demo context, not authenticated identity."));
            history.AddRange(conversation.Messages);
            ChatMessage userMessage = new(ChatRole.User, request.Message);
            history.Add(userMessage);
            ChatResponse response = await client.GetResponseAsync(history, new ChatOptions
            {
                ModelId = settings.Model,
                Temperature = 0.2f,
                MaxOutputTokens = 2048,
                Tools = toolRegister.GetAllTools(patientId == Guid.Empty ? null : patientId).ToList()
            }, token);
            // Commit only a completed turn. A failed request must not leave a dangling user message.
            conversation.Messages.Add(userMessage);
            conversation.Messages.AddRange(response.Messages);
            return GetFinalAnswer(response);
        }
        finally
        {
            conversation.Gate.Release();
        }
    }

    private static string GetFinalAnswer(ChatResponse response)
    {
        string text = response.Messages.LastOrDefault(message => message.Role == ChatRole.Assistant)?.Text
            ?? response.Text;

        // Some OpenAI-compatible providers return an unclosed <think> preamble as text.
        // Only the final assistant message is user-facing; tool-loop drafts must stay internal.
        if (text.StartsWith("<think>", StringComparison.OrdinalIgnoreCase))
        {
            Match boundary = Regex.Match(text, @"(?:\r?\n\s*){3,}");
            if (boundary.Success)
            {
                text = text[(boundary.Index + boundary.Length)..];
            }
        }

        return Regex.Replace(text, @"<think>.*?</think>\s*", string.Empty,
            RegexOptions.IgnoreCase | RegexOptions.Singleline).Trim();
    }
}
