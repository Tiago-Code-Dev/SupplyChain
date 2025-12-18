using EmployeeManagement.Application.Common.Interfaces;

namespace EmployeeManagement.Application.Features.Auth.Commands.ChangePassword;

public sealed record ChangePasswordCommand(
    Guid EmployeeId,
    string CurrentPassword,
    string NewPassword) : ICommand;