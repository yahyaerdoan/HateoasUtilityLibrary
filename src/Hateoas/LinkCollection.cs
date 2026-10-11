using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Hateoas;

/// <summary>
/// A resource's HAL "_links" object: relation type to <see cref="Link"/>, plus the "curies" array whenever a
/// compact (CURIE-prefixed) extension relation is present. Insertion order is preserved in the JSON output.
/// </summary>
[JsonConverter(typeof(LinkCollectionJsonConverter))]
public sealed class LinkCollection : IReadOnlyDictionary<string, Link>
{
    private readonly Dictionary<string, Link> _links = new(StringComparer.Ordinal);
    private readonly List<Curie> _curies = [];

    public IReadOnlyList<Curie> Curies => _curies;

    public int Count => _links.Count;

    public IEnumerable<string> Keys => _links.Keys;

    public IEnumerable<Link> Values => _links.Values;

    public Link this[string key] => _links[key];

    /// <summary>Adds or replaces the link for <paramref name="rel"/>.</summary>
    public LinkCollection Set(string rel, Link link)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rel);
        ArgumentNullException.ThrowIfNull(link);

        if (rel == LinkRelations.Curies)
        {
            throw new ArgumentException($"\"{LinkRelations.Curies}\" is reserved; use {nameof(AddCurie)}.", nameof(rel));
        }

        _links[rel] = link;
        return this;
    }

    /// <summary>Removes the link for <paramref name="rel"/>, if present.</summary>
    public bool Remove(string rel) => _links.Remove(rel);

    /// <summary>Adds a CURIE definition once; a second definition with the same name is ignored.</summary>
    public LinkCollection AddCurie(Curie curie)
    {
        ArgumentNullException.ThrowIfNull(curie);

        if (!_curies.Exists(existing => existing.Name == curie.Name))
        {
            _curies.Add(curie);
        }

        return this;
    }

    public bool ContainsKey(string key) => _links.ContainsKey(key);

    public bool TryGetValue(string key, [MaybeNullWhen(false)] out Link value) => _links.TryGetValue(key, out value);

    public IEnumerator<KeyValuePair<string, Link>> GetEnumerator() => _links.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
