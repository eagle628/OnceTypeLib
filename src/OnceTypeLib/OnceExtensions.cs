namespace OnceTypeLib;

/// <summary>
/// Strict-mode helpers that throw <see cref="InvalidOperationException"/> when a Try operation fails.
/// </summary>
public static class OnceExtensions
{
    /// <summary>Sets the value of a <see cref="WriteOnce{T}"/>.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
    /// <exception cref="InvalidOperationException">A value has already been set.</exception>
    public static void SetOrThrow<T>(this WriteOnce<T> source, T value)
    {
        Guard.NotNull(source, nameof(source));
        if (!source.TrySet(value))
        {
            throw new InvalidOperationException("The value has already been set.");
        }
    }

    /// <summary>Gets the value of a <see cref="WriteOnce{T}"/>.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
    /// <exception cref="InvalidOperationException">No value has been set yet.</exception>
    public static T GetOrThrow<T>(this WriteOnce<T> source)
    {
        Guard.NotNull(source, nameof(source));
        if (!source.TryGet(out T? value))
        {
            throw new InvalidOperationException("The value has not been set yet.");
        }

        return value!;
    }

    /// <summary>Consumes the value of a <see cref="ReadOnce{T}"/>.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
    /// <exception cref="InvalidOperationException">The value has already been consumed.</exception>
    public static T GetOrThrow<T>(this ReadOnce<T> source)
    {
        Guard.NotNull(source, nameof(source));
        if (!source.TryGet(out T? value))
        {
            throw new InvalidOperationException("The value has already been consumed.");
        }

        return value!;
    }

    /// <summary>Sets the value of a <see cref="WriteOnceReadOnce{T}"/>.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
    /// <exception cref="InvalidOperationException">A value has already been set, or has been set and consumed.</exception>
    public static void SetOrThrow<T>(this WriteOnceReadOnce<T> source, T value)
    {
        Guard.NotNull(source, nameof(source));
        if (!source.TrySet(value))
        {
            // Empty here means another thread is still writing its value, so it counts as already set.
            throw new InvalidOperationException(source.State == WriteOnceReadOnceState.Consumed
                ? "The value has already been set and consumed."
                : "The value has already been set.");
        }
    }

    /// <summary>Consumes the value of a <see cref="WriteOnceReadOnce{T}"/>.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
    /// <exception cref="InvalidOperationException">The value has not been set yet, or has already been consumed.</exception>
    public static T GetOrThrow<T>(this WriteOnceReadOnce<T> source)
    {
        Guard.NotNull(source, nameof(source));
        if (!source.TryGet(out T? value))
        {
            // Empty here may also mean another thread is still writing its value; the value is not yet available.
            throw new InvalidOperationException(source.State == WriteOnceReadOnceState.Consumed
                ? "The value has already been consumed."
                : "The value has not been set yet.");
        }

        return value!;
    }
}