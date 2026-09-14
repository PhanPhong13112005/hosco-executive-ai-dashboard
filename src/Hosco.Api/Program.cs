using System.Text;
using System.Text.Json;
using Hosco.Api.Health;
using Hosco.Api.Observability;
using Hosco.Api.Security;
using Hosco.Application.Abstractions;
using Hosco.Application.Models;
using Hosco.Application.Semantics;
using Hosco.Application.Services;
using Hosco.Domain.Enums;
using Hosco.Infrastructure.Persistence;
using Hosco.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

public partial class Program
{
    public static async Task Main(string[] args)
    {
        var app = BuildApp(args);
        await InitializeDatabaseAsync(app);
        await app.RunAsync();
    }

    public static WebApplication BuildApp(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Logging.ClearProviders();
        builder.Logging.AddJsonConsole();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddControllers().AddApplicationPart(typeof(Hosco.Api.Controllers.AuthController).Assembly);
        builder.Services.Configure<ApiBehaviorOptions>(options => options.InvalidModelStateResponseFactory = context =>
        {
            var correlationId = context.HttpContext.Items[CorrelationMiddleware.ItemKey]?.ToString() ?? "unavailable";
            var message = string.Join(" ", context.ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage));
            return new BadRequestObjectResult(new ApiError("validation_error", message, correlationId));
        });
        builder.Services.AddProblemDetails();
        builder.Services.AddRequestTimeouts(options => options.AddPolicy("reporting", TimeSpan.FromSeconds(10)));

        var provider = builder.Configuration["Database:Provider"] ?? "SqlServer";
        if (provider.Equals("InMemory", StringComparison.OrdinalIgnoreCase))
            builder.Services.AddDbContext<HoscoDbContext>(o => o.UseInMemoryDatabase(builder.Configuration["Database:Name"] ?? $"hosco-{Guid.NewGuid()}"));
        else if (provider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase))
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();
            builder.Services.AddSingleton(connection);
            builder.Services.AddDbContext<HoscoDbContext>(o => o.UseSqlite(connection));
        }
        else
            builder.Services.AddDbContext<HoscoDbContext>(o => o.UseSqlServer(
                builder.Configuration.GetConnectionString("HoscoDb"), sql => sql.CommandTimeout(10).EnableRetryOnFailure(3)));

        var jwt = builder.Configuration.GetSection(JwtOptions.Section).Get<JwtOptions>() ?? new JwtOptions();
        if (jwt.SigningKey.Length < 32)
            throw new InvalidOperationException("Jwt:SigningKey must contain at least 32 characters. Configure it with environment variables or dotnet user-secrets.");
        builder.Services.AddSingleton(jwt);
        builder.Services.AddSingleton<JwtTokenService>();
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true, ValidIssuer = jwt.Issuer,
                ValidateAudience = true, ValidAudience = jwt.Audience,
                ValidateLifetime = true, ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
                ClockSkew = TimeSpan.FromSeconds(30), NameClaimType = System.Security.Claims.ClaimTypes.NameIdentifier,
                RoleClaimType = System.Security.Claims.ClaimTypes.Role
            };
            options.Events = new JwtBearerEvents
            {
                OnChallenge = async context =>
                {
                    context.HandleResponse();
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsJsonAsync(new ApiError("unauthorized", "Authentication is required.",
                        context.HttpContext.Items[CorrelationMiddleware.ItemKey]?.ToString() ?? "unavailable"));
                },
                OnForbidden = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return context.Response.WriteAsJsonAsync(new ApiError("forbidden", "The authenticated user is not authorized for this operation.",
                        context.HttpContext.Items[CorrelationMiddleware.ItemKey]?.ToString() ?? "unavailable"));
                }
            };
        });
        builder.Services.AddAuthorization(options => options.AddPolicy("ReportingReader", policy =>
            policy.RequireAuthenticatedUser().RequireRole(Enum.GetNames<SystemRole>())));

        builder.Services.AddScoped<ICurrentUser, CurrentUser>();
        builder.Services.AddScoped<ICorrelationContext, CorrelationContext>();
        builder.Services.AddScoped<IBranchDirectory, BranchDirectory>();
        builder.Services.AddScoped<IBranchScopeValidator, BranchScopeValidator>();
        builder.Services.AddScoped<IReportingScopeFactory, ReportingScopeFactory>();
        builder.Services.AddScoped<IIdentityStore, IdentityStore>();
        builder.Services.AddSingleton<IPasswordVerifier, Pbkdf2PasswordService>();
        builder.Services.AddSingleton<IMetricCatalog, MetricCatalog>();
        builder.Services.AddSingleton<IQueryCatalog, QueryCatalog>();
        builder.Services.AddScoped<IReportingDataStore, ReportingDataStore>();
        builder.Services.AddScoped<IAuditWriter, AuditWriter>();
        builder.Services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("database", tags: ["ready"]);

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo { Title = "HOSCO Reporting API", Version = "v1", Description = "Tenant-safe reporting boundary shared by Dashboard and future Chatbot." });
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization", Type = SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "JWT",
                In = ParameterLocation.Header, Description = "Enter the JWT returned by POST /api/v1/auth/login."
            });
            options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer", document, null)] = []
            });
        });
        // JWT-only API: no cookie payload requires persistent Data Protection keys.
        builder.Services.AddSingleton<IDataProtectionProvider>(new EphemeralDataProtectionProvider());

        var app = builder.Build();
        app.UseMiddleware<CorrelationMiddleware>();
        app.UseMiddleware<ExceptionHandlingMiddleware>();
        app.UseRequestTimeouts();
        if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Swagger:Enabled"))
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers().WithRequestTimeout("reporting");
        app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false, ResponseWriter = WriteHealthResponse }).AllowAnonymous();
        app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready"), ResponseWriter = WriteHealthResponse }).AllowAnonymous();
        return app;
    }

    public static async Task InitializeDatabaseAsync(WebApplication app, CancellationToken ct = default)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<HoscoDbContext>();
        if (db.Database.IsInMemory() || db.Database.IsSqlite()) await db.Database.EnsureCreatedAsync(ct);
        else await db.Database.MigrateAsync(ct);
        if (app.Configuration.GetValue("Seed:Enabled", app.Environment.IsDevelopment())) await DemoSeed.SeedAsync(db, ct);
    }

    private static Task WriteHealthResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";
        return context.Response.WriteAsync(JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(x => new { name = x.Key, status = x.Value.Status.ToString(), description = x.Value.Description })
        }));
    }
}
