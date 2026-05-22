namespace MediFlowAI.Dto;

public sealed record DoctorSlotDto(
    Guid Id,
    Guid DoctorId,
    DateTime StartTime,
    DateTime EndTime,
    bool IsAvailable);
