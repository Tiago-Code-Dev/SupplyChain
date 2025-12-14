using FluentValidation;

namespace EmployeeManagement.Application.Features.Employees.Commands.UpdateEmployee;

public sealed class UpdateEmployeeCommandValidator : AbstractValidator<UpdateEmployeeCommand>
{
    public UpdateEmployeeCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Employee ID is required");

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required")
            .MaximumLength(100).WithMessage("First name must not exceed 100 characters");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required")
            .MaximumLength(100).WithMessage("Last name must not exceed 100 characters");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format")
            .MaximumLength(255).WithMessage("Email must not exceed 255 characters");

        RuleFor(x => x.BirthDate)
            .NotEmpty().WithMessage("Birth date is required")
            .Must(BeAtLeast18YearsOld).WithMessage("Employee must be at least 18 years old");

        RuleFor(x => x.PhoneNumbers)
            .NotEmpty().WithMessage("At least one phone number is required");

        RuleFor(x => x.ManagerId)
            .NotEqual(x => x.Id)
            .When(x => x.ManagerId.HasValue)
            .WithMessage("Employee cannot be their own manager");
    }

    private static bool BeAtLeast18YearsOld(DateTime birthDate) =>
        DateTime.UtcNow.AddYears(-18) >= birthDate;
}