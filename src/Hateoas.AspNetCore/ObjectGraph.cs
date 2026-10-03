using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;

namespace Hateoas.AspNetCore;

/// <summary>Walks a response body (envelopes, lists, pages, nested DTOs) and yields every <see cref="ILinkedResource"/> in it once.</summary>
internal static class ObjectGraph
{
    private const int MaxDepth = 16;

    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> Properties = new();

    public static IEnumerable<ILinkedResource> FindResources(object root)
    {
        var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
        var found = new List<ILinkedResource>();
        Visit(root, 0, visited, found);
        return found;
    }

    public static bool IsScalar(Type type)
    {
        var underlying = Nullable.GetUnderlyingType(type) ?? type;
        return underlying.IsPrimitive
            || underlying.IsEnum
            || underlying == typeof(string)
            || underlying == typeof(decimal)
            || underlying == typeof(Guid)
            || underlying == typeof(DateTime)
            || underlying == typeof(DateTimeOffset)
            || underlying == typeof(DateOnly)
            || underlying == typeof(TimeOnly)
            || underlying == typeof(TimeSpan)
            || underlying == typeof(Uri)
            || underlying == typeof(byte[])
            || underlying == typeof(Type)
            || typeof(Delegate).IsAssignableFrom(underlying)
            || typeof(Stream).IsAssignableFrom(underlying)
            || underlying == typeof(LinkCollection)
            || underlying.Namespace?.StartsWith("System.Text.Json", StringComparison.Ordinal) == true;
    }

    private static void Visit(object? value, int depth, HashSet<object> visited, List<ILinkedResource> found)
    {
        if (value is null || depth > MaxDepth || IsScalar(value.GetType()) || (!value.GetType().IsValueType && !visited.Add(value)))
        {
            return;
        }

        if (value is ILinkedResource resource)
        {
            found.Add(resource);
        }

        if (value is IDictionary dictionary)
        {
            foreach (var item in dictionary.Values)
            {
                Visit(item, depth + 1, visited, found);
            }

            return;
        }

        if (value is IEnumerable items)
        {
            foreach (var item in items)
            {
                Visit(item, depth + 1, visited, found);
            }

            return;
        }

        foreach (var property in Properties.GetOrAdd(value.GetType(), ReadableProperties))
        {
            Visit(property.GetValue(value), depth + 1, visited, found);
        }
    }

    private static PropertyInfo[] ReadableProperties(Type type) =>
        [.. type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.CanRead && property.GetIndexParameters().Length == 0 && !IsScalar(property.PropertyType))];
}
