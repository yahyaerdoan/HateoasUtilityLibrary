using System.Text.Json.Serialization;

namespace Hateoas;

/// <summary>Base class for response DTOs that carry HAL "_links". Use <see cref="LinkedRecord"/> for records.</summary>
public abstract class LinkedResponse : ILinkedResource
{
    [JsonPropertyName("_links")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyOrder(int.MaxValue)]
    public LinkCollection? Links { get; set; }
}
