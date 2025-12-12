using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SupplyChain.Infrastructure;
using SupplyChain.Infrastructure.Model;
using SupplyChain.Infrastructure.Dtos;
using SupplyChain.Infrastructure.Services;

namespace SupplyChain.Infrastructure
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly JwtService _jwtService;
        private readonly ApplicationDbContext _context;

        public AuthController(JwtService jwtService, ApplicationDbContext context)
        {
            _jwtService = jwtService;
            _context = context;
        }

        // POST: api/auth/login
        [HttpPost("login")]
        [EndpointSummary("Login do usuário")]
        public async Task<ActionResult> Login([FromBody] LoginDto loginDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _context.Employees
                .FirstOrDefaultAsync(e => e.Email == loginDto.Email && e.Password == loginDto.Password);

            if (user == null)
            {
                return Unauthorized(new { message = "Email ou senha inválidos." });
            }

            var token = _jwtService.GenerateToken(user.Id.ToString(), user.Email);
            return Ok(new { token, userId = user.Id, email = user.Email });
        }
    }
}