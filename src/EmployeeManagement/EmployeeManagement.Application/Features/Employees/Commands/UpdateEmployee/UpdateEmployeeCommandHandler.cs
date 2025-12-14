using EmployeeManagement.Application.Common.Interfaces;
using EmployeeManagement.Application.Features.Employees.Common;
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
    private readonly ILogger<UpdateEmployeeCommandHandler> _logger;

    public UpdateEmployeeCommandHandler(
        IEmployeeRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<UpdateEmployeeCommandHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
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
                Error.NotFound("Employee", request.Id));
        }

        // Verificar se email está sendo alterado e é único
        if (!employee.Email.Equals(request.Email, StringComparison.OrdinalIgnoreCase))
        {
            var existingByEmail = await _repository.GetByEmailAsync(request.Email, cancellationToken);
            if (existingByEmail is not null)
            {
                return Result<EmployeeResponse>.Failure(
                    Error.Conflict("Email", "Email already exists"));
            }
        }

        // Validar manager
        if (request.ManagerId.HasValue)
        {
            if (request.ManagerId.Value == request.Id)
            {
                return Result<EmployeeResponse>.Failure(
                    Error.Validation("ManagerId", "Employee cannot be their own manager"));
            }

            var manager = await _repository.GetByIdAsync(request.ManagerId.Value, cancellationToken);
            if (manager is null)
            {
                return Result<EmployeeResponse>.Failure(
                    Error.NotFound("Manager", request.ManagerId.Value));
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

        _logger.LogInformation("Employee updated successfully: {Id}", request.Id);

        return Result<EmployeeResponse>.Success(EmployeeResponse.FromEntity(employee));
    }
}