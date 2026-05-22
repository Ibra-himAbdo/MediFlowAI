namespace MediFlowAI.Dto;

public sealed record PatientDto(
    Guid Id,
    string FullName,
    string PhoneNumber,
    DateTime BirthDate);
