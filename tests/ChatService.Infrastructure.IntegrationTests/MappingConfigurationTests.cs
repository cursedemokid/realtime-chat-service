using AutoMapper;
using ChatService.Application;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ChatService.Infrastructure.IntegrationTests;

public class MappingConfigurationTests
{
    [Fact]
    public void Application_mappings_are_valid()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();

        using var provider = services.BuildServiceProvider();
        var mapper = provider.GetRequiredService<IMapper>();

        mapper.ConfigurationProvider.AssertConfigurationIsValid();
    }
}
