using Microsoft.AspNetCore.Authorization;

namespace Day16Demo.Components.Auth
{
    public class DepartmentRequirement : IAuthorizationRequirement
    {
        public string Department { get; }
        // each policy passes in its own department
        public DepartmentRequirement(string department)
        {
            Department = department;
        }
    }
}
