# Camada de Infraestrutura

## Introdução

A **Camada de Infraestrutura** implementa os detalhes técnicos do sistema: persistência de dados, autenticação, cache, serviços externos e outras preocupações de infraestrutura.

**Localização**: `src/EmployeeManagement/EmployeeManagement.Infrastructure`

## Responsabilidades

✅ Implementar repositórios  
✅ Gerenciar Entity Framework Core  
✅ Implementar ASP.NET Identity  
✅ Gerenciar cache (Redis/Memory)  
✅ Implementar serviços de segurança (JWT, hashing)  
✅ Integrar com serviços externos  

## Estrutura

```
Infrastructure/
├── Persistence/
│   ├── AppDbContext.cs
│   ├── Configurations/
│   │   ├── EmployeeConfiguration.cs
│   │   └── PhoneNumberConfiguration.cs
│   ├── Repositories/
│   │   └── EmployeeRepository.cs
│   ├── Migrations/
│   └── DbSeeder.cs
├── Identity/
│   ├── AppIdentityDbContext.cs
│   ├── ApplicationUser.cs
│   ├── ApplicationRole.cs
│   ├── RefreshToken.cs
│   ├── IdentityService.cs
│   └── IdentitySeeder.cs
├── Caching/
│   └── RedisCacheService.cs
├── Security/
│   ├── JwtService.cs
│   └── PasswordHasher.cs
├── Services/
│   └── CurrentUserService.cs
└── DependencyInjection.cs
```

## Persistência

### AppDbContext

```csharp
public class AppDbContext : DbContext, IUnitOfWork
{
    private readonly IPublisher _publisher;
    private readonly ICurrentUserService? _currentUserService;
    
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<PhoneNumber> PhoneNumbers => Set<PhoneNumber>();
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        
        // Global Query Filter para Soft Delete
        modelBuilder.Entity<Employee>().HasQueryFilter(e => !e.IsDeleted);
    }
    
    public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        var currentUserId = _currentUserService?.UserId;
        
        // Aplicar auditoria automaticamente
        foreach (var entry in ChangeTracker.Entries<Entity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.SetCreatedBy(currentUserId);
                    break;
                case EntityState.Modified:
                    entry.Entity.SetUpdatedBy(currentUserId);
                    break;
                case EntityState.Deleted:
                    entry.State = EntityState.Modified;
                    entry.Entity.Delete(currentUserId);
                    break;
            }
        }
        
        // Coletar e publicar domain events
        var domainEvents = ChangeTracker.Entries<Entity>()
            .SelectMany(e => e.Entity.DomainEvents)
            .ToList();
        
        foreach (var entry in ChangeTracker.Entries<Entity>())
            entry.Entity.ClearDomainEvents();
        
        var result = await base.SaveChangesAsync(ct);
        
        foreach (var domainEvent in domainEvents)
            await _publisher.Publish(domainEvent, ct);
        
        return result;
    }
}
```

### Configurações EF Core

```csharp
public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("Employees");
        
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.FirstName)
            .IsRequired()
            .HasMaxLength(100);
        
        builder.Property(e => e.Email)
            .IsRequired()
            .HasMaxLength(255);
        
        builder.HasIndex(e => e.Email).IsUnique();
        builder.HasIndex(e => e.DocumentNumber).IsUnique();
        
        // Relacionamento com PhoneNumbers
        builder.HasMany(e => e.PhoneNumbers)
            .WithOne(p => p.Employee)
            .HasForeignKey(p => p.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // Relacionamento hierárquico (Manager/Subordinates)
        builder.HasOne(e => e.Manager)
            .WithMany(e => e.Subordinates)
            .HasForeignKey(e => e.ManagerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
```

### Repository Pattern

