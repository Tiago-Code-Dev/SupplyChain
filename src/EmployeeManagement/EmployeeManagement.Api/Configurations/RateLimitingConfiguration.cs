using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace EmployeeManagement.Api.Configurations;

public static class RateLimitingConfiguration
{
    public const string FixedPolicy = "fixed";
    public const string SlidingPolicy = "sliding";

    public static IServiceCollection AddRateLimitingConfiguration(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // Fixed Window Limiter
            options.AddFixedWindowLimiter(FixedPolicy, config =>
            {
                config.Window = TimeSpan.FromMinutes(1);
                config.PermitLimit = 100;
                config.QueueLimit = 10;
                config.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            });

            // Sliding Window Limiter
            options.AddSlidingWindowLimiter(SlidingPolicy, config =>
            {
                config.Window = TimeSpan.FromMinutes(1);
                config.PermitLimit = 100;
                config.SegmentsPerWindow = 4;
                config.QueueLimit = 10;
                config.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            });

            // Global limiter
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.User.Identity?.Name ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        Window = TimeSpan.FromMinutes(1),
                        PermitLimit = 200,
                        QueueLimit = 20,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                    });
            });

            options.OnRejected = async (context, token) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter = retryAfter.TotalSeconds.ToString();
                }

                await context.HttpContext.Response.WriteAsJsonAsync(new
                {
                    Title = "Too Many Requests",
                    Status = 429,
                    Detail = "Rate limit exceeded. Please try again later."
                }, token);
            };
        });

        return services;
    }
}