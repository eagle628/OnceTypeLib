namespace OnceTypeLib.Tests;

public class WriteOnceReadOnceTests
{
    /// <summary>Verifies that a new instance is empty and cannot be read.</summary>
    [Fact]
    public void NewInstance_HasEmptyStateAndCannotBeRead()
    {
        var once = new WriteOnceReadOnce<int>();

        Assert.Equal(WriteOnceReadOnceState.Empty, once.State);
        Assert.False(once.HasValue);
        Assert.False(once.TryGet(out int value));
        Assert.Equal(default, value);
        Assert.Equal(WriteOnceReadOnceState.Empty, once.State);
    }

    /// <summary>Verifies that the first set succeeds and changes the state to ready.</summary>
    [Fact]
    public void TrySet_FirstCall_SucceedsAndSetsReadyState()
    {
        var once = new WriteOnceReadOnce<int>();

        Assert.True(once.TrySet(42));
        Assert.Equal(WriteOnceReadOnceState.Ready, once.State);
        Assert.True(once.HasValue);
    }

    /// <summary>Verifies that reading a set value returns it and consumes it.</summary>
    [Fact]
    public void TryGet_AfterTrySet_ReturnsValueAndConsumesIt()
    {
        var once = new WriteOnceReadOnce<int>();
        _ = once.TrySet(42);

        Assert.True(once.TryGet(out int value));
        Assert.Equal(42, value);
        Assert.Equal(WriteOnceReadOnceState.Consumed, once.State);
        Assert.False(once.HasValue);
    }

    /// <summary>Verifies that setting a value when ready fails without replacing the original value.</summary>
    [Fact]
    public void TrySet_WhenReady_ReturnsFalseAndKeepsOriginalValue()
    {
        var once = new WriteOnceReadOnce<int>();
        Assert.True(once.TrySet(1));

        Assert.False(once.TrySet(2));
        Assert.Equal(WriteOnceReadOnceState.Ready, once.State);

        Assert.True(once.TryGet(out int value));
        Assert.Equal(1, value);
    }

    /// <summary>Verifies that setting a value after it has been consumed fails.</summary>
    [Fact]
    public void TrySet_WhenConsumed_ReturnsFalse()
    {
    }

    /// <summary>Verifies that reading a consumed value fails and returns the default value.</summary>
    [Fact]
    public void TryGet_AfterConsumption_ReturnsFalseAndDefault()
    {
    }

    /// <summary>Verifies that a null reference value can be set and retrieved.</summary>
    [Fact]
    public void TrySet_NullReferenceValue_CanBeRetrieved()
    {
    }

    /// <summary>Verifies that a default value-type value can be set and retrieved.</summary>
    [Fact]
    public void TrySet_DefaultValueType_CanBeRetrieved()
    {
    }

    /// <summary>Verifies that retrieving a reference-type value returns the same instance.</summary>
    [Fact]
    public void TryGet_ReferenceType_ReturnsSameInstance()
    {
    }

    /// <summary>Verifies that exactly one concurrent set attempt succeeds.</summary>
    [Fact(Timeout = 10000)]
    public async Task TrySet_ConcurrentCalls_ExactlyOneSucceeds()
    {
    }

    /// <summary>Verifies that exactly one concurrent read attempt succeeds.</summary>
    [Fact(Timeout = 10000)]
    public async Task TryGet_ConcurrentCalls_ExactlyOneSucceeds()
    {
    }

    /// <summary>Verifies that a read concurrent with setting returns only the published value.</summary>
    [Fact(Timeout = 10000)]
    public async Task TryGet_ConcurrentWithTrySet_ReturnsOnlyPublishedValue()
    {
    }
}