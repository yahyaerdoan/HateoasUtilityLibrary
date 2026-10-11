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

public sealed class PlainResourceTests : IAsyncLifetime
{
    private static readonly Ticket Shared = new(42, "Open");

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
        _ = builder.Services.AddHateoas().AddLinkProvider<TicketLinks>().AddLinkProvider<NoteLinks>();

        var app = builder.Build();
        var api = app.MapGroup(string.Empty).WithHateoas();

        _ = api.MapGet("/tickets/{id:int}", (int id) => Results.Json(new Envelope<Ticket>(new Ticket(id, "Open")))).WithName("GetTicket");
        _ = api.MapDelete("/tickets/{id:int}", (int id) => Results.NoContent()).WithName("DeleteTicket");
        _ = api.MapGet("/tickets", () => Results.Json(new Envelope<List<Ticket>>([new Ticket(1, "Open"), new Ticket(2, "Closed")]))).WithName("GetTickets");
        _ = api.MapGet("/shared", () => Results.Json(new Envelope<Ticket>(Shared)));
        _ = api.MapGet("/notes/{id:int}", (int id) => Results.Json(new Note(id, new Ticket(id, "Open"))));
        _ = api.MapGet("/plain", () => Results.Json(new Unlinked(1)));

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
    public async Task Writes_links_of_a_record_without_a_base_type_last()
    {
        var json = await GetJson("/tickets/7");
        var ticket = json.GetProperty("data");

        Assert.Equal($"{Client.BaseAddress}tickets/7", ticket.GetProperty("_links").GetProperty("self").GetProperty("href").GetString());
        Assert.Equal("_links", ticket.EnumerateObject().Last().Name);
    }

    [Fact]
    public async Task Links_every_item_of_a_list()
    {
        var json = await GetJson("/tickets");

        foreach (var ticket in json.GetProperty("data").EnumerateArray())
        {
            Assert.True(ticket.GetProperty("_links").TryGetProperty("self", out _));
        }
    }

    [Fact]
    public async Task Leaves_out_links_when_a_provider_adds_none_and_for_types_without_a_provider()
    {
        var note = await GetJson("/notes/3");
        Assert.False(note.TryGetProperty("_links", out _));
        Assert.True(note.GetProperty("ticket").TryGetProperty("_links", out _));

        var plain = await GetJson("/plain");
        Assert.False(plain.TryGetProperty("_links", out _));
    }

    [Fact]
    public async Task Keeps_links_per_request_for_an_instance_shared_between_requests()
    {
        using var adminRequest = new HttpRequestMessage(HttpMethod.Get, "/shared");
        adminRequest.Headers.Add("X-Admin", "true");
        var admin = await (await Client.SendAsync(adminRequest, Token)).Content.ReadFromJsonAsync<JsonElement>(Token);
        var visitor = await GetJson("/shared");

        Assert.True(admin.GetProperty("data").GetProperty("_links").TryGetProperty("delete", out _));
        Assert.False(visitor.GetProperty("data").GetProperty("_links").TryGetProperty("delete", out _));
    }

    private async Task<JsonElement> GetJson(string path) => await Client.GetFromJsonAsync<JsonElement>(path, Token);

    public sealed record Envelope<T>(T Data);

    public sealed record Ticket(int Id, string Status);

    public sealed record Note(int Id, Ticket Ticket);

    public sealed record Unlinked(int Id);

    private sealed class TicketLinks : LinkProvider<Ticket>
    {
        protected override void AddLinks(Ticket resource, HateoasLinkBuilder links)
            => links.Self("GetTicket", new { id = resource.Id })
                .AddIf(links.HttpContext.Request.Headers.ContainsKey("X-Admin"), "delete", "DeleteTicket", HttpMethods.Delete, new { id = resource.Id });
    }

    private sealed class NoteLinks : LinkProvider<Note>
    {
        protected override void AddLinks(Note resource, HateoasLinkBuilder links)
        {
        }
    }
}
