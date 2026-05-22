# MediFlow AI — AI Integration Guide

Simple, complete explanation of how the AI assistant works in **your current codebase**.  
Read top to bottom once — then use it as a reference during the demo.

---

## What you built

A **chat API** that lets users talk to an LLM (Groq / OpenAI-style) that can **call your real MediFlow services**:

| User does | System does |
|-----------|-------------|
| `POST /api/Ai` with a message | Loads chat history, sends to LLM |
| AI needs data | Calls a **tool** (e.g. `get_doctors_by_specialization_name`) |
| Tool runs | `MediFlowAiTools` → your services → `AppDbContext` → SQL Server |
| AI replies | Plain text answer based on real data |

The AI **never** touches EF or SQL directly — only your services (via `MediFlowAiTools`).

---

## Project structure (your solution)

```
MediFlowAI/
├── src/
│   ├── MediFlowAI.Api/
│   │   ├── system-prompt.md              → Preferred AI system prompt
│   │   ├── Extensions/DatabaseMigrator.cs → Seed + schedule range filler
│   │   └── Controllers/AiController.cs
│   ├── MediFlowAI.Services/
│   │   ├── AI/
│   │   │   ├── AgentService.cs           → Chat + LLM + history
│   │   │   ├── AiToolRegister.cs         → Registers tools for the model
│   │   │   ├── MediFlowAiTools.cs        → Tool implementations (JSON in/out)
│   │   │   ├── SystemPromptResolver.cs
│   │   │   ├── IAgentService.cs
│   │   │   └── Settings/AgentOptions.cs
│   │   ├── Interfaces/
│   │   ├── Services/
│   │   └── DependencyInjection.cs
│   ├── MediFlowAI.Dto/                   → DoctorDto includes AvailableSlots
│   ├── MediFlowAI.Domain/
│   └── MediFlowAI.Infrastructure/
└── scripts/test-apis.ps1
```

**Flow:** `AiController` → `AgentService` → LLM tools → `MediFlowAiTools` → application services → `AppDbContext`.

---

## Architecture diagram

<img src="image.png" alt="Centered Image">

```text
User → POST /api/Ai → AgentService → Groq (function calling)
                              ↓
                        AiToolRegister (25 tools)
                              ↓
                        MediFlowAiTools (parse ids/dates, JSON)
                              ↓
                   Specialization / Doctor / Schedule / Appointment / Review services
                              ↓
                         SQL Server
```

---

## Step 1 — NuGet packages

**File:** `src/MediFlowAI.Services/MediFlowAI.Services.csproj`

```xml
<PackageReference Include="Microsoft.Extensions.AI" Version="10.6.0" />
<PackageReference Include="Microsoft.Extensions.AI.OpenAI" Version="10.6.0" />
```

- **Microsoft.Extensions.AI** — chat + tools abstractions.
- **Microsoft.Extensions.AI.OpenAI** — OpenAI-compatible client (Groq).
- **Microsoft.AspNetCore.App** — `IOptions<>`, `IHostEnvironment`, DI.

---

## Step 2 — Configuration (`appsettings.json`)

**File:** `src/MediFlowAI.Api/appsettings.json`

```json
"AgentOptions": {
  "IsOpenAI": false,
  "BaseUri": "https://api.groq.com/openai/v1",
  "ApiKey": "your-key-here-or-use-user-secrets",
  "Model": "meta-llama/llama-4-scout-17b-16e-instruct",
  "SystemPromptFile": "system-prompt.md",
  "SystemPrompt": "You are MediFlow AI, a medical booking assistant..."
}
```

| Property | Purpose |
|----------|---------|
| `IsOpenAI` | `true` = OpenAI; `false` = Groq / compatible host |
| `BaseUri` | Groq endpoint when `IsOpenAI` is `false` |
| `ApiKey` | LLM API key (use User Secrets in dev) |
| `Model` | Model id on the provider |
| `SystemPromptFile` | Markdown file under API content root (used if file exists) |
| `SystemPrompt` | Fallback if file missing |
| `TimeoutSeconds` | Optional; wire in `ProcessAsync` if needed |

### System prompt file

**File:** `src/MediFlowAI.Api/system-prompt.md`

- Edit behavior without recompiling C#.
- Copied on build (`CopyToOutputDirectory` in `MediFlowAI.Api.csproj`).
- Loaded via `SystemPromptResolver` + `IHostEnvironment.ContentRootPath`.

