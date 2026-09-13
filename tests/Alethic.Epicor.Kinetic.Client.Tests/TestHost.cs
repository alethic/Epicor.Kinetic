using System;
using System.Text;
using Microsoft.Extensions.DependencyInjection;

namespace Alethic.Epicor.Kinetic.Client.Tests;

/// <summary>
/// Builds a real DI container with the Kinetic clients registered against a <see cref="FakeHttpHandler"/>.
/// </summary>
static class TestHost
{

    /// <summary>
    /// Instance root used by every test, deliberately without a trailing slash.
    /// </summary>
    public const string BaseAddress = "https://example.epicorsaas.com/prod";

    /// <summary>
    /// Company ID used by every test.
    /// </summary>
    public const string Company = "100";

    /// <summary>
    /// API key the handler is expected to send.
    /// </summary>
    public const string ApiKey = "key-123";

    /// <summary>
    /// Basic auth user.
    /// </summary>
    public const string Username = "svc.user";

    /// <summary>
    /// Basic auth password.
    /// </summary>
    public const string Password = "p@ss";

    /// <summary>
    /// The Authorization header value the handler is expected to send for the test credentials.
    /// </summary>
    public static string ExpectedBasicAuth => "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes($"{Username}:{Password}"));

    /// <summary>
    /// Creates a service provider wired to a fresh fake handler. The base address deliberately lacks a trailing slash.
    /// </summary>
    public static (ServiceProvider Provider, FakeHttpHandler Handler) Build(Action<KineticOptions>? configure = null)
    {
        var handler = new FakeHttpHandler();
        var services = new ServiceCollection();
        services.AddKineticClient(
            options =>
            {
                options.BaseAddress = BaseAddress;
                options.Company = Company;
                options.ApiKey = ApiKey;
                options.Username = Username;
                options.Password = Password;
                configure?.Invoke(options);
            },
            http => http.ConfigurePrimaryHttpMessageHandler(() => handler));

        return (services.BuildServiceProvider(), handler);
    }

}
