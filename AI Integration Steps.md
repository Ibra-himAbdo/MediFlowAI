# Add an AI agent to an ASP.NET Core service

This guide explains the AI integration used by MediFlow AI and how to reuse the same pattern in another ASP.NET Core application.

The model never talks to Entity Framework or the database. It can call a small allow-list of application operations, and the existing service layer keeps ownership, validation, and business rules.

```text
HTTP request
    │
    ▼
AgentService ──► IChatClient ──► AI provider
    ▲                                │
    │                                │ tool call
    │                                ▼
AiToolRegister ──► Application services ──► AppDbContext
```

## Where each responsibility lives

| Component | Responsibility |
| --- | --- |
| `AgentOptions` | Provider, endpoint, model, timeout, and tool-loop limits |
| `ChatClientFactory` | Creates the OpenAI or OpenAI-compatible client |
| `AiToolRegister` | Defines the operations visible to the model |
| `PatientAppointmentTools` | Adds request-bound patient context to sensitive actions |
| `ToolErrorHandler` | Turns expected failures into structured tool results |
| `AgentService` | Builds chat history, sends the request, and stores completed turns |
| `system-prompt.md` | Explains the role, workflows, and behavioral rules to the model |
| Application services | Enforce business rules and access the database |

`AiToolRegister` is an allow-list, not another service layer. Read operations point directly to existing service methods. A small wrapper is used only where an operation needs context that the model must not control, such as the current patient ID.

The source is grouped by responsibility:

```text
AI/
├── Agent/          # Chat orchestration and public agent contract
├── Configuration/  # Options, provider selection, and prompt loading
├── Providers/      # IChatClient construction
├── Tools/          # Tool catalog, patient context, and error mapping
└── README.md        # Short reading order for new contributors
```

The folders all use the same `MediFlowAI.Services` namespace. They make navigation clearer without creating extra architectural layers.

## 1. Install the AI packages

The Services project references:

```xml
<PackageReference Include="Microsoft.Extensions.AI" Version="10.11.0" />
<PackageReference Include="Microsoft.Extensions.AI.OpenAI" Version="10.11.0" />
```

The first package provides the common chat and tool abstractions. The OpenAI package supplies the client used for OpenAI and OpenAI-compatible APIs.

## 2. Add provider configuration

Create an options class containing only provider concerns:

```csharp
public class AgentOptions
{
    public Provider? Provider { get; set; }
    public string? Endpoint { get; set; }
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string SystemPrompt { get; set; } = string.Empty;
    public string? SystemPromptFile { get; set; }
    public double TimeoutSeconds { get; set; } = 90;
    public int MaximumToolIterations { get; set; } = 12;
}
```

Configure it in `appsettings.json`:

```json
{
  "AgentOptions": {
    "Provider": "OpenAILike",
    "Endpoint": "https://api.groq.com/openai/v1",
    "Model": "meta-llama/llama-4-scout-17b-16e-instruct",
    "SystemPromptFile": "system-prompt.md",
    "TimeoutSeconds": 90,
    "MaximumToolIterations": 12
  }
}
```

Use `Provider: OpenAI` without `Endpoint` for the official OpenAI API. Use `OpenAILike` with an absolute endpoint for Groq, OpenRouter, DeepSeek, or another compatible host.

Keep the API key outside source control:

```powershell
dotnet user-secrets set "AgentOptions:ApiKey" "YOUR_API_KEY" `
  --project src/MediFlowAI.Api
```

The equivalent environment variable is `AgentOptions__ApiKey`.

The selected model must support function calling. An OpenAI-compatible chat endpoint does not automatically guarantee tool support.

## 3. Create the chat client once

`ChatClientFactory` validates the configuration and builds the provider client. Function invocation middleware executes tool calls returned by the model and sends their results back until the model produces a final answer.

```csharp
return new OpenAI.Chat.ChatClient(
        settings.Model,
        new ApiKeyCredential(settings.ApiKey),
        clientOptions)
    .AsIChatClient()
    .AsBuilder()
    .UseFunctionInvocation(configure: invocation =>
    {
        invocation.AllowConcurrentInvocation = false;
        invocation.MaximumIterationsPerRequest = settings.MaximumToolIterations;
    })
    .Build();
```

Concurrent tool invocation is disabled because the tools in one HTTP request share a scoped EF Core `DbContext`, which cannot run multiple operations concurrently.

Register the factory and client with dependency injection:

```csharp
services.Configure<AgentOptions>(configuration.GetSection(nameof(AgentOptions)));

services.AddSingleton<IChatClientFactory, ChatClientFactory>();
services.AddChatClient(serviceProvider =>
    serviceProvider.GetRequiredService<IChatClientFactory>().Create());

services.AddScoped<AiToolRegister>();
services.AddScoped<IAgentService, AgentService>();
```

`AddChatClient` registers the `IChatClient` through the Microsoft.Extensions.AI dependency-injection extensions. The tool registry stays scoped because it references scoped application services.

## 4. Expose safe service methods as tools

Create tools with method delegates rather than reflection strings or manual JSON schemas:

```csharp
private AIFunction Create(Delegate method, string name, string description) =>
    new ToolErrorHandler(AIFunctionFactory.Create(method, new AIFunctionFactoryOptions
    {
        Name = name,
        Description = description
    }), logger);
