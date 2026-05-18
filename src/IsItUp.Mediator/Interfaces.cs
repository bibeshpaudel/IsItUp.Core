namespace IsItUp.Mediator;

// ─── Marker interface for all requests ───────────────────────────────────────

/// <summary>Marker interface for a request that returns no value.</summary>
public interface IRequest : IRequest<Unit> { }

/// <summary>Marker interface for a request that returns <typeparamref name="TResponse"/>.</summary>
public interface IRequest<out TResponse> { }

// ─── Handlers ─────────────────────────────────────────────────────────────────

/// <summary>Handles a <typeparamref name="TRequest"/> that returns <typeparamref name="TResponse"/>.</summary>
public interface IRequestHandler<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
}

/// <summary>Convenience base for void-like handlers (returns <see cref="Unit"/>).</summary>
public interface IRequestHandler<in TRequest> : IRequestHandler<TRequest, Unit>
    where TRequest : IRequest<Unit>
{ }

// ─── Notifications ────────────────────────────────────────────────────────────

/// <summary>Marker interface for notifications (fan-out, no return value).</summary>
public interface INotification { }

/// <summary>Handles a <typeparamref name="TNotification"/>.</summary>
public interface INotificationHandler<in TNotification>
    where TNotification : INotification
{
    Task Handle(TNotification notification, CancellationToken cancellationToken);
}

// ─── Pipeline Behavior ────────────────────────────────────────────────────────

/// <summary>Represents the next step in the pipeline.</summary>
public delegate Task<TResponse> RequestHandlerDelegate<TResponse>();

/// <summary>Middleware that wraps every request/response pair.</summary>
public interface IPipelineBehavior<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken);
}

// ─── Mediator contract ────────────────────────────────────────────────────────

/// <summary>Central dispatcher — send requests and publish notifications.</summary>
public interface IMediator
{
    /// <summary>Send a request and await its response.</summary>
    Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);

    /// <summary>Send a void request (returns <see cref="Unit"/>).</summary>
    Task Send(IRequest request, CancellationToken cancellationToken = default);

    /// <summary>Publish a notification to all registered handlers.</summary>
    Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification;
}
