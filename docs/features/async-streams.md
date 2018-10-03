async-streams (C# 8.0)
----------------------

Async-streams are async variants of enumerables, where getting the next element may involve an async operation. They are types that implement `IAsyncEnumerable<T>`.

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

When you have an async-stream, you can enumerate its items using a special `foreach` statement: `foreach await (var item in asyncStream) { ... }`.
Similarly, if you have an async-disposable, you can use and dispose it with a special `using` statement: `using await (var resource = asyncDisposable) { ... }`
A `using await` statement is just like a `using` statement, but it uses `IAsyncDisposable` instead of `IDisposable`, and `await DisposeAsync()` instead of `Dispose()`.

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

### Detailed design for async `foreach` statement

An async `foreach` is lowered just like a regular `foreach`, except that:
- `GetEnumerator()` is replaced with `await GetEnumeratorAsync()`
- `MoveNext()` is replaced with `await MoveNextAsync()`
- `Dispose()` is replaced with `await DisposeAsync()`

Async foreach is disallowed on collections of type dynamic, as there is no async equivalent of the non-generic `IEnumerable` interface.

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
    // clean up e
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
- to support handling a `yield return` statement, which saves the current value and fulfill the promise with result `true`,
- to support handling a `yield break` statement, which fulfills the promise with result `false`,
- to the handling of exceptions, to set the exception into the promise.
(The handling of an `await` is unchanged)

This is reflected in the implementation, which extends the lowering machinery for async methods to:
1. handle `yield return` and `yield break` statements (add methods `VisitYieldReturnStatement` and `VisitYieldBreakStatement` to `AsyncMethodToStateMachineRewriter`),
2. produce additional state and logic for the promise itself (we use `AsyncIteratorRewriter` instead of `AsyncRewriter` to drive the lowering, and produces the other members: `MoveNextAsync`, `Current`, `DisposeAsync`, and some members supporting the resettable `ValueTask`, namely `GetResult`, `SetStatus`, `OnCompleted` and `Value.get`).

**open issue**: The compiler leverages existing BCL types (including some recently added types from the `System.Threading.Tasks.Extensions` NuGet package) in the state machine it generates. But as part of this feature, we may introduce some additional BCL types, so that the state machine can be further simplified and optimized.

```C#
ValueTask<bool> MoveNextAsync()
{
    if (State == StateMachineStates.FinishedStateMachine)
    {
        return default(ValueTask<bool>)
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

In terms of the lowered code, there are five changes to the `MoveNext()` method of an async-iterator state machine, compared to that of a regular `async` state machines:
- reaching the end of the method
- reaching an `await`
- reaching a `yield return`
- reaching a `yield break`
- handling exceptions
Those changes are described below for information, but they are compiler implementation details which should not be depended on (such generated code is subject to change without notice).

When we reach the end of the method, we need to fulfill the promise of value-or-end with `false` to signal that the end was reached:
```C#
_promiseOfValueOrEnd.SetResult(false);
```

When we reach a `yield return`, we save the "current" value and fulfill the promise of value-or-end with `true` to signal that a value is available:
```C#
_valueOrEndPromise.SetResult(true);
```

When we reach a `yield break`, we fulfill the promise of value-or-end with `false` to signal that the end was reached:
```C#
_promiseOfValueOrEnd.SetResult(false);
return;
```

The `MoveNext()` method of the state machine also includes exception handling.
For regular `async` methods, we catch any such exception and pass it on to the caller of the state machine, by setting the exception in the task being awaited by the caller.
For async-iterators, we also catch any such exception and pass it on to the caller of the state machine (`MoveNextAsync`) via the promise:

```C#
catch (Exception ex)
{
    _state = finishedState;
    _promiseOfValueOrEnd.SetException(ex);
    return;
}
```
