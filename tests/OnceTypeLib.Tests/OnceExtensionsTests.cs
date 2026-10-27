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

	/// <summary>Verifies that SetOrThrow on a WriteOnce throws ArgumentNullException when the source is null.</summary>
	[Fact]
	public void WriteOnce_SetOrThrow_NullSource_ThrowsArgumentNullException()
	{
		WriteOnce<int> once = null!;

		_ = Assert.Throws<ArgumentNullException>(() => once.SetOrThrow(42));
	}

	/// <summary>Verifies that SetOrThrow on an empty WriteOnce stores the value.</summary>
	[Fact]
	public void WriteOnce_SetOrThrow_FirstCallStoresValue()
	{
		var once = new WriteOnce<int>();

		once.SetOrThrow(42);

		Assert.True(once.HasValue);
		Assert.Equal(42, once.GetOrThrow());
	}

	/// <summary>Verifies that SetOrThrow on a WriteOnce that already has a value throws and keeps the first value.</summary>
	[Fact]
	public void WriteOnce_SetOrThrow_AlreadySet_ThrowsInvalidOperationException()
	{
		var once = new WriteOnce<int>();
		once.SetOrThrow(42);

		_ = Assert.Throws<InvalidOperationException>(() => once.SetOrThrow(99));
		Assert.Equal(42, once.GetOrThrow());
	}

	/// <summary>Verifies that GetOrThrow on a WriteOnce throws ArgumentNullException when the source is null.</summary>
	[Fact]
	public void WriteOnce_GetOrThrow_NullSource_ThrowsArgumentNullException()
	{
		WriteOnce<int> once = null!;

		_ = Assert.Throws<ArgumentNullException>(() => once.GetOrThrow());
	}

	/// <summary>Verifies that GetOrThrow on a WriteOnce returns the value and can be called repeatedly.</summary>
	[Fact]
	public void WriteOnce_GetOrThrow_AfterSet_ReturnsValueRepeatedly()
	{
		var once = new WriteOnce<int>();
		once.SetOrThrow(42);

		Assert.Equal(42, once.GetOrThrow());
		Assert.Equal(42, once.GetOrThrow());
	}

	/// <summary>Verifies that GetOrThrow on an unset WriteOnce throws InvalidOperationException.</summary>
	[Fact]
	public void WriteOnce_GetOrThrow_NotSet_ThrowsInvalidOperationException()
	{
		var once = new WriteOnce<int>();

		_ = Assert.Throws<InvalidOperationException>(() => once.GetOrThrow());
	}

	/// <summary>Verifies that SetOrThrow on a WriteOnceReadOnce throws ArgumentNullException when the source is null.</summary>
	[Fact]
	public void WriteOnceReadOnce_SetOrThrow_NullSource_ThrowsArgumentNullException()
	{
		WriteOnceReadOnce<int> once = null!;

		_ = Assert.Throws<ArgumentNullException>(() => once.SetOrThrow(42));
	}

	/// <summary>Verifies that SetOrThrow on an empty WriteOnceReadOnce stores the value.</summary>
	[Fact]
	public void WriteOnceReadOnce_SetOrThrow_FirstCallStoresValue()
	{
		var once = new WriteOnceReadOnce<int>();

		once.SetOrThrow(42);

		Assert.Equal(WriteOnceReadOnceState.Ready, once.State);
	}

	/// <summary>Verifies that SetOrThrow on a WriteOnceReadOnce that already has a value throws and keeps the first value.</summary>
	[Fact]
	public void WriteOnceReadOnce_SetOrThrow_AlreadySet_ThrowsInvalidOperationException()
	{
		var once = new WriteOnceReadOnce<int>();
		once.SetOrThrow(42);

		_ = Assert.Throws<InvalidOperationException>(() => once.SetOrThrow(99));

		Assert.Equal(WriteOnceReadOnceState.Ready, once.State);
		Assert.Equal(42, once.GetOrThrow());
	}

	/// <summary>Verifies that SetOrThrow on a consumed WriteOnceReadOnce throws and leaves the state Consumed.</summary>
	[Fact]
	public void WriteOnceReadOnce_SetOrThrow_AfterConsumption_ThrowsInvalidOperationException()
	{
		var once = new WriteOnceReadOnce<int>();
		once.SetOrThrow(42);
		_ = once.GetOrThrow();

		_ = Assert.Throws<InvalidOperationException>(() => once.SetOrThrow(99));

		Assert.Equal(WriteOnceReadOnceState.Consumed, once.State);
	}

	/// <summary>Verifies that GetOrThrow on a WriteOnceReadOnce throws ArgumentNullException when the source is null.</summary>
	[Fact]
	public void WriteOnceReadOnce_GetOrThrow_NullSource_ThrowsArgumentNullException()
	{
		WriteOnceReadOnce<int> once = null!;

		_ = Assert.Throws<ArgumentNullException>(() => once.GetOrThrow());
	}

	/// <summary>Verifies that the first GetOrThrow on a WriteOnceReadOnce returns the value and consumes it.</summary>
	[Fact]
	public void WriteOnceReadOnce_GetOrThrow_AfterSet_ReturnsValueAndConsumesIt()
	{
		var once = new WriteOnceReadOnce<int>();
		once.SetOrThrow(42);

		Assert.Equal(42, once.GetOrThrow());
		Assert.Equal(WriteOnceReadOnceState.Consumed, once.State);
	}

	/// <summary>Verifies that GetOrThrow on an unset WriteOnceReadOnce throws and leaves the state Empty.</summary>
	[Fact]
	public void WriteOnceReadOnce_GetOrThrow_NotSet_ThrowsInvalidOperationException()
	{
		var once = new WriteOnceReadOnce<int>();

		_ = Assert.Throws<InvalidOperationException>(() => once.GetOrThrow());

		Assert.Equal(WriteOnceReadOnceState.Empty, once.State);
	}

	/// <summary>Verifies that GetOrThrow on a consumed WriteOnceReadOnce throws and leaves the state Consumed.</summary>
	[Fact]
	public void WriteOnceReadOnce_GetOrThrow_AfterConsumption_ThrowsInvalidOperationException()
	{
		var once = new WriteOnceReadOnce<int>();
		once.SetOrThrow(42);
		_ = once.GetOrThrow();

		_ = Assert.Throws<InvalidOperationException>(() => once.GetOrThrow());

		Assert.Equal(WriteOnceReadOnceState.Consumed, once.State);
	}
}