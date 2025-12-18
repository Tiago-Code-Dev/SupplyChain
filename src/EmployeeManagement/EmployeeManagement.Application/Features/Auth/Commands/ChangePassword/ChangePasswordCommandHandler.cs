using EmployeeManagement.Application.Common.Interfaces;
using EmployeeManagement.Application.Interfaces;
using EmployeeManagement.Domain.Common;
using EmployeeManagement.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace EmployeeManagement.Application.Features.Auth.Commands.ChangePassword;

public sealed class ChangePasswordCommandHandler : ICommandHandler<ChangePasswordCommand>
{
    private readonly IEmployeeRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<ChangePasswordCommandHandler> _logger;

    public ChangePasswordCommandHandler(
        IEmployeeRepository repository,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        ILogger<ChangePasswordCommandHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task<Result> Handle(
        ChangePasswordCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Changing password for employee: {Id}", request.EmployeeId);

        var employee = await _repository.GetByIdAsync(request.EmployeeId, cancellationToken);
        
        if (employee is null)
        {
            return Result.Failure(Error.NotFound("Employee", request.EmployeeId));
        }

        if (!_passwordHasher.Verify(request.CurrentPassword, employee.PasswordHash))
        {
            return Result.Failure(Error.Validation("CurrentPassword", "Current password is incorrect"));
        }

        var newPasswordHash = _passwordHasher.Hash(request.NewPassword);
        var updateResult = employee.UpdatePassword(newPasswordHash);

        if (updateResult.IsFailure)
        {
            return updateResult;
        }

        await _repository.UpdateAsync(employee, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Password changed successfully for employee: {Id}", request.EmployeeId);

        return Result.Success();
    }
}