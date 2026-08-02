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

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers(options => options.Filters.Add<RequestValidationFilter>())
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
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
builder.Services.AddHealthChecks();
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
    options.OnRejected = (context, _) => new ValueTask(AuthenticationProblemWriter.WriteAsync(context.HttpContext,
        StatusCodes.Status429TooManyRequests, "Too many requests", "The authentication request limit was exceeded."));
});

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    await scope.ServiceProvider.GetRequiredService<InitialAdministratorInitializer>().InitializeAsync();
}

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

await app.RunAsync();

public partial class Program;
