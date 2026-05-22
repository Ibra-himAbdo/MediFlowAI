using System.ClientModel;
using System.Collections.Concurrent;
using Microsoft.Extensions.AI;

namespace MediFlowAI.Services;

public class AgentService : IAgentService
{
    private readonly AiToolRegister _toolRegister;

    // A simple dictionary to store chat history per user in memory (easy way for demos)
    private static readonly ConcurrentDictionary<string, List<ChatMessage>> UserChatHistory = new();

    private readonly AgentOptions _agentOptions;
    private readonly string _systemPrompt;

    public AgentService(
        IOptions<AgentOptions> options,
        AiToolRegister toolRegister,
        IHostEnvironment environment)
    {
        _toolRegister = toolRegister;
        _agentOptions = options.Value;
        _systemPrompt = SystemPromptResolver.Resolve(_agentOptions, environment);
    }

    public async Task<string> ProcessAsync(ChatRequest request, CancellationToken cancellationToken = default)
    {
        // 1. Get existing history for the user, or create a new list with the system prompt if they don't have one
        string userId = request.UserId ?? Guid.NewGuid().ToString();
        List<ChatMessage> history = UserChatHistory.GetOrAdd(userId, _ =>
            [new(ChatRole.System, _systemPrompt)]
        );

        // 2. Add the user's new message to the history
        history.Add(new ChatMessage(ChatRole.User, request.Message));

        // 3. create Client and ChatOptions
        IChatClient client = CreateChatClient();
        ChatOptions options = CreateChatOptions();

        // 4. Send the entire history to the AI
        ChatResponse response = await client.GetResponseAsync(history, options, cancellationToken);

        // 5. Save the AI's response to the history so it remembers it next time
        history.AddRange(response.Messages);

        if (response.Usage is not null)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;

            Console.WriteLine(
                $"Tokens => Input: {response.Usage.InputTokenCount}, " +
                $"Output: {response.Usage.OutputTokenCount}, " +
                $"Total: {response.Usage.TotalTokenCount}");

            Console.ResetColor();
        }

        //6. return response 
        return response.Text;
    }

    private IChatClient CreateChatClient()
    {
        if (string.IsNullOrWhiteSpace(_agentOptions.ApiKey))
        {
            throw new InvalidOperationException(
                "Agent API key is not configured. Set AgentOptions:API-Key in appsettings, User Secrets, or environment variable AgentOptions__API-Key.");
        }
        // if we are using OpenAI's API.
        if (_agentOptions.IsOpenAI)
        {
            return new OpenAI.Chat.ChatClient(_agentOptions.Model, _agentOptions.ApiKey)
                .AsIChatClient()
                .AsBuilder()
                .UseFunctionInvocation()
                .Build();
        }
        else
        {
            return new OpenAI.Chat.ChatClient(
                    _agentOptions.Model,
                    new ApiKeyCredential(_agentOptions.ApiKey),
                    new OpenAI.OpenAIClientOptions()
                    {
                        Endpoint = new Uri(_agentOptions.BaseUri)
                    })
                .AsIChatClient()
                .AsBuilder()
                .UseFunctionInvocation()
                .Build();
        }
    }

    private ChatOptions CreateChatOptions()
    {
        return new ChatOptions()
        {
            ModelId = _agentOptions.Model,
            Temperature = 0.2f,
            MaxOutputTokens = 2048,
            Tools = _toolRegister.GetAllTools().ToList()
        };
    }
}