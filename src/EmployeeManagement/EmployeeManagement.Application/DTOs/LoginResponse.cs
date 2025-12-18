using EmployeeManagement.Application.Features.Employees.Common;

namespace EmployeeManagement.Application.DTOs;

public record LoginResponse(string Token, DateTime ExpiresAt, EmployeeResponse Employee);