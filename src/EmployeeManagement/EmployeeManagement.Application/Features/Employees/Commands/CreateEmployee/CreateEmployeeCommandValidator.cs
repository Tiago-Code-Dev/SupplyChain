using EmployeeManagement.Application.Resources;
using FluentValidation;

namespace EmployeeManagement.Application.Features.Employees.Commands.CreateEmployee;

public sealed class CreateEmployeeCommandValidator : AbstractValidator<CreateEmployeeCommand>
{
    public CreateEmployeeCommandValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage(ValidationMessages.FirstNameRequired)
            .MaximumLength(100).WithMessage(ValidationMessages.FirstNameMaxLength.Replace("{MaxLength}", "100"));

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage(ValidationMessages.LastNameRequired)
            .MaximumLength(100).WithMessage(ValidationMessages.LastNameMaxLength.Replace("{MaxLength}", "100"));

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage(ValidationMessages.EmailRequired)
            .EmailAddress().WithMessage(ValidationMessages.EmailInvalid)
            .MaximumLength(255).WithMessage(ValidationMessages.EmailMaxLength.Replace("{MaxLength}", "255"));

        RuleFor(x => x.DocumentNumber)
            .NotEmpty().WithMessage(ValidationMessages.DocumentRequired)
            .MaximumLength(20).WithMessage(ValidationMessages.DocumentMaxLength.Replace("{MaxLength}", "20"))
            .Matches(@"^\d{11}$|^\d{14}$").WithMessage(ValidationMessages.DocumentInvalidFormat);

        RuleFor(x => x.BirthDate)
            .NotEmpty().WithMessage(ValidationMessages.BirthDateRequired)
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

    private static bool BeAtLeast18YearsOld(DateTime birthDate) => 
        birthDate <= DateTime.UtcNow.AddYears(-18);
}