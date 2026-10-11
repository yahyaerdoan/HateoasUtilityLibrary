using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Hateoas.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Hateoas.Tests;

public sealed class EndpointFilterTests : IAsyncLifetime
{
    private WebApplication? _app;
    private HttpClient? _client;

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private HttpClient Client => _client ?? throw new InvalidOperationException("Not started.");

    public async ValueTask InitializeAsync()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        _ = builder.Logging.ClearProviders();
        _ = builder.Services.AddRouting();
        _ = builder.Services.AddHateoas(options => options.CurieName = "oc").AddLinkProvider<OrderLinks>().AddLinkProvider<BrokenLinks>();

        var app = builder.Build();
        var api = app.MapGroup(string.Empty).WithHateoas();

        _ = api.MapGet("/orders/{id:int}", (int id) => Results.Json(new Envelope<Order>(new Order(id, id % 2 == 0 ? "Paid" : "Pending")))).WithName("GetOrder");
        _ = api.MapPost("/orders/{id:int}/pay", (int id) => Results.NoContent()).WithName("PayOrder");
        _ = api.MapGet("/orders", (int? index, int? size) => Results.Json(new Envelope<Page<Order>>(new Page<Order>([new Order(1, "Pending"), new Order(2, "Paid")], index ?? 0, size ?? 2, 3))))
            .WithName("GetOrders");
        _ = api.MapPost("/orders", () => Results.Json(new Envelope<int>(7), statusCode: StatusCodes.Status201Created)).WithCreatedLocation("GetOrder");
        _ = api.MapGet("/raw/{id:int}", (int id) => Results.Json(new Order(id, "Pending"))).WithoutHateoas();
        _ = api.MapGet("/broken", () => Results.Json(new Broken(1)));

        await app.StartAsync();
        _app = app;
        var address = app.Services.GetRequiredService<IServer>().Features.GetRequiredFeature<IServerAddressesFeature>().Addresses.First();
        _client = new HttpClient { BaseAddress = new Uri(address) };
    }

    public async ValueTask DisposeAsync()
    {
        _client?.Dispose();
        if (_app is not null)
        {
            await _app.DisposeAsync();
        }
    }

    [Fact]
    public async Task Adds_state_dependent_links_inside_an_envelope()
    {
        var pending = await GetLinks("/orders/1");
        Assert.Equal($"{Client.BaseAddress}orders/1", pending.GetProperty("self").GetProperty("href").GetString());
        Assert.Equal("POST", pending.GetProperty("oc:pay").GetProperty("method").GetString());
        Assert.Equal($"{Client.BaseAddress}rels/{{rel}}", pending.GetProperty("curies")[0].GetProperty("href").GetString());

        var paid = await GetLinks("/orders/2");
        Assert.False(paid.TryGetProperty("oc:pay", out _));
        Assert.False(paid.TryGetProperty("curies", out _));
    }

    [Fact]
    public async Task Links_every_item_of_a_page_and_sets_the_paging_header()
    {
        var response = await Client.GetAsync("/orders?index=1&size=2", Token);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(Token);

        foreach (var order in json.GetProperty("data").GetProperty("items").EnumerateArray())
        {
            Assert.True(order.GetProperty("_links").TryGetProperty("self", out _));
        }

        var header = Assert.Single(response.Headers.GetValues("Link"));
        Assert.Contains("index=0&size=2>; rel=\"prev\"", header);
        Assert.Contains("index=2&size=2>; rel=\"next\"", header);
        Assert.Contains("rel=\"first\"", header);
        Assert.Contains("rel=\"last\"", header);
    }

    [Fact]
    public async Task Sets_location_on_created()
    {
        var response = await Client.PostAsync("/orders", null, Token);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal($"{Client.BaseAddress}orders/7", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task Opted_out_endpoints_are_left_alone()
    {
        var json = await Client.GetFromJsonAsync<JsonElement>("/raw/1", Token);
        Assert.False(json.TryGetProperty("_links", out _));
    }

    [Fact]
    public async Task Unknown_route_names_fail_loudly()
    {
        var response = await Client.GetAsync("/broken", Token);
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    private async Task<JsonElement> GetLinks(string path)
        => (await Client.GetFromJsonAsync<JsonElement>(path, Token)).GetProperty("data").GetProperty("_links");

    public sealed record Envelope<T>(T Data);

    public sealed record Page<T>(IList<T> Items, int Index, int Size, int Pages);

    public sealed record Order(int Id, string Status) : LinkedRecord;

    public sealed record Broken(int Id) : LinkedRecord;

    private sealed class OrderLinks : LinkProvider<Order>
    {
        protected override void AddLinks(Order resource, HateoasLinkBuilder links)
            => links.Self("GetOrder", new { id = resource.Id })
                .AddCustomIf(resource.Status == "Pending", "pay", "PayOrder", HttpMethods.Post, new { id = resource.Id });
    }

    private sealed class BrokenLinks : LinkProvider<Broken>
    {
        protected override void AddLinks(Broken resource, HateoasLinkBuilder links)
            => links.Self("NoSuchRoute", new { id = resource.Id });
    }
}
