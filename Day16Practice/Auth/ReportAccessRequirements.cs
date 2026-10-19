using Microsoft.AspNetCore.Authorization;

namespace Day16Practice.Auth
{
    public class ReportAccessRequirements(string department) : IAuthorizationRequirement
    {
        public string Department { get; } = department;
    }
    //public class ReportAccessRequirements : IAuthorizationRequirement
    //{
    //    public string Department { get; }

    //    public ReportAccessRequirements(string department)
    //    {
    //        Department = department;
    //    }
    //}
}
