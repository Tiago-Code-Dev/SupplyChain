namespace EmployeeManagement.Api.Contracts.Auth;

public record LoginRequest(string Email, string Password);
public record RefreshTokenRequest(string RefreshToken);
public record RevokeTokenRequest(string RefreshToken);
public record ForgotPasswordRequest(string Email);
public record ResetPasswordRequest(string Email, string Token, string NewPassword);
public record AuthResponse(
    string AccessToken,
    DateTime AccessTokenExpiresAt,
    string RefreshToken,
    DateTime RefreshTokenExpiresAt,
    UserResponse User);
public record UserResponse(Guid Id, string Email, string FullName, List<string> Roles);
public record RegisterRequest(string Email, string Password, string FirstName, string LastName, string? Role = null);
public record RegisterResponse(Guid UserId, string Email);
public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
public record AddRoleRequest(string Role);
public record AddClaimRequest(string Type, string Value);
public record UserInfoResponse(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string FullName,
    Guid? EmployeeId,
    bool IsActive,
    List<string> Roles,
    Dictionary<string, string> Claims);