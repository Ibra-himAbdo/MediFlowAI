namespace MediFlowAI.Dto;

public sealed record AppointmentDto(
    Guid Id,
    Guid DoctorId,
    string DoctorName,
    Guid PatientId,
    string PatientName,
    Guid DoctorScheduleId,
    DateTime StartTime,
    DateTime EndTime,
    string Status,
    DateTime CreatedAt);
