using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Zelo.Application.Contract;
using Zelo.Application.Mapping;
using Zelo.Application.Model.Auths;
using Zelo.Application.Services;
using Zelo.Application.Shared.Auths;
using Zelo.Application.Shared.Auths.Jwt;
using Zelo.Domain.Users;
using Zelo.Infrastructure.Data;
using Zelo.Infrastructure.Shared.Auths;
using Zelo.Infrastructure.Shared.Auths.Jwt;
using Zelo.Infrastructure.Shared.DependencyInjection;
using Zelo.Web.Api.ExceptionHandling;
using Zelo.Web.Api.Modules;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
var configuration = builder.Configuration;
var environment = builder.Environment;

#region Infrastructure and Modules

var connectionString = configuration.GetConnectionString("Zelo")
    ?? throw new InvalidOperationException("ConnectionStrings:Zelo is required.");
services.AddHttpContextAccessor();
services.AddZeloInfrastructure(_ =>
{
    return NHibernateSessionFactory.Create(connectionString);
}).AddModules<ZeloModule>();

#endregion

#region Authentication Options and Services

var jwtOptions = new JwtOptions(
    configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("Jwt:Issuer is required."),
    configuration["Jwt:Audience"] ?? throw new InvalidOperationException("Jwt:Audience is required."),
    configuration["Jwt:SigningKey"] ?? throw new InvalidOperationException("Jwt:SigningKey is required."),
    TimeSpan.FromMinutes(configuration.GetValue<int>("Jwt:ExpirationMinutes")));

services.AddSingleton(jwtOptions);
services.AddSingleton(new AuthSessionOptions(TimeSpan.FromDays(
    configuration.GetValue<int>("Auth:RefreshExpirationDays", 30))));
services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
services.AddSingleton<IPasswordHasher<User>, PasswordHasherService<User>>();
services.AddScoped<IAuthAppService, ZeloJwtBearerAuthService>();
services.AddScoped<IAuthService<User, AuthResultDto>>(provider =>
{
    return provider.GetRequiredService<IAuthAppService>();
});

var totpKey = configuration["Auth:TotpEncryptionKey"];
if (string.IsNullOrWhiteSpace(totpKey))
{
    if (!environment.IsDevelopment())
    {
        throw new InvalidOperationException("Auth:TotpEncryptionKey is required.");
    }

    totpKey = Convert.ToBase64String(SHA256.HashData(
        Encoding.UTF8.GetBytes(jwtOptions.SigningKey + ":development-totp")));
}

services.AddSingleton<ISecretProtector>(new AesSecretProtector(totpKey));

#endregion

#region Email Delivery

var emailDelivery = configuration["Auth:EmailDelivery"]
    ?? (environment.IsDevelopment() ? "File" : "Smtp");
if (string.Equals(emailDelivery, "File", StringComparison.OrdinalIgnoreCase))
{
    var outbox = configuration["Auth:DevelopmentOutbox"]
        ?? Path.Combine(Path.GetTempPath(), "zelo-auth-outbox");
    services.AddSingleton<IAuthEmailSender>(new FileAuthEmailSender(outbox));
}
else if (string.Equals(emailDelivery, "Smtp", StringComparison.OrdinalIgnoreCase))
{
    var host = configuration["Auth:Smtp:Host"]
        ?? throw new InvalidOperationException("Auth:Smtp:Host is required.");
    var from = configuration["Auth:Smtp:From"]
        ?? throw new InvalidOperationException("Auth:Smtp:From is required.");
    var port = configuration.GetValue<int>("Auth:Smtp:Port", 587);
    var username = configuration["Auth:Smtp:Username"] ?? string.Empty;
    var password = configuration["Auth:Smtp:Password"] ?? string.Empty;
    var securityValue = configuration["Auth:Smtp:Security"] ?? "StartTls";
    if (!Enum.TryParse<SmtpSecurity>(securityValue, true, out var security) ||
        !Enum.IsDefined(security))
    {
        throw new InvalidOperationException("Auth:Smtp:Security must be StartTls, SslOnConnect, or None.");
    }

    if (security == SmtpSecurity.None &&
        (!environment.IsDevelopment() ||
         !(string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase) ||
           (IPAddress.TryParse(host, out var address) && IPAddress.IsLoopback(address)))))
    {
        throw new InvalidOperationException(
            "Unencrypted SMTP is allowed only for a loopback host in development.");
    }

    services.AddSingleton<IAuthEmailSender>(
        new SmtpAuthEmailSender(host, port, username, password, from, security));
}
else
{
    throw new InvalidOperationException("Auth:EmailDelivery must be File or Smtp.");
}

#endregion

#region HTTP Security and Rate Limiting

services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    var permitLimit = configuration.GetValue<int>("Auth:RateLimitPerMinute", 30);
    if (permitLimit < 1)
    {
        throw new InvalidOperationException("Auth:RateLimitPerMinute must be positive.");
    }

    options.AddPolicy("auth", context =>
    {
        var address = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(address, _ =>
        {
            return new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            };
        });
    });
});

services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var subject = context.Principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
                var session = context.Principal?.FindFirst("sid")?.Value;
                if (!Guid.TryParse(subject, out var userId) ||
                    !Guid.TryParse(session, out var sessionId) ||
                    !await context.HttpContext.RequestServices
                        .GetRequiredService<IUserAuthStore<User>>()
                        .IsActiveAsync(userId, context.HttpContext.RequestAborted) ||
                    !await context.HttpContext.RequestServices
                        .GetRequiredService<IAuthSessionStore>()
                        .IsActiveAsync(userId, sessionId, context.HttpContext.RequestAborted))
                {
                    context.Fail("User is not active.");
                }
            }
        };
    });
services.AddAuthorization();

#endregion

#region API and Mapping

services.AddExceptionHandler<ApplicationExceptionHandler>();
services.AddProblemDetails();
services.AddControllers();
services.AddOpenApi();
services.AddAutoMapper(mapper =>
{
    var licenseKey = configuration["AutoMapper:LicenseKey"]
        ?? Environment.GetEnvironmentVariable("AUTOMAPPER_LICENSE_KEY");
    if (!environment.IsDevelopment() && string.IsNullOrWhiteSpace(licenseKey))
    {
        throw new InvalidOperationException("AutoMapper:LicenseKey is required outside development.");
    }

    if (!string.IsNullOrWhiteSpace(licenseKey))
    {
        mapper.LicenseKey = licenseKey;
    }
}, typeof(ApplicationMappingProfile).Assembly);

#endregion

var app = builder.Build();
app.UseExceptionHandler();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
