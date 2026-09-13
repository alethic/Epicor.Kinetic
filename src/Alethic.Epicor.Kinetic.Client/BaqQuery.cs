using System;
using System.Collections.Generic;

namespace Alethic.Epicor.Kinetic.Client;

/// <summary>
/// Query options for a BAQ call. OData options map to <c>$filter</c> and friends; <see cref="Parameters"/> are BAQ
/// parameters.
/// </summary>
public sealed record BaqQuery
{

    static void Add(List<string> parts, string key, string? value)
    {
        if (!string.IsNullOrEmpty(value))
            parts.Add($"{key}={Uri.EscapeDataString(value)}");
    }

    /// <summary>
    /// OData <c>$filter</c> expression over the BAQ's output columns, for example <c>Customer_CustID eq 'ACME'</c>.
    /// </summary>
    public string? Filter { get; init; }

    /// <summary>
    /// Comma-separated output columns to return, as OData <c>$select</c>.
    /// </summary>
    public string? Select { get; init; }

    /// <summary>
    /// OData <c>$orderby</c> expression.
    /// </summary>
    public string? OrderBy { get; init; }

    /// <summary>
    /// Maximum number of rows to return, as OData <c>$top</c>.
    /// </summary>
    public int? Top { get; init; }

    /// <summary>
    /// Number of rows to skip, as OData <c>$skip</c>.
    /// </summary>
    public int? Skip { get; init; }

    /// <summary>
    /// BAQ parameters, sent by name in the query string.
    /// </summary>
    public IReadOnlyDictionary<string, string>? Parameters { get; init; }

    /// <summary>
    /// Renders the options as a URL query string starting with <c>?</c>, or an empty string when nothing is set.
    /// </summary>
    internal string ToQueryString()
    {
        var parts = new List<string>();
        Add(parts, "$filter", Filter);
        Add(parts, "$select", Select);
        Add(parts, "$orderby", OrderBy);

        if (Top is { } top)
            parts.Add($"$top={top}");

        if (Skip is { } skip)
            parts.Add($"$skip={skip}");

        if (Parameters is not null)
        {
            foreach (var parameter in Parameters)
                parts.Add($"{Uri.EscapeDataString(parameter.Key)}={Uri.EscapeDataString(parameter.Value)}");
        }

        return parts.Count == 0 ? string.Empty : "?" + string.Join("&", parts);
    }

}
