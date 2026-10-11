namespace Hateoas.AspNetCore;

/// <summary>Configures link generation; register through <see cref="ServiceCollectionExtensions.AddHateoas"/>.</summary>
public sealed class HateoasOptions
{
    /// <summary>CURIE prefix for extension (non-IANA) relations, so "pay" is written as "{CurieName}:pay".</summary>
    public string CurieName { get; set; } = "app";

    /// <summary>
    /// RFC 6570 template for the documentation of an extension relation; must contain "{rel}". A relative
    /// value ("/rels/{rel}") is made absolute against the current request.
    /// </summary>
    public string CurieHref { get; set; } = "/rels/{rel}";

    /// <summary>Query string parameter carrying the zero-based page index on paged GET endpoints.</summary>
    public string PageIndexQueryName { get; set; } = "index";

    /// <summary>Query string parameter carrying the page size on paged GET endpoints.</summary>
    public string PageSizeQueryName { get; set; } = "size";

    /// <summary>
    /// Finds paging information in a response body, for the RFC 8288 "Link" header on paged GET endpoints.
    /// The default looks for an object with integer Index, Size and Pages properties, either the body itself
    /// or one of its direct properties.
    /// </summary>
    public Func<object, PageInfo?> PageInfoSelector { get; set; } = PageInfo.Find;

    /// <summary>
    /// Finds the new resource's id in a 201 Created body, for the Location header. The default accepts a scalar
    /// body, or a "Data" / "Id" property holding one.
    /// </summary>
    public Func<object, object?> CreatedIdSelector { get; set; } = CreatedId.Find;
}
