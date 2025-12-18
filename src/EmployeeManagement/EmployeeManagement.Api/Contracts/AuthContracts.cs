namespace EmployeeManagement.Api.Contracts;

/// <summary>
/// Request para login
/// </summary>
public sealed record LoginRequest(
    string Email,
    string Password);

/// <summary>
/// Request para alteração de senha
/// </summary>
public sealed record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword);