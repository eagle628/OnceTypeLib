namespace OnceTypeLib;

/// <summary>
/// A lock-free container that allows a value to be set once and retrieved any number of times.
/// It does not throw exceptions and only provides the Try-pattern.
/// </summary>
/// <typeparam name="T">The type of the value to store.</typeparam>
public sealed class WriteOnce<T>
{
    private const int STATE_EMPTY = 0;
    private const int STATE_READY = 1;
    // Internal state used to prevent readers from observing a value before it is published.
    private const int STATE_WRITING = 2;

    private int _state;
    private T? _value;

    /// <summary>Initializes a new instance of <see cref="WriteOnce{T}"/> without a value.</summary>
    public WriteOnce()
    {
    }

    /// <summary>Gets whether a value has been set.</summary>
    public bool HasValue => Volatile.Read(ref _state) == STATE_READY;

    /// <summary>Sets the value once. Returns false if a value has already been set or is being set.</summary>
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

    /// <summary>Gets the value. Returns false if no value has been set. This method can be called multiple times.</summary>
    /// <param name="value">The retrieved value, or the default value if no value has been set.</param>
    /// <returns>true if the value was retrieved; otherwise, false.</returns>
    public bool TryGet(out T? value)
    {
        if (Volatile.Read(ref _state) == STATE_READY)
        {
            value = _value;
            return true;
        }

        value = default;
        return false;
    }
}