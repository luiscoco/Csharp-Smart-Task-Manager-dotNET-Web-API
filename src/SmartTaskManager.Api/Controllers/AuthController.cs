using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SmartTaskManager.Api.Contracts.Requests;
using SmartTaskManager.Api.Contracts.Responses;
using SmartTaskManager.Api.Security;
using SmartTaskManager.Application.Services;
using SmartTaskManager.Domain.Entities;

namespace SmartTaskManager.Api.Controllers;

/// <summary>
/// Issues JWT tokens for testing protected endpoints in Swagger.
/// </summary>
[ApiController]
[Authorize(Policy = AuthorizationPolicies.DevelopmentOnly)]
[Tags("Authentication")]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly UserService _userService;
    private readonly JwtTokenService _jwtTokenService;

    public AuthController(UserService userService, JwtTokenService jwtTokenService)
    {
        _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        _jwtTokenService = jwtTokenService ?? throw new ArgumentNullException(nameof(jwtTokenService));
    }

    /// <summary>
    /// Creates a JWT bearer token for an existing user.
    /// </summary>
    /// <param name="request">The request containing the existing user name.</param>
    /// <param name="cancellationToken">The cancellation token for the request.</param>
    /// <response code="200">The token was created successfully.</response>
    /// <response code="400">The request payload is invalid.</response>
    /// <response code="403">The token endpoint is available only in Development.</response>
    /// <response code="404">The user was not found.</response>
    [HttpPost("token")]
    [ProducesResponseType(typeof(AccessTokenResponse), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 400)]
    [ProducesResponseType(typeof(ApiErrorResponse), 403)]
    [ProducesResponseType(typeof(ApiErrorResponse), 404)]
    public async Task<ActionResult<AccessTokenResponse>> CreateToken(
        [FromBody] CreateAccessTokenRequest request,
        CancellationToken cancellationToken)
    {
        User user = await _userService.GetUserByNameAsync(request.UserName, cancellationToken);
        AccessTokenResponse response = _jwtTokenService.CreateToken(user);

        return Ok(response);
    }
}
