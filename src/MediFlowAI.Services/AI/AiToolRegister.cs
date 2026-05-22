using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.AI;

namespace MediFlowAI.Services;

public class AiToolRegister
{
    private static readonly Type CancellationTokenType = typeof(CancellationToken);

    private readonly MediFlowAiTools _tools;

    public AiToolRegister(MediFlowAiTools tools)
    {
        _tools = tools;
    }

    public IReadOnlyList<AITool> GetAllTools() => GetAiTools().ToList();

    private IEnumerable<AITool> GetAiTools()
    {
        foreach (AITool tool in GetUtilityTools())
        {
            yield return tool;
        }

        foreach (AITool tool in GetSpecializationTools())
        {
            yield return tool;
        }

        foreach (AITool tool in GetPatientTools())
        {
            yield return tool;
        }

        foreach (AITool tool in GetDoctorTools())
        {
            yield return tool;
        }

        foreach (AITool tool in GetScheduleTools())
        {
            yield return tool;
        }

        foreach (AITool tool in GetAppointmentTools())
        {
            yield return tool;
        }

        foreach (AITool tool in GetReviewTools())
        {
            yield return tool;
        }
    }

    #region Utility

    private IEnumerable<AITool> GetUtilityTools()
    {
        yield return CreateTool(nameof(MediFlowAiTools.GetDateAsync), "get_date",
            "Current UTC date/time for the server. Call before interpreting today, tomorrow, or yyyy-MM-dd dates — do not assume 2024.");
    }

    #endregion

    #region Specializations

    private IEnumerable<AITool> GetSpecializationTools()
    {
        yield return CreateTool(nameof(MediFlowAiTools.GetAllSpecializationsAsync), "get_all_specializations",
            "List all specializations. Each item has id and name. Copy id exactly for Guid parameters.");

        yield return CreateTool(nameof(MediFlowAiTools.GetSpecializationByIdAsync), "get_specialization_by_id",
            "Get one specialization. id: exact UUID from get_all_specializations or search_specializations.",
            typeof(string));

        yield return CreateTool(nameof(MediFlowAiTools.SearchSpecializationsAsync), "search_specializations",
            "Find specializations by name. specializationName: plain text only (e.g. Cardiology), never a JSON object.",
            typeof(string));
    }

    #endregion

    #region Patients

    private IEnumerable<AITool> GetPatientTools()
    {
        yield return CreateTool(nameof(MediFlowAiTools.GetAllPatientsAsync), "get_all_patients",
            "List all patients. Demo AI user id is often used as patientId for booking.");

        yield return CreateTool(nameof(MediFlowAiTools.GetPatientByIdAsync), "get_patient_by_id",
            "Get one patient. patientId: exact UUID (e.g. chat userId for booking).",
            typeof(string));

        yield return CreateTool(nameof(MediFlowAiTools.SearchPatientsAsync), "search_patients",
            "Search patients by name or phone. query: text like Demo or 0100.",
            typeof(string));
    }

    #endregion

    #region Doctors

    private IEnumerable<AITool> GetDoctorTools()
    {
        yield return CreateTool(nameof(MediFlowAiTools.GetAllDoctorsAsync), "get_all_doctors",
            "List active doctors with availableSlots when date is set. date: yyyy-MM-dd or empty.",
            typeof(string));

        yield return CreateTool(nameof(MediFlowAiTools.GetDoctorByIdAsync), "get_doctor_by_id",
            "Get one doctor with availableSlots when date is set. doctorId: UUID. date: yyyy-MM-dd or empty.",
            typeof(string), typeof(string));

        yield return CreateTool(nameof(MediFlowAiTools.GetDoctorsBySpecializationNameAsync),
            "get_doctors_by_specialization_name",
            "Best for 'pediatrics doctors tomorrow'. specializationName: text. date: yyyy-MM-dd (defaults to UTC tomorrow if empty). Returns availableSlots per doctor.",
            typeof(string), typeof(string));

        yield return CreateTool(nameof(MediFlowAiTools.SearchDoctorsAsync), "search_doctors",
            "Search doctors with availableSlots when date is set. query: text. date: yyyy-MM-dd or empty.",
            typeof(string), typeof(string));

        yield return CreateTool(nameof(MediFlowAiTools.GetDoctorsBySpecializationAsync),
            "get_doctors_by_specialization",
            "specializationId: UUID. date: yyyy-MM-dd or empty. Returns availableSlots per doctor.",
            typeof(string), typeof(string));

        yield return CreateTool(nameof(MediFlowAiTools.GetTopRatedDoctorsAsync), "get_top_rated_doctors",
            "Top doctors with availableSlots when date is set. specializationId: UUID. take: number. date: yyyy-MM-dd or empty.",
            typeof(string), typeof(int), typeof(string));
    }

    #endregion

    #region Schedules

