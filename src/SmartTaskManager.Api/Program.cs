using Microsoft.AspNetCore.Builder;
using SmartTaskManager.Api.Configuration;
using SmartTaskManager.Infrastructure.DependencyInjection;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddSmartTaskManagerApiPresentation()
    .AddSmartTaskManagerApiSecurity(builder.Configuration)
    .AddSmartTaskManagerUseCaseServices()
    .AddSmartTaskManagerApiRuntimeServices()
    .AddSmartTaskManagerInfrastructure();

WebApplication app = builder.Build();

app.ConfigureSmartTaskManagerPipeline();
await app.InitializeSmartTaskManagerAsync();
await app.RunAsync();
