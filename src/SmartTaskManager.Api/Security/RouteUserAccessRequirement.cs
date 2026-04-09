using Microsoft.AspNetCore.Authorization;

namespace SmartTaskManager.Api.Security;

public sealed class RouteUserAccessRequirement : IAuthorizationRequirement
{
    public RouteUserAccessRequirement(string routeParameterName = "userId")
    {
        RouteParameterName = routeParameterName;
    }

    public string RouteParameterName { get; }
}
