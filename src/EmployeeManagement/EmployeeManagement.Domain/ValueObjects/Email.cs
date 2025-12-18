using EmployeeManagement.Domain.Common;
using System.Text.RegularExpressions;

namespace EmployeeManagement.Domain.ValueObjects;

public sealed partial class Email : IEquatable<Email>
{
    public string Value { get; }

    private Email(string value) => Value = value;

    public static Result<Email> Create(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return Result<Email>.Failure(Error.Validation("Email", "Email is required"));

        var normalizedEmail = email.Trim().ToLowerInvariant();

        if (normalizedEmail.Length > 255)
            return Result<Email>.Failure(Error.Validation("Email", "Email must not exceed 255 characters"));

        if (!EmailRegex().IsMatch(normalizedEmail))
            return Result<Email>.Failure(Error.Validation("Email", "Invalid email format"));

        return Result<Email>.Success(new Email(normalizedEmail));
    }

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled)]
    private static partial Regex EmailRegex();

    public override string ToString() => Value;

    public bool Equals(Email? other) => 
        other is not null && Value.Equals(other.Value, StringComparison.OrdinalIgnoreCase);

    public override bool Equals(object? obj) => 
        obj is Email email && Equals(email);

    public override int GetHashCode() => 
        Value.ToLowerInvariant().GetHashCode();

    public static implicit operator string(Email email) => email.Value;
}