using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Microsoft.Extensions.Options;

namespace Hateoas.AspNetCore;

/// <summary>Plugs <see cref="LinksJsonTypeInfoModifier"/> into the JSON options of Minimal APIs and MVC.</summary>
internal sealed class HateoasJsonOptionsSetup(LinksJsonTypeInfoModifier modifier) : IConfigureOptions<Microsoft.AspNetCore.Http.Json.JsonOptions>, IConfigureOptions<Microsoft.AspNetCore.Mvc.JsonOptions>
{
    public void Configure(Microsoft.AspNetCore.Http.Json.JsonOptions options) => AddModifier(options.SerializerOptions);

    public void Configure(Microsoft.AspNetCore.Mvc.JsonOptions options) => AddModifier(options.JsonSerializerOptions);

    private void AddModifier(JsonSerializerOptions serializerOptions) =>
        serializerOptions.TypeInfoResolver = (serializerOptions.TypeInfoResolver ?? new DefaultJsonTypeInfoResolver()).WithAddedModifier(modifier.Modify);
}
