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

namespace EmployeeManagement.Application.Features.Employees.Commands.CreateEmployee;

public sealed class CreateEmployeeCommandHandler 
    : ICommandHandler<CreateEmployeeCommand, EmployeeResponse>
{
    private readonly IEmployeeRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ICacheService _cache;
    private readonly ILogger<CreateEmployeeCommandHandler> _logger;

    public CreateEmployeeCommandHandler(
        IEmployeeRepository repository,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        ICacheService cache,
        ILogger<CreateEmployeeCommandHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _cache = cache;
        _logger = logger;
    }

    public async Task<Result<EmployeeResponse>> Handle(
        CreateEmployeeCommand request, 
        CancellationToken cancellationToken)
    {

        _logger.LogInformation("Creating employee with email: {Email}", request.Email);

        if (request.PhoneNumbers == null || !request.PhoneNumbers.Any())
        {
            return Result<EmployeeResponse>.Failure(
                Error.Validation("PhoneNumbers", "Funcionário deve possuir pelo menos um telefone"));
        }

        // Director é o nível máximo - pode criar qualquer role
        if (request.CurrentUserRole != Role.Director && request.CurrentUserRole <= request.Role)
        {
            return Result<EmployeeResponse>.Failure(
                Error.Forbidden(ValidationMessages.CannotCreateHigherRole));
        }

        if (await _repository.EmailExistsAsync(request.Email, cancellationToken: cancellationToken))
        {
            _logger.LogWarning("Email {Email} already exists", request.Email);
            return Result<EmployeeResponse>.Failure(
                Error.Conflict("Email", ValidationMessages.EmailAlreadyExists));
        }

        if (await _repository.DocumentExistsAsync(request.DocumentNumber, cancellationToken: cancellationToken))
        {
            _logger.LogWarning("Document {Document} already exists", request.DocumentNumber);
            return Result<EmployeeResponse>.Failure(
                Error.Conflict("DocumentNumber", ValidationMessages.DocumentAlreadyExists));
        }

        if (request.ManagerId.HasValue)
        {
            var managerExists = await _repository.ExistsAsync(request.ManagerId.Value, cancellationToken);
            if (!managerExists)
            {
                _logger.LogWarning("Manager {ManagerId} not found", request.ManagerId.Value);
                return Result<EmployeeResponse>.Failure(
                    Error.NotFound("ManagerId", request.ManagerId.Value.ToString()));
            }
        }

        var passwordHash = _passwordHasher.Hash(request.Password);
        var employeeResult = Employee.Create(
            request.FirstName,
            request.LastName,
            request.Email,
            request.DocumentNumber,
            request.BirthDate,
            passwordHash,
            request.Role,
            request.ManagerId,
            request.PhoneNumbers);

        if (employeeResult.IsFailure)
        {
            return Result<EmployeeResponse>.Failure(employeeResult.Error);
        }

        var employee = employeeResult.Value;

        await _repository.AddAsync(employee, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _cache.RemoveAsync(CacheKeys.AllEmployees, cancellationToken);

        _logger.LogInformation("Employee created successfully: {Id}", employee.Id);

        return Result<EmployeeResponse>.Success(EmployeeResponse.FromEntity(employee));
    }
}