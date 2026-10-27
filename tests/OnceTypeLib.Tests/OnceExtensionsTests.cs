namespace OnceTypeLib.Tests;

public class OnceExtensionsTests
{
	/// <summary>Verifies that GetOrThrow on a ReadOnce throws ArgumentNullException when the source is null.</summary>
	[Fact]
	public void ReadOnce_GetOrThrow_NullSource_ThrowsArgumentNullException()
	{
		ReadOnce<int> once = null!;

		_ = Assert.Throws<ArgumentNullException>(() => once.GetOrThrow());
	}

	/// <summary>Verifies that the first GetOrThrow on a ReadOnce returns the value and consumes it.</summary>
	[Fact]
	public void ReadOnce_GetOrThrow_FirstCallReturnsValueAndConsumesIt()
	{
		var once = new ReadOnce<int>(42);

		Assert.Equal(42, once.GetOrThrow());
		Assert.False(once.HasValue);
	}

	/// <summary>Verifies that GetOrThrow on a consumed ReadOnce throws InvalidOperationException.</summary>
	[Fact]
	public void ReadOnce_GetOrThrow_AfterConsumption_ThrowsInvalidOperationException()
	{
		var once = new ReadOnce<int>(42);
		_ = once.GetOrThrow();

		_ = Assert.Throws<InvalidOperationException>(() => once.GetOrThrow());
	}
}