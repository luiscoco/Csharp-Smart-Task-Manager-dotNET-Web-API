using System;
using System.Net.Mime;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SmartTaskManager.Api.Contracts.Responses;
using SmartTaskManager.Domain.Common;

namespace SmartTaskManager.Api.Middleware;

public sealed class ApiExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiExceptionMiddleware> _logger;

    public ApiExceptionMiddleware(RequestDelegate next, ILogger<ApiExceptionMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvokeAsync(HttpContext httpContext)
    {
        try
        {
            await _next(httpContext);
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(httpContext, exception);
        }
    }

    private async Task HandleExceptionAsync(HttpContext httpContext, Exception exception)
    {
        int statusCode = ResolveStatusCode(exception);

        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Unhandled API exception.");
        }
        else
        {
            _logger.LogWarning(exception, "API request failed with status code {StatusCode}.", statusCode);
        }

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = MediaTypeNames.Application.Json;

        ApiErrorResponse response = new(
            statusCode,
            exception.Message);

        await httpContext.Response.WriteAsJsonAsync(response);
    }

    private static int ResolveStatusCode(Exception exception)
    {
        return exception switch
        {
            DomainException domainException => ResolveDomainStatusCode(domainException.Message),
            ArgumentException => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status500InternalServerError
        };
    }

    private static int ResolveDomainStatusCode(string message)
    {
        string normalizedMessage = message.ToLowerInvariant();

        if (normalizedMessage.Contains("not found"))
        {
            return StatusCodes.Status404NotFound;
        }

        if (normalizedMessage.Contains("already exists"))
        {
            return StatusCodes.Status409Conflict;
        }

        return StatusCodes.Status400BadRequest;
    }
}
