namespace MediFlowAI.Dto;

public sealed record ReviewDto(
    Guid Id,
    Guid DoctorId,
    Guid PatientId,
    string PatientName,
    Guid AppointmentId,
    int Rate,
    string? Comment,
    DateTime CreatedAt);
