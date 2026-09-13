using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Alethic.Epicor.Kinetic.Client.Internal;

namespace Alethic.Epicor.Kinetic.Client;

/// <summary>
/// Bottom layer. Turns any <see cref="HttpClient"/> into an authenticated Kinetic client by adding the API key,
/// credentials, a JSON Accept header and the optional CallSettings header, and by logging each request at Debug
/// level. Every other class in this library sits on top of it, including the OData contexts.
/// </summary>
public sealed partial class KineticHttpHandler : DelegatingHandler
{

    static partial class Log
    {

        [LoggerMessage(
            EventId = 1,
            Level = LogLevel.Debug,
            Message = "Kinetic {Method} {Uri} responded {StatusCode} in {ElapsedMs:F0} ms")]
        public static partial void RequestCompleted(ILogger logger, string method, Uri? uri, int statusCode, double elapsedMs);

    }

    static readonly MediaTypeWithQualityHeaderValue _jsonMediaType = new("application/json");

    static string? BuildCallSettings(KineticOptions options)
    {
        if (string.IsNullOrEmpty(options.Plant) && string.IsNullOrEmpty(options.Language) && string.IsNullOrEmpty(options.FormatCulture))
            return null;

        var settings = new Dictionary<string, string> { ["Company"] = options.Company };
        if (!string.IsNullOrEmpty(options.Plant))
            settings["Plant"] = options.Plant!;

        if (!string.IsNullOrEmpty(options.Language))
            settings["Language"] = options.Language!;

        if (!string.IsNullOrEmpty(options.FormatCulture))
            settings["FormatCulture"] = options.FormatCulture!;

        return JsonSerializer.Serialize(settings);
    }

    readonly KineticOptions _options;
    readonly ILogger<KineticHttpHandler> _logger;
    readonly string? _basicCredentials;
    readonly string? _callSettings;

    /// <summary>
    /// Creates a handler that authenticates with the given options.
    /// </summary>
    public KineticHttpHandler(IOptions<KineticOptions> options, ILogger<KineticHttpHandler> logger)
    {
        _options = Guard.NotNull(options, nameof(options)).Value;
        _logger = Guard.NotNull(logger, nameof(logger));

        if (_options.AccessTokenProvider is null)
            _basicCredentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_options.Username}:{_options.Password}"));

        _callSettings = BuildCallSettings(_options);
    }

    /// <summary>
    /// Adds the Kinetic headers when the request does not already carry them, sends it, and logs the outcome.
    /// </summary>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.Headers.Authorization ??= await CreateAuthorizationAsync(cancellationToken).ConfigureAwait(false);

        if (!request.Headers.Contains("x-api-key"))
            request.Headers.TryAddWithoutValidation("x-api-key", _options.ApiKey);

        if (request.Headers.Accept.Count == 0)
            request.Headers.Accept.Add(_jsonMediaType);

        if (_callSettings is not null && !request.Headers.Contains("CallSettings"))
            request.Headers.TryAddWithoutValidation("CallSettings", _callSettings);

        var start = Stopwatch.GetTimestamp();
        var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var elapsedMs = (Stopwatch.GetTimestamp() - start) * 1000.0 / Stopwatch.Frequency;
        Log.RequestCompleted(_logger, request.Method.Method, request.RequestUri, (int)response.StatusCode, elapsedMs);
        return response;
    }

    async ValueTask<AuthenticationHeaderValue> CreateAuthorizationAsync(CancellationToken cancellationToken)
    {
        if (_options.AccessTokenProvider is { } provider)
            return new AuthenticationHeaderValue("Bearer", await provider(cancellationToken).ConfigureAwait(false));

        return new AuthenticationHeaderValue("Basic", _basicCredentials);
    }

}
