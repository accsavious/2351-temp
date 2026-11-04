using Microsoft.AspNetCore.Authorization;

namespace Day16Practice.Auth
{
    public class DepartmentHandler : AuthorizationHandler<DepartmentRequirements>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, DepartmentRequirements requirement)
        {
            var deptClaim = context.User.FindFirst("department");
            if (deptClaim?.Value == requirement.Department)
            {
                context.Succeed(requirement);
            }
            return Task.CompletedTask;
        }
    }
}
