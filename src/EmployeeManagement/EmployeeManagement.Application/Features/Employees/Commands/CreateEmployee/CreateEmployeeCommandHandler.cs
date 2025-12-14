using EmployeeManagement.Application.Common.Interfaces;
using EmployeeManagement.Application.Features.Employees.Common;
using EmployeeManagement.Application.Interfaces;
using EmployeeManagement.Domain.Common;
using EmployeeManagement.Domain.Entities;
using EmployeeManagement.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace EmployeeManagement.Application.Features.Employees.Commands.CreateEmployee;

public sealed class CreateEmployeeCommandHandler 
    : ICommandHandler<CreateEmployeeCommand, EmployeeResponse>
{
    private readonly IEmployeeRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<CreateEmployeeCommandHandler> _logger;

    public CreateEmployeeCommandHandler(
        IEmployeeRepository repository,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        ILogger<CreateEmployeeCommandHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task<Result<EmployeeResponse>> Handle(
        CreateEmployeeCommand request, 
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating employee with email: {Email}", request.Email);

        // Validação de autorização
        if (request.CurrentUserRole <= request.Role)
        {
            return Result<EmployeeResponse>.Failure(
                Error.Forbidden("You cannot create an employee with a role equal to or higher than yours"));
        }

        // Verificar email único
        var existingByEmail = await _repository.GetByEmailAsync(request.Email, cancellationToken);
        if (existingByEmail is not null)
        {
            return Result<EmployeeResponse>.Failure(
                Error.Conflict("Email", "Email already exists"));
        }

        // Verificar documento único
        var existingByDocument = await _repository.GetByDocumentAsync(request.DocumentNumber, cancellationToken);
        if (existingByDocument is not null)
        {
            return Result<EmployeeResponse>.Failure(
                Error.Conflict("DocumentNumber", "Document number already exists"));
        }

        // Validar manager se fornecido
        if (request.ManagerId.HasValue)
        {
            var manager = await _repository.GetByIdAsync(request.ManagerId.Value, cancellationToken);
            if (manager is null)
            {
                return Result<EmployeeResponse>.Failure(
                    Error.NotFound("Manager", request.ManagerId.Value));
            }
        }

        // Criar via Factory Method
        var passwordHash = _passwordHasher.Hash(request.Password);
        var employeeResult = Employee.Create(
            request.FirstName,
            request.LastName,
            request.Email,
            request.DocumentNumber,
            request.BirthDate,
            passwordHash,
            request.Role,
            request.ManagerId);

        if (employeeResult.IsFailure)
        {
            return Result<EmployeeResponse>.Failure(employeeResult.Error);
        }

        var employee = employeeResult.Value;

        // Adicionar telefones
        foreach (var phone in request.PhoneNumbers)
        {
            employee.AddPhone(new PhoneNumber(phone, employee.Id));
        }

        await _repository.AddAsync(employee, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Employee created successfully: {Id}", employee.Id);

        return Result<EmployeeResponse>.Success(EmployeeResponse.FromEntity(employee));
    }
}