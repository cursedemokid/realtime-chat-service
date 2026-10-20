using ChatService.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ChatService.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddAutoMapper(cfg => cfg.AddMaps(typeof(DependencyInjection).Assembly));
        services.AddScoped<IPresenceService, PresenceService>();
        services.AddScoped<IConversationSummaryBuilder, ConversationSummaryBuilder>();
        return services;
    }
}