```csharp
public class EmployeeRepository : IEmployeeRepository
{
    private readonly AppDbContext _context;
    
    public async Task<Employee?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return await _context.Employees
            .Include(e => e.PhoneNumbers)
            .Include(e => e.Manager)
            .FirstOrDefaultAsync(e => e.Id == id, ct);
    }
    
    public async Task<Employee?> GetByEmailAsync(string email, CancellationToken ct)
    {
        return await _context.Employees
            .Include(e => e.PhoneNumbers)
            .FirstOrDefaultAsync(e => e.Email == email.ToLower(), ct);
    }
    
    public async Task AddAsync(Employee employee, CancellationToken ct)
    {
        await _context.Employees.AddAsync(employee, ct);
    }
    
    public void Update(Employee employee)
    {
        _context.Employees.Update(employee);
    }
}
```

## Identity & Segurança

### ApplicationUser

```csharp
public class ApplicationUser : IdentityUser<Guid>
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}";
    public Guid? EmployeeId { get; set; }
    public bool IsActive { get; set; } = true;
    
    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
}
```

### RefreshToken

```csharp
public class RefreshToken
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedByIp { get; set; } = string.Empty;
    public DateTime? RevokedAt { get; set; }
    public string? RevokedByIp { get; set; }
    public string? RevokedReason { get; set; }
    public string? ReplacedByToken { get; set; }
    
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsRevoked => RevokedAt != null;
    public bool IsActive => !IsRevoked && !IsExpired;
}
```

### JwtService

```csharp
public class JwtService : IJwtService
{
    private readonly JwtSettings _jwtSettings;
    
    public string GenerateAccessToken(Guid userId, string email, IList<string> roles)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Email, email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        
        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
            signingCredentials: credentials);
        
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
    
    public RefreshToken GenerateRefreshToken(string ipAddress)
    {
        return new RefreshToken
        {
            Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays),
            CreatedAt = DateTime.UtcNow,
            CreatedByIp = ipAddress
        };
    }
}
```

## Cache

### RedisCacheService

```csharp
public class RedisCacheService : ICacheService
{
    private readonly IDistributedCache _cache;
    
    public async Task<T?> GetAsync<T>(string key)
    {
        var data = await _cache.GetStringAsync(key);
        return data == null ? default : JsonSerializer.Deserialize<T>(data);
    }
    
    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
    {
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration ?? TimeSpan.FromMinutes(5)
        };
        
        var serialized = JsonSerializer.Serialize(value);
        await _cache.SetStringAsync(key, serialized, options);
    }
    
    public async Task RemoveAsync(string key)
    {
        await _cache.RemoveAsync(key);
    }
}
```

## Injeção de Dependências

```csharp
public static IServiceCollection AddInfrastructure(
    this IServiceCollection services,
    IConfiguration configuration)
{
    // Database
    services.AddDbContext<AppDbContext>(options =>
        options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));
    
    services.AddDbContext<AppIdentityDbContext>(options =>
        options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));
    
    // Identity
    services.AddIdentity<ApplicationUser, ApplicationRole>()
        .AddEntityFrameworkStores<AppIdentityDbContext>()
        .AddDefaultTokenProviders();
    
    // JWT
    services.Configure<JwtSettings>(configuration.GetSection("Jwt"));
    services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options => { /* configuração */ });
    
    // Repositories
    services.AddScoped<IEmployeeRepository, EmployeeRepository>();
    services.AddScoped<IUnitOfWork>(provider => 
        provider.GetRequiredService<AppDbContext>());
    
    // Services
    services.AddScoped<IIdentityService, IdentityService>();
    services.AddScoped<IJwtService, JwtService>();
    services.AddScoped<ICacheService, RedisCacheService>();
    services.AddScoped<ICurrentUserService, CurrentUserService>();
    
    // Cache
    services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = configuration.GetConnectionString("Redis");
    });
    
    return services;
}
```

## Próximos Passos

- [Camada de API](06-API.md)
- [Autenticação](07-AUTENTICACAO.md)
- [Banco de Dados](08-BANCO-DE-DADOS.md)

