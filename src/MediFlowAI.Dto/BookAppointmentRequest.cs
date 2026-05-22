namespace MediFlowAI.Dto;

public sealed record BookAppointmentRequest(Guid PatientId, Guid DoctorScheduleId);
