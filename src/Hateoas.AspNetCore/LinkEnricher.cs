using System.Collections.Concurrent;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;

namespace Hateoas.AspNetCore;

/// <summary>Fills the "_links" of every <see cref="ILinkedResource"/> inside a response body from the registered <see cref="ILinkProvider"/>s.</summary>
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
        foreach (var resource in ObjectGraph.FindResources(body))
        {
            var providers = _providersByType.GetOrAdd(resource.GetType(), type => [.. _providers.Where(provider => provider.ResourceType.IsAssignableFrom(type))]);
            if (providers.Length == 0)
            {
                continue;
            }

            var builder = new HateoasLinkBuilder(linkGenerator, httpContext, options.Value);
            foreach (var provider in providers)
            {
                provider.AddLinks(resource, builder);
            }

            var links = builder.Build();
            resource.Links = links.Count > 0 ? links : null;
        }
    }
}
