using System.Text.Json.Serialization;

namespace Hateoas;

/// <summary>
/// A single HAL link object: where it points and, as a widely used extension to HAL, the HTTP method the
/// target expects. <see cref="Title"/> is an optional human-readable label; <see cref="Templated"/> marks
/// an RFC 6570 URI template.
/// </summary>
public sealed class Link
{
    [JsonPropertyName("href")]
    public required string Href { get; init; }

    [JsonPropertyName("method")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Method { get; init; }

    [JsonPropertyName("title")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Title { get; init; }

    [JsonPropertyName("templated")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool Templated { get; init; }
}
