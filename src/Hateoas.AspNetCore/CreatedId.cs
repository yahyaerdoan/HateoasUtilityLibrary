using System.Reflection;

namespace Hateoas.AspNetCore;

/// <summary>The default <see cref="HateoasOptions.CreatedIdSelector"/>.</summary>
public static class CreatedId
{
    public static object? Find(object body)
    {
        if (ObjectGraph.IsScalar(body.GetType()))
        {
            return body;
        }

        foreach (var name in (string[])["Data", "Id"])
        {
            if (body.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance)?.GetValue(body) is { } value)
            {
                return ObjectGraph.IsScalar(value.GetType()) ? value : Find(value);
            }
        }

        return null;
    }
}
