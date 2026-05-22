namespace MediFlowAI.Api;

/// <summary>
/// Fixed demo patient used as the default AI chat user (booking, reviews, appointments).
/// Seeded in <see cref="DatabaseMigrator"/> and ensured on every API startup.
/// </summary>
public static class DemoPatient
{
    public const string IdString = "019e513f-b4aa-7dc3-aa1d-2ed781164ae0";

    public static readonly Guid Id = Guid.Parse(IdString);

    public const string FullName = "Demo Patient (AI User)";

    public const string PhoneNumber = "01000000001";
}
