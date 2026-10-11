using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.Http;

namespace Hateoas.AspNetCore;

/// <summary>Adds a HAL "_links" member, written last and only when present, to every type a link provider serves that doesn't carry its own <see cref="ILinkedResource.Links"/>.</summary>
internal sealed class LinksJsonTypeInfoModifier(IEnumerable<ILinkProvider> providers, IHttpContextAccessor httpContextAccessor)
{
    private const string LinksPropertyName = "_links";

    private readonly Type[] _resourceTypes = [.. providers.Select(provider => provider.ResourceType).Distinct()];

    public void Modify(JsonTypeInfo typeInfo)
    {
        if (typeInfo.Kind != JsonTypeInfoKind.Object
            || typeof(ILinkedResource).IsAssignableFrom(typeInfo.Type)
            || !_resourceTypes.Any(resourceType => resourceType.IsAssignableFrom(typeInfo.Type))
            || typeInfo.Properties.Any(property => property.Name == LinksPropertyName))
        {
            return;
        }

        var links = typeInfo.CreateJsonPropertyInfo(typeof(LinkCollection), LinksPropertyName);
        links.Get = resource => ResourceLinkStore.Get(httpContextAccessor.HttpContext, resource);
        links.ShouldSerialize = (_, value) => value is not null;
        links.Order = int.MaxValue;
        typeInfo.Properties.Add(links);
    }
}
