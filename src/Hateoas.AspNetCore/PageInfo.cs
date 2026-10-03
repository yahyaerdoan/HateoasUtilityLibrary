using System.Reflection;

namespace Hateoas.AspNetCore;

/// <summary>Zero-based paging state of a collection response.</summary>
public sealed record PageInfo(int Index, int Size, int Pages)
{
    public bool HasPrevious => Index > 0;

    public bool HasNext => Index + 1 < Pages;

    /// <summary>The default <see cref="HateoasOptions.PageInfoSelector"/>.</summary>
    public static PageInfo? Find(object body) =>
        TryRead(body) ?? body.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.GetIndexParameters().Length == 0 && !ObjectGraph.IsScalar(property.PropertyType))
            .Select(property => property.GetValue(body))
            .Select(value => value is null ? null : TryRead(value))
            .FirstOrDefault(info => info is not null);

    private static PageInfo? TryRead(object value)
    {
        var type = value.GetType();
        return Read(type, value, nameof(Index)) is int index && Read(type, value, nameof(Size)) is int size && Read(type, value, nameof(Pages)) is int pages
            ? new PageInfo(index, size, pages)
            : null;
    }

    private static object? Read(Type type, object value, string name) =>
        type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance) is { PropertyType: var propertyType } property && propertyType == typeof(int)
            ? property.GetValue(value)
            : null;
}
