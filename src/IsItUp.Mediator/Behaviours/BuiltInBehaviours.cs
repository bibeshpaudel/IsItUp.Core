using Microsoft.Extensions.Logging;

namespace IsItUp.Mediator.Behaviours;

// ─── Logging Behavior ─────────────────────────────────────────────────────────

/// <summary>
/// Pipeline behavior that logs request execution time.
/// Register it FIRST so it wraps the entire pipeline.
/// </summary>
public sealed class LoggingBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
        => _logger = logger;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        _logger.LogInformation("[Mediator] Handling {RequestName}", requestName);

        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var response = await next().ConfigureAwait(false);
            sw.Stop();
            _logger.LogInformation("[Mediator] Handled {RequestName} in {Elapsed}ms", requestName, sw.ElapsedMilliseconds);
            return response;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "[Mediator] {RequestName} failed after {Elapsed}ms", requestName, sw.ElapsedMilliseconds);
            throw;
        }
    }
}

// ─── Validation Behavior ─────────────────────────────────────────────────────

/// <summary>
/// Validation result returned by <see cref="IValidator{T}"/>.
/// </summary>
public sealed class ValidationResult
{
    public bool IsValid => Errors.Count == 0;
    public List<ValidationError> Errors { get; } = [];

    public static ValidationResult Success() => new();

    public static ValidationResult Failure(params ValidationError[] errors)
    {
        var result = new ValidationResult();
        result.Errors.AddRange(errors);
        return result;
    }
}

public sealed record ValidationError(string PropertyName, string Message);

/// <summary>Implement this to add validation for a specific request type.</summary>
public interface IValidator<in TRequest>
{
    Task<ValidationResult> ValidateAsync(TRequest request, CancellationToken cancellationToken = default);
}

/// <summary>Exception thrown when validation fails.</summary>
public sealed class ValidationException : Exception
{
    public IReadOnlyList<ValidationError> Errors { get; }

    public ValidationException(IReadOnlyList<ValidationError> errors)
        : base($"Validation failed: {string.Join("; ", errors.Select(e => $"{e.PropertyName}: {e.Message}"))}")
    {
        Errors = errors;
    }
}

/// <summary>
/// Pipeline behavior that runs all registered <see cref="IValidator{TRequest}"/>s
/// before passing to the next step.
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
        => _validators = validators;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
            return await next().ConfigureAwait(false);

        var tasks = _validators.Select(v => v.ValidateAsync(request, cancellationToken));
        var results = await Task.WhenAll(tasks).ConfigureAwait(false);

        var errors = results
            .SelectMany(r => r.Errors)
            .ToList();

        if (errors.Count > 0)
            throw new ValidationException(errors);

        return await next().ConfigureAwait(false);
    }
}
