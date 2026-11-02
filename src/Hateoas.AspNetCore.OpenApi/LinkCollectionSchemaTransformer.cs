using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Hateoas.AspNetCore.OpenApi;

/// <summary>Describes <see cref="LinkCollection"/> as <see cref="LinkCollectionJsonConverter"/> writes it, which the schema generator can't see
/// through the converter.</summary>
internal sealed class LinkCollectionSchemaTransformer : IOpenApiSchemaTransformer
{
    public Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
    {
        if (context.JsonTypeInfo.Type == typeof(LinkCollection))
        {
            schema.Type = JsonSchemaType.Object;
            schema.Description = "HAL links by relation: IANA names such as \"self\", extension relations as \"{curie}:{name}\".";
            schema.Properties = new Dictionary<string, IOpenApiSchema>
            {
                [LinkRelations.Curies] = new OpenApiSchema
                {
                    Type = JsonSchemaType.Array,
                    Description = "Templates that expand an extension relation's prefix to its documentation.",
                    Items = CurieSchema(),
                },
            };
            schema.AdditionalProperties = LinkSchema();
        }

        return Task.CompletedTask;
    }

    private static OpenApiSchema LinkSchema() => new()
    {
        Type = JsonSchemaType.Object,
        Required = new HashSet<string>(StringComparer.Ordinal) { "href" },
        Properties = new Dictionary<string, IOpenApiSchema>
        {
            ["href"] = new OpenApiSchema { Type = JsonSchemaType.String, Description = "Where the link points." },
            ["method"] = new OpenApiSchema { Type = JsonSchemaType.String, Description = "The HTTP method the target expects; GET when left out." },
            ["title"] = new OpenApiSchema { Type = JsonSchemaType.String, Description = "A label for a person to read." },
            ["templated"] = new OpenApiSchema { Type = JsonSchemaType.Boolean, Description = "Whether \"href\" is an RFC 6570 URI template." },
        },
    };

    private static OpenApiSchema CurieSchema() => new()
    {
        Type = JsonSchemaType.Object,
        Required = new HashSet<string>(StringComparer.Ordinal) { "name", "href", "templated" },
        Properties = new Dictionary<string, IOpenApiSchema>
        {
            ["name"] = new OpenApiSchema { Type = JsonSchemaType.String, Description = "The prefix extension relations are written with." },
            ["href"] = new OpenApiSchema { Type = JsonSchemaType.String, Description = "URI template of a relation's documentation, with \"{rel}\" for its name." },
            ["templated"] = new OpenApiSchema { Type = JsonSchemaType.Boolean, Description = "Always true." },
        },
    };
}
