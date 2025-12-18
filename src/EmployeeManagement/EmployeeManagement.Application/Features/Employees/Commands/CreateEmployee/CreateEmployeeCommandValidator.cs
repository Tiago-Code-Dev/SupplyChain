using EmployeeManagement.Application.Resources;
using FluentValidation;

namespace EmployeeManagement.Application.Features.Employees.Commands.CreateEmployee;

public sealed class CreateEmployeeCommandValidator : AbstractValidator<CreateEmployeeCommand>
{
    private const int MinimumBirthYear = 1900;
    private const int MinNameLength = 2;
    private const int MaxNameLength = 100;
    
    public CreateEmployeeCommandValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage(ValidationMessages.FirstNameRequired)
            .MinimumLength(MinNameLength).WithMessage(ValidationMessages.FirstNameMinLength.Replace("{MinLength}", MinNameLength.ToString()))
            .MaximumLength(MaxNameLength).WithMessage(ValidationMessages.FirstNameMaxLength.Replace("{MaxLength}", MaxNameLength.ToString()))
            .Must(NotContainNumbersOrSpecialChars).WithMessage(ValidationMessages.FirstNameInvalidCharacters);

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage(ValidationMessages.LastNameRequired)
            .MinimumLength(MinNameLength).WithMessage(ValidationMessages.LastNameMinLength.Replace("{MinLength}", MinNameLength.ToString()))
            .MaximumLength(MaxNameLength).WithMessage(ValidationMessages.LastNameMaxLength.Replace("{MaxLength}", MaxNameLength.ToString()))
            .Must(NotContainNumbersOrSpecialChars).WithMessage(ValidationMessages.LastNameInvalidCharacters);

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage(ValidationMessages.EmailRequired)
            .Must(NotContainSpaces).WithMessage(ValidationMessages.EmailContainsSpaces)
            .EmailAddress().WithMessage(ValidationMessages.EmailInvalid)
            .MaximumLength(255).WithMessage(ValidationMessages.EmailMaxLength.Replace("{MaxLength}", "255"));

        RuleFor(x => x.DocumentNumber)
            .NotEmpty().WithMessage(ValidationMessages.DocumentRequired)
            .MaximumLength(20).WithMessage(ValidationMessages.DocumentMaxLength.Replace("{MaxLength}", "20"))
            .Matches(@"^\d{11}$|^\d{14}$").WithMessage(ValidationMessages.DocumentInvalidFormat)
            .Must(NotHaveAllEqualDigits).WithMessage(ValidationMessages.DocumentAllDigitsEqual);

        RuleFor(x => x.BirthDate)
            .NotEmpty().WithMessage(ValidationMessages.BirthDateRequired)
            .Must(NotBeInFuture).WithMessage(ValidationMessages.BirthDateInFuture)
            .Must(NotBeTooOld).WithMessage(ValidationMessages.BirthDateTooOld.Replace("{MinYear}", MinimumBirthYear.ToString()))
            .Must(BeAtLeast18YearsOld).WithMessage(ValidationMessages.EmployeeMustBeAdult);

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage(ValidationMessages.PasswordRequired)
            .MinimumLength(8).WithMessage(ValidationMessages.PasswordMinLength.Replace("{MinLength}", "8"))
            .Matches("[A-Z]").WithMessage(ValidationMessages.PasswordUppercase)
            .Matches("[a-z]").WithMessage(ValidationMessages.PasswordLowercase)
            .Matches("[0-9]").WithMessage(ValidationMessages.PasswordDigit)
            .Matches("[^a-zA-Z0-9]").WithMessage(ValidationMessages.PasswordSpecialChar);

        // Validação de telefones - deve ter pelo menos um
        RuleFor(x => x.PhoneNumbers)
            .NotNull().WithMessage(ValidationMessages.PhoneNumbersRequired)
            .Must(phones => phones != null && phones.Count > 0)
            .WithMessage(ValidationMessages.AtLeastOnePhoneRequired);

        // Validação de cada telefone na lista - formato brasileiro
        RuleForEach(x => x.PhoneNumbers)
            .NotEmpty().WithMessage(ValidationMessages.PhoneNumberEmpty)
            .Matches(@"^\d{10,11}$").WithMessage(ValidationMessages.PhoneNumberInvalidFormat);

        RuleFor(x => x.Role)
            .IsInEnum().WithMessage(ValidationMessages.RoleInvalid);
    }

    /// <summary>
    /// Valida se a data de nascimento indica pelo menos 18 anos de idade
    /// </summary>
    private static bool BeAtLeast18YearsOld(DateTime birthDate) => 
        birthDate <= DateTime.UtcNow.AddYears(-18);

    /// <summary>
    /// Valida se a data de nascimento não é anterior ao ano mínimo permitido (1900)
    /// </summary>
    private static bool NotBeTooOld(DateTime birthDate) => 
        birthDate.Year >= MinimumBirthYear;

    /// <summary>
    /// Valida se a data de nascimento não está no futuro
    /// </summary>
    private static bool NotBeInFuture(DateTime birthDate) => 
        birthDate.Date <= DateTime.UtcNow.Date;

    /// <summary>
    /// Valida se o documento não possui todos os dígitos iguais (ex: 11111111111)
    /// </summary>
    private static bool NotHaveAllEqualDigits(string? document)
    {
        if (string.IsNullOrWhiteSpace(document))
            return true; // Deixa a validação de obrigatoriedade para o NotEmpty

        var digits = document.Where(char.IsDigit).ToArray();
        if (digits.Length == 0)
            return true;

        return !digits.All(d => d == digits[0]);
    }

    /// <summary>
    /// Valida se o email não contém espaços
    /// </summary>
    private static bool NotContainSpaces(string? email) => 
        string.IsNullOrEmpty(email) || !email.Contains(' ');

    /// <summary>
    /// Valida se o nome/sobrenome contém apenas letras, espaços e caracteres de acentuação
    /// </summary>
    private static bool NotContainNumbersOrSpecialChars(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return true; // Deixa a validação de obrigatoriedade para o NotEmpty

        // Permite letras (incluindo acentuadas), espaços, hífens e apóstrofos (ex: O'Connor, Anne-Marie)
        return name.All(c => char.IsLetter(c) || c == ' ' || c == '-' || c == '\'');
    }
}