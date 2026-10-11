using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace Hateoas.AspNetCore;

/// <summary>
/// Applies hypermedia to every successful endpoint response it is attached to (see
/// <see cref="EndpointConventionBuilderExtensions.WithHateoas{TBuilder}"/>): fills "_links" of the resources in
/// the body, sets Location on 201 Created when the endpoint names its GET route, and sets the RFC 8288 Link
/// header on paged GET collections.
/// </summary>
internal sealed class HateoasEndpointFilter(ILinkEnricher enricher, LinkGenerator linkGenerator, IOptions<HateoasOptions> options) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var result = await next(context);
        var httpContext = context.HttpContext;

        if (httpContext.GetEndpoint()?.Metadata.GetMetadata<SkipHateoasMetadata>() is not null)
        {
            return result;
        }

        var body = result is IValueHttpResult { Value: { } value } ? value : result is IResult ? null : result;
        var statusCode = (result as IStatusCodeHttpResult)?.StatusCode ?? StatusCodes.Status200OK;
        if (body is null || statusCode is < 200 or > 299)
        {
            return result;
        }

        enricher.Enrich(body, httpContext);

        if (statusCode == StatusCodes.Status201Created)
        {
            SetLocation(httpContext, body);
        }
        else if (HttpMethods.IsGet(httpContext.Request.Method) && options.Value.PageInfoSelector(body) is { } page)
        {
            SetPagingLinkHeader(httpContext, page);
        }

        return result;
    }

    private void SetLocation(HttpContext httpContext, object body)
    {
        if (httpContext.GetEndpoint()?.Metadata.GetMetadata<CreatedLocationMetadata>() is not { } location
            || options.Value.CreatedIdSelector(body) is not { } id)
        {
            return;
        }

        httpContext.Response.Headers.Location = linkGenerator.GetUriByName(httpContext, location.RouteName, new RouteValueDictionary { [location.IdRouteValueName] = id })
            ?? throw new InvalidOperationException($"No URL could be generated for route \"{location.RouteName}\" with {location.IdRouteValueName} = {id}.");
    }

    private void SetPagingLinkHeader(HttpContext httpContext, PageInfo page)
    {
        List<string> entries = [];
        if (page.HasPrevious)
        {
            entries.Add(Entry(httpContext, page.Index - 1, page.Size, LinkRelations.Prev));
        }

        if (page.HasNext)
        {
            entries.Add(Entry(httpContext, page.Index + 1, page.Size, LinkRelations.Next));
        }

        if (page.Pages > 0)
        {
            entries.Add(Entry(httpContext, 0, page.Size, LinkRelations.First));
            entries.Add(Entry(httpContext, page.Pages - 1, page.Size, LinkRelations.Last));
        }

        if (entries.Count > 0)
        {
            httpContext.Response.Headers.Append("Link", string.Join(", ", entries));
        }
    }

    private string Entry(HttpContext httpContext, int index, int size, string rel)
    {
        var request = httpContext.Request;
        var query = QueryHelpers.ParseQuery(request.QueryString.Value);
        query[options.Value.PageIndexQueryName] = index.ToString(System.Globalization.CultureInfo.InvariantCulture);
        query[options.Value.PageSizeQueryName] = size.ToString(System.Globalization.CultureInfo.InvariantCulture);

        var queryString = QueryString.Create(query.SelectMany(pair => pair.Value.Select(item => KeyValuePair.Create(pair.Key, item))));
        return $"<{UriHelper.BuildAbsolute(request.Scheme, request.Host, request.PathBase, request.Path, queryString)}>; rel=\"{rel}\"";
    }
}
