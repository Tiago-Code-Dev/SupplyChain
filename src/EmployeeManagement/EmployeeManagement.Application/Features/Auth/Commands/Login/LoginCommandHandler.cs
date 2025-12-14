using EmployeeManagement.Application.Common.Interfaces;
using EmployeeManagement.Application.Features.Auth.Common;
using EmployeeManagement.Application.Features.Employees.Common;
using EmployeeManagement.Application.Interfaces;
using EmployeeManagement.Domain.Common;
using EmployeeManagement.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace EmployeeManagement.Application.Features.Auth.Commands.Login;

public sealed class LoginCommandHandler : ICommandHandler<LoginCommand, AuthResponse>
{
    private readonly IEmployeeRepository _repository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtService _jwtService;
    private readonly ILogger<LoginCommandHandler> _logger;

    public LoginCommandHandler(
        IEmployeeRepository repository,
        IPasswordHasher passwordHasher,
        IJwtService jwtService,
        ILogger<LoginCommandHandler> logger)
    {
        _repository = repository;
        _passwordHasher = passwordHasher;
        _jwtService = jwtService;
        _logger = logger;
    }

    public async Task<Result<AuthResponse>> Handle(
        LoginCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Login attempt for: {Email}", request.Email);

        var employee = await _repository.GetByEmailAsync(request.Email, cancellationToken);
        
        if (employee is null)
        {
            _logger.LogWarning("Login failed - user not found: {Email}", request.Email);
            return Result<AuthResponse>.Failure(
                Error.Unauthorized("Invalid email or password"));
        }

        if (!_passwordHasher.Verify(request.Password, employee.PasswordHash))
        {
            _logger.LogWarning("Login failed - invalid password: {Email}", request.Email);
            return Result<AuthResponse>.Failure(
                Error.Unauthorized("Invalid email or password"));
        }

        var token = _jwtService.GenerateToken(employee);
        var expiresAt = DateTime.UtcNow.AddHours(8);

        _logger.LogInformation("Login successful for: {Email}", request.Email);

        return Result<AuthResponse>.Success(new AuthResponse(
            token,
            expiresAt,
            EmployeeResponse.FromEntity(employee)));
    }
}