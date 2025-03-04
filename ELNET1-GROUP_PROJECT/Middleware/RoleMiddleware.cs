using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

public class RoleMiddleware
{
    private readonly RequestDelegate _next;

    public RoleMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        string path = context.Request.Path.ToString().ToLower();
        string userRole = context.Request.Cookies["UserRole"];
        string jwtToken = context.Request.Cookies["jwt"]; // Check if user is logged in

        // If no JWT token, redirect to /home
        if (string.IsNullOrEmpty(jwtToken))
        {
            if ((path.StartsWith("/admin") || path.StartsWith("/staff") || path.StartsWith("/home")) && path != "/home" && path != "/")
            {
                context.Response.Redirect("/Restricted/UnauthorizedAccess");
                return;
            }
        }
        else
        {
            // Restrict access based on role
            if (path.StartsWith("/admin") && userRole != "Admin")
            {
                context.Response.Redirect("/Restricted/UnauthorizedAccess");
                return;
            }
            if (path.StartsWith("/staff") && userRole != "Staff")
            {
                context.Response.Redirect("/Restricted/UnauthorizedAccess");
                return;
            }
            if (path.StartsWith("/home") && userRole != "Homeowner")
            {
                context.Response.Redirect("/Restricted/UnauthorizedAccess");
                return;
            }
        }

        await _next(context);
    }
}
