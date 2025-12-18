using EmployeeManagement.Domain.Enums;
using EmployeeManagement.Domain.Events;

namespace EmployeeManagement.Domain.Entities;

public class Employee : Entity
{
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string DocumentNumber { get; private set; } = string.Empty;
    public DateTime BirthDate { get; private set; }
    public string PasswordHash { get; private set; } = string.Empty;
    public Role Role { get; private set; }

    public Guid? ManagerId { get; private set; }
    public Employee? Manager { get; private set; }

    private readonly List<PhoneNumber> _phoneNumbers = [];
    public IReadOnlyCollection<PhoneNumber> PhoneNumbers => _phoneNumbers.AsReadOnly();

    private readonly List<Employee> _subordinates = [];
    public IReadOnlyCollection<Employee> Subordinates => _subordinates.AsReadOnly();

    private Employee() { }

    public static Result<Employee> Create(
        string firstName,
        string lastName,
        string email,
        string documentNumber,
        DateTime birthDate,
        string passwordHash,
        Role role,
        Guid? managerId = null,
        IEnumerable<string>? phoneNumbers = null)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            return Result<Employee>.Failure(Error.Validation("FirstName", "First name is required"));

        if (string.IsNullOrWhiteSpace(lastName))
            return Result<Employee>.Failure(Error.Validation("LastName", "Last name is required"));

        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            return Result<Employee>.Failure(Error.Validation("Email", "Invalid email format"));

        if (string.IsNullOrWhiteSpace(documentNumber))
            return Result<Employee>.Failure(Error.Validation("DocumentNumber", "Document number is required"));

        if (!IsAdultBirthDate(birthDate))
            return Result<Employee>.Failure(Error.Validation("BirthDate", "Employee must be at least 18 years old"));

        if (phoneNumbers == null || !phoneNumbers.Any())
            return Result<Employee>.Failure(Error.Validation("PhoneNumbers", "Funcionário deve possuir pelo menos um telefone"));

        var employee = new Employee
        {
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            Email = email.ToLowerInvariant().Trim(),
            DocumentNumber = documentNumber.Trim(),
            BirthDate = birthDate.Date,
            PasswordHash = passwordHash,
            Role = role,
            ManagerId = managerId
        };

        foreach (var phone in phoneNumbers)
        {
            employee.AddPhone(new PhoneNumber(phone, employee.Id));
        }

        employee.RaiseDomainEvent(new EmployeeCreatedEvent(
            employee.Id,
            employee.Email,
            employee.FullName));

        return Result<Employee>.Success(employee);
    }

    public string FullName => $"{FirstName} {LastName}";

    public bool CanCreateEmployeeWithRole(Role targetRole) => Role > targetRole;

    public bool CanUpdateEmployeeToRole(Role targetRole) => Role > targetRole;

    public Result Update(
        string firstName,
        string lastName,
        string email,
        DateTime birthDate,
        Guid? managerId)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            return Result.Failure(Error.Validation("FirstName", "First name is required"));

        if (string.IsNullOrWhiteSpace(lastName))
            return Result.Failure(Error.Validation("LastName", "Last name is required"));

        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            return Result.Failure(Error.Validation("Email", "Invalid email format"));

        if (!IsAdultBirthDate(birthDate))
            return Result.Failure(Error.Validation("BirthDate", "Employee must be at least 18 years old"));

        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        Email = email.ToLowerInvariant().Trim();
        BirthDate = birthDate.Date;
        ManagerId = managerId;
        SetUpdatedAt();

        RaiseDomainEvent(new EmployeeUpdatedEvent(Id, Email));

        return Result.Success();
    }

    public Result UpdateRole(Role newRole)
    {
        if (Role == newRole)
            return Result.Success();

        var oldRole = Role;
        Role = newRole;
        SetUpdatedAt();

        RaiseDomainEvent(new EmployeeRoleChangedEvent(Id, oldRole, newRole));

        return Result.Success();
    }

    public void AddPhone(PhoneNumber phone) => _phoneNumbers.Add(phone);

    public void ClearPhones() => _phoneNumbers.Clear();

    public Result UpdatePassword(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
            return Result.Failure(Error.Validation("Password", "Password is required"));

        PasswordHash = passwordHash;
        SetUpdatedAt();

        RaiseDomainEvent(new PasswordChangedEvent(Id));

        return Result.Success();
    }

    public override void Delete(Guid? deletedBy = null)
    {
        base.Delete(deletedBy);
        RaiseDomainEvent(new EmployeeDeletedEvent(Id, Email));
    }

    private static bool IsAdultBirthDate(DateTime birthDate) =>
        DateTime.UtcNow.AddYears(-18) >= birthDate;
}