**Order:** `SystemPromptFile` exists → use file; else → `SystemPrompt` from appsettings.

### API key

```bash
dotnet user-secrets set "AgentOptions:ApiKey" "gsk_your_groq_key" --project src/MediFlowAI.Api
```

Environment: `AgentOptions__ApiKey=...`

---

## Step 3 — Dummy data (testing)

**File:** `src/MediFlowAI.Api/Extensions/DatabaseMigrator.cs`

Runs on every API start (migrate + seed if empty + `EnsureScheduleRangeAsync`).

| Rule | Value |
|------|--------|
| Specializations | 6 fixed names (Cardiology, Pediatrics, …) |
| Doctors | **2 per specialization**, all active, random `Dr. {FullName}` |
| Patients | 25 random patients |
| Schedule range | **3 days ago → 10 days ahead** (UTC) |
| Slot times | 09:00, 10:00, 11:00, 14:00, 15:00, 16:00 UTC per day |
| Sun–Thu | All active doctors have slots |
| Fri–Sat | **One on-call doctor per specialization** |
| Appointments | ~35% of slots pre-booked; past → completed, future → pending/confirmed |

**Available slots API** only returns slots where `StartTime >= UtcNow` and no appointment — so **“today”** can be empty after 16:00 UTC or if all slots are booked.

**Fresh seed:** delete the `MediFlowAI` database and restart the API if you need a clean dataset.

### Demo patient for AI booking (authorized test user)

**File:** `src/MediFlowAI.Api/Demo/DemoPatient.cs`

| Constant | Value |
|----------|--------|
| `DemoPatient.Id` | `019e513f-b4aa-7dc3-aa1d-2ed781164ae0` |
| `DemoPatient.FullName` | `Demo Patient (AI User)` |

- Seeded on first database seed (plus 24 other random patients).
- `EnsureDemoPatientAsync` adds this patient on startup if missing (existing DBs).
- `AiController` sets `UserId = DemoPatient.IdString` when the client omits `userId`.

Use this id for AI flows: book, cancel, complete, review — same as an authenticated demo user.

```http
POST /api/Ai
{ "message": "Book the 10:00 slot for me" }
```

Omit `userId` → books as demo patient. Or pass `"userId": "019e513f-b4aa-7dc3-aa1d-2ed781164ae0"` explicitly.

---

## Step 4 — `AgentOptions` and `SystemPromptResolver`

**Files:** `AgentOptions.cs`, `SystemPromptResolver.cs`

`AgentService` constructor:

```csharp
_systemPrompt = SystemPromptResolver.Resolve(_agentOptions, environment);
```

---

## Step 5 — Dependency injection

**File:** `src/MediFlowAI.Services/DependencyInjection.cs`

```csharp
services.Configure<AgentOptions>(configuration.GetSection(nameof(AgentOptions)));

services.AddScoped<MediFlowAiTools>();
services.AddScoped<AiToolRegister>();
services.AddScoped<IAgentService, AgentService>();
```

| Registration | Lifetime | Role |
|--------------|----------|------|
| Domain services | Scoped | Same as `DbContext` |
| `MediFlowAiTools` | Scoped | AI tool implementations |
| `AiToolRegister` | Scoped | Builds `AITool` list from `MediFlowAiTools` |
| `AgentService` | Scoped | Chat per HTTP request |

---

## Step 6 — `ChatRequest` and `AiController`

```csharp
public record ChatRequest(string Message, string? UserId);
```

| Field | Role |
|-------|------|
| `Message` | User question |
| `UserId` | Chat session key (in-memory history) + default `patientId` for booking |

```http
POST /api/Ai
Content-Type: application/json

{
  "message": "List Pediatrics doctors and slots tomorrow",
  "userId": "<patient-guid-from-db>"
}
```

---

## Step 7 — `AgentService`

1. Resolve `userId` (or new Guid for new conversation).
2. Load/append messages in static `UserChatHistory`.
3. System message from `_systemPrompt`.
4. `CreateChatClient()` + `.UseFunctionInvocation()`.
5. `Tools = _toolRegister.GetAllTools()`, `Temperature = 0.2f`.
6. `GetResponseAsync` → append reply → return `response.Text`.

---

## Step 8 — `MediFlowAiTools` (what the LLM actually runs)

**File:** `src/MediFlowAI.Services/AI/MediFlowAiTools.cs`

Wrapper layer between the model and your services.

### Why it exists

