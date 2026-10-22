namespace OnceTypeLib;

/// <summary>
/// A lock-free container that allows a value to be consumed exactly once.
/// </summary>
/// <typeparam name="T">The type of the value to store.</typeparam>
public sealed class ReadOnce<T>
{
    private const int STATE_CONSUMED = 0;
    private const int STATE_UNREAD = 1;

    private int _state;
    private T? _value;

    /// <summary>Initializes a new instance with the specified value.</summary>
    /// <param name="value">The value to store.</param>
    public ReadOnce(T value)
    {
        _value = value;
        _state = STATE_UNREAD;
    }

    /// <summary>Gets whether the value has not yet been consumed.</summary>
    public bool HasValue => Volatile.Read(ref _state) == STATE_UNREAD;

    /// <summary>Consumes and retrieves the value. Returns false on subsequent calls.</summary>
    /// <param name="value">The consumed value, or the default value if it has already been consumed.</param>
    /// <returns>true if the value was consumed; otherwise, false.</returns>
    public bool TryGet(out T? value)
    {
        if (Interlocked.CompareExchange(ref _state, STATE_CONSUMED, STATE_UNREAD) == STATE_UNREAD)
        {
            value = _value;
            _value = default!;
            return true;
        }

        value = default;
        return false;
    }
}