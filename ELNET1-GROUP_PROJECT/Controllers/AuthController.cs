using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;
using ELNET1_GROUP_PROJECT.Data;
using ELNET1_GROUP_PROJECT.Models;
using Microsoft.EntityFrameworkCore;

namespace Subvi.Controllers
{
    public class AuthController : Controller
    {
        private readonly MyAppDBContext _context;
        private readonly PasswordHasher<User_Account> _passwordHasher;

        public AuthController(MyAppDBContext context)
        {
            _context = context;
            _passwordHasher = new PasswordHasher<User_Account>();
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _context.User_Accounts.FirstOrDefaultAsync(u => u.Email == model.Email);
            if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.Password))
            {
                return BadRequest(new { message = "Invalid email or password" });
            }

            return Ok(new { message = "Login successful", redirectUrl = "/dashboard" });
        }

        [HttpPost("register")]
        public IActionResult Register([FromBody] RegisterModel model)
        {
            if (model == null)
            {
                return BadRequest(new { message = "Invalid request data." });
            }

            try
            {
                // Validate if email already exists
                var existingUser = _context.User_Accounts.FirstOrDefault(u => u.Email == model.Email);
                if (existingUser != null)
                {
                    return BadRequest(new { message = "Email is already registered." });
                }

                // Example: Save new user (replace with your actual saving logic)
                User_Account newUser = new User_Account
                {
                    Firstname = model.Firstname,
                    Lastname = model.Lastname,
                    Email = model.Email,
                    Address = model.Address,
                    PhoneNumber = model.Phonenumber,
                    Password = BCrypt.Net.BCrypt.HashPassword(model.Password) // Secure password hashing
                };

                _context.User_Accounts.Add(newUser);
                _context.SaveChanges();

                return Ok(new { message = "Registration successful." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Server error: " + ex.Message });
            }
        }



    }
}
