using System.Text.Json;
using System.Text.Json.Serialization;

namespace Alethic.Epicor.Kinetic.Client;

/// <summary>
/// Response envelope for business object method calls. Kinetic returns the method's return value as <c>returnObj</c>
/// and its <c>ref</c>/<c>out</c> parameters as <c>parameters</c>.
/// </summary>
public class BoResponse<TReturn>
{

    /// <summary>
    /// The method's return value, or <c>default</c> for a void method.
    /// </summary>
    [JsonPropertyName("returnObj")]
    public TReturn? ReturnObj { get; init; }

    /// <summary>
    /// The method's <c>ref</c> and <c>out</c> parameters as raw JSON, keyed by parameter name.
    /// </summary>
    [JsonPropertyName("parameters")]
    public JsonElement? Parameters { get; init; }

}
