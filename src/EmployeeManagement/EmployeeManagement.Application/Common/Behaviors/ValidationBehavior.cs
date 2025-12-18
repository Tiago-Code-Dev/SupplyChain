using EmployeeManagement.Domain.Common;
using FluentValidation;
using MediatR;

namespace EmployeeManagement.Application.Common.Behaviors;

public sealed class ValidationBehavior<TRequest, TResponse> 
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
        {
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);

        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count == 0)
        {
            return await next();
        }

        // Retornar Result.Failure se TResponse é um Result
        var errorMessage = string.Join("; ", failures.Select(f => f.ErrorMessage));
        var error = Error.Validation("Validation", errorMessage);

        // Se é Result sem tipo
        if (typeof(TResponse) == typeof(Result))
        {
            return (TResponse)(object)Result.Failure(error);
        }

        // Se é Result<T>
        if (typeof(TResponse).IsGenericType && 
            typeof(TResponse).GetGenericTypeDefinition() == typeof(Result<>))
        {
            var resultType = typeof(TResponse).GetGenericArguments()[0];
            var failureMethod = typeof(Result<>)
                .MakeGenericType(resultType)
                .GetMethod(nameof(Result<object>.Failure), [typeof(Error)])!;

            return (TResponse)failureMethod.Invoke(null, [error])!;
        }

        // Fallback: lançar exceção para outros tipos
        throw new ValidationException(failures);
    }
}