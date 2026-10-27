# OnceTypeLib

Small, lock-free C# types that express a "one-time access contract" explicitly.

- The core API never throws: it is built on the Try pattern. An optional strict mode throws when you want failures to surface immediately.
- State transitions use `Interlocked` and `Volatile`; there are no locks.
- Targets netstandard2.0, net8.0 and net10.0. Through netstandard2.0 it also works on .NET Framework 4.6.2 and later.

## Installation

```shell
dotnet add package OnceTypeLib
```

## Types

| Type | Write | Read | Side effect of reading | Typical use |
| :--- | :--- | :--- | :--- | :--- |
| `WriteOnce<T>` | Once | Any number of times | None | Settings determined after startup, late-injected context |
| `ReadOnce<T>` | At construction only | Once | Yes (the stored value is discarded) | Plain-text passwords, single-use tokens |
| `WriteOnceReadOnce<T>` | Once | Once | Yes (the stored value is discarded) | One-time hand-off between threads or classes |

All types are `sealed class`. A struct would be copied implicitly on assignment, duplicating its state and breaking the once guarantee.

## Usage

### WriteOnce

`WriteOnce<T>` fits values that are decided later, exactly once. `HasValue` tells whether the value has been set.

```csharp
using OnceTypeLib;

var endpoint = new WriteOnce<string>();

Console.WriteLine(endpoint.HasValue);

endpoint.TrySet("https://example.com");   // true
endpoint.TrySet("https://other.example"); // false (already set)

Console.WriteLine(endpoint.HasValue);

if (endpoint.TryGet(out string? value))
{
    Console.WriteLine(value); // https://example.com (can be read repeatedly)
}
```

### ReadOnce

The stored value is discarded when it is read, so a second read fails. `HasValue` tells whether the value is still available, that is, it has not been consumed yet.

```csharp
var token = new ReadOnce<string>("secret-token");

Console.WriteLine(token.HasValue);

token.TryGet(out string? first);   // true, first == "secret-token"
token.TryGet(out string? second);  // false, second == null

Console.WriteLine(token.HasValue);
```

### WriteOnceReadOnce

A one-time buffer: it can be set once and taken once. `HasValue` tells whether a value is currently available, that is, it has been set and not consumed yet. It is equivalent to `State == WriteOnceReadOnceState.Ready`.  
`State` returns a `WriteOnceReadOnceState`, which is one of `WriteOnceReadOnceState.Empty`, `WriteOnceReadOnceState.Ready` or `WriteOnceReadOnceState.Consumed`.

```csharp
var handoff = new WriteOnceReadOnce<string>();

Console.WriteLine(handoff.State);    // Empty
Console.WriteLine(handoff.HasValue);

handoff.TrySet("ticket");            // true

Console.WriteLine(handoff.State);    // Ready
Console.WriteLine(handoff.HasValue);

handoff.TryGet(out string? ticket);  // true
handoff.TryGet(out _);               // false (already consumed)
handoff.TrySet("again");             // false (already consumed)

Console.WriteLine(handoff.State);    // Consumed
```

### Strict mode

`SetOrThrow` and `GetOrThrow` in `OnceExtensions` throw `InvalidOperationException` on failure.

```csharp
var config = new WriteOnce<int>();

config.SetOrThrow(42);
int value = config.GetOrThrow(); // 42
config.SetOrThrow(43);           // throws InvalidOperationException
```

Strict mode asserts the invariant "this call must succeed here", so a failure indicates a bug and is not meant to be caught and recovered from. Use `TrySet` / `TryGet` when you want to recover, or where races are possible.

## Thread safety and caveats

- When several threads call `TrySet` / `TryGet` concurrently, exactly one of them succeeds.
- To avoid exposing an uninitialized value, `TrySet` of `WriteOnce<T>` and `WriteOnceReadOnce<T>` moves through `Empty -> Writing -> Ready` internally. `Writing` is not public.
- While a value is being written, `WriteOnceReadOnce<T>.State` reports `WriteOnceReadOnceState.Empty`. Therefore `State == WriteOnceReadOnceState.Empty` does not mean that `TrySet` will succeed. `State` is only a snapshot, and it does not guarantee the outcome of a later `TrySet` / `TryGet`.
- The `Writing` phase is normally just the assignment of `T`, which is close to zero for reference types and small value types. It lasts longer for large structs, or if the writing thread is preempted.
- When using the `OrThrow` methods, the caller must guarantee a happens-before relationship with the completion of the write.

## License

MIT. See [LICENSE](LICENSE).
