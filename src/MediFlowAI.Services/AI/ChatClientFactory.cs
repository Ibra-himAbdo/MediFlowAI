using System.ClientModel;
using Microsoft.Extensions.AI;

namespace MediFlowAI.Services;

public enum Provider { OpenAI, OpenAILike }

public interface IChatClientFactory
{
    IChatClient Create();
}

public sealed class ChatClientFactory(IOptions<AgentOptions> options) : IChatClientFactory
{
    public IChatClient Create()
    {
        AgentOptions settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.ApiKey) || string.IsNullOrWhiteSpace(settings.Model))
            throw new InvalidOperationException("Configure AgentOptions:ApiKey and AgentOptions:Model.");
        if (!double.IsFinite(settings.TimeoutSeconds) || settings.TimeoutSeconds <= 0 || settings.TimeoutSeconds > 3600)
            throw new InvalidOperationException("AgentOptions:TimeoutSeconds must be between 0 and 3600 seconds (exclusive of 0).");
        if (settings.MaximumToolIterations is < 1 or > 100)
            throw new InvalidOperationException("AgentOptions:MaximumToolIterations must be between 1 and 100.");

        Provider provider = settings.Provider ?? (settings.IsOpenAI ? Provider.OpenAI : Provider.OpenAILike);
        OpenAI.OpenAIClientOptions clientOptions = new();
        if (provider == Provider.OpenAILike)
        {
            if (!Uri.TryCreate(settings.Endpoint ?? settings.BaseUri, UriKind.Absolute, out Uri? endpoint)
                || (endpoint.Scheme != Uri.UriSchemeHttps && endpoint.Scheme != Uri.UriSchemeHttp))
                throw new InvalidOperationException("Configure AgentOptions:Endpoint with an absolute HTTP(S) URL.");
            clientOptions.Endpoint = endpoint;
        }
        else if (provider != Provider.OpenAI)
            throw new NotSupportedException($"Provider '{provider}' is not supported.");

        return new OpenAI.Chat.ChatClient(settings.Model, new ApiKeyCredential(settings.ApiKey), clientOptions)
            .AsIChatClient().AsBuilder()
            .UseFunctionInvocation(configure: invocation =>
            {
                // Tools share the request-scoped EF DbContext, which cannot run concurrent queries.
                invocation.AllowConcurrentInvocation = false;
                invocation.MaximumIterationsPerRequest = settings.MaximumToolIterations;
            }).Build();
    }
}
