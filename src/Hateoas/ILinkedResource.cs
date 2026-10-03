namespace Hateoas;

/// <summary>
/// A resource representation that can carry HAL "_links". Implemented by <see cref="LinkedResponse"/> (classes)
/// and <see cref="LinkedRecord"/> (records). <see cref="Links"/> stays null, and is left out of the JSON, until
/// a link is added.
/// </summary>
public interface ILinkedResource
{
    LinkCollection? Links { get; set; }
}
