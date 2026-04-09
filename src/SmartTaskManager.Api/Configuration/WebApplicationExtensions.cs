using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SmartTaskManager.Api.Data;
using SmartTaskManager.Infrastructure.Persistence;

namespace SmartTaskManager.Api.Configuration;

public static class WebApplicationExtensions
{
    public static async Task InitializeSmartTaskManagerAsync(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        using IServiceScope scope = app.Services.CreateScope();
        IServiceProvider serviceProvider = scope.ServiceProvider;
        IConfiguration configuration = serviceProvider.GetRequiredService<IConfiguration>();
        ILogger logger = serviceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("SmartTaskManager.Startup");

        DatabaseInitializer databaseInitializer = serviceProvider.GetRequiredService<DatabaseInitializer>();
        await databaseInitializer.ApplyMigrationsAsync();

        if (!ShouldSeedSampleData(configuration))
        {
            return;
        }

        SampleDataSeeder sampleDataSeeder = serviceProvider.GetRequiredService<SampleDataSeeder>();
        await sampleDataSeeder.SeedAsync();
        logger.LogInformation("Sample data seeding completed.");
    }

    private static bool ShouldSeedSampleData(IConfiguration configuration)
    {
        return bool.TryParse(configuration["Seeding:EnableSampleData"], out bool value) && value;
    }
}
