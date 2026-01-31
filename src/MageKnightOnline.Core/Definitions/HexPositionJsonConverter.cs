using System.Text.Json;
using System.Text.Json.Serialization;

namespace MageKnightOnline.Core.Definitions;

/// <summary>
/// Deserializes tile hex position from either old format (int 0-6) or new format (string C, NW, NE, E, SE, SW, W).
/// Old index mapping: 0=C, 1=W, 2=NW, 3=NE, 4=E, 5=SE, 6=SW.
/// </summary>
public class HexPositionJsonConverter : JsonConverter<string>
{
    private static readonly string[] IndexToCompass = { "C", "W", "NW", "NE", "E", "SE", "SW" };

    public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out var index))
        {
            if (index >= 0 && index <= 6)
                return IndexToCompass[index];
            return "C";
        }
        if (reader.TokenType == JsonTokenType.String && reader.GetString() is { } s)
            return string.IsNullOrEmpty(s) ? "C" : s.ToUpperInvariant();
        return "C";
    }

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value ?? "C");
    }
}
