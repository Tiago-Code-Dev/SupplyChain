using EmployeeManagement.Domain.Common;
using System.Text.RegularExpressions;

namespace EmployeeManagement.Domain.ValueObjects;

public sealed partial class DocumentNumber : IEquatable<DocumentNumber>
{
    public string Value { get; }

    private DocumentNumber(string value) => Value = value;

    public static Result<DocumentNumber> Create(string? document)
    {
        if (string.IsNullOrWhiteSpace(document))
            return Result<DocumentNumber>.Failure(
                Error.Validation("DocumentNumber", "Document number is required"));

        // Remove caracteres especiais para normalização
        var normalized = OnlyDigitsRegex().Replace(document, string.Empty);

        if (normalized.Length < 5 || normalized.Length > 20)
            return Result<DocumentNumber>.Failure(
                Error.Validation("DocumentNumber", "Document number must be between 5 and 20 digits"));

        return Result<DocumentNumber>.Success(new DocumentNumber(normalized));
    }

    [GeneratedRegex(@"[^\d]", RegexOptions.Compiled)]
    private static partial Regex OnlyDigitsRegex();

    public override string ToString() => Value;

    public bool Equals(DocumentNumber? other) => 
        other is not null && Value.Equals(other.Value);

    public override bool Equals(object? obj) => 
        obj is DocumentNumber doc && Equals(doc);

    public override int GetHashCode() => Value.GetHashCode();

    public static implicit operator string(DocumentNumber doc) => doc.Value;
}