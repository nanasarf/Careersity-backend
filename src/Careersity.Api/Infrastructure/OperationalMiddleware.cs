using System.Diagnostics;using System.Text.RegularExpressions;using Careersity.Application.LearningContent.Services;using Careersity.Infrastructure.Persistence;using Microsoft.EntityFrameworkCore;using Microsoft.Extensions.Diagnostics.HealthChecks;
namespace Careersity.Api.Infrastructure;
public sealed class DatabaseReadinessHealthCheck(IServiceScopeFactory scopes):IHealthCheck
{
 public async Task<HealthCheckResult>CheckHealthAsync(HealthCheckContext context,CancellationToken token=default){await using var scope=scopes.CreateAsyncScope();var services=scope.ServiceProvider;var db=services.GetService<CareersityDbContext>();if(db is null)return HealthCheckResult.Unhealthy("Database configuration is missing.");try{if(!await db.Database.CanConnectAsync(token))return HealthCheckResult.Unhealthy("Database is unreachable.");var pending=await db.Database.GetPendingMigrationsAsync(token);if(pending.Any())return HealthCheckResult.Unhealthy("Required database migrations are pending.");services.GetRequiredService<ICourseService>();return HealthCheckResult.Healthy("Database is reachable and migrations are current.");}catch(Exception ex){return HealthCheckResult.Unhealthy("Database readiness check failed.",ex);}}
}
public sealed partial class CorrelationMiddleware(RequestDelegate next)
{
 public const string Header="X-Correlation-ID";
 public async Task InvokeAsync(HttpContext context,ILogger<CorrelationMiddleware> logger){var supplied=context.Request.Headers[Header].FirstOrDefault();var id=!string.IsNullOrWhiteSpace(supplied)&&supplied.Length<=128&&Valid().IsMatch(supplied)?supplied:Guid.NewGuid().ToString("N");context.TraceIdentifier=id;context.Response.OnStarting(()=>{context.Response.Headers[Header]=id;return Task.CompletedTask;});using(logger.BeginScope(new Dictionary<string,object>{{"CorrelationId",id}})){await next(context);}}
 [GeneratedRegex("^[A-Za-z0-9._-]+$")]private static partial Regex Valid();
}
public sealed class RequestLoggingMiddleware(RequestDelegate next)
{
 public async Task InvokeAsync(HttpContext context,ILogger<RequestLoggingMiddleware> logger){var sw=Stopwatch.StartNew();try{await next(context);}finally{sw.Stop();var userId=context.User.FindFirst("sub")?.Value;logger.LogInformation("HTTP {Method} {Path} responded {StatusCode} in {ElapsedMilliseconds} ms for user {UserId}",context.Request.Method,context.Request.Path,context.Response.StatusCode,sw.Elapsed.TotalMilliseconds,userId??"anonymous");}}
}
public sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
 public async Task InvokeAsync(HttpContext context){context.Response.OnStarting(()=>{context.Response.Headers["X-Content-Type-Options"]="nosniff";context.Response.Headers["X-Frame-Options"]="DENY";context.Response.Headers["Referrer-Policy"]="no-referrer";if(!context.Request.Path.StartsWithSegments("/swagger"))context.Response.Headers["Content-Security-Policy"]="default-src 'none'; frame-ancestors 'none'";return Task.CompletedTask;});await next(context);}
}
