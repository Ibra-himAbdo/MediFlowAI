namespace MediFlowAI.Services;

public static class DependencyInjection
{
    public static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.AddScoped<ISpecializationService, SpecializationService>();
        services.AddScoped<IDoctorService, DoctorService>();
        services.AddScoped<IScheduleService, ScheduleService>();
        services.AddScoped<IAppointmentService, AppointmentService>();
        services.AddScoped<IReviewService, ReviewService>();

        return services;
    }
}