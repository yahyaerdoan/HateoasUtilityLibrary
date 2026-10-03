using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Hateoas.AspNetCore;

public static class EndpointConventionBuilderExtensions
{
    /// <summary>
    /// Applies hypermedia to the endpoint(s). Call once on the route group every endpoint is mapped under, so
    /// new endpoints get links without any per-endpoint code.
    /// </summary>
    public static TBuilder WithHateoas<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
        => builder.AddEndpointFilter<TBuilder, HateoasEndpointFilter>();

    /// <summary>Sets the Location header of this endpoint's 201 Created response to <paramref name="getRouteName"/> for the new id.</summary>
    public static TBuilder WithCreatedLocation<TBuilder>(this TBuilder builder, string getRouteName, string idRouteValueName = "id")
        where TBuilder : IEndpointConventionBuilder
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(getRouteName);
        return builder.WithMetadata(new CreatedLocationMetadata(getRouteName, idRouteValueName));
    }

    /// <summary>Opts this endpoint out of hypermedia (e.g. a file download or a webhook).</summary>
    public static TBuilder WithoutHateoas<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
        => builder.WithMetadata(SkipHateoasMetadata.Instance);
}
