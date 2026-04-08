namespace SmartTaskManager.Api.Contracts.Responses;

public sealed record ApiErrorResponse(
    int StatusCode,
    string Message);
