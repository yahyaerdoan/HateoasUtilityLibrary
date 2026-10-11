using System.Text.Json;

namespace Hateoas.Tests;

public class LinkCollectionTests
{
    [Fact]
    public void Serializes_as_hal_links_with_curies_last()
    {
        var links = new LinkCollection()
            .Set(LinkRelations.Self, new Link { Href = "https://api.test/orders/1", Method = "GET" })
            .Set("oc:pay", new Link { Href = "https://api.test/orders/1/pay", Method = "POST" })
            .AddCurie(new Curie { Name = "oc", Href = "https://api.test/rels/{rel}" });

        var json = JsonSerializer.Serialize(links);

        Assert.Equal(
            """{"self":{"href":"https://api.test/orders/1","method":"GET"},"oc:pay":{"href":"https://api.test/orders/1/pay","method":"POST"},"curies":[{"name":"oc","href":"https://api.test/rels/{rel}","templated":true}]}""",
            json);
    }

    [Fact]
    public void Round_trips_through_json()
    {
        var links = new LinkCollection()
            .Set(LinkRelations.Edit, new Link { Href = "https://api.test/a/1", Method = "PUT", Title = "Edit" })
            .AddCurie(new Curie { Name = "oc", Href = "/rels/{rel}" });

        var copy = JsonSerializer.Deserialize<LinkCollection>(JsonSerializer.Serialize(links));

        Assert.NotNull(copy);
        Assert.Equal("https://api.test/a/1", copy[LinkRelations.Edit].Href);
        Assert.Equal("Edit", copy[LinkRelations.Edit].Title);
        Assert.Equal("oc", Assert.Single(copy.Curies).Name);
    }

    [Fact]
    public void Curies_key_is_reserved()
        => Assert.Throws<ArgumentException>(() => new LinkCollection().Set(LinkRelations.Curies, new Link { Href = "x" }));

    [Fact]
    public void Linked_types_omit_links_until_one_is_added()
    {
        Assert.Equal("""{"Name":"a"}""", JsonSerializer.Serialize(new SampleRecord("a")));
        Assert.Equal("""{"Name":"b"}""", JsonSerializer.Serialize(new SampleClass { Name = "b" }));

        var withLinks = new SampleRecord("c") { Links = new LinkCollection().Set(LinkRelations.Self, new Link { Href = "h" }) };
        Assert.Equal("""{"Name":"c","_links":{"self":{"href":"h"}}}""", JsonSerializer.Serialize(withLinks));
    }

    private sealed record SampleRecord(string Name) : LinkedRecord;

    private sealed class SampleClass : LinkedResponse
    {
        public required string Name { get; init; }
    }
}
