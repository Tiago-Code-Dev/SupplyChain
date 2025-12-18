using EmployeeManagement.Domain.Enums;

namespace EmployeeManagement.Api.Contracts;

/// <summary>
/// Request para criação de funcionário
/// </summary>
public sealed record CreateEmployeeRequest(
    string FirstName,
    string LastName,
    string Email,
    string DocumentNumber,
    DateTime BirthDate,
    string Password,
    Role Role,
    Guid? ManagerId,
    List<string> PhoneNumbers);

/// <summary>
/// Request para atualização de funcionário
/// </summary>
public sealed record UpdateEmployeeRequest(
    string FirstName,
    string LastName,
    string Email,
    DateTime BirthDate,
    Guid? ManagerId,
    List<string> PhoneNumbers,
    Role? Role = null);