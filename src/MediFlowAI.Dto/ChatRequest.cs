namespace MediFlowAI.Dto;

// **userId** just a placeholder for future use,
// in case we want to implement user-specific features in the future,
// such as personalized responses or user history tracking.
// For now, it can be null or ignored by the AI processing logic.
public record ChatRequest(string Message, string? UserId);