using EmployeeManagement.Application.Common;
using EmployeeManagement.Application.Common.Interfaces;
using EmployeeManagement.Application.Features.Employees.Common;
using EmployeeManagement.Application.Interfaces;
using EmployeeManagement.Application.Resources;
using EmployeeManagement.Domain.Common;
using EmployeeManagement.Domain.Entities;
using EmployeeManagement.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace EmployeeManagement.Application.Features.Employees.Commands.UpdateEmployee;

public sealed class UpdateEmployeeCommandHandler 
    : ICommandHandler<UpdateEmployeeCommand, EmployeeResponse>
{
    private readonly IEmployeeRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cache;
    private readonly ILogger<UpdateEmployeeCommandHandler> _logger;

    public UpdateEmployeeCommandHandler(
        IEmployeeRepository repository,
        IUnitOfWork unitOfWork,
        ICacheService cache,
        ILogger<UpdateEmployeeCommandHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _cache = cache;
        _logger = logger;
    }

    public async Task<Result<EmployeeResponse>> Handle(
        UpdateEmployeeCommand request, 
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating employee: {Id}", request.Id);

        var employee = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (employee is null)
        {
            return Result<EmployeeResponse>.Failure(
                Error.NotFound("Employee", ValidationMessages.EmployeeNotFound));
        }

        // Guardar email antigo para invalidar cache depois
        var oldEmail = employee.Email;

        // Verificar se email está sendo alterado e é único (excluindo o próprio funcionário)
        if (!employee.Email.Equals(request.Email, StringComparison.OrdinalIgnoreCase))
        {
            if (await _repository.EmailExistsAsync(request.Email, request.Id, cancellationToken))
            {
                _logger.LogWarning("Email {Email} already exists for another employee", request.Email);
                return Result<EmployeeResponse>.Failure(
                    Error.Conflict("Email", ValidationMessages.EmailAlreadyExists));
            }
        }

        // Validar manager
        if (request.ManagerId.HasValue)
        {
            if (request.ManagerId.Value == request.Id)
            {
                return Result<EmployeeResponse>.Failure(
                    Error.Validation("ManagerId", ValidationMessages.CannotBeSelfManager));
            }

            var managerExists = await _repository.ExistsAsync(request.ManagerId.Value, cancellationToken);
            if (!managerExists)
            {
                _logger.LogWarning("Manager {ManagerId} not found", request.ManagerId.Value);
                return Result<EmployeeResponse>.Failure(
                    Error.NotFound("Manager", ValidationMessages.ManagerNotFound));
            }
        }

        // Atualizar usando método do domínio
        var updateResult = employee.Update(
            request.FirstName, 
            request.LastName, 
            request.Email, 
            request.BirthDate, 
            request.ManagerId);

        if (updateResult.IsFailure)
        {
            return Result<EmployeeResponse>.Failure(updateResult.Error);
        }

        // Atualizar telefones
        employee.ClearPhones();
        foreach (var phone in request.PhoneNumbers)
        {
            employee.AddPhone(new PhoneNumber(phone, employee.Id));
        }

        await _repository.UpdateAsync(employee, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Invalidar cache do funcionário específico
        await _cache.RemoveAsync(CacheKeys.Employee(request.Id), cancellationToken);
        
        // Invalidar cache de email antigo se foi alterado
        if (!oldEmail.Equals(request.Email, StringComparison.OrdinalIgnoreCase))
        {
            await _cache.RemoveAsync(CacheKeys.EmployeeByEmail(oldEmail), cancellationToken);
        }
        await _cache.RemoveAsync(CacheKeys.EmployeeByEmail(request.Email), cancellationToken);
        
        // Invalidar cache de listagem
        await _cache.RemoveAsync(CacheKeys.AllEmployees, cancellationToken);

        _logger.LogInformation("Employee updated successfully: {Id}", request.Id);

        return Result<EmployeeResponse>.Success(EmployeeResponse.FromEntity(employee));
    }
}