# AI layer reading guide

The AI layer is an adapter around the existing application services. It does not contain business rules and does not access the database directly.

## Start here

Follow one chat request in this order:

1. `DependencyInjection.cs` registers the provider, agent, and tool registry.
2. `Agent/AgentService.cs` builds the conversation and calls `IChatClient`.
3. `Tools/AiToolRegister.cs` defines the operations available to the model.
4. Application services validate the operation and access the database.
5. `AgentService` stores the completed turn and returns the final answer.

## Folders

| Folder | Purpose |
| --- | --- |
| `Agent` | Orchestrates chat history, provider calls, and final responses |
| `Configuration` | Provider settings and system-prompt loading |
| `Providers` | Creates the configured `IChatClient` |
| `Tools` | Exposes a small allow-list of service-layer operations |

`PatientAppointmentTools` is intentionally a wrapper. It supplies the patient ID from the HTTP request so the model cannot choose another patient's identity.

All files keep the `MediFlowAI.Services` namespace. The folders describe responsibilities without adding namespace or dependency complexity.