    private IEnumerable<AITool> GetScheduleTools()
    {
        yield return CreateTool(nameof(MediFlowAiTools.GetAvailableSlotsForSpecializationAsync),
            "get_available_slots_for_specialization",
            "Best for 'cardiology slots tomorrow'. specializationName: text. date: yyyy-MM-dd. Returns doctors and slots.",
            typeof(string), typeof(string));

        yield return CreateTool(nameof(MediFlowAiTools.GetAvailableSlotsByDoctorNameAsync),
            "get_available_slots_by_doctor_name",
            "Slots for a doctor by name. doctorName: text. date: yyyy-MM-dd.",
            typeof(string), typeof(string));

        yield return CreateTool(nameof(MediFlowAiTools.GetAvailableSlotsAsync), "get_available_slots",
            "Slots for one doctor. doctorId: exact UUID from doctor list. date: yyyy-MM-dd.",
            typeof(string), typeof(string));

        yield return CreateTool(nameof(MediFlowAiTools.GetDoctorScheduleAsync), "get_doctor_schedule",
            "All slots in range. doctorId: UUID. fromDate/toDate: yyyy-MM-dd.",
            typeof(string), typeof(string), typeof(string));

        yield return CreateTool(nameof(MediFlowAiTools.IsSlotAvailableAsync), "is_slot_available",
            "Check if slot is free. scheduleId: exact UUID from slot id in get_available_slots.",
            typeof(string));
    }

    #endregion

    #region Appointments

    private IEnumerable<AITool> GetAppointmentTools()
    {
        yield return CreateTool(nameof(MediFlowAiTools.BookAppointmentAsync), "book_appointment",
            "Book a slot. patientId: user UUID from chat. doctorScheduleId: exact slot UUID from get_available_slots.",
            typeof(string), typeof(string));

        yield return CreateTool(nameof(MediFlowAiTools.CancelAppointmentAsync), "cancel_appointment",
            "Cancel booking. appointmentId: exact UUID.",
            typeof(string));

        yield return CreateTool(nameof(MediFlowAiTools.CompleteAppointmentAsync), "complete_appointment",
            "Mark completed before review. appointmentId: exact UUID.",
            typeof(string));

        yield return CreateTool(nameof(MediFlowAiTools.GetAppointmentByIdAsync), "get_appointment_by_id",
            "Get appointment. appointmentId: exact UUID.",
            typeof(string));

        yield return CreateTool(nameof(MediFlowAiTools.GetPatientAppointmentsAsync), "get_patient_appointments",
            "Appointments for patient. patientId: exact UUID.",
            typeof(string));

        yield return CreateTool(nameof(MediFlowAiTools.GetDoctorAppointmentsAsync), "get_doctor_appointments",
            "Appointments for doctor. doctorId: exact UUID.",
            typeof(string));
    }

    #endregion

    #region Reviews

    private IEnumerable<AITool> GetReviewTools()
    {
        yield return CreateTool(nameof(MediFlowAiTools.AddReviewAsync), "add_review",
            "Review after complete_appointment. appointmentId: UUID. rate: 1-5. comment: optional.",
            typeof(string), typeof(int), typeof(string));

        yield return CreateTool(nameof(MediFlowAiTools.GetDoctorReviewsAsync), "get_doctor_reviews",
            "List reviews. doctorId: exact UUID.",
            typeof(string));

        yield return CreateTool(nameof(MediFlowAiTools.GetDoctorAverageRateAsync), "get_doctor_average_rate",
            "Average rating. doctorId: exact UUID.",
            typeof(string));
    }

    #endregion

    #region Helpers

    private AITool CreateTool(string methodName, string toolName, string description, params Type[] extraParameterTypes)
    {
        Type[] parameterTypes = [.. extraParameterTypes, CancellationTokenType];

        MethodInfo method = typeof(MediFlowAiTools).GetMethod(methodName, parameterTypes)
            ?? throw new InvalidOperationException(
                                $"Method '{methodName}' not found on {nameof(MediFlowAiTools)}.");

        return AIFunctionFactory.Create(
            method,
            _tools,
            new AIFunctionFactoryOptions
            {
                Name = toolName,
                Description = description,
                ConfigureParameterBinding = parameter =>
                {
                    if (parameter.ParameterType != typeof(string))
                    {
                        return default;
                    }

                    string parameterName = parameter.Name!;

                    return new AIFunctionFactoryOptions.ParameterBindingOptions
                    {
                        BindParameter = (_, arguments) =>
                        {
                            if (!arguments.TryGetValue(parameterName, out object? raw) || raw is null)
                            {
                                return null;
                            }

                            return CoerceToPlainString(raw);
                        }
                    };
                }
            });
    }

    private static string? CoerceToPlainString(object raw)
    {
        switch (raw)
        {
            case string text:
                return text;
            case JsonElement element:
                return CoerceJsonElementToString(element);
            case JsonNode node:
                return CoerceJsonElementToString(node.Deserialize<JsonElement>());
            case JsonDocument document:
                return CoerceJsonElementToString(document.RootElement.Clone());
            default:
                return raw.ToString();
        }
    }

    private static string? CoerceJsonElementToString(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Object when element.TryGetProperty("name", out JsonElement nameProp) =>
                nameProp.GetString(),
            JsonValueKind.Object when element.TryGetProperty("specializationName", out JsonElement specProp) =>
                specProp.GetString(),
            JsonValueKind.Object when element.TryGetProperty("query", out JsonElement queryProp) =>
                queryProp.GetString(),
            _ => element.ToString()
        };
    }

    #endregion
}
