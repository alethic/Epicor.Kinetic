using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Alethic.Epicor.Kinetic.Client.Internal;

/// <summary>
/// Shared JSON request plumbing used by the typed clients.
/// </summary>
static partial class KineticHttp
{

    static partial class Log
    {

        [LoggerMessage(
            EventId = 2,
            Level = LogLevel.Warning,
            Message = "Kinetic API error {StatusCode} on {Method} {Uri}: {ErrorType} {ErrorMessage} (correlation {CorrelationId})")]
        public static partial void ApiError(
            ILogger logger,
            int statusCode,
            string method,
            Uri uri,
            string? errorType,
            string? errorMessage,
            string? correlationId);

    }

    /// <summary>
    /// Serialises to <c>{}</c>. Kinetic wants a JSON object body even for parameterless calls.
    /// </summary>
    public static object EmptyBody { get; } = new { };

    /// <summary>
    /// <c>HttpMethod.Patch</c> is missing from netstandard2.0.
    /// </summary>
    public static HttpMethod Patch { get; } = new("PATCH");

    /// <summary>
    /// Resolves a relative API path against the client's base address, or the configured one when none is set.
    /// </summary>
    public static Uri Combine(HttpClient http, KineticOptions options, string relativePath)
    {
        return new Uri(http.BaseAddress ?? options.GetBaseUri(), relativePath);
    }

    /// <summary>
    /// Escapes a value for use as a single URL path segment.
    /// </summary>
    public static string Segment(string value)
    {
        return Uri.EscapeDataString(value);
    }

    /// <summary>
    /// Sends an optional JSON body and deserialises the JSON response. Throws <see cref="KineticApiException"/> on a
    /// non-success status. Returns <c>default</c> for an empty response.
    /// </summary>
    public static async Task<T?> SendJsonAsync<T>(
        HttpClient http,
        ILogger logger,
        HttpMethod method,
        Uri uri,
        object? body,
        JsonSerializerOptions json,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, uri);
        if (body is not null)
            request.Content = new StringContent(JsonSerializer.Serialize(body, body.GetType(), json), Encoding.UTF8, "application/json");

        using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            var error = await KineticApiException.CreateAsync(request, response).ConfigureAwait(false);
            Log.ApiError(logger, (int)error.StatusCode, method.Method, uri, error.ErrorType, error.ErrorMessage, error.CorrelationId);
            throw error;
        }

        if (response.StatusCode == HttpStatusCode.NoContent || response.Content.Headers.ContentLength == 0)
            return default;

        using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
        return await JsonSerializer.DeserializeAsync<T>(stream, json, cancellationToken).ConfigureAwait(false);
    }

}
