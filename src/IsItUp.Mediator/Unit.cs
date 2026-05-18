namespace IsItUp.Mediator;

/// <summary>
/// Represents a void return type — used so every handler has a consistent
/// <c>Task&lt;TResponse&gt;</c> signature even when there is nothing to return.
/// </summary>
public readonly struct Unit : IEquatable<Unit>, IComparable<Unit>, IComparable
{
    /// <summary>The single <see cref="Unit"/> value.</summary>
    public static readonly Unit Value = new();

    /// <summary>A completed <see cref="Task{Unit}"/> wrapping <see cref="Value"/>.</summary>
    public static readonly Task<Unit> Task = System.Threading.Tasks.Task.FromResult(Value);

    public bool Equals(Unit other) => true;
    public override bool Equals(object? obj) => obj is Unit;
    public override int GetHashCode() => 0;
    public int CompareTo(Unit other) => 0;
    public int CompareTo(object? obj) => 0;

    public static bool operator ==(Unit left, Unit right) => true;
    public static bool operator !=(Unit left, Unit right) => false;

    public override string ToString() => "()";
}
