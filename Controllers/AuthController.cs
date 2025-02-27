using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CurrencyConverterAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace CurrencyConverterAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        // Sample user data (will be validated with database in real time)
        private readonly Dictionary<string, string> _users = new Dictionary<string, string>
        {
            { "admin", "admin" }, 
            { "testuser1", "testuser1" } 
        };

        public AuthController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// Generates a JWT token based on provided username and password.
        /// </summary>
        /// <param name="username">The username of the user.</param>
        /// <param name="password">The password of the user.</param>
        /// <returns>A JWT token if the credentials are valid, otherwise an error response.</returns>
        [HttpPost("token")]
        public IActionResult GenerateToken([FromBody] UserCredentials credentials)
        {
            // Validate the user credentials
            if (!_users.ContainsKey(credentials.Username) || _users[credentials.Username] != credentials.Password)
            {
                return Unauthorized(new { error = "Invalid username or password." });
            }

            // If credentials are valid, determine the role
            string role = credentials.Username == "admin" ? "Admin" : "User";

            var tokenHandler = new JwtSecurityTokenHandler();

            // Get JWT secret key from configuration
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings["SecretKey"];

            // Ensure the secretKey exists
            if (string.IsNullOrEmpty(secretKey))
            {
                return BadRequest(new { error = "Secret key not found in configuration." });
            }

            var key = Encoding.ASCII.GetBytes(secretKey); // Convert secret to byte array

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.Name, credentials.Username),
                    new Claim(ClaimTypes.Role, role)
                }),
                Expires = DateTime.UtcNow.AddHours(1),
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature
                ),
                Audience = jwtSettings["Audience"],
                Issuer = jwtSettings["Issuer"]
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return Ok(new { token = tokenHandler.WriteToken(token) });
        }
    }
}
