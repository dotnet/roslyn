async-streams (C# 8.0)
----------------------

Async-streams are asynchronous variants of enumerables, where getting the next element may involve an asynchronous operation. They are types that implement `IAsyncEnumerable<T>`.

```C#
// Those interfaces will ship as part of .NET Core 3
namespace System.Collections.Generic
{
    public interface IAsyncEnumerable<out T>
    {
        IAsyncEnumerator<T> GetAsyncEnumerator();
    }

    public interface IAsyncEnumerator<out T> : System.IAsyncDisposable
    {
        System.Threading.Tasks.ValueTask<bool> MoveNextAsync();
        T Current { get; }
    }
}
namespace System
{
    public interface IAsyncDisposable
    {
        System.Threading.Tasks.ValueTask DisposeAsync();
    }
}
```

When you have an async-stream, you can enumerate its items using an asynchronous `foreach` statement: `await foreach (var item in asyncStream) { ... }`.
An `await foreach` statement is just like a `foreach` statement, but it uses `IAsyncEnumerable` instead of `IEnumerable`, each iteration evaluates an `await MoveNextAsync()`, and the disposable of the enumerator is asynchronous.

Similarly, if you have an async-disposable, you can use and dispose it with asynchronous `using` statement: `await using (var resource = asyncDisposable) { ... }`
An `await using` statement is just like a `using` statement, but it uses `IAsyncDisposable` instead of `IDisposable`, and `await DisposeAsync()` instead of `Dispose()`.

The user can implement those interfaces manually, or can take advantage of the compiler generating a state-machine from a user-defined method (called an "async-iterator" method).
An async-iterator method is a method that:
1. is declared `async`,
2. returns an `IAsyncEnumerable<T>` type,
3. uses both `await` expressions and `yield` statements.

For example:
```C#
async IAsyncEnumerable<int> GetValuesFromServer()
{
    while (true)
    {
        IEnumerable<int> batch = await GetNextBatch();
        if (batch == null) yield break;

        foreach (int item in batch)
        {
            yield return item;
        }
    }
}
```

**open issue**: Design async LINQ

### Detailed design for `await using` statement

An asynchronous `using` is lowered just like a regular `using`, except that `Dispose()` is replaced with `await DisposeAsync()`.

### Detailed design for `await foreach` statement

An `await foreach` is lowered just like a regular `foreach`, except that:
- `GetEnumerator()` is replaced with `await GetEnumeratorAsync()`
- `MoveNext()` is replaced with `await MoveNextAsync()`
- `Dispose()` is replaced with `await DisposeAsync()`

Asynchronous foreach loops are disallowed on collections of type dynamic, as there is no asynchronous equivalent of the non-generic `IEnumerable` interface.

```C#
E e = ((C)(x)).GetAsyncEnumerator();
try
{
    while (await e.MoveNextAsync())
    {
        V v = (V)(T)e.Current;  -OR-  (D1 d1, ...) = (V)(T)e.Current;
        // body
    }
}
finally
{
    await e.DisposeAsync();
}
```

### Detailed design for async-iterator methods

An async-iterator method is replaced by a kick-off method, which initializes a state machine. It does not start running the state machine (unlike kick-off methods for regular async method).
The kick-off method method is marked with both `AsyncStateMachineAttribute` and `IteratorStateMachineAttribute`.

The state machine for an async-iterator method primarily implements `IAsyncEnumerable<T>` and `IAsyncEnumerator<T>`.
It is similar to a state machine produced for an async method. It contains builder and awaiter fields, used to run the state machine in the background (when an `await` is reached in the async-iterator). It also captures parameter values (if any) or `this` (if needed).
But it contains additional state:
- a promise of a value-or-end,
- a current yielded value of type `T`.

The central method of the state machine is `MoveNext()`. It gets run by `MoveNextAsync()`, or as a background continuation initiated from these from an `await` in the method.

The promise of a value-or-end is returned from `MoveNextAsync`. It can be fulfilled with either:
- `true` (when a value becomes available following background execution of the state machine),
- `false` (if the end is reached),
- an exception.
The promise is implemented as a `ManualResetValueTaskSourceLogic<bool>` (which is a re-usable and allocation-free way of producing and fulfilling `ValueTask<bool>` instances) and its surrounding interfaces on the state machine: `IValueTaskSource<bool>` and `IStrongBox<ManualResetValueTaskSourceLogic<bool>>`.

Compared to the state machine for a regular async method, the `MoveNext()` for an async-iterator method adds logic:
- to support handling a `yield return` statement, which saves the current value and fulfills the promise with result `true`,
- to support handling a `yield break` statement, which fulfills the promise with result `false`,
- to exit the method, which fulfills the promise with result `false`,
- to the handling of exceptions, to set the exception into the promise.
(The handling of an `await` is unchanged)

This is reflected in the implementation, which extends the lowering machinery for async methods to:
1. handle `yield return` and `yield break` statements (add methods `VisitYieldReturnStatement` and `VisitYieldBreakStatement` to `AsyncMethodToStateMachineRewriter`),
2. produce additional state and logic for the promise itself (we use `AsyncIteratorRewriter` instead of `AsyncRewriter` to drive the lowering, and produces the other members: `MoveNextAsync`, `Current`, `DisposeAsync`, and some members supporting the resettable `ValueTask`, namely `GetResult`, `SetStatus`, `OnCompleted` and `Value.get`).

```C#
ValueTask<bool> MoveNextAsync()
{
    if (State == StateMachineStates.FinishedStateMachine)
    {
        return default(ValueTask<bool>);
    }
    _valueOrEndPromise.Reset();
    var inst = this;
    _builder.Start(ref inst);
    return new ValueTask<bool>(this, _valueOrEndPromise.Version);
}
```

```C#
T Current => _current;
```