```

Register only the operations the assistant needs:

```csharp
return
[
    Create(
        doctors.SearchDoctorsAsync,
        "search_doctors",
        "Find doctors by doctor or specialty name. Set slotsDate to include available slots."),

    Create(
        schedules.GetAvailableSlotsAsync,
        "get_available_slots",
        "List available slots for a doctor and UTC date."),

    Create(
        patientTools.BookAsync,
        "book_appointment",
        "Book a selected doctorScheduleId for the current patient.")
];
```

`AIFunctionFactory` derives the JSON schema from the C# signature. It binds values such as `Guid`, `DateOnly`, optional arguments, and `CancellationToken`. Services return DTOs directly; there is no manual JSON-string bridge.

Keep the list focused. Overlapping tools make selection harder for the model. MediFlow exposes ten operations:

| Area | Tools |
| --- | --- |
| Date | `get_date` |
| Specializations | `get_all_specializations` |
| Doctors | `get_all_doctors`, `search_doctors` |
| Schedules | `get_available_slots` |
| Reviews | `get_doctor_reviews` |
| Current patient | `get_patient_appointments`, `book_appointment`, `cancel_appointment`, `add_review` |

## 5. Bind sensitive context outside the tool schema

The model must not choose the patient for a booking. `PatientAppointmentTools` captures the patient ID supplied by the application and exposes methods without a `patientId` argument:

```csharp
public Task<BookingResultDto> BookAsync(
    Guid doctorScheduleId,
    CancellationToken cancellationToken = default)
{
    RequireId(doctorScheduleId);
    return appointments.BookAppointmentAsync(
        PatientId,
        doctorScheduleId,
        cancellationToken);
}
```

Cancellation and review methods load the appointment and verify `appointment.PatientId == PatientId` before calling the service mutation.

In this demo, `userId` is caller-supplied and defaults to a seeded patient. In production, obtain the patient ID from authenticated claims. Tool binding protects the model boundary, but it does not authenticate the HTTP caller.

## 6. Return tool failures to the model

Expected validation, authorization, and business-rule failures should become structured tool results so the model can explain the problem without claiming success:

```json
{
  "error": true,
  "message": "The operation could not be completed. Check the record and its status."
}
```

Do not convert request cancellation into a normal tool result. Let `OperationCanceledException` propagate so the API can return a timeout or stop work when the client disconnects.

## 7. Build one request and return one final answer

For each request, `AgentService`:

1. Loads the system prompt.
2. Adds the patient context supplied by the application.
3. Appends previous completed conversation turns.
4. Adds the new user message.
5. Supplies the request-scoped tool list.
6. Calls the provider using the configured timeout.
7. Stores the turn only after the provider and tools succeed.
8. Returns the final assistant message.

Returning only the last assistant message matters. Some providers include intermediate tool-loop drafts or reasoning text in `ChatResponse.Text`. Returning the concatenated text can expose `<think>` content or an answer produced before the real tool result.

The demo serializes requests for the same conversation with `SemaphoreSlim`, because two simultaneous turns must not mutate the same history list.

## 8. Write a workflow-oriented system prompt

The prompt should define behavior without duplicating business rules:

- Tell the model when to use each tool.
- State date and time conventions.
- Require confirmation before changing the selected doctor, date, or time.
- Tell it never to invent identifiers or successful mutations.
- Explain that patient identity is application context.
- Keep authorization and business validation in C# services.

MediFlow loads `system-prompt.md` at startup through `SystemPromptResolver`. If the file is missing, it uses the inline `SystemPrompt` setting.

## 9. Add the HTTP endpoint

The controller validates the request, applies the demo patient when needed, and maps provider timeout to HTTP 504:

```csharp
[HttpPost]
public async Task<IActionResult> AskAgentAsync(
    ChatRequest request,
    CancellationToken cancellationToken)
{
    if (string.IsNullOrWhiteSpace(request.Message))
    {
        return BadRequest("Request message cannot be empty.");
    }

    try
    {
        return Ok(await agentService.ProcessAsync(request, cancellationToken));
    }
    catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
    {
        return Problem(
            statusCode: StatusCodes.Status504GatewayTimeout,
            title: "The AI provider timed out.");
    }
}
```

## 10. Run and verify the integration

Build and start the API:

```powershell
dotnet build MediFlowAI.slnx -m:1
dotnet run --project src/MediFlowAI.Api
```

Send a read request:

```bash
curl --insecure https://localhost:7141/api/Ai \
  --header "Content-Type: application/json" \
  --data '{
    "message": "Show Cardiology doctors and available slots tomorrow"
  }'
```

Then verify a complete development flow:

```text
search doctors → choose slot → book → read appointment → cancel
```

For mutation tests, use a development database and verify the resulting appointment directly through the REST API. Do not rely only on the natural-language response.

## Production checklist

Before using this pattern in production:

- Derive patient identity from authentication claims.
- Enforce ownership again inside the application service or authorization layer.
- Store bounded conversation history outside process memory.
- Add idempotency for booking and other retry-sensitive mutations.
- Log tool names and outcomes without secrets or private medical data.
- Use provider retry and rate-limit policies appropriate for the application.
- Add integration tests against the database and a provider test environment.
- Decide how cancellation releases a slot while retaining appointment history.

The current data model keeps a one-to-one relationship between a schedule slot and its appointment. A cancelled appointment therefore does not reopen the slot. Fixing that correctly requires a data-model change and migration, not a prompt change.
