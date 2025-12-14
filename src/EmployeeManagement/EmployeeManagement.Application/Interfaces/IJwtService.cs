using EmployeeManagement.Domain.Entities;

namespace EmployeeManagement.Application.Interfaces;

public interface IJwtService
{
    string GenerateToken(Employee employee);
}