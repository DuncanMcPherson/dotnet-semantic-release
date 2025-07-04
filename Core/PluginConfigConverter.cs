using System.Text.Json;
using System.Text.Json.Serialization;

namespace SemanticRelease.Core;

public class PluginConfigConverter : JsonConverter<RawPluginData>
{
    public override RawPluginData? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var config = new RawPluginData();

        switch (reader.TokenType)
        {
            case JsonTokenType.String:
                config.PluginName = reader.GetString();
                break;
            case JsonTokenType.StartObject:
                config.Metadata = JsonSerializer.Deserialize<PluginMetadata>(ref reader, options);
                break;
            default:
                throw new JsonException("Unsupported plugin config type");
        }

        return config;
    }

    public override void Write(Utf8JsonWriter writer, RawPluginData value, JsonSerializerOptions options)
    {
        if (value.IsString)
        {
            writer.WriteStringValue(value.PluginName);
        } else if (value.IsObject && value.Metadata != null)
        {
            JsonSerializer.Serialize(writer, value.Metadata, options);
        } else
        {
            throw new JsonException("Unsupported plugin config type");
        }
    }
}