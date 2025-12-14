using EmployeeManagement.Application.Common;
using EmployeeManagement.Application.Common.Interfaces;
using EmployeeManagement.Application.Interfaces;
using EmployeeManagement.Domain.Common;
using EmployeeManagement.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace EmployeeManagement.Application.Features.Employees.Commands.DeleteEmployee;

public sealed class DeleteEmployeeCommandHandler : ICommandHandler<DeleteEmployeeCommand>
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
        _logger.LogInformation("Deleting employee: {Id}", request.Id);

        var employee = await _repository.GetByIdAsync(request.Id, cancellationToken);

        if (employee is null)
        {
            return Result.Failure(Error.NotFound("Employee", request.Id));
        }

        employee.Delete();
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Invalidar cache
        await _cache.RemoveAsync(CacheKeys.Employee(request.Id), cancellationToken);
        await _cache.RemoveAsync(CacheKeys.AllEmployees, cancellationToken);

        _logger.LogInformation("Employee deleted successfully: {Id}", request.Id);

        return Result.Success();
    }
}