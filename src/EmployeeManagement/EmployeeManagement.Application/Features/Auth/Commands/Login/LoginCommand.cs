using EmployeeManagement.Application.Common.Interfaces;
using EmployeeManagement.Application.Features.Auth.Common;

namespace EmployeeManagement.Application.Features.Auth.Commands.Login;

public sealed record LoginCommand(
    string Email,
    string Password) : ICommand<AuthResponse>;