using System;

namespace SmartTaskManager.Api.Contracts.Responses;

public sealed record AccessTokenResponse(
    string AccessToken,
    string TokenType,
    DateTime ExpiresAtUtc,
    Guid UserId,
    string UserName);