| Problem | How `MediFlowAiTools` fixes it |
|---------|--------------------------------|
| Model sends `"doctorId from previous result"` | Parameters are **strings**; Groq validates less strictly; you parse `Guid` in C# |
| Invalid UUID crashes the API | Returns JSON `{ "error": true, "hint": "..." }` for retry |
| Model guesses dates | Slot tools return `utcToday`, `utcTomorrow` in JSON |
| Model needs JSON text | All methods return `Task<string>` (serialized JSON) |

### Behavior

- Calls `ISpecializationService`, `IPatientService`, `IDoctorService`, etc. (same as REST API).
- `TryParseGuid` / `TryParseDate` / `TryResolveSlotsDate` on inputs.
- `Serialize` / `SerializeWithDateContext` on outputs (camelCase).
- `get_doctors_by_specialization_name`: if `date` is empty → defaults to **UTC tomorrow**.

### Doctor list with slots

Doctor tools accept optional `date` (`yyyy-MM-dd`). When set, `DoctorDto.AvailableSlots` is filled (same logic as schedule service: future, unbooked only).

REST example:

```http
GET /api/Doctors/specialization/name/Pediatrics?date=2026-05-26
GET /api/Doctors?date=2026-05-26
```

---

## Step 9 — `AiToolRegister`

**File:** `src/MediFlowAI.Services/AI/AiToolRegister.cs`

- Holds `MediFlowAiTools` (not raw `IServiceProvider` per service).
- `AIFunctionFactory.Create(method, _tools, options)` per method.
- **25 tools**, snake_case names, descriptions for the model.

### Tool catalog

**Specializations (3)**

| Tool | Notes |
|------|--------|
| `get_all_specializations` | List all; copy `id` for UUID tools |
| `get_specialization_by_id` | `id` = UUID string |
| `search_specializations` | `specializationName` = plain text, e.g. Cardiology (not a JSON object) |

**Patients (3)**

| Tool | Notes |
|------|--------|
| `get_all_patients` | List patients; find demo user id for booking |
| `get_patient_by_id` | `patientId` = UUID (chat `userId`) |
| `search_patients` | By name or phone, e.g. `Demo` |

**Doctors (6)**

| Tool | Notes |
|------|--------|
| `get_all_doctors` | `date` optional → `availableSlots` |
| `get_doctor_by_id` | `doctorId` + optional `date` |
| `get_doctors_by_specialization_name` | Best for “pediatrics doctors”; `date` defaults to UTC tomorrow |
| `search_doctors` | `query` + optional `date` |
| `get_doctors_by_specialization` | `specializationId` = UUID + optional `date` |
| `get_top_rated_doctors` | `specializationId`, `take`, optional `date` |

**Schedules (5)**

| Tool | Notes |
|------|--------|
| `get_available_slots_for_specialization` | **Best one-shot**: specialty name + `date` |
| `get_available_slots_by_doctor_name` | Doctor name + `date` |
| `get_available_slots` | `doctorId` UUID + `date` |
| `get_doctor_schedule` | `doctorId`, `fromDate`, `toDate` |
| `is_slot_available` | `scheduleId` = slot UUID |

**Appointments (6)**

| Tool | Notes |
|------|--------|
| `book_appointment` | `patientId`, `doctorScheduleId` (slot `id`) |
| `cancel_appointment` | `appointmentId` |
| `complete_appointment` | Before review |
| `get_appointment_by_id` | |
| `get_patient_appointments` | |
| `get_doctor_appointments` | |

**Reviews (3)**

| Tool | Notes |
|------|--------|
| `add_review` | `appointmentId`, `rate` 1–5, `comment` |
| `get_doctor_reviews` | |
| `get_doctor_average_rate` | |

---

## Recommended AI flows

### Pediatrics + slots (single tool)

| Tool | Parameters |
|------|------------|
| `get_doctors_by_specialization_name` | `Pediatrics`, `date` = `yyyy-MM-dd` or empty (→ tomorrow UTC) |

Response includes each doctor’s `availableSlots`.

### Or one-shot specialty + slots

| Tool | Parameters |
|------|------------|
| `get_available_slots_for_specialization` | `Pediatrics`, `date` = `yyyy-MM-dd` |

### Book

| Tool | Parameters |
|------|------------|
| `book_appointment` | `patientId` = chat `userId` (real patient UUID), `doctorScheduleId` = slot `id` from prior tool |

