namespace MediFlowAI.Api;

public static class DatabaseMigrator
{
    public static async Task MigrateDatabaseAsync(this WebApplication app)
    {
        using IServiceScope scope = app.Services.CreateScope();
        IServiceProvider serviceProvider = scope.ServiceProvider;
        AppDbContext dbContext = serviceProvider.GetRequiredService<AppDbContext>();
        ILogger<Program> logger = app.Services.GetRequiredService<ILogger<Program>>();

        logger.LogInformation("Starting database migration...");

        Stopwatch stopwatch = Stopwatch.StartNew();

        try
        {
            await dbContext.Database.MigrateAsync();

            stopwatch.Stop();
            logger.LogInformation(
                "Database migration completed successfully in {ElapsedMilliseconds} ms",
                stopwatch.ElapsedMilliseconds
            );
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger.LogError(
                ex,
                "Database migration failed after {ElapsedMilliseconds} ms",
                stopwatch.ElapsedMilliseconds
            );

            throw;
        }
    }
}