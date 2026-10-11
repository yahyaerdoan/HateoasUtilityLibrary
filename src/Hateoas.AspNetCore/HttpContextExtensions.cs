using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Hateoas.AspNetCore;

/// <summary>
/// Manual counterparts of what <see cref="HateoasEndpointFilter"/> does automatically, for the rare endpoint that
/// needs to build links itself. Prefer an <see cref="ILinkProvider"/> plus <see cref="EndpointConventionBuilderExtensions.WithHateoas{TBuilder}"/>.
/// </summary>
public static class HttpContextExtensions
{
    /// <summary>Starts a <see cref="HateoasLinkBuilder"/> bound to the current request.</summary>
    public static HateoasLinkBuilder Links(this HttpContext httpContext, LinkGenerator linkGenerator)
        => new(linkGenerator, httpContext, Options(httpContext));

    /// <summary>Sets the Location header to the canonical GET URI of a just-created resource (RFC 9110, 201 Created).</summary>
    public static void SetCreatedLocationHeader(this HttpContext httpContext, LinkGenerator linkGenerator, string getRouteName, object? routeValues)
        => httpContext.Response.Headers.Location = linkGenerator.GetUriByName(httpContext, getRouteName, routeValues)
            ?? throw new InvalidOperationException($"No URL could be generated for route \"{getRouteName}\".");

    internal static HateoasOptions Options(HttpContext httpContext)
        => httpContext.RequestServices.GetService<IOptions<HateoasOptions>>()?.Value ?? new HateoasOptions();
}
