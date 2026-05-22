namespace MediFlowAI.Dto;

public sealed record AddReviewRequest(Guid AppointmentId, int Rate, string? Comment);
