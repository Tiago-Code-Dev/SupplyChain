namespace EmployeeManagement.Application.Common;

/// <summary>
/// Chaves de cache centralizadas
/// </summary>
public static class CacheKeys
{
    private const string EmployeesPrefix = "employees";

    public static string Employee(Guid id) => $"{EmployeesPrefix}:{id}";
    public static string EmployeeByEmail(string email) => $"{EmployeesPrefix}:email:{email.ToLower()}";
    public static string EmployeesList(int page, int size, string? search) => 
        $"{EmployeesPrefix}:list:{page}:{size}:{search ?? "all"}";
    public static string AllEmployees => $"{EmployeesPrefix}:all";
}