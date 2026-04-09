using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SmartTaskManager.Api.Contracts.Requests;
using SmartTaskManager.Api.Contracts.Responses;
using SmartTaskManager.Application.Services;
using SmartTaskManager.Domain.Entities;

namespace SmartTaskManager.Api.Controllers;

/// <summary>
/// Manages user creation and user lookups.
/// </summary>
[ApiController]
[AllowAnonymous]
[Tags("Users")]
[Route("api/users")]
public sealed class UsersController : ControllerBase
{
    private readonly UserService _userService;

    public UsersController(UserService userService)
    {
        _userService = userService ?? throw new ArgumentNullException(nameof(userService));
    }

    /// <summary>
    /// Creates a new user account.
    /// </summary>
    /// <param name="request">The user details required to create a new account.</param>
    /// <param name="cancellationToken">The cancellation token for the request.</param>
    /// <response code="201">The user was created successfully.</response>
    /// <response code="400">The request payload is invalid.</response>
    /// <response code="409">A user with the same name already exists.</response>
    [HttpPost]
    [ProducesResponseType(typeof(UserResponse), 201)]
    [ProducesResponseType(typeof(ApiErrorResponse), 400)]
    [ProducesResponseType(typeof(ApiErrorResponse), 409)]
    public async Task<ActionResult<UserResponse>> CreateUser(
        [FromBody] CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        User user = await _userService.CreateUserAsync(request.UserName, cancellationToken);
        UserResponse response = UserResponse.FromDomain(user);

        return CreatedAtAction(
            nameof(GetUser),
            new { userId = response.Id },
            response);
    }

    /// <summary>
    /// Returns all users ordered by name.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token for the request.</param>
    /// <response code="200">The users were returned successfully.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<UserResponse>), 200)]
    public async Task<ActionResult<IReadOnlyCollection<UserResponse>>> ListUsers(CancellationToken cancellationToken)
    {
        IReadOnlyCollection<User> users = await _userService.ListUsersAsync(cancellationToken);
        return Ok(UserResponse.FromDomain(users));
    }

    /// <summary>
    /// Returns a single user by identifier.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="cancellationToken">The cancellation token for the request.</param>
    /// <response code="200">The user was found and returned.</response>
    /// <response code="404">The user was not found.</response>
    [HttpGet("{userId:guid}")]
    [ProducesResponseType(typeof(UserResponse), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 404)]
    public async Task<ActionResult<UserResponse>> GetUser(
        [FromRoute] Guid userId,
        CancellationToken cancellationToken)
    {
        User user = await _userService.GetUserAsync(userId, cancellationToken);
        return Ok(UserResponse.FromDomain(user));
    }
}
