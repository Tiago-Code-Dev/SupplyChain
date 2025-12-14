using FluentValidation;

namespace EmployeeManagement.Application.Features.Employees.Commands.CreateEmployee;

public sealed class CreateEmployeeCommandValidator : AbstractValidator<CreateEmployeeCommand>
{
    public CreateEmployeeCommandValidator()
    {
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

        RuleFor(x => x.DocumentNumber)
            .NotEmpty().WithMessage("Document number is required")
            .MaximumLength(20).WithMessage("Document number must not exceed 20 characters");

        RuleFor(x => x.BirthDate)
            .NotEmpty().WithMessage("Birth date is required")
            .Must(BeAtLeast18YearsOld).WithMessage("Employee must be at least 18 years old");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters")
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit")
            .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character");

        RuleFor(x => x.PhoneNumbers)
            .NotEmpty().WithMessage("At least one phone number is required");

        RuleFor(x => x.Role)
            .IsInEnum().WithMessage("Invalid role");
    }

    private static bool BeAtLeast18YearsOld(DateTime birthDate) => 
        DateTime.UtcNow.AddYears(-18) >= birthDate;
}