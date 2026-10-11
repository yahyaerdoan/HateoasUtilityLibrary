using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Hateoas.AspNetCore;

/// <summary>
/// Fluent builder for a resource's "_links". Hrefs come from named routes (never hand-built strings, so
/// links survive route template changes) and an unknown route name throws instead of producing an empty
/// link. IANA relations ("self", "edit", ...) are used as-is; application relations ("pay", "cancel")
/// go through <see cref="AddCustom"/> and are written with the configured CURIE prefix. Use the
/// "...If" overloads to emit a link only when the action is valid for the resource's current state and
/// allowed for <see cref="User"/>.
/// </summary>
public sealed class HateoasLinkBuilder
{
    private readonly IUriResolver _uriResolver;
    private readonly HateoasOptions _options;
    private readonly LinkCollection _links = new();
    private bool _usesCurie;

    /// <summary>For MVC controllers - resolves routes via <see cref="IUrlHelper"/>.</summary>
    public HateoasLinkBuilder(IUrlHelper urlHelper, HttpContext httpContext, HateoasOptions options)
        : this(new UrlHelperUriResolver(urlHelper), httpContext, options)
    {
    }

    /// <summary>For Minimal API endpoints - resolves routes via <see cref="LinkGenerator"/>.</summary>
    public HateoasLinkBuilder(LinkGenerator linkGenerator, HttpContext httpContext, HateoasOptions options)
        : this(new LinkGeneratorUriResolver(linkGenerator, httpContext), httpContext, options)
    {
    }

    private HateoasLinkBuilder(IUriResolver uriResolver, HttpContext httpContext, HateoasOptions options)
    {
        _uriResolver = uriResolver;
        _options = options;
        HttpContext = httpContext;
    }

    public HttpContext HttpContext { get; }

    /// <summary>The caller; decide action links with it so a client is never offered something it would be refused.</summary>
    public ClaimsPrincipal User => HttpContext.User;

    /// <summary>Adds the "self" (GET) link.</summary>
    public HateoasLinkBuilder Self(string routeName, object? routeValues = null)
        => Add(LinkRelations.Self, routeName, HttpMethods.Get, routeValues);

    /// <summary>Adds a link under an IANA-registered relation type (see <see cref="LinkRelations"/>).</summary>
    public HateoasLinkBuilder Add(string rel, string routeName, string method, object? routeValues = null, string? title = null)
    {
        _ = _links.Set(rel, CreateLink(routeName, method, routeValues, title));
        return this;
    }

    /// <summary>Same as <see cref="Add"/>, only when <paramref name="condition"/> holds.</summary>
    public HateoasLinkBuilder AddIf(bool condition, string rel, string routeName, string method, object? routeValues = null, string? title = null)
        => condition ? Add(rel, routeName, method, routeValues, title) : this;

    /// <summary>
    /// Adds a link under an application-defined (extension) relation such as "pay" or "cancel", written as
    /// "{CurieName}:{relName}" with a matching "curies" entry pointing at the relation's documentation.
    /// </summary>
    public HateoasLinkBuilder AddCustom(string relName, string routeName, string method, object? routeValues = null, string? title = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(relName);

        _ = _links.Set($"{_options.CurieName}:{relName}", CreateLink(routeName, method, routeValues, title));
        _usesCurie = true;
        return this;
    }

    /// <summary>Same as <see cref="AddCustom"/>, only when <paramref name="condition"/> holds.</summary>
    public HateoasLinkBuilder AddCustomIf(bool condition, string relName, string routeName, string method, object? routeValues = null, string? title = null)
        => condition ? AddCustom(relName, routeName, method, routeValues, title) : this;

    /// <summary>The links added so far, with the CURIE definition when an extension relation was used.</summary>
    public LinkCollection Build()
    {
        if (_usesCurie)
        {
            _ = _links.AddCurie(new Curie { Name = _options.CurieName, Href = CurieHref() });
        }

        return _links;
    }

    private Link CreateLink(string routeName, string method, object? routeValues, string? title)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(routeName);

        var href = _uriResolver.Resolve(routeName, routeValues)
            ?? throw new InvalidOperationException(
                $"No URL could be generated for route \"{routeName}\" with values {routeValues ?? "(none)"}. Check the endpoint's WithName(...) and that every route parameter is supplied.");

        return new Link { Href = href, Method = method, Title = title };
    }

    private string CurieHref()
    {
        if (Uri.TryCreate(_options.CurieHref, UriKind.Absolute, out _))
        {
            return _options.CurieHref;
        }

        var request = HttpContext.Request;
        return $"{request.Scheme}://{request.Host}{request.PathBase}{_options.CurieHref}";
    }
}
