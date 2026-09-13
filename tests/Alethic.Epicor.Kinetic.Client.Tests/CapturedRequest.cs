using System;
using System.Collections.Generic;
using System.Net.Http;

namespace Alethic.Epicor.Kinetic.Client.Tests;

/// <summary>
/// Snapshot of one request as it reached the fake handler.
/// </summary>
/// <param name="Method">HTTP method.</param>
/// <param name="Uri">Absolute request URL.</param>
/// <param name="Headers">Request headers, joined with commas when repeated.</param>
/// <param name="Body">Request body as text, or <c>null</c> when there was none.</param>
/// <param name="ContentType">Content-Type of the body, or <c>null</c> when there was none.</param>
sealed record CapturedRequest(
    HttpMethod Method,
    Uri Uri,
    IReadOnlyDictionary<string, string> Headers,
    string? Body,
    string? ContentType)
{

    /// <summary>
    /// Returns the value of a request header, or <c>null</c> when it was not sent.
    /// </summary>
    public string? Header(string name)
    {
        return Headers.TryGetValue(name, out var value) ? value : null;
    }

}
