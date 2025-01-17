using System.Security.Claims;
using BookonnectAPI.Data;
using Microsoft.AspNetCore.Authorization;

namespace BookonnectAPI.Lib;

public class ClaimValueCheckHandler : AuthorizationHandler<ClaimRequirement>
{
    private readonly IServiceProvider _serviceProvider;

    public ClaimValueCheckHandler(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, ClaimRequirement requirement)
    {
        // check if user is authenticated and has a claim
        if (context.User == null || !context.User.HasClaim(c => c.Type == requirement.ClaimType))
        {
            return Task.CompletedTask;
        }


        var claimValue = context.User.FindFirstValue(requirement.ClaimType);
        using (var scope = _serviceProvider.CreateScope())
        {
            var bookonnectContext = scope.ServiceProvider.GetRequiredService<BookonnectContext>();
            /**
             * Check if 
             * - claim value exists
             * - claim value can be converted to int
             * - user id is valid
             */
            if (claimValue != null &&
                int.TryParse(claimValue, out int userID) &&
                bookonnectContext.Users.Any(u => u.ID == userID))
            {
                context.Succeed(requirement);
            }
            else
            {
                context.Fail();
            }
        }
        return Task.CompletedTask;
    }
}
