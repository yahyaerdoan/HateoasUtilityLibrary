using System.Text.Json.Serialization;

namespace Hateoas;

/// <summary>Base record for response DTOs that carry HAL "_links" (records cannot derive from <see cref="LinkedResponse"/>).</summary>
public abstract record LinkedRecord : ILinkedResource
{
    [JsonPropertyName("_links")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyOrder(int.MaxValue)]
    public LinkCollection? Links { get; set; }
}
