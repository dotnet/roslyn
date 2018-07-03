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
        System.Threading.Tasks.ValueTask<bool> WaitForNextAsync();
        T TryGetNext(out bool success);
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

PROTOTYPE(async-streams): TODO: async LINQ

### Detailed design for async `foreach` statement
PROTOTYPE(async-streams): TODO

### Detailed design for async `using` statement
PROTOTYPE(async-streams): TODO

### Detailed design for async-iterator methods

The state machine for an async-iterator method primarily implements `IAsyncEnumerable<T>` and `IAsyncEnumerator<T>`.
It is similar to a state machine produced for an async method. It contains builder and awaiter fields, used to run the state machine in the background (when an `await` is reached in the async-iterator).
But it contains additional state:
- a promise of a value-or-end,
- a `bool` flag indicating whether the promise is active or not,
- a current yielded value of type `T`.

The promise of a value-or-end is returned from `WaitForNextAsync`. It can be fulfilled with either:
- `true` (when a value becomes available following background execution of the state machine),
- `false` (if the end is reached),
- an exception.
The promise is implemented as a `ManualResetValueTaskSourceLogic<bool>` (which is a re-usable and allocation-free way of producing and fulfilling `ValueTask<bool>` instances) and its surrounding interfaces on the state machine: `IValueTaskSource<bool>` and `IStrongBox<ManualResetValueTaskSourceLogic<bool>>`.

Compared to the state machine for a regular async method, the `MoveNext()` for an async-iterator method adds logic:
- to the handling of an `await`, to reset the promise,
- to the handling of exceptions, to set the exception into the promise, if active, or rethrow it otherwise,
- to support handling a `yield return` statement, which saves the current value and fulfill the promise (if active),
- to support handling a `yield break` statement, which resets the promise (if active) and fulfills it with result `false`.

This is reflected in the implementation, which extends the lowering machinery for async methods to:
1. properly signal through the promise (update in various methods in `AsyncMethodToStateMachineRewriter`, such as `VisitAwaitExpression`)
2. handle `yield return` and `yield break` statements (add methods `VisitYieldReturnStatement` and `VisitYieldBreakStatement` to `AsyncMethodToStateMachineRewriter`),
3. produce additional state and logic for the promise itself (we use `AsyncIteratorRewriter` instead of `AsyncRewriter` to drive the lowering, and produces the other members: `WaitForNextAsync`, `TryGetNext`, `DisposeAsync`, and some members supporting the resettable `ValueTask`, namely `GetResult`, `SetStatus`, `OnCompleted` and `Value.get`).

The contract of the `MoveNext()` method is that it returns either:
- in completed state,
- leaving the promise inactive (when started with an inactive promise and a value is immediately available),
- with an exception (when started with an inactive promise and an exception is thrown),
- an active promise, which will later be fulfilled (with `true`, `false` or an exception).

If the promise is active:
- the builder is running the `MoveNext()` logic,
- a call to `WaitForNextAsync` will not move the state machine forward (ie. it won't call `MoveNext()`),
- a call to `TryGetNext` APIs will throw.

PROTOTYPE(async-streams): The compiler leverages existing BCL types (including some recently added types from the `System.Threading.Tasks.Extensions` NuGet package) in the state machine it generates. But as part of this feature, we may introduce some additional BCL types, so that the state machine can be further simplified and optimized.

```C#
ValueTask<bool> WaitForNextAsync()
{
    if (State == StateMachineStates.FinishedStateMachine)
    {
        return default(ValueTask<bool>);
    }

    if (!this._promiseIsActive || this.State == StateMachineStates.NotStartedStateMachine)
    {
        var inst = this;
        this._builder.Start(ref inst);
    }

    return new ValueTask<bool>(this, _valueOrEndPromise.Version);
}
```

```C#
T TryGetNext(out bool success)
{
   if (this._promiseIsActive)
   {
       if (_valueOrEndPromise.GetStatus(_valueOrEndPromise.Version) == ValueTaskSourceStatus.Pending) throw new Exception();
       _promiseIsActive = false;
   }
   else
   {
       var inst = this;
       this._builder.Start(ref inst);
   }

   if (_promiseIsActive || State == StateMachineStates.FinishedStateMachine)
   {
       success = false;
       return default;
   }

   success = true;
   return _current;
}
```

