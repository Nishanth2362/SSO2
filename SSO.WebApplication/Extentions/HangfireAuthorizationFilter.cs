using Hangfire.Dashboard;
using SSO.Common.Constants.Permission;
using System.Diagnostics.CodeAnalysis;

namespace SSO.WebApplication.Server.Extensions
{
    public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize([NotNull] DashboardContext context)
        {
            var httpContext = context.GetHttpContext();

            // Allow all authenticated users with the Hangfire permission
            // Or users in the "Admin" role if you have a specific master admin role
            return httpContext.User.Identity?.IsAuthenticated == true && 
                   httpContext.User.HasClaim(c => c.Type == ApplicationClaimTypes.Permission && c.Value == Permissions.Hangfire.View);
        }
    }
}
