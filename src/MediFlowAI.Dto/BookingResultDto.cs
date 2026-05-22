namespace MediFlowAI.Dto;

public sealed record BookingResultDto(bool Success, Guid? AppointmentId, string? ErrorMessage);
