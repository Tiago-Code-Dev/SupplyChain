using EmployeeManagement.Application.Common;
using EmployeeManagement.Application.Common.Interfaces;
using EmployeeManagement.Application.Interfaces;
using EmployeeManagement.Domain.Common;
using EmployeeManagement.Domain.Enums;
using EmployeeManagement.Domain.Interfaces;
using MediatR;
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

    public async Task<Result> Handle(DeleteEmployeeCommand request, CancellationToken cancellationToken)
    {
        var employee = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (employee == null)
        {
            return Result.Failure(Error.NotFound("Employee", "Funcionário não encontrado"));
        }

        if (request.CurrentUserRole == Role.Employee)
        {
            return Result.Failure(Error.Forbidden("Você não tem permissão para excluir funcionários"));
        }

        employee.Delete(); 
        

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _cache.RemoveAsync(CacheKeys.Employee(request.Id), cancellationToken);
        await _cache.RemoveAsync(CacheKeys.EmployeeByEmail(employee.Email), cancellationToken);
        await _cache.RemoveAsync(CacheKeys.AllEmployees, cancellationToken);

        _logger.LogInformation("Employee deleted: {Id}, Email: {Email}, DeletedByRole: {Role}", 
            request.Id, employee.Email, request.CurrentUserRole);

        return Result.Success();
    }
}
