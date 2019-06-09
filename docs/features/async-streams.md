async-streams (C# 8.0)
----------------------

Async-streams are asynchronous variants of enumerables, where getting the next element may involve an asynchronous operation. They are types that implement `IAsyncEnumerable<T>`.

```csharp
// Those interfaces will ship as part of .NET Core 3
namespace System.Collections.Generic
{
    public interface IAsyncEnumerable<out T>
    {
        IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken token = default);
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
2. returns an `IAsyncEnumerable<T>` or `IAsyncEnumerator<T>` type,
3. uses both `await` syntax (`await` expression, `await foreach` or `await using` statements) and `yield` statements (`yield return`, `yield break`).

For example:
```csharp
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

Just like in iterator methods, there are several restrictions on where a yield statement can appear in async-iterator methods:
- It is a compile-time error for a `yield` statement (of either form) to appear in the `finally` clause of a `try` statement.
- It is a compile-time error for a `yield return` statement to appear anywhere in a `try` statement that contains any `catch` clauses.

### Detailed design for `await using` statement

An asynchronous `using` is lowered just like a regular `using`, except that `Dispose()` is replaced with `await DisposeAsync()`.

Note that pattern-based lookup for `DisposeAsync` binds to instance methods that can be invoked without arguments.
Extension methods do not contribute. The result of `DisposeAsync` must be awaitable.

### Detailed design for `await foreach` statement

An `await foreach` is lowered just like a regular `foreach`, except that:
- `GetEnumerator()` is replaced with `await GetAsyncEnumerator()`
- `MoveNext()` is replaced with `await MoveNextAsync()`
- `Dispose()` is replaced with `await DisposeAsync()`

Note that pattern-based lookup for `GetAsyncEnumerator`, `MoveNextAsync` and `DisposeAsync` binds to instance methods that can be invoked without arguments.
Extension methods do not contribute. The result of `MoveNextAsync` and `DisposeAsync` must be awaitable.
Disposal for `await foreach` does not include a fallback to a runtime check for an interface implementation.

Asynchronous foreach loops are disallowed on collections of type dynamic,
as there is no asynchronous equivalent of the non-generic `IEnumerable` interface.

But wrapper types can pass non-default values (see `.WithCancellation(CancellationToken)` extension method),
thereby allowing consumers of async-streams to control cancellation.
A producer of async-streams can make use of the cancellation token by writing an
`IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken)` async-iterator method in a custom type.

```csharp
E e = ((C)(x)).GetAsyncEnumerator(default);
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
The kick-off method method is marked with `AsyncIteratorStateMachineAttribute`.

The state machine for an enumerable async-iterator method primarily implements `IAsyncEnumerable<T>` and `IAsyncEnumerator<T>`.
For an enumerator async-iterator, it only implements `IAsyncEnumerator<T>`.
It is similar to a state machine produced for an async method.
It contains builder and awaiter fields, used to run the state machine in the background (when an `await` is reached in the async-iterator).
It also captures parameter values (if any) or `this` (if needed).

But it contains additional state:
- a promise of a value-or-end,
- a current yielded value of type `T`,
- an `int` capturing the id of the thread that created it,
- a `bool` flag indicating "dispose mode",
- a `CancellationTokenSource` for combining tokens (in enumerables).

The central method of the state machine is `MoveNext()`. It gets run by `MoveNextAsync()`, or as a background continuation initiated from these from an `await` in the method.

The promise of a value-or-end is returned from `MoveNextAsync`. It can be fulfilled with either:
- `true` (when a value becomes available following background execution of the state machine),
- `false` (if the end is reached),
- an exception.
The promise is implemented as a `ManualResetValueTaskSourceCore<bool>` (which is a re-usable and allocation-free way of producing and fulfilling `ValueTask<bool>` or `ValueTask` instances)
and its surrounding interfaces on the state machine: `IValueTaskSource<bool>` and `IValueTaskSource`.
See more details about those types at https://blogs.msdn.microsoft.com/dotnet/2018/11/07/understanding-the-whys-whats-and-whens-of-valuetask/

Compared to the state machine for a regular async method, the `MoveNext()` for an async-iterator method adds logic:
- to support handling a `yield return` statement, which saves the current value and fulfills the promise with result `true`,
- to support handling a `yield break` statement, which sets the dispose mode on and jumps to the enclosing `finally` or exit,
- to dispatch execution to `finally` blocks (when disposing),
- to exit the method, which disposes the `CancellationTokenSource` (if any) and fulfills the promise with result `false`,
- to catch exceptions, which disposes the `CancellationTokenSource` (if any) and sets the exception into the promise.
(The handling of an `await` is unchanged)

This is reflected in the implementation, which extends the lowering machinery for async methods to:
1. handle `yield return` and `yield break` statements (see methods `VisitYieldReturnStatement` and `VisitYieldBreakStatement` to `AsyncIteratorMethodToStateMachineRewriter`),
2. handle `try` statements (see methods `VisitTryStatement` and `VisitExtractedFinallyBlock` in `AsyncIteratorMethodToStateMachineRewriter`)
3. produce additional state and logic for the promise itself (see `AsyncIteratorRewriter`, which produces various other members: `MoveNextAsync`, `Current`, `DisposeAsync`,
and some members supporting the resettable `ValueTask` behavior, namely `GetResult`, `SetStatus`, `OnCompleted`).

```csharp
ValueTask<bool> MoveNextAsync()
{
    if (state == StateMachineStates.FinishedStateMachine)
    {
        return default(ValueTask<bool>);
    }
    valueOrEndPromise.Reset();
    var inst = this;
    builder.Start(ref inst);
    var version = valueOrEndPromise.Version;
    if (valueOrEndPromise.GetStatus(version) == ValueTaskSourceStatus.Succeeded)
    {
        return new ValueTask<bool>(valueOrEndPromise.GetResult(version));
    }
    return new ValueTask<bool>(this, version); // note this leverages the state machine's implementation of IValueTaskSource<bool>
}
```

```csharp
T Current => current;
```

The kick-off method and the initialization of the state machine for an async-iterator method follows those for regular iterator methods.
In particular, the synthesized `GetAsyncEnumerator()` method is like `GetEnumerator()` except that it sets the initial state to to StateMachineStates.NotStartedStateMachine (-1):
```csharp
IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken token)
{
    {StateMachineType} result;
    if (initialThreadId == /*managedThreadId*/ && state == StateMachineStates.FinishedStateMachine)
    {
        state = InitialState; // -3
        disposeMode = false;
        result = this;
    }
    else
    {
        result = new {StateMachineType}(InitialState);
    }
    /* copy each parameter proxy, or in the case of the parameter marked with [EnumeratorCancellation] combine it with `GetAsyncEnumerator`'s `token` parameter */
}
```

For the parameter with `[EnumeratorCancellation]`, `GetAsyncEnumerator` initializes it by combining the two available tokens:
```csharp
if (this.parameterProxy.Equals(default))
{
    result.parameter = token;
}
else if (token.Equals(this.parameterProxy) || token.Equals(default))
{
    result.parameter = this.parameterProxy;
}
else
{
    result.combinedTokens = CancellationTokenSource.CreateLinkedTokenSource(this.parameterProxy, token);
    result.parameter = combinedTokens.Token;
}
```
For a discussion of the threadID check, see https://github.com/dotnet/corefx/issues/3481

Similarly, the kick-off method is much like those of regular iterator methods:
```csharp
{
    {StateMachineType} result = new {StateMachineType}(StateMachineStates.FinishedStateMachine); // -2
    /* save parameters into parameter proxies */
    return result;
}
```

#### Disposal

Iterator and async-iterator methods need disposal because their execution steps are controlled by the caller, which could choose to dispose the enumerator before getting all of its elements.
For example, `foreach (...) { if (...) break; }`.
In contrast, async methods continue running autonomously until they are done. They are never left suspended in the middle of execution from the caller's perspective, so they don't need to be disposed.

In summary, disposal of an async-iterator works based on four design elements:
- `yield return` (jumps to finally when resuming in dispose mode)
- `yield break` (enters dispose mode and jumps to enclosing finally)
- `finally` (after a `finally` we jump to the next enclosing one)
- `DisposeAsync` (enters dispose mode and resumes execution)

The caller of an async-iterator method should only call `DisposeAsync()` when the method completed or was suspended by a `yield return`.
`DisposeAsync` sets a flag on the state machine ("dispose mode") and (if the method wasn't completed) resumes the execution from the current state.
The state machine can resume execution from a given state (even those located within a `try`).
When the execution is resumed in dispose mode, it jumps straight to the enclosing `finally`.
`finally` blocks may involve pauses and resumes, but only for `await` expressions. As a result of the restrictions imposed on `yield return` (described above), dispose mode never runs into a `yield return`.
Once a `finally` block completes, the execution in dispose mode jumps to the next enclosing `finally`, or the end of the method once we reach the top-level.

Reaching a `yield break` also sets the dispose mode flag and jumps to the enclosing `finally` (or end of the method).
By the time we return control to the caller (completing the promise as `false` by reaching the end of the method) all disposal was completed,
and the state machine is left in finished state. So `DisposeAsync()` has no work left to do.

Looking at disposal from the perspective of a given `finally` block, the code in that block can get executed:
- by normal execution (ie. after the code in the `try` block),
- by raising an exception inside the `try` block (which will execute the necessary `finally` blocks and terminate the method in Finished state),
- by calling `DisposeAsync()` (which resumes execution in dispose mode and jumps to the enclosing finally),
- following a `yield break` (which enters dispose mode and jumps to the enclosing finally),
- in dispose mode, following a nested `finally`.

A `yield return` is lowered as:
```csharp
_current = expression;
_state = <next_state>;
goto <exprReturnTruelabel>; // which does _valueOrEndPromise.SetResult(true); return;

