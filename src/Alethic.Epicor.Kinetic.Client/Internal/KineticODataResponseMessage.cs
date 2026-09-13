using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using Microsoft.OData;

namespace Alethic.Epicor.Kinetic.Client.Internal;

/// <summary>
/// Exposes a buffered <see cref="HttpResponseMessage"/> to Microsoft.OData.Client.
/// </summary>
sealed class KineticODataResponseMessage : IODataResponseMessage, IDisposable
{

    readonly HttpResponseMessage _response;
    readonly Stream _stream;
    readonly Dictionary<string, string> _headers = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Wraps a response whose body has already been buffered into <paramref name="stream"/>.
    /// </summary>
    public KineticODataResponseMessage(HttpResponseMessage response, Stream stream)
    {
        _response = response;
        _stream = stream;

        foreach (var header in response.Headers)
            _headers[header.Key] = string.Join(",", header.Value);

        foreach (var header in response.Content.Headers)
            _headers[header.Key] = string.Join(",", header.Value);
    }

    /// <summary>
    /// Response and content headers, merged.
    /// </summary>
    public IEnumerable<KeyValuePair<string, string>> Headers => _headers;

    /// <summary>
    /// HTTP status code of the response. Setting it is not supported.
    /// </summary>
    public int StatusCode
    {
        get => (int)_response.StatusCode;
        set => throw new NotSupportedException("The status code of a received response cannot be changed.");
    }

    /// <summary>
    /// Returns a response header value, or <c>null</c> when the server did not send it.
    /// </summary>
    public string GetHeader(string headerName)
    {
        return _headers.TryGetValue(headerName, out var value) ? value : null!;
    }

    /// <summary>
    /// Overrides a response header value as seen by the OData reader.
    /// </summary>
    public void SetHeader(string headerName, string headerValue)
    {
        _headers[headerName] = headerValue;
    }

    /// <summary>
    /// Returns the fully buffered response body.
    /// </summary>
    public Stream GetStream()
    {
        return _stream;
    }

    /// <summary>
    /// Releases the body and the underlying response.
    /// </summary>
    public void Dispose()
    {
        _stream.Dispose();
        _response.Dispose();
    }

}
