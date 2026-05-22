namespace MediFlowAI.Dto;

public sealed record DoctorDto(
    Guid Id,
    string Name,
    Guid SpecializationId,
    string SpecializationName,
    decimal ConsultationFee,
    double AverageRate,
    int TotalReviews,
    bool IsActive,
    IReadOnlyList<DoctorSlotDto> AvailableSlots);
