# Runtime Async-Streams Design

## Overview

Async methods that return `IAsyncEnumerable<T>` or `IAsyncEnumerator<T>` are transformed by the compiler into state machines.
States are created for each `await` and `yield`.
Runtime-async support was added in .NET 10 as a preview feature and reduces the overhead of async methods by letting the runtime handling `await` suspensions.
The following design describes how the compiler generates code for async-stream methods when targeting a runtime that supports runtime async.
In short, the compiler generates a state machine similar to async-streams, that implements `IAsyncEnumerable<T>` and `IAsyncEnumerator<T>`.
The states corresponding to `yield` suspensions match those of existing async-streams.
No state is created for `await` expressions, which are lowered to a runtime call instead.

See `docs/features/async-streams.md` and `Runtime Async Design.md` for more background information.

## Structure

For an async-stream method, the compiler generates the following members:
- kickoff method
- state machine class
  - fields
  - constructor
  - `GetAsyncEnumerator` method
  - `Current` property
  - `DisposeAsync` method
  - `MoveNextAsync` method

Considering a simple async-iterator method:
```csharp
class C
{
    public static async System.Collections.Generic.IAsyncEnumerable<int> M()
    {
        Write("1");
        await System.Threading.Tasks.Task.Yield();
        Write("2");
        yield return 3;
        Write("4");
    }
}
```

The following pseudo-code illustrates the intermediate implementation the compiler generates.  
Note that async methods `MoveNextAsync` and `DisposeAsync` will be further lowered following runtime-async design.
```csharp
class C
{
    public static IAsyncEnumerable<int> M()
    {
        return new M_d__0(-2);
    }

    [CompilerGenerated]
    private sealed class M_d__0 : IAsyncEnumerable<int>, IAsyncEnumerator<int>, IAsyncDisposable
    {
        public int 1__state;
        private int 2__current;
        private bool w__disposeMode;
        private int l__initialThreadId;

        [DebuggerHidden]
        public M_d__0(int state)
        {
            1__state = state;
            l__initialThreadId = Environment.CurrentManagedThreadId;
        }

        [DebuggerHidden]
        IAsyncEnumerator<int> IAsyncEnumerable<int>.GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            M_d__0 result;

            if (1__state == -2 && l__initialThreadId == Environment.CurrentManagedThreadId)
            {
                1__state = -3;
                w__disposeMode = false;
                result = this;
            }
            else
            {
                result = new <M>d__0(-3);
            }

            return result;
        }

        ValueTask<bool> IAsyncEnumerator<int>.MoveNextAsync()
        {
            int temp1 = 1__state;
            try
            {
                switch (temp1)
                {
                    case -4:
                        goto <stateMachine-7>;
                }

                if (w__disposeMode)
                    goto <topLevelDisposeLabel-5>;

                1__state = temp1 = -1;
                Write("1");
                runtime-await Task.Yield(); // `runtime-await` will be lowered to a call to runtime helper method
                Write("2");

                {
                    // suspension for `yield return 3;`
                    2__current = 3;
                    1__state = temp1 = -4;
                    return true;

                    <stateMachine-7>:;
                    1__state = temp1 = -1;

                    if (w__disposeMode)
                        goto <topLevelDisposeLabel-5>;
                }

                Write("4");

                w__disposeMode = true;
                goto <topLevelDisposeLabel-5>;
            }
            catch (Exception)
            {
                1__state = -2;
                2__current = default;
                throw;
            }

            <topLevelDisposeLabel-5>: ;
            1__state = -2;
            2__current = default;
            return false;
        }

        [DebuggerHidden]
        int IAsyncEnumerator<int>.Current
        {
            get => 2__current;
        }

        [DebuggerHidden]
        async ValueTask IAsyncDisposable.DisposeAsync()
        {
            if (<>1__state >= -1)
                throw new NotSupportedException();

            if (<>1__state == -2)
                return;

            w__disposeMode = true;
            runtime-await MoveNextAsync(); // `runtime-await` will be lowered to a runtime call
        }
    }
}
```

## Lowering details

The overall lowering strategy is similar to existing async-streams lowering,
except for simplifications since `await` expressions are left to the runtime to handle.
PROTOTYPE overall lifecycle diagram

### Kickoff method, fields and constructor

The state machine class contains fields for:
- the state (an `int`),
- the current value (of the yield type of the async iterator),
- the dispose mode (a `bool`),
- the initial thread ID (an `int`),
- the combined cancellation token (a `CancellationTokenSource`) when the `[EnumeratorCancellation]` attribute is applied,
- hoisted variables (parameters and locals) as needed.  
- parameter proxies (serve to initialize hoisted parameters when producing an enumerator when the method is declared as enumerable)

