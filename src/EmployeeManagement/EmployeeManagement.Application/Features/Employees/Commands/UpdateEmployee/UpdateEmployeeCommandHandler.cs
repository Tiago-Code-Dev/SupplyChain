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
    private readonly IIdentityService _identityService;
    private readonly ILogger<UpdateEmployeeCommandHandler> _logger;

    public UpdateEmployeeCommandHandler(
        IEmployeeRepository repository,
        IUnitOfWork unitOfWork,
        ICacheService cache,
        IIdentityService identityService,
        ILogger<UpdateEmployeeCommandHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _cache = cache;
        _identityService = identityService;
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
        var oldRole = employee.Role;

        if (request.NewRole.HasValue && request.NewRole.Value != employee.Role)
        {
            // Admin é o nível máximo - pode atualizar qualquer role
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
                    Error.Validation("Manager", ValidationMessages.ManagerNotFound));
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

            // Atualizar CustomRole se fornecido
            if (request.CustomRoleId.HasValue || request.NewRole.HasValue)
            {
                var newRole = request.NewRole ?? employee.Role;
                var customRoleUpdateResult = employee.UpdateCustomRole(request.CustomRoleId, newRole);
                if (customRoleUpdateResult.IsFailure)
                {
                    return Result<EmployeeResponse>.Failure(customRoleUpdateResult.Error);
                }

                _logger.LogInformation(
                    "Employee {Id} role updated to {NewRole} with CustomRoleId {CustomRoleId}",
                    request.Id, newRole, request.CustomRoleId);
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

                // Sincronizar mudança de role com o Identity se houver alteração
                if (employee.Role != oldRole)
                {
                    var identityUser = await _identityService.GetUserByEmailAsync(employee.Email, cancellationToken);
                    if (identityUser != null)
                    {
                        // Remover a role antiga
                        var removeResult = await _identityService.RemoveFromRoleAsync(identityUser.Id, oldRole.ToString(), cancellationToken);
                        if (removeResult.IsFailure)
                        {
                            _logger.LogWarning("Failed to remove old role {OldRole} from user {UserId}: {Error}",
                                oldRole, identityUser.Id, removeResult.Error.Description);
                        }

                        // Adicionar a nova role
                        var addResult = await _identityService.AddToRoleAsync(identityUser.Id, employee.Role.ToString(), cancellationToken);
                        if (addResult.IsFailure)
                        {
                            _logger.LogWarning("Failed to add new role {NewRole} to user {UserId}: {Error}",
                                employee.Role, identityUser.Id, addResult.Error.Description);
                        }
                        else
                        {
                            _logger.LogInformation("Identity role updated for user {UserId}: {OldRole} -> {NewRole}",
                                identityUser.Id, oldRole, employee.Role);
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Identity user not found for employee {EmployeeId} with email {Email}",
                            employee.Id, employee.Email);
                    }
                }

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