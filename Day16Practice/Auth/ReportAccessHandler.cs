using Microsoft.AspNetCore.Authorization;

namespace Day16Practice.Auth
{
    public class ReportAccessHandler : AuthorizationHandler<ReportAccessRequirements>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, ReportAccessRequirements requirement)
        {
            if (context.User.IsInRole("Admin"))
            {
                context.Succeed(requirement);
            }

            var depClaim = context.User.FindFirst("department");
            if (depClaim?.Value == requirement.Department)
            {
                context.Succeed(requirement);
            }
            return Task.CompletedTask;
        }
    }
}
