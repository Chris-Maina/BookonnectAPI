using System.Globalization;
using System.Security.Claims;
using BookonnectAPI.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace BookonnectAPI.Lib;

public sealed class AdminAuthorizationHandler : AuthorizationHandler<AdminRequirement>
{
    private readonly V2AuthorizationOptions _options;

    public AdminAuthorizationHandler(IOptions<V2AuthorizationOptions> options)
    {
        _options = options.Value;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context, AdminRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated == true &&
            context.User.HasClaim(V2Authentication.IdentityStoreClaim, V2Authentication.IdentityStore) &&
            int.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier),
                NumberStyles.None, CultureInfo.InvariantCulture, out var userId) &&
            userId > 0 && _options.AdminUserIds.Contains(userId))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
