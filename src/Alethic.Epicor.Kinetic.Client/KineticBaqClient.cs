using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Alethic.Epicor.Kinetic.Client.Internal;

namespace Alethic.Epicor.Kinetic.Client;

/// <inheritdoc cref="IKineticBaqClient"/>
public sealed class KineticBaqClient : IKineticBaqClient
{

    sealed class ODataCollection<T>
    {

        /// <summary>
        /// The rows of the OData collection payload.
        /// </summary>
        [JsonPropertyName("value")]
        public List<T>? Value { get; init; }

    }

    readonly HttpClient _http;
    readonly KineticOptions _options;
    readonly ILogger<KineticBaqClient> _logger;

    /// <summary>
    /// Creates a client over an HttpClient that carries the <see cref="KineticHttpHandler"/>.
    /// </summary>
    public KineticBaqClient(HttpClient httpClient, IOptions<KineticOptions> options, ILogger<KineticBaqClient> logger)
    {
        _http = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<TRow>> QueryAsync<TRow>(string baqId, BaqQuery? query = null, CancellationToken cancellationToken = default)
    {
        var uri = DataUri(baqId, query?.ToQueryString() ?? string.Empty);
        var json = _options.JsonSerializerOptions;
        var page = await KineticHttp.SendJsonAsync<ODataCollection<TRow>>(_http, _logger, HttpMethod.Get, uri, null, json, cancellationToken)
            .ConfigureAwait(false);

        return page?.Value ?? new List<TRow>();
    }

    /// <inheritdoc/>
    public Task<TRow?> AddRowAsync<TRow>(string baqId, TRow row, CancellationToken cancellationToken = default)
    {
        Guard.NotNull((object?)row, nameof(row));
        var uri = DataUri(baqId, string.Empty);
        return KineticHttp.SendJsonAsync<TRow>(_http, _logger, HttpMethod.Post, uri, row, _options.JsonSerializerOptions, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<TRow?> UpdateRowAsync<TRow>(string baqId, TRow row, CancellationToken cancellationToken = default)
    {
        Guard.NotNull((object?)row, nameof(row));
        var uri = DataUri(baqId, string.Empty);
        return KineticHttp.SendJsonAsync<TRow>(_http, _logger, KineticHttp.Patch, uri, row, _options.JsonSerializerOptions, cancellationToken);
    }

    Uri DataUri(string baqId, string queryString)
    {
        Guard.NotNullOrWhiteSpace(baqId, nameof(baqId));
        var company = KineticHttp.Segment(_options.Company);
        return KineticHttp.Combine(_http, _options, $"api/v2/odata/{company}/BaqSvc/{KineticHttp.Segment(baqId)}/Data{queryString}");
    }

}
