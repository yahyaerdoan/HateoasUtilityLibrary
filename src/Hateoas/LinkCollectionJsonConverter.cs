using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hateoas;

/// <summary>Writes a <see cref="LinkCollection"/> as a HAL "_links" object (links by relation, then "curies") and reads it back.</summary>
public sealed class LinkCollectionJsonConverter : JsonConverter<LinkCollection>
{
    public override LinkCollection Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Expected a HAL \"_links\" object.");
        }

        var links = new LinkCollection();
        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            var rel = reader.GetString() ?? throw new JsonException("A link relation name is missing.");
            _ = reader.Read();

            if (rel == LinkRelations.Curies)
            {
                foreach (var curie in JsonSerializer.Deserialize<List<Curie>>(ref reader, options) ?? [])
                {
                    _ = links.AddCurie(curie);
                }
            }
            else
            {
                _ = links.Set(rel, JsonSerializer.Deserialize<Link>(ref reader, options) ?? throw new JsonException($"The \"{rel}\" link is empty."));
            }
        }

        return links;
    }

    public override void Write(Utf8JsonWriter writer, LinkCollection value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        foreach (var (rel, link) in value)
        {
            writer.WritePropertyName(rel);
            JsonSerializer.Serialize(writer, link, options);
        }

        if (value.Curies.Count > 0)
        {
            writer.WritePropertyName(LinkRelations.Curies);
            JsonSerializer.Serialize(writer, value.Curies, options);
        }

        writer.WriteEndObject();
    }
}
