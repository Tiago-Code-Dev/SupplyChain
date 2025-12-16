using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace EmployeeManagement.Api.Configurations;

public static class RateLimitingConfiguration
{
    public const string FixedPolicy = "fixed";
    public const string SlidingPolicy = "sliding";
    public const string LoginPolicy = "login";

    public static IServiceCollection AddRateLimitingConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        var rateLimitSettings = configuration.GetSection("RateLimiting");
        var permitLimit = rateLimitSettings.GetValue<int>("PermitLimit", 100);
        var windowInSeconds = rateLimitSettings.GetValue<int>("WindowInSeconds", 60);
        var queueLimit = rateLimitSettings.GetValue<int>("QueueLimit", 10);
        var loginPermitLimit = rateLimitSettings.GetValue<int>("LoginPermitLimit", 5);
        var loginWindowInSeconds = rateLimitSettings.GetValue<int>("LoginWindowInSeconds", 60);

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // Fixed Window Limiter (geral)
            options.AddFixedWindowLimiter(FixedPolicy, config =>
            {
                config.Window = TimeSpan.FromSeconds(windowInSeconds);
                config.PermitLimit = permitLimit;
                config.QueueLimit = queueLimit;
                config.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            });

            // Sliding Window Limiter
            options.AddSlidingWindowLimiter(SlidingPolicy, config =>
            {
                config.Window = TimeSpan.FromSeconds(windowInSeconds);
                config.PermitLimit = permitLimit;
                config.SegmentsPerWindow = 4;
                config.QueueLimit = queueLimit;
                config.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            });

            // Login Policy - mais restritivo para proteger contra força bruta
            options.AddFixedWindowLimiter(LoginPolicy, config =>
            {
                config.Window = TimeSpan.FromSeconds(loginWindowInSeconds);
                config.PermitLimit = loginPermitLimit;
                config.QueueLimit = 0; // Sem fila para login
                config.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            });

            // Global limiter baseado em IP
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.User.Identity?.Name ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        Window = TimeSpan.FromSeconds(windowInSeconds),
                        PermitLimit = permitLimit * 2, // Global é mais permissivo
                        QueueLimit = queueLimit * 2,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                    });
            });

            options.OnRejected = async (context, token) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.HttpContext.Response.ContentType = "application/json";

                var retryAfterSeconds = windowInSeconds;
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    retryAfterSeconds = (int)retryAfter.TotalSeconds;
                    context.HttpContext.Response.Headers.RetryAfter = retryAfterSeconds.ToString();
                }

                await context.HttpContext.Response.WriteAsJsonAsync(new
                {
                    titulo = "Muitas Requisições",
                    status = 429,
                    detalhe = "Limite de requisições excedido. Por favor, tente novamente mais tarde.",
                    tentarNovamenteEm = $"{retryAfterSeconds} segundos"
                }, token);
            };
        });

        return services;
    }
}