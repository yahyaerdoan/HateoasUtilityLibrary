using System.Text.Json;
using Hateoas.AspNetCore;
using Hateoas.AspNetCore.OpenApi;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Hateoas.Tests;

public sealed class OpenApiDocumentationTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Documents_links_as_a_map_of_links_by_relation_plus_curies()
    {
        using var document = await OpenApiDocumentAsync();
        var schemas = document.RootElement.GetProperty("components").GetProperty("schemas");

        var ticket = schemas.GetProperty("Ticket").GetProperty("properties");
        Assert.Equal("#/components/schemas/LinkCollection", ticket.GetProperty("_links").GetProperty("$ref").GetString());

        var links = schemas.GetProperty("LinkCollection");
        Assert.Equal("object", links.GetProperty("type").GetString());
        Assert.Equal("array", links.GetProperty("properties").GetProperty("curies").GetProperty("type").GetString());

        var link = links.GetProperty("additionalProperties");
        Assert.Equal(["href"], link.GetProperty("required").EnumerateArray().Select(name => name.GetString()));
        Assert.Equal(["href", "method", "title", "templated"], link.GetProperty("properties").EnumerateObject().Select(member => member.Name));
    }

    private static async Task<JsonDocument> OpenApiDocumentAsync()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        _ = builder.Logging.ClearProviders();
        _ = builder.Services.AddHateoas().AddLinkProvider<TicketLinks>();
        _ = builder.Services.AddOpenApi(options => options.AddHateoasDocumentation());

        await using var app = builder.Build();
        _ = app.MapOpenApi();
        _ = app.MapGroup(string.Empty).WithHateoas()
            .MapGet("/tickets/{id:int}", (int id) => TypedResults.Ok(new Ticket(id)))
            .WithName("GetTicket");
        await app.StartAsync(Token);

        var address = app.Services.GetRequiredService<IServer>().Features.GetRequiredFeature<IServerAddressesFeature>().Addresses.First();
        using var client = new HttpClient { BaseAddress = new Uri(address) };
        var json = await client.GetStringAsync("/openapi/v1.json", Token);
        await app.StopAsync(Token);
        return JsonDocument.Parse(json);
    }

    public sealed record Ticket(int Id);

    private sealed class TicketLinks : LinkProvider<Ticket>
    {
        protected override void AddLinks(Ticket resource, HateoasLinkBuilder links) => links.Self("GetTicket", new { id = resource.Id });
    }
}
