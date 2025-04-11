using Microsoft.AspNetCore.Mvc;
using ELNET1_GROUP_PROJECT.Data;
using ELNET1_GROUP_PROJECT.Models;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using System;
using System.Linq;

namespace ELNET1_GROUP_PROJECT.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly MyAppDBContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(MyAppDBContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpPost("refresh-session")]
        public IActionResult RefreshSession()
        {
            RefreshJwtCookies();
            return Ok(new { message = "Session refreshed successfully!" });
        }

        // This is the sign up in landing page remove this if everything is now good, also the signup.js
        [HttpPost("signup")]
        public async Task<IActionResult> SignUp([FromBody] User_Account user)
        {
            RefreshJwtCookies();
            if (await _context.User_Accounts.AnyAsync(u => u.Email == user.Email))
            {
                return BadRequest("Email already in use.");
            }

            user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);
            _context.User_Accounts.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "User registered successfully!" });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDTO loginDTO)
        {
            RefreshJwtCookies();
            var existingToken = Request.Cookies["jwt"];
            if (!string.IsNullOrEmpty(existingToken) && ValidateJwtToken(existingToken) != null)
            {
                var existingUser = GetUserFromToken(existingToken);
                if (existingUser != null)
                {
                    return RedirectToRole(existingUser.Role);
                }
            }

            var user = await _context.User_Accounts.FirstOrDefaultAsync(u => u.Email == loginDTO.Email);
            if (user == null || !BCrypt.Net.BCrypt.Verify(loginDTO.Password, user.Password))
            {
                return Unauthorized(new { message = "Login Failed. Please check your email or password." });
            }

            var token = GenerateJwtToken(user);
            SetJwtCookie(token, user.Role, user.Id.ToString()); 

            return RedirectToRole(user.Role);
        }

        private string GenerateJwtToken(User_Account user)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = Environment.GetEnvironmentVariable("JWT_SECRET") ?? _configuration["JwtSettings:Secret"];
            var key = Encoding.UTF8.GetBytes(secretKey);

            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim("UserId", user.Id.ToString()),
                    new Claim("Role", user.Role) 
                }),
                Expires = DateTime.UtcNow.AddMinutes(int.Parse(jwtSettings["ExpiryMinutes"])),
                Issuer = jwtSettings["Issuer"],
                Audience = jwtSettings["Audience"],
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature
                )
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private ClaimsPrincipal ValidateJwtToken(string token)
        {
            try
            {
                var jwtSettings = _configuration.GetSection("JwtSettings");
                var key = Encoding.ASCII.GetBytes(jwtSettings["Secret"]);

                var tokenHandler = new JwtSecurityTokenHandler();
                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidIssuer = jwtSettings["Issuer"],
                    ValidAudience = jwtSettings["Audience"],
                    ValidateLifetime = true
                };

                return tokenHandler.ValidateToken(token, validationParameters, out _);
            }
            catch
            {
                return null;
            }
        }

        private void SetJwtCookie(string token, string role, string id)
        {
            var options = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict
            };

            Response.Cookies.Append("jwt", token, options);

            // Non-HttpOnly cookies for frontend access
            options.HttpOnly = false;
            Response.Cookies.Append("UserRole", role, options);
            Response.Cookies.Append("Id", id, options);
        }

        //Resetting the cookies time
        private void RefreshJwtCookies()
        {
            var token = Request.Cookies["jwt"];
            var role = Request.Cookies["UserRole"];
            var id = Request.Cookies["Id"];

            if (!string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(role) && !string.IsNullOrEmpty(id))
            {
                // Reset cookies with updated expiration
                var expiryMinutes = 15;  // Reset to 15 minutes

                var options = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTime.UtcNow.AddMinutes(expiryMinutes)
                };

                Response.Cookies.Append("jwt", token, options);
                Response.Cookies.Append("UserRole", role, new CookieOptions
                {
                    HttpOnly = false,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTime.UtcNow.AddMinutes(expiryMinutes)
                });
                Response.Cookies.Append("Id", id, new CookieOptions
                {
                    HttpOnly = false,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTime.UtcNow.AddMinutes(expiryMinutes)
                });
            }
        }

        private User_Account GetUserFromToken(string token)
        {
            var principal = ValidateJwtToken(token);
            if (principal == null) return null;

            var userId = principal.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value;
            if (userId == null) return null;

            return _context.User_Accounts.Find(int.Parse(userId));
        }

        private IActionResult RedirectToRole(string role)
        {
            return role switch
            {
                "Admin" => Ok(new { redirectUrl = "/admin/dashboard" }),
                "Homeowner" => Ok(new { redirectUrl = "/home/dashboard" }),
                "Staff" => Ok(new { redirectUrl = "/staff/dashboard" }),
                _ => Ok(new { redirectUrl = "/home" })
            };
        }

        [HttpPost("logout")]
        public IActionResult Logout()
        {
            Response.Cookies.Delete("jwt");
            Response.Cookies.Delete("UserRole");
            Response.Cookies.Delete("Id");
            return Ok(new { redirectUrl = "/home", message = "Logged out successfully!" });
        }

        /*
        [HttpPost("check-google-user")]
        public async Task<IActionResult> CheckGoogleUser([FromBody] LoginDTO loginDTO)
        {
            var user = await _context.User_Accounts.FirstOrDefaultAsync(u => u.Email == loginDTO.Email);
            return Ok(new { exists = user != null });
        }
        */

        /*
        [HttpPost("google-signup")]
        public async Task<IActionResult> GoogleSignUp([FromBody] User_Account user)
        {
            var existingUser = await _context.User_Accounts.FirstOrDefaultAsync(u => u.Email == user.Email);

            if (existingUser == null)
            {
                user.Password = "null";
                _context.User_Accounts.Add(user);
                await _context.SaveChangesAsync();
            }
            else
            {
                user = existingUser;
            }

            var token = GenerateJwtToken(user);

            SetJwtCookie(token);

            return Ok(new { message = "Google Sign-In successful!" });
        }
        */
    }
}