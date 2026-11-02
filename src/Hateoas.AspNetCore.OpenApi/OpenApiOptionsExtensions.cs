using Microsoft.AspNetCore.OpenApi;

namespace Hateoas.AspNetCore.OpenApi;

public static class OpenApiOptionsExtensions
{
    /// <summary>Documents HAL "_links" as written: links by relation, each with "href" and the optional "method", "title" and "templated",
    /// plus the "curies" that define compact extension relations.</summary>
    public static OpenApiOptions AddHateoasDocumentation(this OpenApiOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return options.AddSchemaTransformer<LinkCollectionSchemaTransformer>();
    }
}
