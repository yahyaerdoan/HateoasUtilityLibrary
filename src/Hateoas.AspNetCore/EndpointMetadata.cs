namespace Hateoas.AspNetCore;

/// <summary>Names the GET route of the resource an endpoint creates, for the Location header of its 201 Created response.</summary>
public sealed record CreatedLocationMetadata(string RouteName, string IdRouteValueName);

/// <summary>Opts an endpoint out of <see cref="HateoasEndpointFilter"/> (e.g. a file download or a webhook).</summary>
public sealed class SkipHateoasMetadata
{
    public static readonly SkipHateoasMetadata Instance = new();

    private SkipHateoasMetadata()
    {
    }
}
