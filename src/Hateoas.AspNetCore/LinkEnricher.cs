using System.Collections.Concurrent;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;

namespace Hateoas.AspNetCore;

/// <summary>Fills the "_links" of every resource inside a response body that has a registered <see cref="ILinkProvider"/>.</summary>
public interface ILinkEnricher
{
    void Enrich(object body, HttpContext httpContext);
}

internal sealed class LinkEnricher(IEnumerable<ILinkProvider> providers, LinkGenerator linkGenerator, IOptions<HateoasOptions> options) : ILinkEnricher
{
    private readonly ILinkProvider[] _providers = [.. providers];
    private readonly ConcurrentDictionary<Type, ILinkProvider[]> _providersByType = new();

    public void Enrich(object body, HttpContext httpContext)
    {
        foreach (var resource in ObjectGraph.FindResources(body, type => ProvidersFor(type).Length > 0))
        {
            var providers = ProvidersFor(resource.GetType());

            var builder = new HateoasLinkBuilder(linkGenerator, httpContext, options.Value);
            foreach (var provider in providers)
            {
                provider.AddLinks(resource, builder);
            }

            var links = builder.Build();
            if (resource is ILinkedResource linkedResource)
            {
                linkedResource.Links = links.Count > 0 ? links : null;
            }
            else if (links.Count > 0)
            {
                ResourceLinkStore.Set(httpContext, resource, links);
            }
        }
    }

    private ILinkProvider[] ProvidersFor(Type type) =>
        _providersByType.GetOrAdd(type, resourceType => [.. _providers.Where(provider => provider.ResourceType.IsAssignableFrom(resourceType))]);
}
