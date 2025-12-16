using EmployeeManagement.Application.Resources;
using FluentValidation;

namespace EmployeeManagement.Application.Features.Employees.Commands.UpdateEmployee;

public sealed class UpdateEmployeeCommandValidator : AbstractValidator<UpdateEmployeeCommand>
{
    public UpdateEmployeeCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage(ValidationMessages.EmployeeIdRequired);

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

        RuleFor(x => x.BirthDate)
            .NotEmpty().WithMessage(ValidationMessages.BirthDateRequired)
            .Must(BeAtLeast18YearsOld).WithMessage(ValidationMessages.EmployeeMustBeAdult);

        // Validação de telefones - deve ter pelo menos um
        RuleFor(x => x.PhoneNumbers)
            .NotNull().WithMessage(ValidationMessages.PhoneNumbersRequired)
            .Must(phones => phones != null && phones.Count > 0)
            .WithMessage(ValidationMessages.AtLeastOnePhoneRequired);

        // Validação de cada telefone na lista - formato brasileiro
        RuleForEach(x => x.PhoneNumbers)
            .NotEmpty().WithMessage(ValidationMessages.PhoneNumberEmpty)
            .Matches(@"^\d{10,11}$").WithMessage(ValidationMessages.PhoneNumberInvalidFormat);

        // Validação de manager - não pode ser o próprio funcionário
        RuleFor(x => x.ManagerId)
            .Must((command, managerId) => !managerId.HasValue || managerId.Value != command.Id)
            .WithMessage(ValidationMessages.CannotBeSelfManager);
    }

    private static bool BeAtLeast18YearsOld(DateTime birthDate) =>
        birthDate <= DateTime.UtcNow.AddYears(-18);
}