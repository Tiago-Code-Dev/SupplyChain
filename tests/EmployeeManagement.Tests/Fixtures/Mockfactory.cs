using Microsoft.Extensions.Logging;

namespace EmployeeManagement.Tests.Fixtures;

/// <summary>
/// Factory para criação de mocks reutilizáveis
/// </summary>
public static class MockFactory
{
    /// <summary>
    /// Cria mock do IEmployeeRepository
    /// </summary>
    public static Mock<IEmployeeRepository> CreateEmployeeRepositoryMock()
    {
        return new Mock<IEmployeeRepository>();
    }

    /// <summary>
    /// Cria mock do IUnitOfWork
    /// </summary>
    public static Mock<IUnitOfWork> CreateUnitOfWorkMock()
    {
        var mock = new Mock<IUnitOfWork>();
        mock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        return mock;
    }

    /// <summary>
    /// Cria mock do IPasswordHasher
    /// </summary>
    public static Mock<IPasswordHasher> CreatePasswordHasherMock(
        string? expectedPassword = null,
        string? expectedHash = null)
    {
        var mock = new Mock<IPasswordHasher>();

        mock.Setup(x => x.Hash(It.IsAny<string>()))
            .Returns((string pwd) => $"hashed_{pwd}");

        if (expectedPassword != null && expectedHash != null)
        {
            mock.Setup(x => x.Verify(expectedPassword, expectedHash))
                .Returns(true);
        }

        mock.Setup(x => x.Verify(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(false);

        return mock;
    }

    /// <summary>
    /// Cria mock do IJwtService
    /// </summary>
    public static Mock<IJwtService> CreateJwtServiceMock(string token = "test-jwt-token")
    {
        var mock = new Mock<IJwtService>();
        mock.Setup(x => x.GenerateToken(It.IsAny<Employee>()))
            .Returns(token);
        return mock;
    }

    /// <summary>
    /// Cria mock do ICacheService
    /// </summary>
    public static Mock<ICacheService> CreateCacheServiceMock()
    {
        var mock = new Mock<ICacheService>();
        mock.Setup(x => x.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return mock;
    }

    /// <summary>
    /// Cria mock de ILogger genérico
    /// </summary>
    public static Mock<ILogger<T>> CreateLoggerMock<T>()
    {
        return new Mock<ILogger<T>>();
    }
}