using EmployeeManagement.Tests.Helpers;

namespace EmployeeManagement.Tests.UnitTests.Domain;

/// <summary>
/// Testes unitários para a entidade Employee
/// </summary>
public class EmployeeTests
{
    #region Create Tests

    [Fact]
    [Trait("Category", "Domain")]
    public void Create_ComDadosValidos_DeveRetornarSucesso()
    {
        // Arrange
        var firstName = "João";
        var lastName = "Silva";
        var email = "joao.silva@empresa.com";
        var documentNumber = TestHelper.GenerateValidCpf();
        var birthDate = TestHelper.GenerateAdultBirthDate();
        var passwordHash = "hashed_password";
        var role = Role.Employee;

        // Act
        var result = Employee.Create(
            firstName,
            lastName,
            email,
            documentNumber,
            birthDate,
            passwordHash,
            role,
            null,
            new List<string> { "(11) 99999-8888" });

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.FirstName.Should().Be(firstName);
        result.Value.LastName.Should().Be(lastName);
        result.Value.Email.Should().Be(email.ToLowerInvariant());
        result.Value.Role.Should().Be(role);
    }

    [Theory]
    [Trait("Category", "Domain")]
    [InlineData("", "Silva")]
    [InlineData("  ", "Silva")]
    [InlineData(null, "Silva")]
    public void Create_ComPrimeiroNomeInvalido_DeveRetornarFalha(string? firstName, string lastName)
    {
        // Arrange
        var email = "test@test.com";
        var documentNumber = TestHelper.GenerateValidCpf();
        var birthDate = TestHelper.GenerateAdultBirthDate();

        // Act
        var result = Employee.Create(
            firstName!,
            lastName,
            email,
            documentNumber,
            birthDate,
            "hash",
            Role.Employee);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Description.Should().Contain("First name is required");
    }

    [Theory]
    [Trait("Category", "Domain")]
    [InlineData("João", "")]
    [InlineData("João", "  ")]
    [InlineData("João", null)]
    public void Create_ComSobrenomeInvalido_DeveRetornarFalha(string firstName, string? lastName)
    {
        // Arrange
        var email = "test@test.com";
        var documentNumber = TestHelper.GenerateValidCpf();
        var birthDate = TestHelper.GenerateAdultBirthDate();

        // Act
        var result = Employee.Create(
            firstName,
            lastName!,
            email,
            documentNumber,
            birthDate,
            "hash",
            Role.Employee);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Description.Should().Contain("Last name is required");
    }

