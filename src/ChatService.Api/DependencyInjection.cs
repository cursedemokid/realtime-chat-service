using System.Text.Json.Serialization;
using ChatService.Api.Infrastructure;
using ChatService.Api.Hubs;
using ChatService.Api.Services;
using ChatService.Api.Services.Clients;
using ChatService.Application;
using ChatService.Application.Services;
using ChatService.Infrastructure;



namespace ChatService.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddApplication();
        services.AddInfrastructure(configuration);
        services.AddChatHealthChecks(configuration);

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

        services.AddScoped<IUserService, UserServiceClient>();
        services.AddScoped<IBloggerService, BloggerServiceClient>();
        services.AddScoped<ChatService.Application.Services.IParticipantResolver, ParticipantResolverAdapter>();
        services.AddScoped<ChatService.Application.Services.INotificationService, NotificationServiceAdapter>();
        services.AddScoped<IConversationRealtimeNotifier, ConversationHubNotifier>();
        services.AddScoped<IInboxUpdateService, InboxUpdateService>();
        services.AddSingleton<ITypingIndicatorService, InMemoryTypingIndicatorService>();

        services.Configure<PresenceOptions>(configuration.GetSection(PresenceOptions.SectionName));
        services.Configure<StorageServiceOptions>(configuration.GetSection(StorageServiceOptions.SectionName));
        services.AddHttpContextAccessor();
        services.AddScoped<LocalFileStorageService>();
        services.AddScoped<IFileStorageService>(sp => sp.GetRequiredService<LocalFileStorageService>());
        services.AddScoped<IAttachmentAccess>(sp => sp.GetRequiredService<LocalFileStorageService>());
        services.AddHostedService<PresenceSweepWorker>();

        services.AddSignalR()
            .AddJsonProtocol(options =>
            {
                options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });

        services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new() { Title = "Realtime Chat · тест-кейсы", Version = "v1" });
            options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.OpenApiSecurityScheme
            {
                Type = Microsoft.OpenApi.SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "JWT",
                Description = "Получите JWT через POST /api/dev/token. Вставьте token без префикса Bearer."
            });
            options.AddSecurityRequirement(document => new Microsoft.OpenApi.OpenApiSecurityRequirement
            {
                [new Microsoft.OpenApi.OpenApiSecuritySchemeReference("Bearer", document)] = []
            });
            options.OperationFilter<ChatService.Api.Demo.ChatExamplesOperationFilter>();
        });

        services.AddPortfolioAuthentication(configuration);

        return services;
    }
}
