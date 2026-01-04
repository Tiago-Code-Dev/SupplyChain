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
    private readonly IIdentityService _identityService;
    private readonly ILogger<CreateEmployeeCommandHandler> _logger;

    public CreateEmployeeCommandHandler(
        IEmployeeRepository repository,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        ICacheService cache,
        IIdentityService identityService,
        ILogger<CreateEmployeeCommandHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _cache = cache;
        _identityService = identityService;
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

        // Validação de permissões para criação baseada na hierarquia
        // Employee (1): Não pode criar ninguém
        // Leader (2): Só pode criar Employee (1)
        // Director (3): Pode criar Director (3), Leader (2), Employee (1)
        // Admin (4): Pode criar qualquer role
        var canCreate = request.CurrentUserRole switch
        {
            Role.Admin => true, // Admin pode criar qualquer role
            Role.Director => request.Role <= Role.Director, // Director pode criar Director, Leader, Employee
            Role.Leader => request.Role == Role.Employee, // Leader só pode criar Employee
            _ => false // Employee não pode criar ninguém
        };

        if (!canCreate)
        {
            _logger.LogWarning("User with role {UserRole} tried to create employee with role {TargetRole}", 
                request.CurrentUserRole, request.Role);
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

                // Determinar o ManagerId: se não foi especificado, usar o ID do usuário que está criando
                // Isso estabelece automaticamente a hierarquia: quem cria é o superior
                var effectiveManagerId = request.ManagerId ?? request.CreatedByUserId;

                if (effectiveManagerId.HasValue)
                {
                    // Verificar se o manager existe (pode ser um Employee ou um User do Identity)
                    var managerExists = await _repository.ExistsAsync(effectiveManagerId.Value, cancellationToken);
                    if (!managerExists)
                    {
                        // Se não existe como Employee, pode ser que o usuário seja apenas do Identity (ex: Admin)
                        // Nesse caso, não definimos manager
                        _logger.LogInformation("Manager {ManagerId} not found as employee, setting manager to null", effectiveManagerId.Value);
                        effectiveManagerId = null;
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
                    effectiveManagerId,
                    request.PhoneNumbers,
                    request.CustomRoleId);

        if (employeeResult.IsFailure)
        {
            return Result<EmployeeResponse>.Failure(employeeResult.Error);
        }

        var employee = employeeResult.Value;

        await _repository.AddAsync(employee, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Criar usuário no Identity para permitir login
        var identityResult = await _identityService.CreateUserAsync(
            request.Email,
            request.Password,
            request.FirstName,
            request.LastName,
            employee.Id,
            cancellationToken);

        if (identityResult.IsFailure)
        {
            _logger.LogError("Failed to create identity user for employee {Id}: {Error}", 
                employee.Id, identityResult.Error.Description);
            // Nota: O employee já foi criado, mas não conseguiu criar no Identity
            // Em produção, considerar usar transação distribuída ou compensação
        }
        else
        {
            // Adicionar role ao usuário no Identity
            var roleName = request.Role.ToString();
            await _identityService.AddToRoleAsync(identityResult.Value, roleName, cancellationToken);
            _logger.LogInformation("Identity user created for employee {Id} with role {Role}", 
                employee.Id, roleName);
        }

        await _cache.RemoveAsync(CacheKeys.AllEmployees, cancellationToken);

        _logger.LogInformation("Employee created successfully: {Id}", employee.Id);

        return Result<EmployeeResponse>.Success(EmployeeResponse.FromEntity(employee));
    }
}