using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Reflection;

namespace IsItUp.Mediator;

/// <summary>Extension methods for registering the Mediator with the DI container.</summary>
public static class MediatorServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="IMediator"/> and scans the provided assemblies for
    /// request handlers, notification handlers, and pipeline behaviors.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional action to configure options.</param>
    /// <param name="assemblies">
    ///   Assemblies to scan. If none are provided, the calling assembly is used.
    /// </param>
    public static IServiceCollection AddMediator(
        this IServiceCollection services,
        Action<MediatorOptions>? configure = null,
        params Assembly[] assemblies)
    {
        var options = new MediatorOptions();
        configure?.Invoke(options);

        if (assemblies.Length == 0)
            assemblies = [Assembly.GetCallingAssembly()];

        // Register the mediator itself
        services.TryAdd(new ServiceDescriptor(typeof(IMediator), typeof(Mediator), options.Lifetime));

        // Scan assemblies
        var allTypes = assemblies
            .Distinct()
            .SelectMany(a => a.GetTypes())
            .Where(t => t.IsClass && !t.IsAbstract)
            .ToList();

        foreach (var type in allTypes)
        {
            RegisterRequestHandlers(services, type, options.Lifetime);
            RegisterNotificationHandlers(services, type, options.Lifetime);
            RegisterPipelineBehaviors(services, type, options.Lifetime);
        }

        return services;
    }

    // ── Overload: pass assemblies containing marker types ───────────────────

    /// <summary>
    /// Convenience overload — pass types whose assemblies will be scanned.
    /// <code>services.AddMediator(typeof(MyHandler), typeof(AnotherHandler));</code>
    /// </summary>
    public static IServiceCollection AddMediator(
        this IServiceCollection services,
        params Type[] markerTypes)
        => services.AddMediator(null, markerTypes.Select(t => t.Assembly).Distinct().ToArray());

    // ── Private helpers ──────────────────────────────────────────────────────

    private static void RegisterRequestHandlers(IServiceCollection services, Type type, ServiceLifetime lifetime)
    {
        var interfaces = type.GetInterfaces()
            .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>));

        foreach (var iface in interfaces)
        {
            services.Add(new ServiceDescriptor(iface, type, lifetime));

            // Also register the void-convenience interface IRequestHandler<TRequest>
            var args = iface.GetGenericArguments(); // [TRequest, TResponse]
            if (args[1] == typeof(Unit))
            {
                var voidInterface = typeof(IRequestHandler<>).MakeGenericType(args[0]);
                if (voidInterface.IsAssignableFrom(type))
                    services.TryAdd(new ServiceDescriptor(voidInterface, type, lifetime));
            }
        }
    }

    private static void RegisterNotificationHandlers(IServiceCollection services, Type type, ServiceLifetime lifetime)
    {
        var interfaces = type.GetInterfaces()
            .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(INotificationHandler<>));

        foreach (var iface in interfaces)
            services.Add(new ServiceDescriptor(iface, type, lifetime));
    }

    private static void RegisterPipelineBehaviors(IServiceCollection services, Type type, ServiceLifetime lifetime)
    {
        var interfaces = type.GetInterfaces()
            .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>));

        foreach (var iface in interfaces)
            services.Add(new ServiceDescriptor(iface, type, lifetime));
    }
}

/// <summary>Options for configuring how the mediator is registered.</summary>
public sealed class MediatorOptions
{
    /// <summary>
    /// Lifetime for handlers and the mediator itself.
    /// Defaults to <see cref="ServiceLifetime.Transient"/>.
    /// </summary>
    public ServiceLifetime Lifetime { get; set; } = ServiceLifetime.Transient;
}
