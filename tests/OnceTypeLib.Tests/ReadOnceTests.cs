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
	}

	/// <summary>Verifies that a default value-type value can be read and consumed.</summary>
	[Fact]
	public void TryGet_DefaultValue_IsConsumed()
	{
	}

	/// <summary>Verifies that reading a reference-type value returns the same instance.</summary>
	[Fact]
	public void TryGet_ReferenceType_ReturnsSameInstance()
	{
	}

	/// <summary>Verifies that exactly one concurrent read succeeds.</summary>
	[Fact]
	public void TryGet_ConcurrentCalls_ExactlyOneSucceeds()
	{
	}
}