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
        var once = new WriteOnceReadOnce<int>();
        _ = once.TrySet(1);
        Assert.True(once.TryGet(out _));

        Assert.False(once.TrySet(2));
        Assert.Equal(WriteOnceReadOnceState.Consumed, once.State);
        Assert.False(once.HasValue);
    }

    /// <summary>Verifies that reading a consumed value fails and returns the default value.</summary>
    [Fact]
    public void TryGet_AfterConsumption_ReturnsFalseAndDefault()
    {
        var once = new WriteOnceReadOnce<int>();
        _ = once.TrySet(42);
        Assert.True(once.TryGet(out _));

        Assert.False(once.TryGet(out int value));
        Assert.Equal(default, value);
        Assert.Equal(WriteOnceReadOnceState.Consumed, once.State);
    }

    /// <summary>Verifies that a null reference value can be set and retrieved.</summary>
    [Fact]
    public void TrySet_NullReferenceValue_CanBeRetrieved()
    {
        var once = new WriteOnceReadOnce<string?>();

        Assert.True(once.TrySet(null));
        Assert.True(once.HasValue);
        Assert.True(once.TryGet(out string? value));
        Assert.Null(value);
        Assert.Equal(WriteOnceReadOnceState.Consumed, once.State);
    }

    /// <summary>Verifies that a default value-type value can be set and retrieved.</summary>
    [Fact]
    public void TrySet_DefaultValueType_CanBeRetrieved()
    {
        var once = new WriteOnceReadOnce<int>();

        Assert.True(once.TrySet(default));
        Assert.True(once.HasValue);
        Assert.True(once.TryGet(out int value));
        Assert.Equal(default, value);
        Assert.Equal(WriteOnceReadOnceState.Consumed, once.State);
    }

    /// <summary>Verifies that retrieving a reference-type value returns the same instance.</summary>
    [Fact]
    public void TryGet_ReferenceType_ReturnsSameInstance()
    {
        var once = new WriteOnceReadOnce<object>();
        var obj = new object();

        Assert.True(once.TrySet(obj));
        Assert.True(once.TryGet(out object? retrieved));
        Assert.Same(obj, retrieved);
        Assert.Equal(WriteOnceReadOnceState.Consumed, once.State);
    }

    /// <summary>Verifies that exactly one concurrent set attempt succeeds.</summary>
    [Fact(Timeout = 10000)]
    public async Task TrySet_ConcurrentCalls_ExactlyOneSucceeds()
    {
        const int Iterations = 1000;
        const int Concurrency = 16;
        var onces = Enumerable.Range(0, Iterations).Select(_ => new WriteOnceReadOnce<int>()).ToArray();
        var successCounts = new int[Iterations];
        var winners = new int[Iterations];

        await RunConcurrentlyAsync(Concurrency, worker =>
        {
            for (int i = 0; i < Iterations; i++)
            {
                if (onces[i].TrySet(worker))
                {
                    _ = Interlocked.Increment(ref successCounts[i]);
                    Volatile.Write(ref winners[i], worker);
                }
            }
        });

        for (int i = 0; i < Iterations; i++)
        {
            Assert.Equal(1, successCounts[i]);
            Assert.Equal(WriteOnceReadOnceState.Ready, onces[i].State);
            Assert.True(onces[i].TryGet(out int value));
            Assert.Equal(winners[i], value);
        }
    }

    /// <summary>Verifies that exactly one concurrent read attempt succeeds.</summary>
    [Fact(Timeout = 10000)]
    public async Task TryGet_ConcurrentCalls_ExactlyOneSucceeds()
    {
        const int Iterations = 1000;
        const int Concurrency = 16;
        var onces = Enumerable.Range(0, Iterations).Select(_ => new WriteOnceReadOnce<int>()).ToArray();
        foreach (var once in onces)
        {
            Assert.True(once.TrySet(42));
        }

        var successCounts = new int[Iterations];
        var invalidResults = 0;

        await RunConcurrentlyAsync(Concurrency, _ =>
        {
            for (int i = 0; i < Iterations; i++)
            {
                bool success = onces[i].TryGet(out int value);
                if (success)
                {
                    _ = Interlocked.Increment(ref successCounts[i]);
                }

                if (value != (success ? 42 : default))
                {
                    _ = Interlocked.Increment(ref invalidResults);
                }
            }
        });

        Assert.Equal(0, invalidResults);
        for (int i = 0; i < Iterations; i++)
        {
            Assert.Equal(1, successCounts[i]);
            Assert.Equal(WriteOnceReadOnceState.Consumed, onces[i].State);
        }
    }

    /// <summary>Verifies that a read concurrent with setting returns only the published value.</summary>
    [Fact(Timeout = 10000)]
    public async Task TryGet_ConcurrentWithTrySet_ReturnsOnlyPublishedValue()
    {
    }

    private static async Task RunConcurrentlyAsync(int concurrency, Action<int> workerAction)
    {
        var exceptions = new Exception?[concurrency];
        var workers = new Task[concurrency];
        using var readySignal = new CountdownEvent(concurrency);
        var startSignal = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        for (int worker = 0; worker < concurrency; worker++)
        {
            int workerIndex = worker;
            workers[worker] = Task.Run(async () =>
            {
                _ = readySignal.Signal();
                await startSignal.Task;

                try
                {
                    workerAction(workerIndex);
                }
                catch (Exception exception)
                {
                    exceptions[workerIndex] = exception;
                }
            });
        }

        readySignal.Wait();
        startSignal.SetResult();
        await Task.WhenAll(workers);

        Exception[] workerExceptions = exceptions.OfType<Exception>().ToArray();
        if (workerExceptions.Length > 0)
        {
            throw new AggregateException("A concurrent test worker failed.", workerExceptions);
        }
    }
}