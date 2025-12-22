# Camada de API

## Introdução

A **Camada de API** é a camada de apresentação, responsável por expor endpoints HTTP/HTTPS, gerenciar autenticação, aplicar middlewares e documentar a API.

**Localização**: `src/EmployeeManagement/EmployeeManagement.Api`

## Controllers

### EmployeesController (V1)

```csharp
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
[Authorize]
public class EmployeesController : MainController
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? searchTerm = null,
        CancellationToken cancellationToken = default)
    {
        var query = new GetAllEmployeesQuery(pageNumber, pageSize, searchTerm, ...);
        var result = await Sender.Send(query, cancellationToken);
        return Ok(result);
    }
    
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var query = new GetEmployeeByIdQuery(id);
        var result = await Sender.Send(query, ct);
        return result is null ? NotFound() : Ok(result);
    }
    
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateEmployeeRequest request,
        CancellationToken ct)
    {
        var command = new CreateEmployeeCommand(...);
        var result = await Sender.Send(command, ct);
        return HandleCreatedResult(result, nameof(GetById), e => new { id = e.Id });
    }
    
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateEmployeeRequest request,
        CancellationToken ct)
    {
        var command = new UpdateEmployeeCommand(id, ...);
        var result = await Sender.Send(command, ct);
        return HandleResult(result);
    }
    
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var command = new DeleteEmployeeCommand(id, GetCurrentUserRole<Role>());
        var result = await Sender.Send(command, ct);
        return HandleResult(result);
    }
}
```

### AuthController (V1)

```csharp
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
public class AuthController : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _identityService.AuthenticateAsync(
            request.Email, request.Password, GetIpAddress());
        
        if (result.IsFailure)
            return Unauthorized(new { error = result.Error.Description });
        
        return Ok(new AuthResponse(...));
    }
    
    [HttpPost("refresh-token")]
    [AllowAnonymous]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        var result = await _identityService.RefreshTokenAsync(
            request.RefreshToken, GetIpAddress());
        
        if (result.IsFailure)
            return Unauthorized(new { error = result.Error.Description });
        
        return Ok(new AuthResponse(...));
    }
    
    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userId = GetCurrentUserId();
        var result = await _identityService.ChangePasswordAsync(
            userId.Value, request.CurrentPassword, request.NewPassword);
        
        if (result.IsFailure)
            return BadRequest(new { error = result.Error.Description });
        
        return NoContent();
    }
}
```

## Middlewares

### GlobalExceptionMiddleware

```csharp
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }
    
    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        
        var response = new
        {
            error = "Internal Server Error",
            message = exception.Message
        };
        
        return context.Response.WriteAsJsonAsync(response);
    }
}
```

### CorrelationIdMiddleware

```csharp
public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private const string CorrelationIdHeader = "X-Correlation-ID";
    
    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[CorrelationIdHeader].FirstOrDefault()
            ?? Guid.NewGuid().ToString();
        
        context.Items["CorrelationId"] = correlationId;
        context.Response.Headers.Add(CorrelationIdHeader, correlationId);
        
        await _next(context);
    }
}
```

## Configurações

### Swagger

```csharp
public static class SwaggerConfiguration
{
    public static IServiceCollection AddSwaggerConfiguration(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Employee Management API",
                Version = "v1",
                Description = "API para gerenciamento de funcionários"
            });
            
            // JWT Authentication
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer"
            });
            
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
            
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            options.IncludeXmlComments(xmlPath);
        });
        
        return services;
    }
}
```

### Rate Limiting

```csharp
public static class RateLimitingConfiguration
{
    public const string GeneralPolicy = "general";
    public const string LoginPolicy = "login";
    
    public static IServiceCollection AddRateLimitingConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddRateLimiter(options =>
        {
            // Política geral: 100 requisições por minuto
            options.AddFixedWindowLimiter(GeneralPolicy, opt =>
            {
                opt.PermitLimit = 100;
                opt.Window = TimeSpan.FromMinutes(1);
                opt.QueueLimit = 2;
            });
            
            // Política de login: 5 requisições por minuto
            options.AddFixedWindowLimiter(LoginPolicy, opt =>
            {
                opt.PermitLimit = 5;
                opt.Window = TimeSpan.FromMinutes(1);
                opt.QueueLimit = 0;
            });
            
            options.OnRejected = async (context, token) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await context.HttpContext.Response.WriteAsync(
                    "Too many requests. Please try again later.", token);
            };
        });
        
        return services;
    }
}
```

### CORS

```csharp
public static class CorsConfiguration
{
    public static IServiceCollection AddCorsConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? new[] { "http://localhost:3000" };
        
        services.AddCors(options =>
        {
            options.AddDefaultPolicy(builder =>
            {
                builder.WithOrigins(allowedOrigins)
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials();
            });
        });
        
        return services;
    }
}
```

### Health Checks

```csharp
public static class HealthCheckConfiguration
{
    public static IServiceCollection AddHealthCheckConfiguration(
        this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddDbContextCheck<AppDbContext>("database")
            .AddDbContextCheck<AppIdentityDbContext>("identity-database");
        
        return services;
    }
    
    public static IApplicationBuilder UseHealthCheckConfiguration(
        this IApplicationBuilder app)
    {
        app.UseHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = async (context, report) =>
            {
                context.Response.ContentType = "application/json";
                var result = JsonSerializer.Serialize(new
                {
                    status = report.Status.ToString(),
                    checks = report.Entries.Select(e => new
                    {
                        name = e.Key,
                        status = e.Value.Status.ToString(),
                        duration = e.Value.Duration.TotalMilliseconds
                    })
                });
                await context.Response.WriteAsync(result);
            }
        });
        
        return app;
    }
}
```

## Pipeline de Middleware (Program.cs)

```csharp
var app = builder.Build();

// 1. Correlation ID
app.UseCorrelationId();

// 2. Exception Handling
app.UseMiddleware<GlobalExceptionMiddleware>();

// 3. Swagger
app.UseSwaggerConfiguration();

// 4. Compression
app.UseResponseCompression();

// 5. CORS
app.UseCorsConfiguration();

// 6. Health Checks
app.UseHealthCheckConfiguration();

// 7. Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// 8. Rate Limiting
app.UseRateLimiter();

// 9. Map Controllers
app.MapControllers();

app.Run();
```

## Versionamento de API

```csharp
services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = new UrlSegmentApiVersionReader();
});
```

## Próximos Passos

- [Autenticação](07-AUTENTICACAO.md)
- [API Reference](12-API-REFERENCE.md)
- [Guia de Desenvolvimento](13-GUIA-DESENVOLVIMENTO.md)

