using System.Text.Json;

namespace MediFlowAI.Services;

/// <summary>
/// LLM-facing tools with string IDs/dates so the provider does not reject placeholder Guids
/// before our code runs. Invalid values return JSON errors for the model to retry.
/// </summary>
public sealed class MediFlowAiTools(
    ISpecializationService specializations,
    IPatientService patients,
    IDoctorService doctors,
    IScheduleService schedules,
    IAppointmentService appointments,
    IReviewService reviews)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    #region Specializations

    public Task<string> GetAllSpecializationsAsync(CancellationToken cancellationToken = default) =>
        Wrap(() => specializations.GetAllAsync(cancellationToken));

    public async Task<string> GetSpecializationByIdAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        if (!TryParseGuid(id, out Guid guid, out string? error))
        {
            return error;
        }

        SpecializationDto? specialization =
            await specializations.GetByIdAsync(guid, cancellationToken);

        return Serialize(
            specialization ?? (object)new { message = "Specialization not found." });
    }

    public Task<string> SearchSpecializationsAsync(
        string name,
        CancellationToken cancellationToken = default) =>
        Wrap(() => specializations.SearchByNameAsync(name, cancellationToken));

    #endregion

    #region Patients

    public Task<string> GetAllPatientsAsync(CancellationToken cancellationToken = default) =>
        Wrap(() => patients.GetAllAsync(cancellationToken));

    public async Task<string> GetPatientByIdAsync(
        string patientId,
        CancellationToken cancellationToken = default)
    {
        if (!TryParseGuid(patientId, out Guid guid, out string? error))
        {
            return error;
        }

        PatientDto? patient = await patients.GetByIdAsync(guid, cancellationToken);

        return Serialize(patient ?? (object)new { message = "Patient not found." });
    }

    public Task<string> SearchPatientsAsync(
        string query,
        CancellationToken cancellationToken = default) =>
        Wrap(() => patients.SearchAsync(query, cancellationToken));

    #endregion

    #region Doctors

    public async Task<string> GetAllDoctorsAsync(
        string date,
        CancellationToken cancellationToken = default)
    {
        if (!TryResolveSlotsDate(date, out DateOnly? slotsDate, out string? dateError))
        {
            return dateError;
        }

        return await Wrap(() => doctors.GetAllAsync(slotsDate, cancellationToken));
    }

    public async Task<string> GetDoctorByIdAsync(
        string doctorId,
        string date,
        CancellationToken cancellationToken = default)
    {
        if (!TryParseGuid(doctorId, out Guid guid, out string? error))
        {
            return error;
        }

        if (!TryResolveSlotsDate(date, out DateOnly? slotsDate, out string? dateError))
        {
            return dateError;
        }

        DoctorDto? doctor = await doctors.GetByIdAsync(guid, slotsDate, cancellationToken);

        return Serialize(doctor ?? (object)new { message = "Doctor not found." });
    }

    public async Task<string> GetDoctorsBySpecializationNameAsync(
        string specializationName,
        string date,
        CancellationToken cancellationToken = default)
    {
        if (!TryResolveSlotsDate(date, out DateOnly slotsDate, out string? dateError, defaultToTomorrow: true))
        {
            return dateError;
        }

        IReadOnlyList<DoctorDto> doctorList =
            await doctors.GetBySpecializationNameAsync(specializationName, slotsDate, cancellationToken);

        return SerializeWithDateContext(slotsDate, new { doctors = doctorList });
    }

    public async Task<string> SearchDoctorsAsync(
        string query,
        string date,
        CancellationToken cancellationToken = default)
    {
        if (!TryResolveSlotsDate(date, out DateOnly? slotsDate, out string? dateError))
        {
            return dateError;
        }

        return await Wrap(() => doctors.SearchDoctorsAsync(query, slotsDate, cancellationToken));
    }

    public async Task<string> GetDoctorsBySpecializationAsync(
        string specializationId,
        string date,
        CancellationToken cancellationToken = default)
    {
        if (!TryParseGuid(specializationId, out Guid guid, out string? error))
        {
            return error;
        }

        if (!TryResolveSlotsDate(date, out DateOnly? slotsDate, out string? dateError))
        {
            return dateError;
        }

        return await Wrap(() => doctors.GetBySpecializationAsync(guid, slotsDate, cancellationToken));
    }

    public async Task<string> GetTopRatedDoctorsAsync(
        string specializationId,
        int take,
        string date,
        CancellationToken cancellationToken = default)
    {
        if (!TryParseGuid(specializationId, out Guid guid, out string? error))
        {
            return error;
        }

        if (!TryResolveSlotsDate(date, out DateOnly? slotsDate, out string? dateError))
        {
            return dateError;
        }

        return await Wrap(() => doctors.GetTopRatedDoctorsAsync(guid, take, slotsDate, cancellationToken));
    }

    #endregion

    #region Schedules

    public async Task<string> GetAvailableSlotsAsync(
        string doctorId,
        string date,
        CancellationToken cancellationToken = default)
    {
        if (!TryParseGuid(doctorId, out Guid docId, out string? idError))
        {
            return idError;
        }

        if (!TryParseDate(date, out DateOnly day, out string? dateError))
        {
            return dateError;
        }

        IReadOnlyList<DoctorSlotDto> slots =
            await schedules.GetAvailableSlotsAsync(docId, day, cancellationToken);

        return SerializeWithDateContext(day, new { doctorId = docId, slots });
    }

    public async Task<string> GetAvailableSlotsByDoctorNameAsync(
        string doctorName,
        string date,
        CancellationToken cancellationToken = default)
    {
        if (!TryParseDate(date, out DateOnly day, out string? dateError))
        {
            return dateError;
        }

        IReadOnlyList<DoctorDto> matches = await doctors.SearchDoctorsAsync(doctorName, day, cancellationToken);
        if (matches.Count == 0)
        {
            return Error("doctorName", doctorName,
                "No doctor found. Try search_doctors or get_doctors_by_specialization_name.");
        }

        DoctorDto doctor = matches[0];
        IReadOnlyList<DoctorSlotDto> slots =
            await schedules.GetAvailableSlotsAsync(doctor.Id, day, cancellationToken);

        return SerializeWithDateContext(day, new { doctor, slots });
    }

    public async Task<string> GetAvailableSlotsForSpecializationAsync(
        string specializationName,
        string date,
        CancellationToken cancellationToken = default)
    {
        if (!TryParseDate(date, out DateOnly day, out string? dateError))
        {
            return dateError;
        }

        IReadOnlyList<DoctorDto> doctorList =
            await doctors.GetBySpecializationNameAsync(specializationName, day, cancellationToken);

        if (doctorList.Count == 0)
        {
            return Error("specializationName", specializationName,
                "No doctors found for this specialization. Try search_specializations first.");
        }

        return SerializeWithDateContext(day, new { specializationName, doctors = doctorList });
    }

    public async Task<string> GetDoctorScheduleAsync(
        string doctorId,
        string fromDate,
        string toDate,
        CancellationToken cancellationToken = default)
    {
        if (!TryParseGuid(doctorId, out Guid docId, out string? idError))
        {
            return idError;
        }

        if (!TryParseDate(fromDate, out DateOnly from, out string? fromError))
        {
            return fromError;
        }

        if (!TryParseDate(toDate, out DateOnly to, out string? toError))
        {
            return toError;
        }

        return await Wrap(() => schedules.GetDoctorScheduleAsync(docId, from, to, cancellationToken));
    }

    public async Task<string> IsSlotAvailableAsync(
        string scheduleId,
        CancellationToken cancellationToken = default)
    {
        if (!TryParseGuid(scheduleId, out Guid id, out string? error))
        {
            return error;
        }

        bool available = await schedules.IsSlotAvailableAsync(id, cancellationToken);
        return Serialize(new { scheduleId = id, isAvailable = available });
    }

    #endregion

    #region Appointments

    public async Task<string> BookAppointmentAsync(
        string patientId,
        string doctorScheduleId,
        CancellationToken cancellationToken = default)
    {
        if (!TryParseGuid(patientId, out Guid patient, out string? patientError))
        {
            return patientError;
        }

        if (!TryParseGuid(doctorScheduleId, out Guid schedule, out string? scheduleError))
        {
            return scheduleError;
        }

        return await Wrap(() => appointments.BookAppointmentAsync(patient, schedule, cancellationToken));
    }

    public async Task<string> CancelAppointmentAsync(
        string appointmentId,
        CancellationToken cancellationToken = default)
    {
        if (!TryParseGuid(appointmentId, out Guid id, out string? error))
        {
            return error;
        }

        await appointments.CancelAppointmentAsync(id, cancellationToken);
        return Serialize(new { message = "Appointment cancelled.", appointmentId = id });
    }

    public async Task<string> CompleteAppointmentAsync(
        string appointmentId,
        CancellationToken cancellationToken = default)
    {
        if (!TryParseGuid(appointmentId, out Guid id, out string? error))
        {
            return error;
        }

        await appointments.CompleteAppointmentAsync(id, cancellationToken);
        return Serialize(new { message = "Appointment completed.", appointmentId = id });
    }

    public async Task<string> GetAppointmentByIdAsync(
        string appointmentId,
        CancellationToken cancellationToken = default)
    {
        if (!TryParseGuid(appointmentId, out Guid id, out string? error))
        {
            return error;
        }

        AppointmentDto? appointment = await appointments.GetByIdAsync(id, cancellationToken);

        return Serialize(appointment ?? (object)new { message = "Appointment not found." });
    }

    public async Task<string> GetPatientAppointmentsAsync(
        string patientId,
        CancellationToken cancellationToken = default)
    {
        if (!TryParseGuid(patientId, out Guid id, out string? error))
        {
            return error;
        }

        return await Wrap(() => appointments.GetPatientAppointmentsAsync(id, cancellationToken));
    }

    public async Task<string> GetDoctorAppointmentsAsync(
        string doctorId,
        CancellationToken cancellationToken = default)
    {
        if (!TryParseGuid(doctorId, out Guid id, out string? error))
        {
            return error;
        }

        return await Wrap(() => appointments.GetDoctorAppointmentsAsync(id, cancellationToken));
    }

    #endregion

    #region Reviews

    public async Task<string> AddReviewAsync(
        string appointmentId,
        int rate,
        string? comment,
        CancellationToken cancellationToken = default)
    {
        if (!TryParseGuid(appointmentId, out Guid id, out string? error))
        {
            return error;
        }

        await reviews.AddReviewAsync(id, rate, comment, cancellationToken);
        return Serialize(new { message = "Review added.", appointmentId = id, rate });
    }

    public async Task<string> GetDoctorReviewsAsync(
        string doctorId,
        CancellationToken cancellationToken = default)
    {
        if (!TryParseGuid(doctorId, out Guid id, out string? error))
        {
            return error;
        }

        return await Wrap(() => reviews.GetDoctorReviewsAsync(id, cancellationToken));
    }

    public async Task<string> GetDoctorAverageRateAsync(
        string doctorId,
        CancellationToken cancellationToken = default)
    {
        if (!TryParseGuid(doctorId, out Guid id, out string? error))
        {
            return error;
        }

        double average = await reviews.GetDoctorAverageRateAsync(id, cancellationToken);
        return Serialize(new { doctorId = id, averageRate = average });
    }

    #endregion

    #region Helpers

    private static async Task<string> Wrap<T>(Func<Task<T>> action)
    {
        T result = await action();
        return Serialize(result);
    }

    private static string Serialize<T>(T value) =>
        JsonSerializer.Serialize(value, JsonOptions);

    private static string SerializeWithDateContext<T>(DateOnly requestedDate, T payload)
    {
        DateOnly utcToday = DateOnly.FromDateTime(DateTime.UtcNow);
        return Serialize(new
        {
            requestedDate = requestedDate.ToString("yyyy-MM-dd"),
            utcToday = utcToday.ToString("yyyy-MM-dd"),
            utcTomorrow = utcToday.AddDays(1).ToString("yyyy-MM-dd"),
            data = payload
        });
    }

    private static string Error(string parameter, string? received, string message) =>
        Serialize(new
        {
            error = true,
            parameter,
            received,
            message,
            hint = "Copy the exact id from the previous tool JSON (36-character UUID). Do not use placeholders."
        });

    private static bool TryParseGuid(string? input, out Guid guid, out string errorJson)
    {
        if (!string.IsNullOrWhiteSpace(input) && Guid.TryParse(input.Trim(), out guid))
        {
            errorJson = string.Empty;
            return true;
        }

        guid = default;
        errorJson = Error("id", input,
            "Invalid UUID. Call a list/search tool first, then copy the exact id field from the JSON response.");
        return false;
    }

    private static bool TryResolveSlotsDate(
        string? date,
        out DateOnly? slotsDate,
        out string errorJson,
        bool defaultToTomorrow = false)
    {
        if (string.IsNullOrWhiteSpace(date))
        {
            if (defaultToTomorrow)
            {
                slotsDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1);
                errorJson = string.Empty;
                return true;
            }

            slotsDate = null;
            errorJson = string.Empty;
            return true;
        }

        if (DateOnly.TryParse(date.Trim(), out DateOnly day))
        {
            slotsDate = day;
            errorJson = string.Empty;
            return true;
        }

        slotsDate = null;
        errorJson = Error("date", date, "Use date format yyyy-MM-dd (example: 2026-05-23).");
        return false;
    }

    private static bool TryResolveSlotsDate(
        string? date,
        out DateOnly slotsDate,
        out string errorJson,
        bool defaultToTomorrow = false)
    {
        if (TryResolveSlotsDate(date, out DateOnly? optionalDate, out errorJson, defaultToTomorrow)
            && optionalDate is not null)
        {
            slotsDate = optionalDate.Value;
            return true;
        }

        slotsDate = default;
        return false;
    }

    private static bool TryParseDate(string? input, out DateOnly date, out string errorJson)
    {
        if (!string.IsNullOrWhiteSpace(input) && DateOnly.TryParse(input.Trim(), out date))
        {
            errorJson = string.Empty;
            return true;
        }

        date = default;
        errorJson = Error("date", input, "Use date format yyyy-MM-dd (example: 2026-05-23).");
        return false;
    }

    #endregion
}
