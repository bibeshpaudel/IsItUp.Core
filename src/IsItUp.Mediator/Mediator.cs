using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using System.Reflection;

namespace IsItUp.Mediator;

/// <summary>
/// Default implementation of <see cref="IMediator"/>.
/// Resolves handlers and pipeline behaviors from the DI container at runtime,
/// caching the dispatch delegates for performance.
/// </summary>
public sealed class Mediator : IMediator
{
    private readonly IServiceProvider _serviceProvider;

    // Cache compiled send-delegates keyed by request type
    private static readonly ConcurrentDictionary<Type, Func<IServiceProvider, object, CancellationToken, Task<object?>>> _sendCache = new();

    // Cache compiled publish-delegates keyed by notification type
    private static readonly ConcurrentDictionary<Type, Func<IServiceProvider, object, CancellationToken, Task>> _publishCache = new();

    public Mediator(IServiceProvider serviceProvider)
        => _serviceProvider = serviceProvider;

    // ─── Send (with response) ────────────────────────────────────────────────

    public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var requestType = request.GetType();
        var dispatcher = _sendCache.GetOrAdd(requestType, BuildSendDispatcher<TResponse>);
        var result = await dispatcher(_serviceProvider, request, cancellationToken).ConfigureAwait(false);
        return (TResponse)result!;
    }

    public Task Send(IRequest request, CancellationToken cancellationToken = default)
        => Send<Unit>(request, cancellationToken);

    // ─── Publish ─────────────────────────────────────────────────────────────

    public async Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification
    {
        ArgumentNullException.ThrowIfNull(notification);

        var notificationType = notification.GetType();
        var dispatcher = _publishCache.GetOrAdd(notificationType, BuildPublishDispatcher);
        await dispatcher(_serviceProvider, notification, cancellationToken).ConfigureAwait(false);
    }

    // ─── Builder: send dispatcher ────────────────────────────────────────────

    private static Func<IServiceProvider, object, CancellationToken, Task<object?>> BuildSendDispatcher<TResponse>(Type requestType)
    {
        // Reflect into the generic helper using the concrete request + response types
        var responseType = typeof(TResponse);
        var method = typeof(Mediator)
            .GetMethod(nameof(DispatchRequest), BindingFlags.Static | BindingFlags.NonPublic)!
            .MakeGenericMethod(requestType, responseType);

        return (sp, req, ct) =>
            (Task<object?>)method.Invoke(null, [sp, req, ct])!;
    }

    private static async Task<object?> DispatchRequest<TRequest, TResponse>(
        IServiceProvider sp,
        object request,
        CancellationToken cancellationToken)
        where TRequest : IRequest<TResponse>
    {
        var concreteRequest = (TRequest)request;

        // Resolve handler
        var handler = sp.GetService<IRequestHandler<TRequest, TResponse>>()
            ?? throw new InvalidOperationException(
                $"No handler registered for request type '{typeof(TRequest).FullName}'. " +
                $"Make sure you called AddMediator() and that the handler is in a scanned assembly.");

        // Resolve pipeline behaviors (ordered by DI registration)
        var behaviors = sp.GetServices<IPipelineBehavior<TRequest, TResponse>>()
                          .Reverse()  // wrap outermost last so first-registered runs first
                          .ToList();

        // Build the pipeline tail → head
        RequestHandlerDelegate<TResponse> pipeline = () =>
            handler.Handle(concreteRequest, cancellationToken);

        foreach (var behavior in behaviors)
        {
            var next = pipeline;          // capture for closure
            var b = behavior;
            pipeline = () => b.Handle(concreteRequest, next, cancellationToken);
        }

        var result = await pipeline().ConfigureAwait(false);
        return result;
    }

    // ─── Builder: publish dispatcher ─────────────────────────────────────────

    private static Func<IServiceProvider, object, CancellationToken, Task> BuildPublishDispatcher(Type notificationType)
    {
        var method = typeof(Mediator)
            .GetMethod(nameof(DispatchNotification), BindingFlags.Static | BindingFlags.NonPublic)!
            .MakeGenericMethod(notificationType);

        return (sp, notification, ct) =>
            (Task)method.Invoke(null, [sp, notification, ct])!;
    }

    private static async Task DispatchNotification<TNotification>(
        IServiceProvider sp,
        object notification,
        CancellationToken cancellationToken)
        where TNotification : INotification
    {
        var concreteNotification = (TNotification)notification;
        var handlers = sp.GetServices<INotificationHandler<TNotification>>();

        // Fire all handlers; aggregate exceptions if multiple fail
        var exceptions = new List<Exception>();

        foreach (var handler in handlers)
        {
            try
            {
                await handler.Handle(concreteNotification, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        }

        if (exceptions.Count == 1)
            throw exceptions[0];

        if (exceptions.Count > 1)
            throw new AggregateException(
                $"One or more handlers for '{typeof(TNotification).Name}' failed.", exceptions);
    }
}
