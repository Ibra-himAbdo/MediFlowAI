# AI integration

## Architecture

`AiController -> AgentService -> IChatClient (function invocation) -> registered service methods -> AppDbContext`

`AiToolRegister` is an explicit allow-list, not a second service layer. Public reads bind directly to service method delegates. `AIFunctionFactory` derives schemas and binds typed `Guid`, `DateOnly`, optional arguments and cancellation tokens. Services return DTOs; the SDK serializes results once. There is no manual JSON-string bridge.

`PatientAppointmentTools` contains only operations that need patient context: booking, cancellation, review submission and access to the current patient's appointments. It captures a patient ID per request, checks appointment ownership, and delegates business rules to the services. Patient identity is not a tool argument. `ToolErrorHandler` is a shared error boundary; it does not coerce or bind arguments.

`ChatClientFactory` selects OpenAI or an OpenAI-compatible endpoint. The client is shared and disposed by DI; tool instances and EF services remain request-scoped. Tool execution is sequential because EF DbContext does not support concurrent operations. AgentService serializes turns for the same conversation, applies a timeout and only retains completed turns.

## Configuration

```json
{
  "AgentOptions": {
    "Provider": "OpenAILike",
    "Endpoint": "https://api.groq.com/openai/v1",
    "Model": "YOUR_TOOL_CAPABLE_MODEL",
    "SystemPromptFile": "system-prompt.md",
    "TimeoutSeconds": 60,
    "MaximumToolIterations": 12
  }
}
```

Supply `AgentOptions:ApiKey` via User Secrets or environment variable `AgentOptions__ApiKey`. OpenAI uses `Provider: OpenAI` without a custom endpoint. Legacy `IsOpenAI` and `BaseUri` remain fallbacks when Provider/Endpoint are absent. The selected model must support function calling; a compatible endpoint alone does not establish that capability.

## Tool contract

| Group | Tools |
| --- | --- |
| Clock | get_date |
| Specializations | get_all_specializations |
| Doctors | get_all_doctors, search_doctors |
| Schedules | get_available_slots |
| Published reviews | get_doctor_reviews |
| Current patient | get_patient_appointments, book_appointment, cancel_appointment, add_review |

There are 10 tools. Search results already include ratings, fees and optional slots; patient appointment lists already include booking details, so separate lookup/rating tools are unnecessary. Broad patient lists/searches, other doctors' appointment lists and appointment completion are not exposed to the patient assistant. Existing REST services/controllers remain separate.

- `search_doctors` takes `query`, matching either doctor name or specialty. Use `get_all_doctors` to browse without a search term.
- Doctor reads take optional `slotsDate`; omit it or use null, never an empty string. Dates are yyyy-MM-dd in UTC.
- `get_available_slots` takes `doctorId` and `date`. Booking itself checks availability; there is no separate availability-check step.
- `book_appointment` takes only `doctorScheduleId`. The application supplies patient context.
- `get_patient_appointments` takes no patient ID. The list is scoped to the current patient; cancellation and reviews verify appointment ownership.
- `add_review` takes `appointmentId`, `rate` (1-5) and optional `comment`; the service requires an already completed appointment.
- Results are structured objects, arrays or scalar values. Null lookup results mean not found. Failures return `error: true`; cancellation propagates instead of being converted into a tool error.

## Workflows

For specialty slots, call `search_doctors` with the specialty name in `query` and the requested `slotsDate`; inspect each doctor's `availableSlots`. Compare ratings and fees from the same result.

For a named doctor, call `search_doctors`. Ask the user to choose when several doctors match. Select a slot matching the requested UTC date/time. Ask for confirmation before substituting another date, time or doctor, then pass its ID to `book_appointment`.

For cancellation/reviews, use `get_patient_appointments` to discover appointment IDs. Never mark an appointment completed just to enable a review; completion is not available through this assistant.

## Validation

```powershell
dotnet build MediFlowAI.slnx -m:1
```

Build validation does not exercise a live model or database. Verify the configured provider with representative booking conversations against a development database.

## Demo limitations

The HTTP API currently accepts caller-supplied userId and defaults to a shared demo patient. This is not authentication. Production must supply patient identity from authenticated claims and enforce authorization in the service/API layer as well. The tool boundary prevents the model from choosing another patient, but does not authenticate the HTTP caller.

Histories are in-memory and unbounded. Timeouts cannot roll back a service mutation that has already committed. Booking retries need idempotency/reconciliation. Cancellation currently retains the schedule relationship and does not reopen the slot.
