namespace MediFlowAI.Services;

public static class DependencyInjection
{
    public static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.AddScoped<ISpecializationService, SpecializationService>();

        return services;
    }
}