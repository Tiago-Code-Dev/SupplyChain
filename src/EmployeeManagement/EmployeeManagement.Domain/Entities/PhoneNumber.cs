namespace EmployeeManagement.Domain.Entities;

public class PhoneNumber : Entity
{
    public string Number { get; private set; } = string.Empty;
    public Guid EmployeeId { get; private set; }
    public Employee Employee { get; private set; } = null!;

    private PhoneNumber() { }

    public PhoneNumber(string number, Guid employeeId)
    {
        Number = number;
        EmployeeId = employeeId;
    }
}