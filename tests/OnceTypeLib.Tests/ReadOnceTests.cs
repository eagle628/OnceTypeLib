namespace OnceTypeLib.Tests;

public class ReadOnceTests
{
	/// <summary>Verifies that a new instance has an available value.</summary>
	[Fact]
	public void NewInstance_HasValue()
	{
		var once = new ReadOnce<int>(42);

		Assert.True(once.HasValue);
	}

	/// <summary>Verifies that the first read returns the initial value and consumes it.</summary>
	[Fact]
	public void TryGet_FirstCallReturnsValueAndConsumesIt()
	{
		var once = new ReadOnce<int>(42);

		Assert.True(once.TryGet(out int value));
		Assert.Equal(42, value);
		Assert.False(once.HasValue);
	}

	/// <summary>Verifies that reads after consumption fail and return the default value.</summary>
	[Fact]
	public void TryGet_AfterConsumption_ReturnsFalseAndDefault()
	{
		var once = new ReadOnce<int>(42);
		Assert.True(once.TryGet(out _));

		Assert.False(once.TryGet(out int firstValue));
		Assert.Equal(default, firstValue);

		Assert.False(once.TryGet(out int secondValue));
		Assert.Equal(default, secondValue);
	}

	/// <summary>Verifies that a null reference value can be read and consumed.</summary>
	[Fact]
	public void TryGet_NullValue_IsConsumed()
	{
		var once = new ReadOnce<string?>(null);

		Assert.True(once.HasValue);
		Assert.True(once.TryGet(out string? value));
		Assert.Null(value);
        
		Assert.False(once.HasValue);
		Assert.False(once.TryGet(out string? afterConsumption));
		Assert.Null(afterConsumption);
	}

	/// <summary>Verifies that a default value-type value can be read and consumed.</summary>
	[Fact]
	public void TryGet_DefaultValue_IsConsumed()
	{
		var once = new ReadOnce<int>(default);

		Assert.True(once.HasValue);
		Assert.True(once.TryGet(out int value));
		Assert.Equal(default, value);
        
		Assert.False(once.HasValue);
		Assert.False(once.TryGet(out int afterConsumption));
		Assert.Equal(default, afterConsumption);
	}

	/// <summary>Verifies that reading a reference-type value returns the same instance.</summary>
	[Fact]
	public void TryGet_ReferenceType_ReturnsSameInstance()
	{
		var instance = new object();
		var once = new ReadOnce<object>(instance);

		Assert.True(once.TryGet(out object? value));
		Assert.Same(instance, value);
	}

	/// <summary>Verifies that exactly one concurrent read succeeds.</summary>
	[Fact(Timeout = 10000)]
	public async Task TryGet_ConcurrentCalls_ExactlyOneSucceeds()
	{
		const int Concurrency = 16;
		var once = new ReadOnce<int>(42);
		var successes = new bool[Concurrency];
		var values = new int[Concurrency];
		using var readySignal = new CountdownEvent(Concurrency);
		var startSignal = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

		Task[] readers = Enumerable.Range(0, Concurrency)
			.Select(worker => Task.Run(async () =>
			{
				_ = readySignal.Signal();
				_ = await startSignal.Task;

				successes[worker] = once.TryGet(out int value);
				values[worker] = value;
			}))
			.ToArray();

		readySignal.Wait();
		startSignal.SetResult(true);
		await Task.WhenAll(readers);

		Assert.Equal(1, successes.Count(success => success));
		for (int worker = 0; worker < Concurrency; worker++)
		{
			Assert.Equal(successes[worker] ? 42 : default, values[worker]);
		}

		Assert.False(once.HasValue);
	}
}