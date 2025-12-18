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
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cache;
    private readonly ILogger<DeleteEmployeeCommandHandler> _logger;

    public DeleteEmployeeCommandHandler(
        IEmployeeRepository repository,
        IUnitOfWork unitOfWork,
        ICacheService cache,
        ILogger<DeleteEmployeeCommandHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _cache = cache;
        _logger = logger;
    }

    public async Task<Result> Handle(
        DeleteEmployeeCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting employee: {Id} by user with role: {Role}", 
            request.Id, request.CurrentUserRole);

        // Valida��o de autoriza��o - apenas Leader e Director podem excluir
        if (request.CurrentUserRole < Role.Leader)
        {
            _logger.LogWarning("User with role {Role} tried to delete employee {Id} without permission", 
                request.CurrentUserRole, request.Id);
            return Result.Failure(
                Error.Forbidden(ValidationMessages.NoPermissionToDelete));
        }

        // Buscar funcion�rio para obter email (para invalidar cache)
        var employee = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (employee is null)
        {
            return Result.Failure(Error.NotFound("Employee", ValidationMessages.EmployeeNotFound));
        }

        // Verificar se funcion�rio possui subordinados
        var hasSubordinates = await _repository.HasSubordinatesAsync(request.Id, cancellationToken);
        if (hasSubordinates)
        {
            _logger.LogWarning("Cannot delete employee {Id} because they have subordinates", request.Id);
            return Result.Failure(
                Error.Validation("Employee", ValidationMessages.CannotDeleteWithSubordinates));
        }

        await _repository.DeleteAsync(request.Id, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Invalidar cache
        await _cache.RemoveAsync(CacheKeys.Employee(request.Id), cancellationToken);
        await _cache.RemoveAsync(CacheKeys.EmployeeByEmail(employee.Email), cancellationToken);
        await _cache.RemoveAsync(CacheKeys.AllEmployees, cancellationToken);

        _logger.LogInformation("Employee deleted: {Id}, Email: {Email}, DeletedByRole: {Role}", 
            request.Id, employee.Email, request.CurrentUserRole);

        return Result.Success();
    }
}