**Never** pass placeholder text into UUID fields (`cardiology-specialization-id`, `doctorId from previous result`).

### When you need a specialization UUID

1. `search_specializations` or `get_all_specializations`  
2. Copy exact `id` from JSON  
3. `get_doctors_by_specialization` or `get_top_rated_doctors`

---

## Demo script (live)

| # | Ask | Expected tools |
|---|-----|----------------|
| 1 | What specializations exist? | `get_all_specializations` |
| 2 | Find Cardiology | `search_specializations` |
| 3 | Pediatrics doctors with slots on &lt;date&gt; | `get_doctors_by_specialization_name` |
| 4 | Book morning slot | `book_appointment` |
| 5 | Complete and 5-star review | `complete_appointment` → `add_review` |

Use a **weekday** within the next 10 days (UTC). Avoid relying on “today” after clinic hours.

---

## Commands (run & call AI)

### 1. API key (once)

```bash
dotnet user-secrets set "AgentOptions:ApiKey" "YOUR_GROQ_KEY" --project src/MediFlowAI.Api
```

### 2. Start API

```bash
dotnet run --project src/MediFlowAI.Api
```

- Scalar UI: `https://localhost:7141/scalar/v1`
- HTTP (if profile uses it): `http://localhost:5167`

### 3. REST smoke test (no AI)

```powershell
powershell -File scripts/test-apis.ps1
```

Change `$Base` in the script if your port differs.

### 4. Demo patient id (booking / reviews)

```text
019e513f-b4aa-7dc3-aa1d-2ed781164ae0
```

Omit `userId` in AI requests → this patient is used automatically.

### 5. `POST /api/Ai` with curl (PowerShell)

Replace `DATE` with a weekday in range (UTC), e.g. tomorrow:  
`(Get-Date).ToUniversalTime().AddDays(1).ToString("yyyy-MM-dd")`

```powershell
$Base = "https://localhost:7141"
$DemoUser = "019e513f-b4aa-7dc3-aa1d-2ed781164ae0"

# Simple question
$body = '{"message":"What specializations do you have?","userId":"' + $DemoUser + '"}'
curl.exe -sk -X POST "$Base/api/Ai" -H "Content-Type: application/json" -d $body

# Pediatrics + slots (use real DATE)
$body = '{"message":"List Pediatrics doctors and available slots on DATE","userId":"' + $DemoUser + '"}'
curl.exe -sk -X POST "$Base/api/Ai" -H "Content-Type: application/json" -d $body
```

### 6. `POST /api/Ai` with curl (bash / Git Bash)

```bash
BASE="https://localhost:7141"
USER="019e513f-b4aa-7dc3-aa1d-2ed781164ae0"
DATE="2026-05-26"

curl -sk -X POST "$BASE/api/Ai" \
  -H "Content-Type: application/json" \
  -d "{\"message\":\"List Pediatrics doctors and slots on $DATE\",\"userId\":\"$USER\"}"
```

### 7. New chat session (clear in-memory history)

Use a **new** `userId` (any Guid) or restart the API. Same `userId` = same conversation.

### 8. Quick REST before AI

```http
GET /api/Specializations
GET /api/Patients/search?q=Demo
GET /api/Doctors/specialization/name/Pediatrics?date=DATE
GET /api/Schedules/available?doctorId={doctorId}&date=DATE
```

---

## AI test questions (copy into Scalar or curl)

Use these in `POST /api/Ai` → body field **`message`**.  
Pick dates within **3 days ago … 10 days ahead** (UTC). Prefer a **weekday**; Fri/Sat have fewer doctors on duty.

| # | Test question | What it should exercise |
|---|---------------|-------------------------|
| 1 | What specializations are available? | `get_all_specializations` |
| 2 | Search for Cardiology specialization | `search_specializations` |
| 3 | List all patients | `get_all_patients` |
| 4 | Find the demo patient named Demo | `search_patients` |
| 5 | List all active doctors | `get_all_doctors` |
| 6 | Search for doctors in Cardiology | `search_doctors` |
| 7 | Show me Pediatrics doctors and their available slots tomorrow | `get_doctors_by_specialization_name` (+ date) |
| 8 | Show available cardiology slots on DATE | `get_available_slots_for_specialization` |
| 9 | Show available slots for Dr. FIRSTNAME LASTNAME on DATE | `get_available_slots_by_doctor_name` |
| 10 | Who are the top 3 rated doctors in Cardiology? | `search_specializations` → `get_top_rated_doctors` |
| 11 | Book the 10:00 slot for me (after slots listed) | `book_appointment` |
| 12 | Show my appointments | `get_patient_appointments` |
| 13 | Cancel my latest appointment | `get_patient_appointments` → `cancel_appointment` |
| 14 | Complete my appointment so I can review it | `complete_appointment` |
| 15 | Add a 5 star review with comment "Great visit" | `add_review` |
| 16 | What is Dr. X average rating? | `get_doctor_average_rate` |
| 17 | List reviews for Dr. X | `get_doctor_reviews` |