The constructor of the state machine class has the signature `.ctor(int state)`.
Its body is:
```
{
    this.state = state;
    this.initialThreadId = {managedThreadId};
    this.instanceId = LocalStoreTracker.GetNewStateMachineInstanceId(); // when local state tracking is enabled
}
```

The kickoff method has the signature of the user's method. It simply creates and returns a new instance of the state machine class, capturing the necessary context.

### GetAsyncEnumerator

The signature of this method is `IAsyncEnumerator<Y> IAsyncEnumerable<Y>.GetAsyncEnumerator(CancellationToken cancellationToken = default)`
where `Y` is the yield type of the async iterator.

The `GetAsyncEnumerator` method either returns the current instance if it can be reused,
or creates a new instance of the state machine class.

Assuming that the unspeakble state machine class is named `Unspeakable`, `GetAsyncEnumerator` is emitted as:
```
{
    Unspeakable result;
    if (__state == FinishedState && __initialThreadId == Environment.CurrentManagedThreadId)
    {
        __state = InitialState;
        result = this;
        __disposeMode = false;
    }
    else
    {
        result = new Unspeakable(InitialState);
    }
    return result;
}
```

### Current property

The signature of the property is `Y IAsyncEnumerator<Y>.Current { get; }`
where `Y` is the yield type of the async iterator.  
The getter simply returns the field holding the current value.

### DisposeAsync
       
The signature of this method is `ValueTask IAsyncDisposable.DisposeAsync()`.
This method is emitted with the `async` runtime modifier, so it need only `return;`.

Its body is:
```
{
    if (__state >= NotStartedStateMachine)
    {
        // running
        throw new NotSupportedException();
    }

    if (__state == FinishedState)
    {
        // already disposed
        return;
    }

    __disposeMode = true;
    runtime-await MoveNextAsync(); // `runtime-await` will be lowered to a call to runtime helper method
    return;
}
```

PROTOTYPE different ways to reach disposal

### MoveNextAsync

The signature of this method is `ValueTask<bool> IAsyncEnumerator<Y>.MoveNextAsync()`
where `Y` is the yield type of the async iterator.
This method is emitted with the `async` runtime modifier, so it need only `return` with a `bool`.

A number of techniques from existing async-streams lowering are reused here (PROTOTYPE provide more details on these):
- replacement of generic type parameters
- dispatching based on state
- extraction of exception handlers
- dispatching out of try/catch
- replacing cancellation token parameter with one from combined tokens when `[EnumeratorCancellation]` is used

PROTOTYPE do we still need spilling for `await` expressions?

#### Lowering of `yield return`

`yield return` is disallowed in finally, in try with catch and in catch.  
`yield return` is lowered as a suspension of the state machine (essentially `__current = ...; return true;` with a way of resuming execution after the return):

```
// a `yield return 42;` in user code becomes:
__state = stateForThisYieldReturn;
__current = 42;
return true; // in an ValueTask<bool>-returning runtime-async method, we need only return a boolean

labelForThisYieldReturn:
__state = RunningState;
if (__disposeMode) /* jump to enclosing finally or exit */
```

#### Lowering of `yield break`

`yield break` is disallowed in finally.  
When a `yield break;` is reached, the relevant `finally` blocks should get executed immediately.

```
// a `yield break;` in user code becomes:
disposeMode = true;
/* jump to enclosing finally or exit */
```

Note that in this case, the caller will not get a result from `MoveNextAsync()`
until we've reached the end of the method (**finished** state) and so `DisposeAsync()` will have no work left to do.

#### Lowering of `await`

`await` is disallowed in lock bodies, and in catch filters.
`await` expressions are lowered to runtime calls (instead of being transformed into state machine logic for regular async-streams),
following the runtime-async design.

#### Overall method structure

A catch-all `try` wraps the entire body of the method:

```csharp
cachedState = __state;
cachedThis = __capturedThis; // if needed

try
{
    ... dispatch based on cachedState ...

    initialStateResumeLabel:
    if (__disposeMode) { goto topLevelDisposeLabel; }

    __state = RunningState;

    ... method body with lowered `await`, `yield return` and `yield break` ...

    __disposeMode = true;
    goto topLevelDisposeLabel;
}
catch (Exception)
{
    __state = FinishedState;
    ... clear locals ...
    if (__combinedTokens != null) { __combinedTokens.Dispose(); __combinedTokens = null; }
    __current = default;
    throw;
}

topLevelDisposeLabel:
__state = FinishedState;
... clear locals ...
if (__combinedTokens != null) { __combinedTokens.Dispose(); __combinedTokens = null; }
__current = default;
return false;
```

## Open issues

Question: AsyncIteratorStateMachineAttribute, or IteratorStateMachineAttribute, or other attribute on kickoff method?
