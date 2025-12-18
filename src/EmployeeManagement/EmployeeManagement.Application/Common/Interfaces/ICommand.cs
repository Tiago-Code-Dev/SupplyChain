using EmployeeManagement.Domain.Common;
using MediatR;

namespace EmployeeManagement.Application.Common.Interfaces;

/// <summary>
/// Command sem retorno de valor (apenas Result)
/// </summary>
public interface ICommand : IRequest<Result>;

/// <summary>
/// Command com retorno de valor tipado
/// </summary>
public interface ICommand<TResponse> : IRequest<Result<TResponse>>;

/// <summary>
/// Handler para commands sem valor de retorno
/// </summary>
public interface ICommandHandler<in TCommand> : IRequestHandler<TCommand, Result>
    where TCommand : ICommand;

/// <summary>
/// Handler para commands com valor de retorno
/// </summary>
public interface ICommandHandler<in TCommand, TResponse> : IRequestHandler<TCommand, Result<TResponse>>
    where TCommand : ICommand<TResponse>;