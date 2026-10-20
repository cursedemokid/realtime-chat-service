using ChatService.Application.Abstractions;
using ChatService.Application.Services;
using ChatService.Domain.Repositories;
using ChatService.Infrastructure.Messaging;
using ChatService.Infrastructure.Persistence;
using ChatService.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ChatService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ConversationDbContext>(options =>
            options.UseNpgsql(ChatDbConnection.Resolve(configuration)));

        services.AddScoped<IConversationRepository, EfConversationRepository>();
        services.AddScoped<IMessageRepository, EfMessageRepository>();
        services.AddScoped<ISupportTicketRepository, EfSupportTicketRepository>();
        services.AddScoped<IConversationAuditRepository, EfConversationAuditRepository>();
        services.AddScoped<IMessageVersionRepository, EfMessageVersionRepository>();
        services.AddScoped<IUserPresenceRepository, EfUserPresenceRepository>();
        services.AddScoped<IParticipantNotificationMuteRepository, EfParticipantNotificationMuteRepository>();

        services.AddScoped<IConversationAuditService, ConversationAuditService>();

        services.AddSingleton<IOptions<RabbitMqOptions>>(_ =>
        {
            var options = new RabbitMqOptions();
            configuration.GetSection(RabbitMqOptions.SectionName).Bind(options);
            return Options.Create(options);
        });
        services.AddSingleton<INotificationPublisher, RabbitMqNotificationPublisher>();

        services.AddScoped<EfUnitOfWork>();
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<EfUnitOfWork>());
        services.AddScoped<IUnitOfWorkExecutor>(sp => sp.GetRequiredService<EfUnitOfWork>());

        return services;
    }
}
