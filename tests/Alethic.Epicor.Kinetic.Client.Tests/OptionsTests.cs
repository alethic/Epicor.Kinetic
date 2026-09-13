using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Alethic.Epicor.Kinetic.Client.Tests;

public class OptionsTests
{

    /// <summary>
    /// Every option binds from a configuration section, and the base address gains its trailing slash.
    /// </summary>
    [Fact]
    public void Options_bind_from_a_configuration_section()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Kinetic:BaseAddress"] = "https://example.epicorsaas.com/prod",
                ["Kinetic:Company"] = "100",
                ["Kinetic:ApiKey"] = "key-123",
                ["Kinetic:Username"] = "svc.user",
                ["Kinetic:Password"] = "p@ss",
                ["Kinetic:Plant"] = "MfgSys",
                ["Kinetic:Timeout"] = "00:00:30",
            })
            .Build();
        var services = new ServiceCollection();
        services.AddKineticClient(configuration.GetSection(KineticOptions.SectionName));
        using var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<KineticOptions>>().Value;

        Assert.Equal("100", options.Company);
        Assert.Equal("MfgSys", options.Plant);
        Assert.Equal(TimeSpan.FromSeconds(30), options.Timeout);
        Assert.Equal(new Uri("https://example.epicorsaas.com/prod/"), options.GetBaseUri());
        Assert.Same(KineticJson.Default, options.JsonSerializerOptions);
        Assert.NotNull(provider.GetRequiredService<IKineticFunctionClient>());
    }

    /// <summary>
    /// A missing API key is reported when the first client is resolved.
    /// </summary>
    [Fact]
    public void Missing_api_key_fails_validation_when_a_client_is_resolved()
    {
        var services = new ServiceCollection();
        services.AddKineticClient(options =>
        {
            options.BaseAddress = "https://example.epicorsaas.com/prod/";
            options.Company = "100";
            options.Username = "svc.user";
            options.Password = "p@ss";
        });
        using var provider = services.BuildServiceProvider();

        var ex = Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IKineticServiceClient>());

        Assert.Contains("ApiKey", ex.Message);
    }

    /// <summary>
    /// A token provider satisfies the credential requirement on its own.
    /// </summary>
    [Fact]
    public void Missing_credentials_pass_validation_when_a_token_provider_is_set()
    {
        var services = new ServiceCollection();
        services.AddKineticClient(options =>
        {
            options.BaseAddress = "https://example.epicorsaas.com/prod/";
            options.Company = "100";
            options.ApiKey = "key-123";
            options.AccessTokenProvider = _ => new ValueTask<string>("token");
        });
        using var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetRequiredService<IKineticBaqClient>());
    }

    /// <summary>
    /// A relative base address is rejected.
    /// </summary>
    [Fact]
    public void Relative_base_address_fails_validation()
    {
        var services = new ServiceCollection();
        services.AddKineticClient(options =>
        {
            options.BaseAddress = "prod/";
            options.Company = "100";
            options.ApiKey = "key-123";
            options.Username = "svc.user";
            options.Password = "p@ss";
        });
        using var provider = services.BuildServiceProvider();

        var ex = Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IKineticFunctionClient>());

        Assert.Contains("BaseAddress", ex.Message);
    }

}
