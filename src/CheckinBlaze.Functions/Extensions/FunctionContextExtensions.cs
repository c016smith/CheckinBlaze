using System.Security.Claims;
using Microsoft.Azure.Functions.Worker;

namespace CheckinBlaze.Functions.Extensions
{
    public static class FunctionContextExtensions
    {
        public static ClaimsPrincipal GetUser(this FunctionContext context)
        {
            if (context.Items.TryGetValue("ClaimsPrincipal", out var principal) && principal is ClaimsPrincipal claimsPrincipal)
            {
                // Return the authenticated claims principal
                return claimsPrincipal;
            }
            
            // Return an unauthenticated principal
            return new ClaimsPrincipal(new ClaimsIdentity());
        }

        public static bool IsAuthenticatedUser(this FunctionContext context)
        {
            var principal = GetUser(context);
            return principal?.Identity?.IsAuthenticated == true;
        }

        public static string GetUserId(this FunctionContext context)
        {
            var principal = GetUser(context);
            return principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }

        public static string GetUserName(this FunctionContext context)
        {
            var principal = GetUser(context);
            return principal?.FindFirst("name")?.Value ?? 
                   principal?.FindFirst(ClaimTypes.Name)?.Value ?? 
                   "Unknown User";
        }

        public static bool IsInRole(this FunctionContext context, string role)
        {
            var principal = GetUser(context);
            return principal?.IsInRole(role) == true;
        }
    }
}