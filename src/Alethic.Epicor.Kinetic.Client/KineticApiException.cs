using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Alethic.Epicor.Kinetic.Client;

/// <summary>
/// Raised when the Kinetic REST API answers with a non-success status. Carries the Epicor error payload.
/// </summary>
public sealed class KineticApiException : Exception
{

    sealed class ErrorPayload
    {

        /// <summary>
        /// The <c>ErrorMessage</c> field of the payload.
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// The <c>ErrorType</c> field of the payload.
        /// </summary>
        public string? ErrorType { get; set; }

        /// <summary>
        /// The <c>CorrelationId</c> field of the payload.
        /// </summary>
        public string? CorrelationId { get; set; }

        /// <summary>
        /// The <c>ErrorDetails</c> array of the payload.
        /// </summary>
        public List<KineticErrorDetail>? ErrorDetails { get; set; }

    }

    static readonly JsonSerializerOptions _payloadJson = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Builds an exception from a failed response, parsing the JSON or XML error payload when present.
    /// </summary>
    internal static async Task<KineticApiException> CreateAsync(HttpRequestMessage request, HttpResponseMessage response)
    {
        string? body = null;
        try
        {
            body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        }
        catch
        {
            // The body is diagnostic only. Never let reading it mask the real failure.
        }

        var payload = Parse(body) ?? new ErrorPayload();

        return new KineticApiException(
            response.StatusCode,
            request.Method,
            request.RequestUri,
            payload.ErrorMessage ?? response.ReasonPhrase,
            payload.ErrorType,
            payload.CorrelationId,
            (IReadOnlyList<KineticErrorDetail>?)payload.ErrorDetails ?? Array.Empty<KineticErrorDetail>(),
            body);
    }

    static ErrorPayload? Parse(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return null;

        var trimmed = body!.TrimStart();
        try
        {
            if (trimmed.StartsWith("{", StringComparison.Ordinal))
                return JsonSerializer.Deserialize<ErrorPayload>(trimmed, _payloadJson);

            if (trimmed.StartsWith("<", StringComparison.Ordinal))
            {
                var root = XDocument.Parse(trimmed).Root;
                if (root is null)
                    return null;

                return new ErrorPayload
                {
                    ErrorMessage = Element(root, "ErrorMessage"),
                    ErrorType = Element(root, "ErrorType"),
                    CorrelationId = Element(root, "CorrelationId"),
                };
            }
        }
        catch
        {
            // Unparseable body: fall back to status and reason phrase.
        }

        return null;
    }

    static string? Element(XElement root, string localName)
    {
        return root.Elements().FirstOrDefault(e => e.Name.LocalName == localName)?.Value;
    }

    static string BuildMessage(HttpStatusCode statusCode, HttpMethod method, Uri? requestUri, string? errorMessage)
    {
        var path = requestUri?.PathAndQuery ?? "(unknown)";
        var detail = string.IsNullOrWhiteSpace(errorMessage) ? statusCode.ToString() : errorMessage;
        return $"Kinetic API returned {(int)statusCode} for {method} {path}: {detail}";
    }

    /// <summary>
    /// Creates an exception from an already parsed error payload.
    /// </summary>
    public KineticApiException(
        HttpStatusCode statusCode,
        HttpMethod method,
        Uri? requestUri,
        string? errorMessage,
        string? errorType,
        string? correlationId,
        IReadOnlyList<KineticErrorDetail> errorDetails,
        string? responseBody)
        : base(BuildMessage(statusCode, method, requestUri, errorMessage))
    {
        StatusCode = statusCode;
        Method = method;
        RequestUri = requestUri;
        ErrorMessage = errorMessage;
        ErrorType = errorType;
        CorrelationId = correlationId;
        ErrorDetails = errorDetails;
        ResponseBody = responseBody;
    }

    /// <summary>
    /// HTTP status returned by the server.
    /// </summary>
    public HttpStatusCode StatusCode { get; }

    /// <summary>
    /// HTTP method of the failed request.
    /// </summary>
    public HttpMethod Method { get; }

    /// <summary>
    /// URL of the failed request.
    /// </summary>
    public Uri? RequestUri { get; }

    /// <summary>
    /// The <c>ErrorMessage</c> field from the Epicor payload, when the body could be parsed.
    /// </summary>
    public string? ErrorMessage { get; }

    /// <summary>
    /// The <c>ErrorType</c> field, for example <c>Ice.Common.BusinessObjectException</c>.
    /// </summary>
    public string? ErrorType { get; }

    /// <summary>
    /// Server correlation id, useful when raising a case with Epicor.
    /// </summary>
    public string? CorrelationId { get; }

    /// <summary>
    /// Per-row or per-field details for business object errors.
    /// </summary>
    public IReadOnlyList<KineticErrorDetail> ErrorDetails { get; }

    /// <summary>
    /// Raw response body.
    /// </summary>
    public string? ResponseBody { get; }

}
