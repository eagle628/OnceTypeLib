namespace OnceTypeLib;

/// <summary>
/// Represents the public lifecycle state of a <see cref="WriteOnceReadOnce{T}"/>.
/// </summary>
public enum WriteOnceReadOnceState
{
    /// <summary>No value has been set yet.</summary>
    Empty = 0,

    /// <summary>A value has been set and has not been consumed yet.</summary>
    Ready = 1,

    /// <summary>The value has been consumed.</summary>
    Consumed = 2,
}

/// <summary>
/// A lock-free container that allows a value to be set once and consumed once.
/// It does not throw exceptions and only provides the Try-pattern.
/// </summary>
/// <typeparam name="T">The type of the value to store.</typeparam>
public sealed class WriteOnceReadOnce<T>
{
    private const int STATE_EMPTY = (int)WriteOnceReadOnceState.Empty;
    private const int STATE_READY = (int)WriteOnceReadOnceState.Ready;
    private const int STATE_CONSUMED = (int)WriteOnceReadOnceState.Consumed;
    // Internal state used to prevent readers from observing a value before it is published.
    private const int STATE_WRITING = 3;

    private int _state;
    private T? _value;

    /// <summary>Initializes a new instance of <see cref="WriteOnceReadOnce{T}"/> without a value.</summary>
    public WriteOnceReadOnce()
    {
    }

    /// <summary>Gets the current state. A value that is still being written is reported as <see cref="WriteOnceReadOnceState.Empty"/>.</summary>
    public WriteOnceReadOnceState State
    {
        get
        {
            int state = Volatile.Read(ref _state);
            return state == STATE_WRITING ? WriteOnceReadOnceState.Empty : (WriteOnceReadOnceState)state;
        }
    }

    /// <summary>Gets whether a value has been set and has not been consumed yet.</summary>
    public bool HasValue => Volatile.Read(ref _state) == STATE_READY;

    /// <summary>Sets the value once. Returns false if a value has already been set, is being set, or has been consumed.</summary>
    /// <param name="value">The value to set.</param>
    /// <returns>true if the value was set successfully; otherwise, false.</returns>
    public bool TrySet(T value)
    {
        if (Interlocked.CompareExchange(ref _state, STATE_WRITING, STATE_EMPTY) != STATE_EMPTY)
        {
            return false;
        }

        _value = value;
        Volatile.Write(ref _state, STATE_READY);
        return true;
    }

    /// <summary>Consumes and retrieves the value. Returns false if no value is set or it has already been consumed.</summary>
    /// <param name="value">The consumed value, or the default value if unavailable.</param>
    /// <returns>true if the value was consumed; otherwise, false.</returns>
    public bool TryGet(out T? value)
    {
        if (Interlocked.CompareExchange(ref _state, STATE_CONSUMED, STATE_READY) == STATE_READY)
        {
            value = _value;
            _value = default;
            return true;
        }

        value = default;
        return false;
    }
}