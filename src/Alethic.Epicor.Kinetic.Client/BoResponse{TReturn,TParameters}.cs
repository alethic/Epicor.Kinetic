using System.Text.Json.Serialization;

namespace Alethic.Epicor.Kinetic.Client;

/// <summary>
/// Response envelope with typed <c>parameters</c>, for methods that return data through ref/out arguments.
/// </summary>
public sealed class BoResponse<TReturn, TParameters>
{

    /// <summary>
    /// The method's return value, or <c>default</c> for a void method.
    /// </summary>
    [JsonPropertyName("returnObj")]
    public TReturn? ReturnObj { get; init; }

    /// <summary>
    /// The method's <c>ref</c> and <c>out</c> parameters, deserialised into <typeparamref name="TParameters"/>.
    /// </summary>
    [JsonPropertyName("parameters")]
    public TParameters? Parameters { get; init; }

}
