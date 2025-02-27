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

namespace ELNET1_GROUP_PROJECT.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly MyAppDBContext _context;
        private readonly IConfiguration _configuration; // ✅ Add this

        public AuthController(MyAppDBContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration; // ✅ Assign it
        }

        [HttpPost("signup")]
        public async Task<IActionResult> SignUp([FromBody] User_Account user)
        {
            if (await _context.User_Accounts.AnyAsync(u => u.Email == user.Email))
            {
                return BadRequest("Email already in use.");
            }

            user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);
            _context.User_Accounts.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "User registered successfully!" });
        }

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

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDTO loginDTO)
        {
            var user = await _context.User_Accounts.FirstOrDefaultAsync(u => u.Email == loginDTO.Email);

            if (user == null || !BCrypt.Net.BCrypt.Verify(loginDTO.Password, user.Password))
            {
                return Unauthorized("Invalid credentials.");
            }

            var token = GenerateJwtToken(user);

            // ✅ Store token in HTTP-only cookie
            SetJwtCookie(token);

            return Ok(new { message = "Login successful!" });
        }

        [HttpPost("check-google-user")]
        public async Task<IActionResult> CheckGoogleUser([FromBody] LoginDTO loginDTO)
        {
            var user = await _context.User_Accounts.FirstOrDefaultAsync(u => u.Email == loginDTO.Email);
            return Ok(new { exists = user != null });
        }

        [Authorize]
        [HttpGet("profile")]
        public async Task<IActionResult> GetUserProfile()
        {
            if (!Request.Cookies.TryGetValue("jwt", out var token))
            {
                return Unauthorized("No token provided.");
            }

            var principal = ValidateJwtToken(token);
            if (principal == null) return Unauthorized("Invalid token.");

            var userId = principal.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value;

            if (userId == null) return Unauthorized();

            var user = await _context.User_Accounts.FindAsync(int.Parse(userId));

            return Ok(user);
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
                    new Claim("UserId", user.Id.ToString())
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

            try
            {
                var principal = tokenHandler.ValidateToken(token, validationParameters, out _);
                return principal;
            }
            catch
            {
                return null;
            }
        }

        private void SetJwtCookie(string token)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true, // Prevents JavaScript access (secure against XSS attacks)
                Secure = true,   
                SameSite = SameSiteMode.Strict, 
                Expires = DateTime.UtcNow.AddMinutes(60)
            };
            Response.Cookies.Append("jwt", token, cookieOptions);
        }

        [HttpPost("logout")]
        public IActionResult Logout()
        {
            Response.Cookies.Delete("jwt");
            return Ok(new { message = "Logged out successfully!" });
        }
    }
}
