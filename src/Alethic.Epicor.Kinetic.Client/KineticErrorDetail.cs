namespace Alethic.Epicor.Kinetic.Client;

/// <summary>
/// One entry from the Epicor <c>ErrorDetails</c> array.
/// </summary>
public sealed record KineticErrorDetail
{

    /// <summary>
    /// Human-readable description of the problem.
    /// </summary>
    public string? Message { get; init; }

    /// <summary>
    /// Severity as reported by Kinetic, typically <c>Error</c> or <c>Warning</c>.
    /// </summary>
    public string? Type { get; init; }

    /// <summary>
    /// Tableset table the problem relates to, when known.
    /// </summary>
    public string? Table { get; init; }

    /// <summary>
    /// Column the problem relates to, when known.
    /// </summary>
    public string? Field { get; init; }

    /// <summary>
    /// Server assembly that raised the error.
    /// </summary>
    public string? Program { get; init; }

    /// <summary>
    /// Server method that raised the error.
    /// </summary>
    public string? Method { get; init; }

}