    [Theory]
    [Trait("Category", "Domain")]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("invalidemail")]
    [InlineData("invalid.email")]
    public void Create_ComEmailInvalido_DeveRetornarFalha(string email)
    {
        // Arrange
        var firstName = "João";
        var lastName = "Silva";
        var documentNumber = TestHelper.GenerateValidCpf();
        var birthDate = TestHelper.GenerateAdultBirthDate();

        // Act
        var result = Employee.Create(
            firstName,
            lastName,
            email,
            documentNumber,
            birthDate,
            "hash",
            Role.Employee);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Description.Should().Contain("Invalid email format");
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void Create_ComMenorDeIdade_DeveRetornarFalha()
    {
        // Arrange
        var birthDate = TestHelper.GenerateMinorBirthDate();

        // Act
        var result = Employee.Create(
            "João",
            "Silva",
            "joao@empresa.com",
            TestHelper.GenerateValidCpf(),
            birthDate,
            "hash",
            Role.Employee);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Description.Should().Contain("at least 18 years old");
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void Create_DeveGerarDomainEvent()
    {
        // Arrange & Act
        var result = Employee.Create(
            "João",
            "Silva",
            "joao@empresa.com",
            TestHelper.GenerateValidCpf(),
            TestHelper.GenerateAdultBirthDate(),
            "hash",
            Role.Employee,
            null,
            new List<string> { "(11) 99999-8888" });

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.DomainEvents.Should().NotBeEmpty();
        result.Value.DomainEvents.Should().ContainSingle(e =>
            e.GetType().Name == "EmployeeCreatedEvent");
    }

    #endregion

    #region Update Tests

    [Fact]
    [Trait("Category", "Domain")]
    public void Update_ComDadosValidos_DeveRetornarSucesso()
    {
        // Arrange
        var employee = TestHelper.CreateValidEmployee();
        var newFirstName = "Carlos";
        var newLastName = "Eduardo";
        var newEmail = "carlos.eduardo@empresa.com";
        var newBirthDate = TestHelper.GenerateAdultBirthDate();

        // Act
        var result = employee.Update(
            newFirstName,
            newLastName,
            newEmail,
            newBirthDate,
            null);

        // Assert
        result.IsSuccess.Should().BeTrue();
        employee.FirstName.Should().Be(newFirstName);
        employee.LastName.Should().Be(newLastName);
        employee.Email.Should().Be(newEmail.ToLowerInvariant());
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void Update_ComEmailInvalido_DeveRetornarFalha()
    {
        // Arrange
        var employee = TestHelper.CreateValidEmployee();

        // Act
        var result = employee.Update(
            "João",
            "Silva",
            "invalid-email",
            TestHelper.GenerateAdultBirthDate(),
            null);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Description.Should().Contain("Invalid email format");
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void Update_DeveGerarEmployeeUpdatedEvent()
    {
        // Arrange
        var employee = TestHelper.CreateValidEmployee();
        employee.ClearDomainEvents();

        // Act
        var result = employee.Update(
            "Novo",
            "Nome",
            "novo@email.com",
            TestHelper.GenerateAdultBirthDate(),
            null);

        // Assert
        result.IsSuccess.Should().BeTrue();
        employee.DomainEvents.Should().ContainSingle(e =>
            e.GetType().Name == "EmployeeUpdatedEvent");
    }

    #endregion

    #region UpdatePassword Tests

    [Fact]
    [Trait("Category", "Domain")]
    public void UpdatePassword_ComHashValido_DeveRetornarSucesso()
    {
        // Arrange
        var employee = TestHelper.CreateValidEmployee();
        var newPasswordHash = "new_hashed_password";

        // Act
        var result = employee.UpdatePassword(newPasswordHash);

        // Assert
        result.IsSuccess.Should().BeTrue();
        employee.PasswordHash.Should().Be(newPasswordHash);
    }

    [Theory]
    [Trait("Category", "Domain")]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData(null)]
    public void UpdatePassword_ComHashInvalido_DeveRetornarFalha(string? passwordHash)
    {
        // Arrange
        var employee = TestHelper.CreateValidEmployee();

        // Act
        var result = employee.UpdatePassword(passwordHash!);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Description.Should().Contain("Password is required");
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void UpdatePassword_DeveGerarPasswordChangedEvent()
    {
        // Arrange
        var employee = TestHelper.CreateValidEmployee();
        employee.ClearDomainEvents();

        // Act
        var result = employee.UpdatePassword("new_hash");

        // Assert
        result.IsSuccess.Should().BeTrue();
        employee.DomainEvents.Should().ContainSingle(e =>
            e.GetType().Name == "PasswordChangedEvent");
    }

    #endregion

    #region Delete Tests (Soft Delete)

    [Fact]
    [Trait("Category", "Domain")]
    public void Delete_DeveMarcarComoExcluido()
    {
        // Arrange
        var employee = TestHelper.CreateValidEmployee();

        // Act
        employee.Delete();

        // Assert
        employee.IsDeleted.Should().BeTrue();
        employee.DeletedAt.Should().NotBeNull();
        employee.DeletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void Delete_DeveGerarEmployeeDeletedEvent()
    {
        // Arrange
        var employee = TestHelper.CreateValidEmployee();
        employee.ClearDomainEvents();

        // Act
        employee.Delete();

        // Assert
        employee.DomainEvents.Should().ContainSingle(e =>
            e.GetType().Name == "EmployeeDeletedEvent");
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void Restore_DeveRestaurarFuncionarioExcluido()
    {
        // Arrange
        var employee = TestHelper.CreateValidEmployee();
        employee.Delete();

        // Act
        employee.Restore();

        // Assert
        employee.IsDeleted.Should().BeFalse();
        employee.DeletedAt.Should().BeNull();
    }

    #endregion

    #region Business Rules Tests

    [Theory]
    [Trait("Category", "Domain")]
    [InlineData(Role.Director, Role.Leader, true)]
    [InlineData(Role.Director, Role.Employee, true)]
    [InlineData(Role.Leader, Role.Employee, true)]
    [InlineData(Role.Leader, Role.Leader, false)]
    [InlineData(Role.Employee, Role.Employee, false)]
    [InlineData(Role.Employee, Role.Leader, false)]
    public void CanCreateEmployeeWithRole_DeveRetornarResultadoCorreto(
        Role currentUserRole,
        Role targetRole,
        bool expectedResult)
    {
        // Arrange
        var employee = TestHelper.CreateValidEmployee(role: currentUserRole);

        // Act
        var result = employee.CanCreateEmployeeWithRole(targetRole);

        // Assert
        result.Should().Be(expectedResult);
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void FullName_DeveConcatenarPrimeiroEUltimoNome()
    {
        // Arrange
        var employee = TestHelper.CreateValidEmployee(
            firstName: "João",
            lastName: "Silva");

        // Act
        var fullName = employee.FullName;

        // Assert
        fullName.Should().Be("João Silva");
    }

    #endregion

    #region PhoneNumber Tests

    [Fact]
    [Trait("Category", "Domain")]
    public void AddPhone_DeveAdicionarTelefoneNaColecao()
    {
        // Arrange
        var employee = TestHelper.CreateValidEmployee();
        employee.ClearPhones();
        var phone = new PhoneNumber("(11) 99999-8888", employee.Id);

        // Act
        employee.AddPhone(phone);

        // Assert
        employee.PhoneNumbers.Should().ContainSingle();
        employee.PhoneNumbers.First().Number.Should().Be("(11) 99999-8888");
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void ClearPhones_DeveRemoverTodosTelefones()
    {
        // Arrange
        var employee = TestHelper.CreateValidEmployee();
        employee.AddPhone(new PhoneNumber("(11) 99999-8888", employee.Id));
        employee.AddPhone(new PhoneNumber("(11) 88888-7777", employee.Id));

        // Act
        employee.ClearPhones();

        // Assert
        employee.PhoneNumbers.Should().BeEmpty();
    }

    #endregion
}