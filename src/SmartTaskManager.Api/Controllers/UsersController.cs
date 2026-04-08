using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SmartTaskManager.Api.Contracts.Requests;
using SmartTaskManager.Api.Contracts.Responses;
using SmartTaskManager.Application.Services;
using SmartTaskManager.Domain.Entities;

namespace SmartTaskManager.Api.Controllers;

[ApiController]
[Route("api/users")]
public sealed class UsersController : ControllerBase
{
    private readonly UserService _userService;

    public UsersController(UserService userService)
    {
        _userService = userService ?? throw new ArgumentNullException(nameof(userService));
    }

    [HttpPost]
    [ProducesResponseType(typeof(UserResponse), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(409)]
    public async Task<ActionResult<UserResponse>> CreateUser(
        [FromBody] CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        User user = await _userService.CreateUserAsync(request.UserName, cancellationToken);
        UserResponse response = UserResponse.FromDomain(user);

        return CreatedAtAction(
            nameof(GetUserById),
            new { userId = response.Id },
            response);
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<UserResponse>), 200)]
    public async Task<ActionResult<IReadOnlyCollection<UserResponse>>> GetUsers(CancellationToken cancellationToken)
    {
        IReadOnlyCollection<User> users = await _userService.ListUsersAsync(cancellationToken);
        return Ok(users.Select(UserResponse.FromDomain).ToList());
    }

    [HttpGet("{userId:guid}")]
    [ProducesResponseType(typeof(UserResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<UserResponse>> GetUserById(Guid userId, CancellationToken cancellationToken)
    {
        User user = await _userService.GetUserAsync(userId, cancellationToken);
        return Ok(UserResponse.FromDomain(user));
    }
}
