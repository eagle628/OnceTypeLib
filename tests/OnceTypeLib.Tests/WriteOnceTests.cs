namespace OnceTypeLib.Tests;

public class WriteOnceTests
{
    /// <summary>Verifies that a new instance has no value.</summary>
    [Fact]
    public void NewInstance_HasNoValue()
    {
        var once = new WriteOnce<int>();

        Assert.False(once.HasValue);
        Assert.False(once.TryGet(out int value));
        Assert.Equal(default, value);
    }


    /// <summary>Verifies that a value can be set and then retrieved.</summary>
    [Fact]
    public void TrySet_ThenTryGet_ReturnsValue()
    {
        var once = new WriteOnce<string>();

        Assert.True(once.TrySet("hello"));
        Assert.True(once.HasValue);
        Assert.True(once.TryGet(out string? value));
        Assert.Equal("hello", value);
    }

    /// <summary>Verifies that a set value can be retrieved repeatedly.</summary>
    [Fact]
    public void TryGet_CanBeCalledMultipleTimes()
    {
        var once = new WriteOnce<int>();
        _ = once.TrySet(42);

        for (int i = 0; i < 3; i++)
        {
            Assert.True(once.TryGet(out int value));
            Assert.Equal(42, value);
        }

        Assert.True(once.HasValue);
    }

    /// <summary>Verifies that subsequent set attempts fail without replacing the first value.</summary>
    [Fact]
    public void TrySet_SecondCall_ReturnsFalseAndKeepsFirstValue()
    {
        var once = new WriteOnce<int>();

        Assert.True(once.TrySet(1));
        Assert.False(once.TrySet(2));
        Assert.False(once.TrySet(3));

        Assert.True(once.TryGet(out int value));
        Assert.Equal(1, value);
    }

    /// <summary>Verifies that null is accepted as a set reference value.</summary>
    [Fact]
    public void TrySet_NullReferenceValue_IsTreatedAsSet()
    {
        var once = new WriteOnce<string?>();

        Assert.True(once.TrySet(null));
        Assert.True(once.HasValue);
        Assert.True(once.TryGet(out string? value));
        Assert.Null(value);
        Assert.False(once.TrySet("other"));
    }

    /// <summary>Verifies that the default value of a value type is accepted as a set value.</summary>
    [Fact]
    public void TrySet_DefaultValueType_IsTreatedAsSet()
    {
        var once = new WriteOnce<int>();

        Assert.True(once.TrySet(0));
        Assert.True(once.HasValue);
        Assert.False(once.TrySet(1));
        Assert.True(once.TryGet(out int value));
        Assert.Equal(0, value);
    }

    /// <summary>Verifies that retrieving a reference-type value returns the same instance.</summary>
    [Fact]
    public void TryGet_ReferenceType_ReturnsSameInstance()
    {
        var once = new WriteOnce<object>();
        var instance = new object();
        _ = once.TrySet(instance);

        Assert.True(once.TryGet(out object? value));
        Assert.Same(instance, value);
    }

    /// <summary>Verifies that exactly one concurrent set attempt succeeds for each instance.</summary>
    [Fact(Timeout = 10000)]
    public async Task TrySet_ConcurrentCalls_ExactlyOneSucceeds()
    {
        const int Iterations = 1000;
        const int testParallelism = 16;
        var once = Enumerable.Range(0, Iterations).Select(_ => new WriteOnce<int>()).ToArray();
        var successCounts = new int[Iterations];
        var winners = Enumerable.Repeat(-1, Iterations).ToArray();

        await RunConcurrentlyAsync(testParallelism, worker =>
        {
            for (int iteration = 0; iteration < Iterations; iteration++)
            {
                if (once[iteration].TrySet(worker))
                {
                    _ = Interlocked.Increment(ref successCounts[iteration]);
                    Volatile.Write(ref winners[iteration], worker);
                }
            }
        });

        for (int iteration = 0; iteration < Iterations; iteration++)
        {
            Assert.Equal(1, successCounts[iteration]);
            Assert.True(once[iteration].HasValue);
            Assert.True(once[iteration].TryGet(out int value));
            Assert.Equal(winners[iteration], value);
        }
    }

    /// <summary>Checks that successful reads during a concurrent set attempt return the published value.</summary>
    /// <remarks>
    /// The start signal releases all workers together but does not guarantee that a read overlaps
    /// the write. This test verifies that successful reads return the published value; it does not
    /// deterministically exercise or assert the internal writing state.
    /// </remarks>
    [Fact(Timeout = 10000)]
    public async Task TryGet_ConcurrentWithTrySet_ReturnsOnlyPublishedValue()
    {
        const int ReaderCount = 16;
        const int ReadAttempts = 100_000;
        var once = new WriteOnce<string>();
        int invalidReads = 0;
        using var readySignal = new CountdownEvent(ReaderCount + 1);
        using var startSignal = new ManualResetEventSlim();

        Task writer = Task.Run(() =>
        {
            _ = readySignal.Signal();
            startSignal.Wait();
            Assert.True(once.TrySet("ready"));
        });

        Task[] readers = Enumerable.Range(0, ReaderCount)
            .Select(i => Task.Run(() =>
            {
                _ = readySignal.Signal();
                startSignal.Wait();

                for (int attempt = 0; attempt < ReadAttempts; attempt++)
                {
                    if (once.TryGet(out string? value) && value != "ready")
                    {
                        _ = Interlocked.Increment(ref invalidReads);
                    }
                }
            }))
            .ToArray();

        readySignal.Wait();
        startSignal.Set();
        await Task.WhenAll(readers.Concat(new[] { writer }));

        Assert.Equal(0, invalidReads);
        Assert.True(once.TryGet(out string? final));
        Assert.Equal("ready", final);
    }

    /// <summary>Verifies that concurrent reads of a set value all observe the same value.</summary>
    [Fact(Timeout = 10000)]
    public async Task TryGet_ConcurrentReads_AllObserveSameValue()
    {
        var once = new WriteOnce<int>();
        _ = once.TrySet(123);
        int mismatches = 0;

        await RunConcurrentlyAsync(16, _ =>
        {
            for (int i = 0; i < 625; i++)
            {
                if (!once.TryGet(out int value) || value != 123)
                {
                    _ = Interlocked.Increment(ref mismatches);
                }
            }
        });

        Assert.Equal(0, mismatches);
    }

    private static async Task RunConcurrentlyAsync(int concurrency, Action<int> workerAction)
    {
        var exceptions = new Exception?[concurrency];
        var workers = new Task[concurrency];
        using var readySignal = new CountdownEvent(concurrency);
        var startSignal = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        for (int worker = 0; worker < concurrency; worker++)
        {
            int workerIndex = worker;
            workers[worker] = Task.Run(async () =>
            {
                _ = readySignal.Signal();
                _ = await startSignal.Task;

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
        startSignal.SetResult(true);

        await Task.WhenAll(workers);

        Exception[] workerExceptions = exceptions.OfType<Exception>().ToArray();
        if (workerExceptions.Length > 0)
        {
            throw new AggregateException("A concurrent test worker failed.", workerExceptions);
        }
    }
}
