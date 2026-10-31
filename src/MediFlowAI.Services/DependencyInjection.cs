using Microsoft.Extensions.AI;

namespace MediFlowAI.Services;

public static class DependencyInjection
{
    public static IServiceCollection AddServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ISpecializationService, SpecializationService>();
        services.AddScoped<IPatientService, PatientService>();
        services.AddScoped<IDoctorService, DoctorService>();
        services.AddScoped<IScheduleService, ScheduleService>();
        services.AddScoped<IAppointmentService, AppointmentService>();
        services.AddScoped<IReviewService, ReviewService>();

        services.Configure<AgentOptions>(configuration.GetSection(nameof(AgentOptions)));

        services.AddSingleton<IChatClientFactory, ChatClientFactory>();
        services.AddChatClient(serviceProvider =>
            serviceProvider.GetRequiredService<IChatClientFactory>().Create());

        services.AddScoped<AiToolRegister>();
        services.AddScoped<IAgentService, AgentService>();

        return services;
    }
}
