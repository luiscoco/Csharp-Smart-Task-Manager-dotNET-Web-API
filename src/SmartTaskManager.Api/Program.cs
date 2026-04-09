using Microsoft.AspNetCore.Builder;
using SmartTaskManager.Api.Configuration;
using SmartTaskManager.Api.Middleware;
using SmartTaskManager.Infrastructure.DependencyInjection;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddSmartTaskManagerApiPresentation()
    .AddSmartTaskManagerApiSecurity(builder.Configuration)
    .AddSmartTaskManagerApplicationServices()
    .AddSmartTaskManagerInfrastructure();

WebApplication app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "SmartTaskManager API v1");
    options.RoutePrefix = "swagger";
    options.DocumentTitle = "SmartTaskManager API";
});

app.UseMiddleware<ApiExceptionMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

await app.InitializeSmartTaskManagerAsync();
await app.RunAsync();
