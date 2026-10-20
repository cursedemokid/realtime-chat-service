using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace ChatService.Api;

public static class PortfolioAuthentication
{
    public static IServiceCollection AddPortfolioAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var secret = configuration["JwtOptions:SecretKey"];
        if (string.IsNullOrWhiteSpace(secret) || Encoding.UTF8.GetByteCount(secret) < 32)
            throw new InvalidOperationException("JwtOptions:SecretKey must contain at least 32 bytes.");
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true, ValidateAudience = true, ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = configuration["JwtOptions:Issuer"] ?? "ChatService",
                ValidAudience = configuration["JwtOptions:Audience"] ?? "ChatService",
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
                ClockSkew = TimeSpan.FromSeconds(15)
            };
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    if (context.Request.Path.StartsWithSegments("/chatHub") ||
                        context.Request.Path.StartsWithSegments("/api/chat/hub"))
                        context.Token = context.Request.Query["access_token"];
                    return Task.CompletedTask;
                }
            };
        });
        services.AddAuthorization();
        return services;
    }
}
