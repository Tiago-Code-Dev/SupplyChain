namespace EmployeeManagement.Domain.Exceptions;

public abstract class DomainException : Exception
{
    public string Code { get; }
    
    protected DomainException(string code, string message) : base(message)
    {
        Code = code;
    }
}

public sealed class EmployeeNotFoundException : DomainException
{
    public EmployeeNotFoundException(Guid id) 
        : base("Employee.NotFound", $"Employee with ID '{id}' was not found") { }
}