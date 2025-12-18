using MediatR;

namespace EmployeeManagement.Application.Common.Interfaces;

/// <summary>
/// Marker interface para Queries
/// </summary>
public interface IQuery<TResponse> : IRequest<TResponse>
{
}

/// <summary>
/// Handler para Queries
/// </summary>
public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
}