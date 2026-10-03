using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Hateoas.AspNetCore;

/// <summary>
/// Abstracts "resolve a named route + route values into an absolute URI" so
/// <see cref="HateoasLinkBuilder"/> can work from either an MVC <see cref="ControllerBase"/>
/// (<see cref="IUrlHelper"/>) or a Minimal API <see cref="HttpContext"/> (<see cref="LinkGenerator"/>)
/// without knowing which host style it's running under.
/// </summary>
internal interface IUriResolver
{
    string? Resolve(string routeName, object? routeValues);
}

internal sealed class UrlHelperUriResolver(IUrlHelper urlHelper) : IUriResolver
{
    public string? Resolve(string routeName, object? routeValues) => urlHelper.Link(routeName, routeValues);
}

internal sealed class LinkGeneratorUriResolver(LinkGenerator linkGenerator, HttpContext httpContext) : IUriResolver
{
    public string? Resolve(string routeName, object? routeValues) => linkGenerator.GetUriByName(httpContext, routeName, routeValues);
}
