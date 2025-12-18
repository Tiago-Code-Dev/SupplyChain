using EmployeeManagement.Application.Common;
using EmployeeManagement.Application.Common.Interfaces;
using EmployeeManagement.Application.Features.Employees.Common;
using EmployeeManagement.Application.Interfaces;
using EmployeeManagement.Application.Resources;
using EmployeeManagement.Domain.Common;
using EmployeeManagement.Domain.Entities;
using EmployeeManagement.Domain.Enums;
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

        var oldEmail = employee.Email;

        if (request.NewRole.HasValue && request.NewRole.Value != employee.Role)
        {
            if (request.CurrentUserRole != Role.Admin)
            {
                if (request.CurrentUserRole <= request.NewRole.Value)
                {
                    _logger.LogWarning(
                        "User with role {CurrentRole} tried to update employee {Id} to role {TargetRole}",
                        request.CurrentUserRole, request.Id, request.NewRole.Value);
                    return Result<EmployeeResponse>.Failure(
                        Error.Forbidden(ValidationMessages.CannotUpdateToHigherRole));
                }

                if (request.CurrentUserRole <= employee.Role)
                {
                    _logger.LogWarning(
                        "User with role {CurrentRole} tried to update role of employee {Id} with role {EmployeeRole}",
                        request.CurrentUserRole, request.Id, employee.Role);
                    return Result<EmployeeResponse>.Failure(
                        Error.Forbidden(ValidationMessages.CannotUpdateHigherRoleEmployee));
                }
            }
        }

        if (!employee.Email.Equals(request.Email, StringComparison.OrdinalIgnoreCase))
        {
            if (await _repository.EmailExistsAsync(request.Email, request.Id, cancellationToken))
            {
                _logger.LogWarning("Email {Email} already exists for another employee", request.Email);
                return Result<EmployeeResponse>.Failure(
                    Error.Conflict("Email", ValidationMessages.EmailAlreadyExists));
            }
        }

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

        if (request.NewRole.HasValue && request.NewRole.Value != employee.Role)
        {
            var roleUpdateResult = employee.UpdateRole(request.NewRole.Value);
            if (roleUpdateResult.IsFailure)
            {
                return Result<EmployeeResponse>.Failure(roleUpdateResult.Error);
            }
            
            _logger.LogInformation(
                "Employee {Id} role updated from {OldRole} to {NewRole}",
                request.Id, employee.Role, request.NewRole.Value);
        }

        if (request.PhoneNumbers == null || !request.PhoneNumbers.Any())
        {
            return Result<EmployeeResponse>.Failure(
                Error.Validation("PhoneNumbers", "Funcionário deve possuir pelo menos um telefone"));
        }

        employee.ClearPhones();
        foreach (var phone in request.PhoneNumbers)
        {
            employee.AddPhone(new PhoneNumber(phone, employee.Id));
        }

        await _repository.UpdateAsync(employee, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _cache.RemoveAsync(CacheKeys.Employee(request.Id), cancellationToken);
        if (!oldEmail.Equals(employee.Email, StringComparison.OrdinalIgnoreCase))
        {
            await _cache.RemoveAsync(CacheKeys.EmployeeByEmail(oldEmail), cancellationToken);
        }
        await _cache.RemoveAsync(CacheKeys.EmployeeByEmail(employee.Email), cancellationToken);
        await _cache.RemoveAsync(CacheKeys.AllEmployees, cancellationToken);

        _logger.LogInformation("Employee updated successfully: {Id}", employee.Id);

        return Result<EmployeeResponse>.Success(EmployeeResponse.FromEntity(employee));
    }
}