using System;
using System.Threading.Tasks;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SmartTaskManager.Api.Data;
using SmartTaskManager.Api.Middleware;
using SmartTaskManager.Application.Abstractions.Services;
using SmartTaskManager.Application.Filters;
using SmartTaskManager.Application.Services;
using SmartTaskManager.Domain.Entities;
using SmartTaskManager.Domain.Interfaces;
using SmartTaskManager.Infrastructure.Notifications;
using SmartTaskManager.Infrastructure.Persistence;
using SmartTaskManager.Infrastructure.Repositories;
using SmartTaskManager.Infrastructure.Time;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.AddRouting(options =>
{
    options.LowercaseUrls = true;
});

builder.Services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
builder.Services.AddSingleton<HighPriorityTaskFilter>();
builder.Services.AddSingleton<StatusTaskFilter>();
builder.Services.AddSingleton<OverdueTaskFilter>();
builder.Services.AddSingleton<INotificationService, SilentNotificationService>();
builder.Services.AddSingleton(CreateDbContextFactory);
builder.Services.AddSingleton<DatabaseInitializer>();
builder.Services.AddScoped<IRepository<User>, UserRepository>();
builder.Services.AddScoped<ITaskRepository, TaskRepository>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<TaskService>();
builder.Services.AddScoped<SampleDataSeeder>();

WebApplication app = builder.Build();

app.UseMiddleware<ApiExceptionMiddleware>();

await InitializeDatabaseAsync(app);

app.MapControllers();

await app.RunAsync();

static SmartTaskManagerDbContextFactory CreateDbContextFactory(IServiceProvider serviceProvider)
{
    IConfiguration configuration = serviceProvider.GetRequiredService<IConfiguration>();
    ILoggerFactory loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

    string connectionString = configuration.GetConnectionString("SmartTaskManager")
        ?? throw new InvalidOperationException("Connection string 'SmartTaskManager' is not configured.");

    DatabaseLoggingOptions loggingOptions = new()
    {
        EnableEfLogging = GetBoolean(configuration, "Database:EnableEfLogging"),
        EnableDetailedErrors = GetBoolean(configuration, "Database:EnableDetailedErrors"),
        EnableSensitiveDataLogging = GetBoolean(configuration, "Database:EnableSensitiveDataLogging")
    };

    ILogger logger = loggerFactory.CreateLogger("SmartTaskManager.Database");

    return new SmartTaskManagerDbContextFactory(
        connectionString,
        loggingOptions,
        message => logger.LogInformation("{EfMessage}", message));
}

static async Task InitializeDatabaseAsync(WebApplication app)
{
    using IServiceScope scope = app.Services.CreateScope();
    IServiceProvider serviceProvider = scope.ServiceProvider;
    ILogger logger = serviceProvider
        .GetRequiredService<ILoggerFactory>()
        .CreateLogger("SmartTaskManager.Startup");

    DatabaseInitializer databaseInitializer = serviceProvider.GetRequiredService<DatabaseInitializer>();
    await databaseInitializer.ApplyMigrationsAsync();

    if (GetBoolean(serviceProvider.GetRequiredService<IConfiguration>(), "Seeding:EnableSampleData"))
    {
        SampleDataSeeder sampleDataSeeder = serviceProvider.GetRequiredService<SampleDataSeeder>();
        await sampleDataSeeder.SeedAsync();
        logger.LogInformation("Sample data seeding completed.");
    }
}

static bool GetBoolean(IConfiguration configuration, string key)
{
    return bool.TryParse(configuration[key], out bool value) && value;
}
