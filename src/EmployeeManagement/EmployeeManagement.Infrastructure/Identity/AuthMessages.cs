namespace EmployeeManagement.Infrastructure.Identity;

/// <summary>
/// Mensagens de autenticação localizadas (PT-BR)
/// </summary>
internal static class AuthMessages
{
    public const string InvalidCredentials = "Credenciais inválidas";
    public const string UserInactive = "Usuário inativo";
    public const string AccountLocked = "Conta bloqueada. Tente novamente mais tarde.";
    public const string InvalidRefreshToken = "Refresh token inválido ou expirado";
    public const string TokenRevoked = "Token revogado";
    public const string UserNotFound = "Usuário não encontrado";
}