### Sample JSON bodies (Scalar / Postman)

**Minimal (uses demo patient + new session if you omit userId):**

```json
{
  "message": "What specializations do you have?"
}
```

**With demo user (recommended for booking tests):**

```json
{
  "message": "List Pediatrics doctors and show available slots tomorrow",
  "userId": "019e513f-b4aa-7dc3-aa1d-2ed781164ae0"
}
```

**Full booking flow (ask in order, same `userId`):**

```json
{ "message": "Pediatrics doctors and free slots on 2026-05-26", "userId": "019e513f-b4aa-7dc3-aa1d-2ed781164ae0" }
```

```json
{ "message": "Book the first available morning slot for me", "userId": "019e513f-b4aa-7dc3-aa1d-2ed781164ae0" }
```

```json
{ "message": "Show my appointments", "userId": "019e513f-b4aa-7dc3-aa1d-2ed781164ae0" }
```

```json
{ "message": "Complete that appointment and leave a 5 star review saying Excellent care", "userId": "019e513f-b4aa-7dc3-aa1d-2ed781164ae0" }
```

### What to watch in the console

Yellow lines = token usage per AI request. Multiple tool calls = normal for slot/booking questions.

---

## How to test (checklist)

1. **DB:** start API → migrate + seed / `EnsureScheduleRangeAsync` / `EnsureDemoPatientAsync`.
2. **API key:** User Secrets `AgentOptions:ApiKey`.
3. **REST:** `powershell -File scripts/test-apis.ps1`
4. **AI:** questions above via Scalar or curl.

After `system-prompt.md` changes: **restart API**; use a **new `userId`** for a fresh chat session.

---

## Troubleshooting

| Problem | Likely cause | Fix |
|---------|--------------|-----|
| `ApiKey` null | Not in secrets/config | `AgentOptions:ApiKey` |
| `tool_use_failed` invalid `uuid` | Placeholder id from model | Use name-based tools; `MediFlowAiTools` returns error JSON on bad UUID |
| No slots “today” | Past times filtered; Fri/Sat reduced staff; 35% booked | Use `?date=` within −3..+10 range; try next weekday |
| No slots tomorrow (Fri) | Fri = 1 doctor per spec only | Try `get_available_slots_for_specialization` or another date |
| Booking fails | Demo patient missing | Restart API (`EnsureDemoPatientAsync`) or reseed; use `DemoPatient.IdString` |
| AI ignores new prompt | In-memory history | Restart API; new `userId` |
| Old doctor names | Seed skipped | Drop DB and reseed |

---

## Files checklist

| File | Responsibility |
|------|----------------|
| `appsettings.json` | `AgentOptions` |
| `system-prompt.md` | AI behavior + tool rules |
| `Demo/DemoPatient.cs` | Fixed AI demo patient id |
| `DatabaseMigrator.cs` | Dummy data + demo patient + schedule range |
| `AgentOptions.cs` | Config model |
| `SystemPromptResolver.cs` | Load prompt file |
| `DependencyInjection.cs` | Registers `MediFlowAiTools`, AI |
| `AgentService.cs` | LLM + history |
| `MediFlowAiTools.cs` | 25 tool methods, JSON + validation |
| `AiToolRegister.cs` | Exposes tools to the model |
| `AiController.cs` | `POST /api/Ai` |
| `DoctorDto.cs` | Includes `AvailableSlots` |
| `DoctorService.cs` | `AttachAvailableSlotsAsync` when `date` passed |
| `PatientDto.cs` / `PatientService.cs` / `PatientsController.cs` | Patients API + search |
| `SpecializationService.cs` | `SearchByNameAsync` |

---

## One-line summary

**User → `/api/Ai` → `AgentService` → Groq with 25 tools → `MediFlowAiTools` → your services → SQL Server → natural language reply.**

That is MediFlow AI integration in your project today.
