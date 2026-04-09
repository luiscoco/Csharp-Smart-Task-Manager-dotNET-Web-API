using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;

namespace SmartTaskManager.Api.Security;

public sealed class RouteUserAccessAuthorizationHandler
    : AuthorizationHandler<RouteUserAccessRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        RouteUserAccessRequirement requirement)
    {
        string? routeUserId = GetRouteValue(context.Resource, requirement.RouteParameterName);
        string? currentUserId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (Guid.TryParse(routeUserId, out Guid routeUserGuid)
            && Guid.TryParse(currentUserId, out Guid currentUserGuid)
            && routeUserGuid == currentUserGuid)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }

    private static string? GetRouteValue(object? resource, string routeParameterName)
    {
        return resource switch
        {
            HttpContext httpContext => httpContext.Request.RouteValues[routeParameterName]?.ToString(),
            AuthorizationFilterContext filterContext => filterContext.RouteData.Values[routeParameterName]?.ToString(),
            _ => null
        };
    }
}
