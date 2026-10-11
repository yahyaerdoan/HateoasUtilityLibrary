using Microsoft.AspNetCore.Http;

namespace Hateoas.AspNetCore;

/// <summary>
/// The links of plain (non-<see cref="ILinkedResource"/>) resources, kept per request rather than on the objects, so a resource type needs
/// no base class and one instance shared between requests never shows another caller's links.
/// </summary>
internal static class ResourceLinkStore
{
    private static readonly object ItemsKey = new();

    public static void Set(HttpContext httpContext, object resource, LinkCollection links)
    {
        if (httpContext.Items[ItemsKey] is not Dictionary<object, LinkCollection> store)
        {
            store = new Dictionary<object, LinkCollection>(ReferenceEqualityComparer.Instance);
            httpContext.Items[ItemsKey] = store;
        }

        store[resource] = links;
    }

    public static LinkCollection? Get(HttpContext? httpContext, object resource) =>
        httpContext?.Items[ItemsKey] is Dictionary<object, LinkCollection> store && store.TryGetValue(resource, out var links) ? links : null;
}
