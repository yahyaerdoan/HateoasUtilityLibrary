namespace Hateoas;

/// <summary>
/// A resource representation that carries its own HAL "_links". Optional: with Hateoas.AspNetCore any class can be a resource
/// and its links are written as "_links" without this interface. Implemented by <see cref="LinkedResponse"/> (classes)
/// and <see cref="LinkedRecord"/> (records). <see cref="Links"/> stays null, and is left out of the JSON, until
/// a link is added.
/// </summary>
public interface ILinkedResource
{
    LinkCollection? Links { get; set; }
}
