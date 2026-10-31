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

        Guid? patientId = ParsePatientId(request.UserId);
        Conversation conversation = GetConversation(request.UserId);

        await conversation.Gate.WaitAsync(timeout.Token);

        try
        {
            ChatMessage userMessage = new(ChatRole.User, request.Message);
            List<ChatMessage> history = BuildHistory(settings, conversation, userMessage, patientId);
            ChatOptions chatOptions = CreateChatOptions(settings, patientId);

            ChatResponse response = await client.GetResponseAsync(
                history,
                chatOptions,
                timeout.Token);

            // Persist the turn only after the provider and all requested tools succeed.
            conversation.Messages.Add(userMessage);
            conversation.Messages.AddRange(response.Messages);

            return GetFinalAnswer(response);
        }
        finally
        {
            conversation.Gate.Release();
        }
    }

    private static Conversation GetConversation(string? userId) =>
        string.IsNullOrWhiteSpace(userId)
            ? new Conversation()
            : Conversations.GetOrAdd(userId, _ => new Conversation());

    private List<ChatMessage> BuildHistory(
        AgentOptions settings,
        Conversation conversation,
        ChatMessage userMessage,
        Guid? patientId)
    {
        List<ChatMessage> history =
        [
            new(ChatRole.System, SystemPromptResolver.Resolve(settings, environment))
        ];

        if (patientId is not null)
        {
            history.Add(new ChatMessage(
                ChatRole.System,
                $"Demo request patientId: {patientId:D}. " +
                "Patient tools are already bound to this identity; never pass a patientId argument. " +
                "This demo context is not authentication."));
        }

        history.AddRange(conversation.Messages);
        history.Add(userMessage);

        return history;
    }

    private ChatOptions CreateChatOptions(AgentOptions settings, Guid? patientId) =>
        new()
        {
            ModelId = settings.Model,
            Temperature = 0.2f,
            MaxOutputTokens = 2048,
            Tools = toolRegister.GetAllTools(patientId).ToList()
        };

    private static Guid? ParsePatientId(string? userId) =>
        Guid.TryParse(userId, out Guid patientId) && patientId != Guid.Empty
            ? patientId
            : null;

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
