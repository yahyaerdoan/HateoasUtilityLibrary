using Microsoft.AspNetCore.Mvc;

namespace Hateoas.AspNetCore;

/// <summary>MVC controller counterparts of <see cref="HttpContextExtensions"/>.</summary>
public static class ControllerBaseExtensions
{
    /// <summary>Starts a <see cref="HateoasLinkBuilder"/> bound to the current controller's <see cref="ControllerBase.Url"/>.</summary>
    public static HateoasLinkBuilder Links(this ControllerBase controller)
        => new(controller.Url, controller.HttpContext, HttpContextExtensions.Options(controller.HttpContext));

    /// <summary>Sets the Location header to the canonical GET URI of a just-created resource (RFC 9110, 201 Created).</summary>
    public static void SetCreatedLocationHeader(this ControllerBase controller, string getRouteName, object? routeValues)
        => controller.Response.Headers.Location = controller.Url.Link(getRouteName, routeValues)
            ?? throw new InvalidOperationException($"No URL could be generated for route \"{getRouteName}\".");
}
