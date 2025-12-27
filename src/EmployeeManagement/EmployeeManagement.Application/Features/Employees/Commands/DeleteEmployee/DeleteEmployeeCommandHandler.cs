using EmployeeManagement.Application.Common;
using EmployeeManagement.Application.Common.Interfaces;
using EmployeeManagement.Application.Interfaces;
using EmployeeManagement.Application.Resources;
using EmployeeManagement.Domain.Common;
using EmployeeManagement.Domain.Enums;
using EmployeeManagement.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace EmployeeManagement.Application.Features.Employees.Commands.DeleteEmployee;

public class DeleteEmployeeCommandHandler : ICommandHandler<DeleteEmployeeCommand>
{
    private readonly IEmployeeRepository _repository;
    private readonly ICacheService _cache;
    private readonly ILogger<DeleteEmployeeCommandHandler> _logger;

    public DeleteEmployeeCommandHandler(
        IEmployeeRepository repository,
        ICacheService cache,
        ILogger<DeleteEmployeeCommandHandler> logger)
    {
        _repository = repository;
        _cache = cache;
        _logger = logger;
    }

    public async Task<Result> Handle(
        DeleteEmployeeCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting employee: {Id} by user with role: {Role}", 
            request.Id, request.CurrentUserRole);

        // Employee não pode deletar ninguém
        if (request.CurrentUserRole < Role.Leader)
        {
            _logger.LogWarning("User with role {Role} tried to delete employee {Id} without permission", 
                request.CurrentUserRole, request.Id);
            return Result.Failure(
                Error.Forbidden(ValidationMessages.NoPermissionToDelete));
        }

        // Buscar para validações
        var employee = await _repository.GetByIdForDeleteAsync(request.Id, cancellationToken);
        if (employee is null)
        {
            return Result.Failure(Error.NotFound("Employee", ValidationMessages.EmployeeNotFound));
        }

        // Validação de permissões para exclusão baseada na hierarquia
        // Leader (2): Só pode deletar Employee (1)
        // Director (3): Pode deletar Leader (2), Employee (1) - NÃO pode deletar Director
        // Admin (4): Pode deletar qualquer role
        var canDelete = request.CurrentUserRole switch
        {
            Role.Admin => true, // Admin pode deletar qualquer role
            Role.Director => employee.Role < Role.Director, // Director pode deletar Leader e Employee
            Role.Leader => employee.Role == Role.Employee, // Leader só pode deletar Employee
            _ => false // Employee não pode deletar ninguém
        };

        if (!canDelete)
        {
            _logger.LogWarning("User with role {UserRole} tried to delete employee with role {TargetRole}", 
                request.CurrentUserRole, employee.Role);
            return Result.Failure(
                Error.Forbidden(ValidationMessages.NoPermissionToDelete));
        }

        var hasSubordinates = await _repository.HasSubordinatesAsync(request.Id, cancellationToken);
        if (hasSubordinates)
        {
            _logger.LogWarning("Cannot delete employee {Id} because they have subordinates", request.Id);
            return Result.Failure(
                Error.Validation("Employee", ValidationMessages.CannotDeleteWithSubordinates));
        }

        // Soft delete direto no banco - evita problemas de tracking
        await _repository.SoftDeleteAsync(request.Id, cancellationToken);

        // Limpar cache
        await _cache.RemoveAsync(CacheKeys.Employee(request.Id), cancellationToken);
        await _cache.RemoveAsync(CacheKeys.EmployeeByEmail(employee.Email), cancellationToken);
        await _cache.RemoveAsync(CacheKeys.AllEmployees, cancellationToken);

        _logger.LogInformation("Employee deleted: {Id}, Email: {Email}, DeletedByRole: {Role}", 
            request.Id, employee.Email, request.CurrentUserRole);

        return Result.Success();
    }
}
