using EmployeeManagement.Application.Features.Employees.Common;

namespace EmployeeManagement.Application.Features.Auth.Common;

public sealed record AuthResponse(
    string Token,
    DateTime ExpiresAt,
    EmployeeResponse Employee);