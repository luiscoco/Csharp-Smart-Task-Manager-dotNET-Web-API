using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using SmartTaskManager.Api.Contracts.Responses;
using SmartTaskManager.Api.Data;
using SmartTaskManager.Api.Security;
using SmartTaskManager.Application.Filters;
using SmartTaskManager.Application.Services;

namespace SmartTaskManager.Api.Configuration;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSmartTaskManagerApiPresentation(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services
            .AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                options.JsonSerializerOptions.UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow;
            });

        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                ApiErrorResponse response = CreateValidationErrorResponse(context);
                return new BadRequestObjectResult(response);
            };
        });

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "SmartTaskManager API",
                Version = "v1",
                Summary = "Task management API built with ASP.NET Core and Clean Architecture.",
                Description =
                    "Manage users, create and update tasks, review task history, apply filters, and inspect dashboard summaries."
            });

            string xmlFileName = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            string xmlFilePath = Path.Combine(AppContext.BaseDirectory, xmlFileName);

            if (File.Exists(xmlFilePath))
            {
                options.IncludeXmlComments(xmlFilePath);
            }

            OpenApiSecurityScheme bearerScheme = new()
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Enter a valid JWT bearer token. Example: Bearer {your token}"
            };

            options.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, bearerScheme);
            options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference(
                    JwtBearerDefaults.AuthenticationScheme,
                    document,
                    externalResource: null)] = new List<string>()
            });
        });

        services.AddRouting(options =>
        {
            options.LowercaseUrls = true;
        });

        return services;
    }

    public static IServiceCollection AddSmartTaskManagerApiSecurity(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        JwtOptions jwtOptions = CreateJwtOptions(configuration);
        byte[] signingKeyBytes = Encoding.UTF8.GetBytes(jwtOptions.SigningKey);
        SymmetricSecurityKey signingKey = new(signingKeyBytes);

        services.AddSingleton(jwtOptions);
        services.AddSingleton<JwtTokenService>();

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtOptions.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = signingKey,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };

                options.Events = new JwtBearerEvents
                {
                    OnChallenge = context =>
                    {
                        context.HandleResponse();

                        ApiErrorResponse response = ApiErrorResponse.Create(
                            StatusCodes.Status401Unauthorized,
                            "Authentication required.",
                            "A valid bearer token is required to access this resource.",
                            context.HttpContext.TraceIdentifier,
                            context.HttpContext.Request.Path.Value ?? "/");

                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        return context.Response.WriteAsJsonAsync(response);
                    },
                    OnForbidden = context =>
                    {
                        ApiErrorResponse response = ApiErrorResponse.Create(
                            StatusCodes.Status403Forbidden,
                            "Access denied.",
                            "You do not have permission to perform this action.",
                            context.HttpContext.TraceIdentifier,
                            context.HttpContext.Request.Path.Value ?? "/");

                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        return context.Response.WriteAsJsonAsync(response);
                    }
                };
            });

        services.AddAuthorization();

        return services;
    }

    public static IServiceCollection AddSmartTaskManagerApplicationServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<HighPriorityTaskFilter>();
        services.AddSingleton<StatusTaskFilter>();
        services.AddSingleton<OverdueTaskFilter>();

        services.AddScoped<UserService>();
        services.AddScoped<TaskService>();
        services.AddScoped<SampleDataSeeder>();

        return services;
    }

    private static ApiErrorResponse CreateValidationErrorResponse(ActionContext context)
    {
        Dictionary<string, string[]> errors = context.ModelState
            .Where(entry => entry.Value is not null && entry.Value.Errors.Count > 0)
            .ToDictionary(
                entry => NormalizeModelStateKey(entry.Key),
                entry => entry.Value!.Errors
                    .Select(error => string.IsNullOrWhiteSpace(error.ErrorMessage)
                        ? "The input value is invalid."
                        : error.ErrorMessage)
                    .Distinct(StringComparer.Ordinal)
                    .ToArray(),
                StringComparer.OrdinalIgnoreCase);

        string path = context.HttpContext.Request.Path.HasValue
            ? context.HttpContext.Request.Path.Value!
            : "/";

        return ApiErrorResponse.Validation(
            context.HttpContext.TraceIdentifier,
            path,
            errors);
    }

    private static string NormalizeModelStateKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return "request";
        }

        return key;
    }

    private static JwtOptions CreateJwtOptions(IConfiguration configuration)
    {
        JwtOptions jwtOptions = configuration
            .GetSection(JwtOptions.SectionName)
            .Get<JwtOptions>() ?? new JwtOptions();

        if (string.IsNullOrWhiteSpace(jwtOptions.Issuer))
        {
            throw new InvalidOperationException("JWT issuer is not configured.");
        }

        if (string.IsNullOrWhiteSpace(jwtOptions.Audience))
        {
            throw new InvalidOperationException("JWT audience is not configured.");
        }

        if (string.IsNullOrWhiteSpace(jwtOptions.SigningKey) || jwtOptions.SigningKey.Length < 32)
        {
            throw new InvalidOperationException("JWT signing key must be configured and contain at least 32 characters.");
        }

        if (jwtOptions.TokenLifetimeMinutes <= 0)
        {
            throw new InvalidOperationException("JWT token lifetime must be greater than zero.");
        }

        return jwtOptions;
    }
}