// resuming from state=<next_state> will dispatch execution to this label
<next_state_label>: ;
this.state = cachedState = NotStartedStateMachine;
if (disposeMode) /* jump to enclosing finally or exit */
```

A `yield break` is lowered as:
```csharp
disposeMode = true;
/* jump to enclosing finally or exit */
```

```csharp
ValueTask IAsyncDisposable.DisposeAsync()
{
    if (state >= StateMachineStates.NotStartedStateMachine /* -1 */)
    {
        throw new NotSupportedException();
    }
    if (state == StateMachineStates.FinishedStateMachine /* -2 */)
    {
        return default;
    }
    disposeMode = true;
    _valueOrEndPromise.Reset();
    var inst = this;
    _builder.Start(ref inst);
    return new ValueTask(this, _valueOrEndPromise.Version);  // note this leverages the state machine's implementation of IValueTaskSource
}
```

##### Regular versus extracted finally

When the `finally` clause contains no `await` expressions, a `try/finally` is lowered as:
```csharp
try
{
    ...
    finallyEntryLabel:
}
finally
{
    ...
}
if (disposeMode) /* jump to enclosing finally or exit */
```

When a `finally` contains `await` expressions, it is extracted before async rewriting (by AsyncExceptionHandlerRewriter). In those cases, we get:
```csharp
try
{
    ...
    goto finallyEntryLabel;
}
catch (Exception e)
{
    ... save exception ...
}
finallyEntryLabel:
{
    ... original code from finally and additional handling for exception ...
}
```

In both cases, we will add a `if (disposeMode) /* jump to enclosing finally or exit */` after the block for `finally` logic.

#### State values and transitions

The enumerable starts with state -2.
Calling GetAsyncEnumerator sets the state to -3, or returns a fresh enumerator (also with state -3).

From there, MoveNext will either:
- reach the end of the method (-2, we're done and disposed)
- reach a `yield break` (state unchanged, dispose mode = true)
- reach a `yield return` (-N, decreasing from -4)
- reach an `await` (N, increasing from 0)

From suspended state N or -N, MoveNext will resume execution (-1).
But if the suspension was a `yield return` (-N), you could also call DisposeAsync, which resumes execution (-1) in dispose mode.

When in dispose mode, MoveNext continues to suspend (N) and resume (-1) until the end of the method is reached (-2).

The result of invoking `DisposeAsync` from states -1 or N is unspecified. This compiler generates `throw new NotSupportException()` for those cases.

```
        DisposeAsync                              await
 +------------------------+             +------------------------> N
 |                        |             |                          |
 v   GetAsyncEnumerator   |             |        resuming          |
-2 --------------------> -3 --------> -1 <-------------------------+    Dispose mode = false
 ^                                   |  |                          |
 |         done and disposed         |  |      yield return        |
 +-----------------------------------+  +-----------------------> -N
 |        or exception thrown        |                             |
 |                                   |                             |
 |                             yield |                             |
 |                             break |           DisposeAsync      |
 |                                   |  +--------------------------+
 |                                   |  |
 |                                   |  |
 |         done and disposed         v  v    suspension (await)
 +----------------------------------- -1 ------------------------> N
                                        ^                          |    Dispose mode = true
                                        |         resuming         |
                                        +--------------------------+
```

