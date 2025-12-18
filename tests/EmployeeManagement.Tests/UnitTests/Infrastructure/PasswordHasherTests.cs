using EmployeeManagement.Infrastructure.Security;

namespace EmployeeManagement.Tests.UnitTests.Infrastructure;

/// <summary>
/// Testes unitários para PasswordHasher
/// </summary>
public class PasswordHasherTests
{
    private readonly PasswordHasher _passwordHasher;

    public PasswordHasherTests()
    {
        _passwordHasher = new PasswordHasher();
    }

    #region Hash Tests

    [Fact]
    [Trait("Category", "Infrastructure")]
    public void Hash_ComSenhaValida_DeveRetornarHashNaoVazio()
    {
        // Arrange
        var password = "MySecurePassword123!";

        // Act
        var hashedPassword = _passwordHasher.Hash(password);

        // Assert
        hashedPassword.Should().NotBeNullOrEmpty();
        hashedPassword.Should().NotBe(password);
    }

    [Fact]
    [Trait("Category", "Infrastructure")]
    public void Hash_MesmaSenha_DeveRetornarHashesDiferentes()
    {
        // Arrange
        var password = "MySecurePassword123!";

        // Act
        var hash1 = _passwordHasher.Hash(password);
        var hash2 = _passwordHasher.Hash(password);

        // Assert - BCrypt gera hashes diferentes devido ao salt único
        hash1.Should().NotBe(hash2);
    }

    [Fact]
    [Trait("Category", "Infrastructure")]
    public void Hash_DeveGerarHashNoFormatoBCrypt()
    {
        // Arrange
        var password = "TestPassword123!";

        // Act
        var hashedPassword = _passwordHasher.Hash(password);

        // Assert - BCrypt hashes começam com $2a$, $2b$ ou $2y$
        hashedPassword.Should().MatchRegex(@"^\$2[aby]\$\d{2}\$.{53}$");
    }

    [Theory]
    [Trait("Category", "Infrastructure")]
    [InlineData("simple")]
    [InlineData("Complex@Password123!")]
    [InlineData("a")]
    [InlineData("verylongpasswordwithmanycharactersandnumbers123456789!@#$%")]
    [InlineData("Senha com espaços e acentuação")]
    public void Hash_ComVariasSenhas_DeveProuzirHashValido(string password)
    {
        // Act
        var hashedPassword = _passwordHasher.Hash(password);

        // Assert
        hashedPassword.Should().NotBeNullOrEmpty();
        _passwordHasher.Verify(password, hashedPassword).Should().BeTrue();
    }

    #endregion

    #region Verify Tests

    [Fact]
    [Trait("Category", "Infrastructure")]
    public void Verify_ComSenhaCorreta_DeveRetornarTrue()
    {
        // Arrange
        var password = "MySecurePassword123!";
        var hashedPassword = _passwordHasher.Hash(password);

        // Act
        var result = _passwordHasher.Verify(password, hashedPassword);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Infrastructure")]
    public void Verify_ComSenhaIncorreta_DeveRetornarFalse()
    {
        // Arrange
        var password = "MySecurePassword123!";
        var wrongPassword = "WrongPassword456!";
        var hashedPassword = _passwordHasher.Hash(password);

        // Act
        var result = _passwordHasher.Verify(wrongPassword, hashedPassword);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    [Trait("Category", "Infrastructure")]
    public void Verify_ComSenhaVazia_DeveRetornarFalse()
    {
        // Arrange
        var password = "MySecurePassword123!";
        var hashedPassword = _passwordHasher.Hash(password);

        // Act
        var result = _passwordHasher.Verify("", hashedPassword);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    [Trait("Category", "Infrastructure")]
    public void Verify_CaseSensitive_DeveRetornarFalseParaCaseDiferente()
    {
        // Arrange
        var password = "MyPassword123";
        var hashedPassword = _passwordHasher.Hash(password);

        // Act
        var resultLower = _passwordHasher.Verify("mypassword123", hashedPassword);
        var resultUpper = _passwordHasher.Verify("MYPASSWORD123", hashedPassword);

        // Assert
        resultLower.Should().BeFalse();
        resultUpper.Should().BeFalse();
    }

    [Fact]
    [Trait("Category", "Infrastructure")]
    public void Verify_ComEspacosNaSenha_DeveVerificarCorretamente()
    {
        // Arrange
        var passwordWithSpaces = " password with spaces ";
        var hashedPassword = _passwordHasher.Hash(passwordWithSpaces);

        // Act
        var resultExact = _passwordHasher.Verify(passwordWithSpaces, hashedPassword);
        var resultTrimmed = _passwordHasher.Verify("password with spaces", hashedPassword);

        // Assert
        resultExact.Should().BeTrue();
        resultTrimmed.Should().BeFalse();
    }

    #endregion
}