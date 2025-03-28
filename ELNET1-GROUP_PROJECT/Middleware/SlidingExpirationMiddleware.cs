using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;

namespace ELNET1_GROUP_PROJECT.Middleware
{
    public class SlidingExpirationMiddleware
    {
        private readonly RequestDelegate _next;
        private const int ExpiryMinutes = 15;

        public SlidingExpirationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.Request.Cookies.TryGetValue("jwt", out var jwt))
            {
                var expiry = DateTime.UtcNow.AddMinutes(ExpiryMinutes);
                var options = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = expiry
                };
                context.Response.Cookies.Append("jwt", jwt, options);

                // Refresh UserRole and Id cookies too
                options.HttpOnly = false;
                if (context.Request.Cookies.TryGetValue("UserRole", out var userRole))
                {
                    context.Response.Cookies.Append("UserRole", userRole, options);
                }
                if (context.Request.Cookies.TryGetValue("Id", out var id))
                {
                    context.Response.Cookies.Append("Id", id, options);
                }

                // Send expiry info to frontend
                context.Response.Headers.Append("Session-Expiry", expiry.ToString("o"));  // ISO format
            }

            await _next(context);
        }
    }
}
