namespace EmployeeManagement.Domain.Common;

/// <summary>
/// Representa o resultado de uma operação sem valor de retorno
/// </summary>
public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error Error { get; }

    protected Result(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None)
            throw new InvalidOperationException("Success result cannot have an error");

        if (!isSuccess && error == Error.None)
            throw new InvalidOperationException("Failure result must have an error");

        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Success() => new(true, Error.None);
    public static Result Failure(Error error) => new(false, error);

    public static Result<TValue> Success<TValue>(TValue value) => Result<TValue>.Success(value);
    public static Result<TValue> Failure<TValue>(Error error) => Result<TValue>.Failure(error);
}

/// <summary>
/// Representa o resultado de uma operação com valor de retorno tipado
/// </summary>
public class Result<TValue> : Result
{
    private readonly TValue? _value;

    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access value of a failed result");

    private Result(TValue? value, bool isSuccess, Error error)
        : base(isSuccess, error)
    {
        _value = value;
    }

    public static Result<TValue> Success(TValue value) => new(value, true, Error.None);
    public new static Result<TValue> Failure(Error error) => new(default, false, error);

    public TResult Match<TResult>(
        Func<TValue, TResult> onSuccess,
        Func<Error, TResult> onFailure) =>
        IsSuccess ? onSuccess(Value) : onFailure(Error);

    public static implicit operator Result<TValue>(TValue value) => Success(value);
}

/// <summary>
/// Representa um erro de domínio
/// </summary>
public sealed record Error(string Code, string Description)
{
    public static readonly Error None = new(string.Empty, string.Empty);

    // Factory methods para erros comuns
    public static Error NotFound(string entity, Guid id) =>
        new($"{entity}.NotFound", $"{entity} with ID '{id}' was not found");

    public static Error NotFound(string entity, string identifier) =>
        new($"{entity}.NotFound", $"{entity} '{identifier}' was not found");

    public static Error Conflict(string entity, string message) =>
        new($"{entity}.Conflict", message);

    public static Error Validation(string field, string message) =>
        new($"{field}.Validation", message);

    public static Error Forbidden(string message) =>
        new("Authorization.Forbidden", message);

    public static Error Unauthorized(string message) =>
        new("Authorization.Unauthorized", message);
}