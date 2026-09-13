using System.Text.Json;
using System.Text.Json.Serialization;

namespace Alethic.Epicor.Kinetic.Client;

/// <summary>
/// Default JSON settings for Kinetic payloads. Property names are written exactly as declared, because Kinetic mixes
/// conventions: method parameters are camelCase (<c>custNum</c>, <c>ds</c>) while tableset columns are PascalCase
/// (<c>CustNum</c>, <c>RowMod</c>). Reading is case-insensitive.
/// </summary>
public static class KineticJson
{

    /// <summary>
    /// Shared default instance.
    /// </summary>
    public static JsonSerializerOptions Default { get; } = Create();

    /// <summary>
    /// Creates a fresh, mutable copy of the default settings.
    /// </summary>
    public static JsonSerializerOptions Create()
    {
        return new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            PropertyNamingPolicy = null,
            DictionaryKeyPolicy = null,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };
    }

}
