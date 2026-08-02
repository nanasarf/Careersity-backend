using Careersity.Application;
using Careersity.Infrastructure;
using Careersity.Api.Infrastructure;
using System.Text.Json.Serialization;
using System.Text;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Careersity.Application.Abstractions.Authentication;
using Careersity.Domain.Enums;
using Careersity.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Careersity.Infrastructure.Initialization;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("CareersityDatabase");
var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
if (builder.Environment.IsProduction())
{
    if (string.IsNullOrWhiteSpace(connectionString)) throw new InvalidOperationException("Production requires ConnectionStrings:CareersityDatabase.");
    if (corsOrigins.Length == 0 || corsOrigins.Any(x => x == "*")) throw new InvalidOperationException("Production requires explicit non-wildcard Cors:AllowedOrigins.");
}

builder.Services.AddControllers(options => options.Filters.Add<RequestValidationFilter>())
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Careersity API", Version = "v1", Description = "MVP API for organizing third-party learning resources into career pathways. Careersity does not claim ownership of external content." });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization", Type = SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "JWT",
        In = ParameterLocation.Header, Description = "Enter the JWT access token."
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }] = Array.Empty<string>()
    });
});
builder.Services.AddHealthChecks().AddCheck("self",()=>HealthCheckResult.Healthy(),["live"]).AddCheck<DatabaseReadinessHealthCheck>("postgresql",tags:["ready","database"]);
builder.Services.AddCors(options=>options.AddPolicy("ConfiguredOrigins",policy=>{if(corsOrigins.Length>0)policy.WithOrigins(corsOrigins).WithMethods("GET","POST","PUT","DELETE").WithHeaders("Authorization","Content-Type",CorrelationMiddleware.Header).WithExposedHeaders(CorrelationMiddleware.Header);}));
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddCareersityApplication();
builder.Services.AddCareersityInfrastructure(builder.Configuration);
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IOptions<JwtOptions>>((options, configuredJwt) =>
{
    var jwt = configuredJwt.Value;
    options.MapInboundClaims = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true, ValidIssuer = jwt.Issuer,
        ValidateAudience = true, ValidAudience = jwt.Audience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
        ValidateLifetime = true, ClockSkew = TimeSpan.Zero,
        RoleClaimType = ClaimTypes.Role,
        NameClaimType = "email"
    };
    options.Events = new JwtBearerEvents
    {
        OnChallenge = context =>
        {
            context.HandleResponse();
            return AuthenticationProblemWriter.WriteAsync(context.HttpContext, StatusCodes.Status401Unauthorized,
                "Authentication required", "Authentication is required to access this resource.");
        },
        OnForbidden = context => AuthenticationProblemWriter.WriteAsync(context.HttpContext, StatusCodes.Status403Forbidden,
            "Access forbidden", "You do not have permission to access this resource.")
    };
});
builder.Services.AddAuthorization(options => options.AddPolicy(SecurityPolicies.AdministratorOnly,
    policy => policy.RequireRole(nameof(UserRole.Administrator))));
builder.Services.AddRateLimiter(options =>
{
    static string Key(HttpContext context) => context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    options.AddPolicy(SecurityPolicies.LoginRateLimit, context => RateLimitPartition.GetFixedWindowLimiter(Key(context),
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    options.AddPolicy(SecurityPolicies.RegistrationRateLimit, context => RateLimitPartition.GetFixedWindowLimiter(Key(context),
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromHours(1), QueueLimit = 0 }));
    options.AddPolicy(SecurityPolicies.RefreshRateLimit, context => RateLimitPartition.GetFixedWindowLimiter(Key(context),
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 20, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    options.AddPolicy(SecurityPolicies.MutationRateLimit, context => RateLimitPartition.GetFixedWindowLimiter(
        context.User.FindFirst("sub")?.Value ?? Key(context), _ => new FixedWindowRateLimiterOptions { PermitLimit = 30, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    options.OnRejected = (context, _) => new ValueTask(AuthenticationProblemWriter.WriteAsync(context.HttpContext,
        StatusCodes.Status429TooManyRequests, "Too many requests", "The authentication request limit was exceeded."));
});

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    await scope.ServiceProvider.GetRequiredService<ApplicationStartupInitializer>().InitializeAsync();
}

app.UseMiddleware<CorrelationMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("ConfiguredOrigins");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health",new HealthCheckOptions{Predicate=x=>x.Tags.Contains("live")});
app.MapHealthChecks("/health/ready",new HealthCheckOptions{Predicate=x=>x.Tags.Contains("ready")});

await app.RunAsync();

public partial class Program;
