namespace EmployeeManagement.Application.Common;

/// <summary>
/// Chaves de cache centralizadas
/// </summary>
public static class CacheKeys
{
    private const string Prefix = "employee-management:";
    
    public static string Employee(Guid id) => $"{Prefix}employee:{id}";
    public static string EmployeeByEmail(string email) => $"{Prefix}employee:email:{email.ToLowerInvariant()}";
    public static string AllEmployees => $"{Prefix}employees:all";
    public static string EmployeesList(int page, int size) => $"{Prefix}employees:list:{page}:{size}";
}
