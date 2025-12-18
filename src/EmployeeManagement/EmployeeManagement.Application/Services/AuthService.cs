using EmployeeManagement.Application.DTOs;
using EmployeeManagement.Application.Exceptions;
using EmployeeManagement.Application.Features.Employees.Common;
using EmployeeManagement.Application.Interfaces;
using EmployeeManagement.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace EmployeeManagement.Application.Services;

public class AuthService : IAuthService
{
    private readonly IEmployeeRepository _repository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtService _jwtService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IEmployeeRepository repository,
        IPasswordHasher passwordHasher,
        IJwtService jwtService,
        ILogger<AuthService> logger)
    {
        _repository = repository;
        _passwordHasher = passwordHasher;
        _jwtService = jwtService;
        _logger = logger;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Login attempt for: {Email}", request.Email);

        var employee = await _repository.GetByEmailAsync(request.Email, cancellationToken)
            ?? throw new UnauthorizedException("Invalid email or password");

        if (!_passwordHasher.Verify(request.Password, employee.PasswordHash))
        {
            _logger.LogWarning("Failed login attempt for: {Email}", request.Email);
            throw new UnauthorizedException("Invalid email or password");
        }

        var token = _jwtService.GenerateToken(employee);
        var expiresAt = DateTime.UtcNow.AddHours(8);

        _logger.LogInformation("Login successful for: {Email}", request.Email);

        return new LoginResponse(
            token,
            expiresAt,
            EmployeeResponse.FromEntity(employee));
    }
}