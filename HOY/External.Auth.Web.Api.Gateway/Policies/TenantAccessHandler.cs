using Microsoft.AspNetCore.Authorization;

namespace External.Auth.Web.Api.Gateway.Policies;

public sealed class TenantAccessHandler : AuthorizationHandler<TenantAccessRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, TenantAccessRequirement requirement)
    {
        if (context.Resource is HttpContext httpContext &&
            httpContext.Request.Headers.TryGetValue("X-Tenant-Id", out var tenantId) &&
            !string.IsNullOrWhiteSpace(tenantId))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
