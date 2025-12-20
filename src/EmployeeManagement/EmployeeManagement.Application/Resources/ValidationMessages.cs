namespace EmployeeManagement.Application.Resources;

/// <summary>
/// Mensagens de validação localizadas (PT-BR)
/// </summary>
public static class ValidationMessages
{
    // Employee - Campos obrigatórios
    public const string FirstNameRequired = "O nome é obrigatório";
    public const string FirstNameMaxLength = "O nome não pode exceder {MaxLength} caracteres";
    public const string FirstNameMinLength = "O nome deve ter pelo menos {MinLength} caracteres";
    public const string FirstNameInvalidCharacters = "O nome não pode conter números ou caracteres especiais";
    public const string LastNameRequired = "O sobrenome é obrigatório";
    public const string LastNameMaxLength = "O sobrenome não pode exceder {MaxLength} caracteres";
    public const string LastNameMinLength = "O sobrenome deve ter pelo menos {MinLength} caracteres";
    public const string LastNameInvalidCharacters = "O sobrenome não pode conter números ou caracteres especiais";
    public const string EmailRequired = "O email é obrigatório";
    public const string EmailInvalid = "Formato de email inválido";
    public const string EmailMaxLength = "O email não pode exceder {MaxLength} caracteres";
    public const string EmailContainsSpaces = "O email não pode conter espaços";
    public const string DocumentRequired = "O documento é obrigatório";
    public const string DocumentMaxLength = "O documento não pode exceder {MaxLength} caracteres";
    public const string DocumentInvalidFormat = "O documento deve ser um CPF (11 dígitos) ou CNPJ (14 dígitos) válido";
    public const string DocumentAllDigitsEqual = "O documento não pode ter todos os dígitos iguais";
    public const string BirthDateRequired = "A data de nascimento é obrigatória";
    public const string EmployeeMustBeAdult = "O funcionário deve ter pelo menos 18 anos";
    public const string BirthDateTooOld = "A data de nascimento não pode ser anterior a {MinYear}";
    public const string BirthDateInFuture = "A data de nascimento não pode ser no futuro";
    
    // Employee - Senha
    public const string PasswordRequired = "A senha é obrigatória";
    public const string PasswordMinLength = "A senha deve ter pelo menos {MinLength} caracteres";
    public const string PasswordUppercase = "A senha deve conter pelo menos uma letra maiúscula";
    public const string PasswordLowercase = "A senha deve conter pelo menos uma letra minúscula";
    public const string PasswordDigit = "A senha deve conter pelo menos um número";
    public const string PasswordSpecialChar = "A senha deve conter pelo menos um caractere especial";
    
    // Employee - Telefones
    public const string PhoneNumbersRequired = "A lista de telefones é obrigatória";
    public const string AtLeastOnePhoneRequired = "É necessário informar pelo menos um telefone";
    public const string PhoneNumberEmpty = "O número de telefone não pode estar vazio";
    public const string PhoneNumberInvalidFormat = "O telefone deve ter 10 ou 11 dígitos (DDD + número)";
    
    // Employee - Role/Permissão
    public const string RoleInvalid = "Permissão inválida";
    public const string EmployeeIdRequired = "O ID do funcionário é obrigatório";
    
    // Employee - Manager/Gestor
    public const string CannotBeSelfManager = "O funcionário não pode ser seu próprio gestor";
    public const string ManagerNotFound = "Gestor não encontrado";
    
    // Employee - Operações
    public const string EmployeeNotFound = "Funcionário não encontrado";
    public const string EmailAlreadyExists = "Email já cadastrado";
    public const string DocumentAlreadyExists = "Documento já cadastrado";
    public const string CannotCreateHigherRole = "Você não pode criar um funcionário com permissão igual ou superior à sua";
    public const string CannotUpdateToHigherRole = "Você não tem permissão para alterar usuários para nível de permissão superior";
    public const string CannotUpdateHigherRoleEmployee = "Você não tem permissão para alterar a role de funcionários com permissão igual ou superior à sua";
    public const string CannotDeleteWithSubordinates = "Não é possível excluir funcionário que possui subordinados";
    public const string NoPermissionToDelete = "Você não tem permissão para excluir funcionários";
    
    // Auth
    public const string InvalidCredentials = "Credenciais inválidas";
    public const string UserInactive = "Usuário inativo";
    public const string AccountLocked = "Conta bloqueada. Tente novamente mais tarde.";
    public const string TokenExpired = "Token expirado";
    public const string Unauthorized = "Não autorizado";
    public const string RefreshTokenInvalid = "Refresh token inválido ou expirado";
}
