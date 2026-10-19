using Microsoft.AspNetCore.Authorization;

namespace Day16Practice.Auth
{
    public class DepartmentRequirements : IAuthorizationRequirement
    {
        public string Department { get; }

        public DepartmentRequirements(string department)
        {
            Department = department;
        }
    }
}
