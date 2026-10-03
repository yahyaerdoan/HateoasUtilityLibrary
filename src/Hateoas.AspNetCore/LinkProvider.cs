namespace Hateoas.AspNetCore;

/// <summary>Adds the links of one resource type. Prefer deriving from <see cref="LinkProvider{TResource}"/>.</summary>
public interface ILinkProvider
{
    Type ResourceType { get; }

    void AddLinks(object resource, HateoasLinkBuilder links);
}

/// <summary>
/// The single place that decides a resource type's links: written once per type and applied automatically
/// to every endpoint response that contains the type (single item, list, page or nested), so no endpoint can
/// forget them. Implementations must be stateless; they are registered as singletons.
/// </summary>
public abstract class LinkProvider<TResource> : ILinkProvider
    where TResource : ILinkedResource
{
    public Type ResourceType => typeof(TResource);

    void ILinkProvider.AddLinks(object resource, HateoasLinkBuilder links) => AddLinks((TResource)resource, links);

    protected abstract void AddLinks(TResource resource, HateoasLinkBuilder links);
}
