using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Sicomoro.Api.Middlewares;
using Sicomoro.Api.Services;
using Sicomoro.Application;
using Sicomoro.Application.Interfaces;
using Sicomoro.Infrastructure;
using Sicomoro.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);
var jwtKey = builder.Configuration["Jwt:Key"] ?? "Sicomoro-dev-key-change-this-value-32chars";

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IReportesProxy, ReportesProxy>();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddCors(options =>
{
    var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
        ?? ["http://localhost:3000", "http://127.0.0.1:3000"];
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins(origins)
            .AllowAnyHeader()
            .AllowAnyMethod());
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Sicomoro API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header usando Bearer.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }] = []
    });
});
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "Sicomoro",
            ValidAudience = builder.Configuration["Jwt:Issuer"] ?? "Sicomoro",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();
if (app.Environment.IsDevelopment() || app.Configuration.GetValue("Swagger:Enabled", false))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (app.Configuration.GetValue("ApplyMigrationsOnStartup", false))
{
    using var scope = app.Services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var databaseTarget = GetDatabaseTarget(app.Configuration);
    logger.LogInformation(
        "Applying database migrations. Source: {DatabaseSource}. Host: {DatabaseHost}. Database: {DatabaseName}.",
        databaseTarget.Source,
        databaseTarget.Host,
        databaseTarget.Database);
    await ApplyMigrationsWithRetryAsync(
        scope.ServiceProvider.GetRequiredService<SicomoroDbContext>(),
        logger);
}

if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapGet("/health", () => Results.Ok(new { status = "ok", app = "Sicomoro" })).AllowAnonymous();
app.MapControllers();
app.MapFallbackToFile("index.html");
app.Run();

static async Task ApplyMigrationsWithRetryAsync(SicomoroDbContext dbContext, ILogger logger)
{
    const int maxAttempts = 12;
    for (var attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            await dbContext.Database.MigrateAsync();
            logger.LogInformation("Database migrations completed.");
            return;
        }
        catch (Exception ex) when (attempt < maxAttempts)
        {
            logger.LogWarning(
                ex,
                "Database migration attempt {Attempt}/{MaxAttempts} failed. Retrying in 10 seconds.",
                attempt,
                maxAttempts);
            await Task.Delay(TimeSpan.FromSeconds(10));
        }
    }

    await dbContext.Database.MigrateAsync();
}

static (string Source, string Host, string Database) GetDatabaseTarget(IConfiguration configuration)
{
    var databaseUrl = configuration["DATABASE_URL"];
    if (!string.IsNullOrWhiteSpace(databaseUrl) && Uri.TryCreate(databaseUrl, UriKind.Absolute, out var uri))
    {
        return ("DATABASE_URL", uri.Host, uri.AbsolutePath.TrimStart('/'));
    }

    var connectionString = configuration.GetConnectionString("DefaultConnection");
    if (!string.IsNullOrWhiteSpace(connectionString))
    {
        var values = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => part.Split('=', 2))
            .Where(part => part.Length == 2)
            .ToDictionary(
                part => part[0].Trim(),
                part => part[1].Trim(),
                StringComparer.OrdinalIgnoreCase);

        values.TryGetValue("Host", out var host);
        values.TryGetValue("Database", out var database);
        return ("ConnectionStrings:DefaultConnection", host ?? "(not set)", database ?? "(not set)");
    }

    return ("not configured", "(not set)", "(not set)");
}

public partial class Program { }
