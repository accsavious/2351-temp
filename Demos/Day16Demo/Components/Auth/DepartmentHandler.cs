using Microsoft.AspNetCore.Authorization;

namespace Day16Demo.Components.Auth
{
    public class DepartmentHandler : AuthorizationHandler<DepartmentRequirement>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context,
                                                        DepartmentRequirement requirement)
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
