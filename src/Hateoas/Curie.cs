using System.Text.Json.Serialization;

namespace Hateoas;

/// <summary>
/// A HAL CURIE definition: lets extension relation types be written compactly as "{Name}:{rel}" while
/// <see cref="Href"/> (an RFC 6570 template containing "{rel}") points at each relation's documentation.
/// Keeps relation keys stable across environments, unlike keys built from the request host.
/// </summary>
public sealed class Curie
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("href")]
    public required string Href { get; init; }

    [JsonPropertyName("templated")]
    public bool Templated => true;
}
