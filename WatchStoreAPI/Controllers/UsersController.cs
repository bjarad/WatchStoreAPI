using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using WatchStoreAPI.Data;
using WatchStoreAPI.Models;

namespace WatchStoreAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private ApiDbContext dbContext;
        private IConfiguration config;

        public UsersController(ApiDbContext dbContext, IConfiguration config)
        {
            this.dbContext = dbContext;
            this.config = config;
        }
        [HttpPost("Register")]
        public async Task<IActionResult> Register([FromBody] User user)
        {
            var exisitingUser = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == user.Email);
            if (exisitingUser != null)
            {
                return BadRequest("User with same Email is already Exist..");
            }
            var passwordHasher = new PasswordHasher<User>();
            user.PasswordHash = passwordHasher.HashPassword(user, user.PasswordHash);
            await dbContext.Users.AddAsync(user);
            await dbContext.SaveChangesAsync();
            return StatusCode(StatusCodes.Status201Created);
        }
        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var currentUser = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == request.Email );
            if (currentUser == null) {
                return NotFound("User Not Found");
            }
            var passwordHasher = new PasswordHasher<User>();
            var result = passwordHasher.VerifyHashedPassword(currentUser, currentUser.PasswordHash, request.Password);
            if (result != PasswordVerificationResult.Success) {
                return NotFound("Invalid Password");
            }
            //Generate Jwt
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]));

            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            //Claims
            var claims = new[]{
                new Claim(ClaimTypes.Email,request.Email),
                 new Claim(ClaimTypes.Role,currentUser.Role)
            };
            var token = new JwtSecurityToken(issuer: config["jwt:Issuer"], audience: config["Jwt:Audience"], claims: claims, expires: DateTime.Now.AddDays(60), signingCredentials: credentials);
            var jwt = new JwtSecurityTokenHandler().WriteToken(token);
            return new ObjectResult(new
            {
                access_token = jwt,
                token_type = "bearer",
                user_id = currentUser.Id,
                user_name=currentUser.Name
            });
        }
    }
    public class LoginRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }
}