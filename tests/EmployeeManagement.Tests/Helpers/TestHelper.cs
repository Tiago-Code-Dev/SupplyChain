using Bogus;

namespace EmployeeManagement.Tests.Helpers;

/// <summary>
/// Helper para criação de dados de teste
/// </summary>
public static class TestHelper
{
    private static readonly Faker Faker = new("pt_BR");

    /// <summary>
    /// Cria um Employee válido para testes
    /// </summary>
    public static Employee CreateValidEmployee(
        string? firstName = null,
        string? lastName = null,
        string? email = null,
        string? documentNumber = null,
        DateTime? birthDate = null,
        string? passwordHash = null,
        Role role = Role.Employee,
        Guid? managerId = null,
        IEnumerable<string>? phoneNumbers = null)
    {
        var phones = phoneNumbers ?? new List<string> { GenerateValidPhoneNumber() };

        var result = Employee.Create(
            firstName ?? Faker.Name.FirstName(),
            lastName ?? Faker.Name.LastName(),
            email ?? Faker.Internet.Email(),
            documentNumber ?? GenerateValidCpf(),
            birthDate ?? GenerateAdultBirthDate(),
            passwordHash ?? "hashed_password_123",
            role,
            managerId,
            phones);

        if (result.IsFailure)
        {
            throw new InvalidOperationException($"Falha ao criar Employee de teste: {result.Error.Description}");
        }

        return result.Value;
    }

    /// <summary>
    /// Gera um CPF válido para testes
    /// </summary>
    public static string GenerateValidCpf()
    {
        var random = new Random();
        var cpf = new int[11];

        for (int i = 0; i < 9; i++)
            cpf[i] = random.Next(0, 10);

        int sum = 0;
        for (int i = 0; i < 9; i++)
            sum += cpf[i] * (10 - i);
        int remainder = sum % 11;
        cpf[9] = remainder < 2 ? 0 : 11 - remainder;

        sum = 0;
        for (int i = 0; i < 10; i++)
            sum += cpf[i] * (11 - i);
        remainder = sum % 11;
        cpf[10] = remainder < 2 ? 0 : 11 - remainder;

        return string.Join("", cpf);
    }

    /// <summary>
    /// Gera email válido
    /// </summary>
    public static string GenerateValidEmail() => Faker.Internet.Email();

    /// <summary>
    /// Gera senha válida
    /// </summary>
    public static string GenerateValidPassword() => Faker.Internet.Password(12, false, "\\w", "!@#$");

    /// <summary>
    /// Gera telefone válido
    /// </summary>
    public static string GenerateValidPhoneNumber() => Faker.Phone.PhoneNumber("(##) #####-####");

    /// <summary>
    /// Gera data de nascimento de adulto (18+ anos)
    /// </summary>
    public static DateTime GenerateAdultBirthDate() =>
        Faker.Date.Past(30, DateTime.UtcNow.AddYears(-18));

    /// <summary>
    /// Gera data de nascimento de menor de idade
    /// </summary>
    public static DateTime GenerateMinorBirthDate() =>
        DateTime.UtcNow.AddYears(-17).AddDays(1);
}