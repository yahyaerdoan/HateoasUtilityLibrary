using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Hateoas.AspNetCore;

public static class ServiceCollectionExtensions
{
    /// <summary>Registers link generation. Add providers with <see cref="HateoasBuilder.AddLinkProvidersFromAssembly"/>.</summary>
    public static HateoasBuilder AddHateoas(this IServiceCollection services, Action<HateoasOptions>? configure = null)
    {
        _ = services.AddOptions<HateoasOptions>();
        if (configure is not null)
        {
            _ = services.Configure(configure);
        }

        services.TryAddSingleton<ILinkEnricher, LinkEnricher>();
        return new HateoasBuilder(services);
    }
}

public sealed class HateoasBuilder(IServiceCollection services)
{
    public IServiceCollection Services { get; } = services;

    /// <summary>Registers every concrete <see cref="ILinkProvider"/> in <paramref name="assembly"/> as a singleton.</summary>
    public HateoasBuilder AddLinkProvidersFromAssembly(Assembly assembly)
    {
        foreach (var type in assembly.GetTypes().Where(type => type is { IsClass: true, IsAbstract: false } && typeof(ILinkProvider).IsAssignableFrom(type)))
        {
            Services.TryAddEnumerable(ServiceDescriptor.Singleton(typeof(ILinkProvider), type));
        }

        return this;
    }

    /// <summary>Registers one <see cref="ILinkProvider"/> as a singleton.</summary>
    public HateoasBuilder AddLinkProvider<TProvider>()
        where TProvider : class, ILinkProvider
    {
        Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILinkProvider, TProvider>());
        return this;
    }
}
