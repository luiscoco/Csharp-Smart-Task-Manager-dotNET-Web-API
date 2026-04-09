using Microsoft.AspNetCore.Builder;
using SmartTaskManager.Api.Configuration;
using SmartTaskManager.Infrastructure.DependencyInjection;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddSmartTaskManagerApiPresentation()
    .AddSmartTaskManagerApiSecurity(builder.Configuration)
    .AddSmartTaskManagerApplicationServices()
    .AddSmartTaskManagerInfrastructure();

WebApplication app = builder.Build();

app.ConfigureSmartTaskManagerPipeline();
await app.InitializeSmartTaskManagerAsync();
await app.RunAsync();
