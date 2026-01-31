using System.Text.Json;
using System.Text.Json.Serialization;

namespace MageKnightOnline.Core.Definitions;

/// <summary>
/// Custom converter so "position" can be either int (0-6) or string (C, NW, NE, ...).
/// </summary>
public class TileHexDefinitionJsonConverter : JsonConverter<TileHexDefinition>
{
    private static readonly string[] IndexToCompass = { "C", "W", "NW", "NE", "E", "SE", "SW" };

    public override TileHexDefinition Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("Expected object");
        var def = new TileHexDefinition();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                return def;
            if (reader.TokenType != JsonTokenType.PropertyName)
                continue;
            var prop = reader.GetString();
            reader.Read();
            switch (prop)
            {
                case "position":
                    def.Position = ReadPosition(ref reader);
                    break;
                case "terrain":
                    def.Terrain = reader.GetString() ?? "Plains";
                    break;
                case "site":
                    def.Site = reader.TokenType == JsonTokenType.Null ? null : reader.GetString();
                    break;
                default:
                    reader.Skip();
                    break;
            }
        }
        throw new JsonException("Unexpected end");
    }

    private static string ReadPosition(ref Utf8JsonReader reader)
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

    public override void Write(Utf8JsonWriter writer, TileHexDefinition value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("position", value.Position ?? "C");
        writer.WriteString("terrain", value.Terrain ?? "Plains");
        if (value.Site != null)
            writer.WriteString("site", value.Site);
        else
            writer.WriteNull("site");
        writer.WriteEndObject();
    }
}
