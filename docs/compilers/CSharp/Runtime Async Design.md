# Runtime Async Design

See also the ECMA-335 specification change for this feature: https://github.com/dotnet/runtime/pull/104063, https://github.com/dotnet/runtime/blob/main/docs/design/specs/runtime-async.md (when the PR completes). https://github.com/dotnet/runtime/issues/109632 tracks open issues for the feature in the runtime.

This document goes over the general design of how Roslyn works with the Runtime Async feature to produce IL. In general, we try to avoid exposing this feature at the user level; initial binding is almost entirely
unaffected by runtime async. Exposed symbols do not give direct information about whether they were compiled with runtime async, and indeed the compiler has no idea whether a method from a referenced assembly is
compiled with runtime async or not.

## Supporting runtime apis

We use the following runtime flag to drive whether feature can be used. This flag must be defined in the same assembly that defines `object`, and the assembly cannot reference any other assemblies. In terms of
CoreFX, this means it must be defined in the `System.Runtime` reference assembly.

TODO: Determine whether just the presence of this flag will cause the compiler to generate in runtime async mode.

```cs
namespace System.Runtime.CompilerServices;

public static class RuntimeFeature
{
    public const string Async = nameof(Async);
}
```

We use the following helper APIs to indicate suspension points to the runtime, in addition to the runtime async call syntax:

```cs
namespace System.Runtime.CompilerServices;

// These methods are used to await things that cannot use runtime async signature form
// TODO: Clarify which of these should be preferred? Should we always emit the `Unsafe` version when awaiting something that implements `ICriticalNotifyCompletion`?
namespace System.Runtime.CompilerServices;

public static class RuntimeHelpers
{
    [RuntimeAsyncMethod]
    public static Task AwaitAwaiterFromRuntimeAsync<TAwaiter>(TAwaiter awaiter) where TAwaiter : INotifyCompletion;
    [RuntimeAsyncMethod]
    public static Task UnsafeAwaitAwaiterFromRuntimeAsync<TAwaiter>(TAwaiter awaiter) where TAwaiter : ICriticalNotifyCompletion;
}
```

Additionally, we use the following helper attributes to indicate information to the runtime. If these attributes are not present in the reference assemblies, we will generate them; the runtime matches by full
name, not by type identity, so we do not need to care about using the "canonical" versions.

```cs
namespace System.Runtime.CompilerServices;

// Used to tell the runtime to generate the async state machinery for this method
[AttributeUsage(AttributeTargets.Method)]
public class RuntimeAsyncMethodAttribute() : Attribute();

// Used to mark locals that should be hoisted to the generated async closure. Note that the runtime does not guarantee that all locals marked with this attribute will be hoisted; if it can prove that it
// doesn't need to hoist a variable, it may avoid doing so.
public class HoistedLocal();
```

For experimentation purposes, we recognize an attribute that can be used to force the compiler to generate the runtime async code, or to force the compiler to generate a full state machine. This attribute is not
defined in the BCL, and exists as an escape hatch for experimentation. It may be removed when the feature ships in stable.

```cs
namespace System.Runtime.CompilerServices;

[AttributeUsage(AttributeTargets.Method)]
public class RuntimeAsyncMethodGenerationAttribute(bool runtimeAsync) : Attribute();
```

## Transformation strategy

As mentioned previously, we try to expose as little of this to initial binding as possible. The one major exception to this is our handling of the `RuntimeAsyncMethodAttribute`; we do not let this be applied to
user code, and will issue an error if a user tries to do this by hand.

Compiler generated async state machines and runtime generated async share some of the same building blocks. Both need to have `await`s with in `catch` and `finally` blocks rewritten to pend the exceptions,
perform the `await` outside of the `catch`/`finally` region, and then have the exceptions restored as necessary.

TODO: Go over `IAsyncEnumerable` and confirm that the initial rewrite to a `Task`-based method produces code that can then be implemented with runtime async, rather than a full compiler state machine.

TODO: Clarify with the debugger team where NOPs need to be inserted for debugging/ENC scenarios.
    We will likely need to insert AwaitYieldPoint and AwaitResumePoints for the scenarios where we emit calls to `RuntimeHelpers` async helpers, but can we avoid them for calls in runtime async form?

### Example transformations

Below are some examples of what IL is generated for specific examples.

TODO: Include debug versions

#### Await `Task`-returning method

```cs
class C
{
    static Task M();
}

await C.M();
```

```il
call modreq(class [System.Runtime]System.Threading.Tasks.Task) void C::M()
```

---------------------------

```cs
class C
{
    Task M();
}

var c = new();
await c.M();
```

```il
newobj instance void C::.ctor()
callvirt instance modreq(class [System.Runtime]System.Threading.Tasks.Task) void C::M()
```

#### Await a concrete `T` `Task<T>`-returning method

```cs
class C
{
    static Task<int> M();
}

await C.M();
```

```il
call modreq(class [System.Runtime]System.Threading.Tasks.Task`1<int32>) int32 C::M()
```

---------------------------

```cs
class C
{
    Task<int> M();
}

var c = new();
await c.M();
```

```il
newobj instance void C::.ctor()
callvirt instance modreq(class [System.Runtime]System.Threading.Tasks.Task`1<int32>) int32 C::M()
```

#### Await local of type `Task`

```cs
class C
{
    static Task M();
}

var local = M();
await local;
```

Translated C#:

```cs
var local = C.M();
{
    var awaiter = local.GetAwaiter();
    if (!awaiter.IsComplete)
    {
        /* Runtime-Async Call */ System.Runtime.CompilerServices.RuntimeHelpers.AwaitAwaiterFromRuntimeAsync<System.Runtime.CompilerServices.TaskAwaiter>(awaiter);
    }
    awaiter.GetResult()
}
```

```il
{
    .locals init (
        [0] valuetype [System.Runtime]System.Runtime.CompilerServices.TaskAwaiter awaiter
    )

    IL_0000: call class [System.Runtime]System.Threading.Tasks.Task C::M()
    IL_0005: callvirt instance valuetype [System.Runtime]System.Runtime.CompilerServices.TaskAwaiter [System.Runtime]System.Threading.Tasks.Task::GetAwaiter()
    IL_000a: stloc.0
    IL_000b: ldloca.s 0
    IL_000d: call instance bool [System.Runtime]System.Runtime.CompilerServices.TaskAwaiter::get_IsCompleted()
    IL_0012: brtrue.s IL_001b

    IL_0014: ldloc.0
    IL_0015: call class [System.Runtime]System.Threading.Tasks.Task System.Runtime.CompilerServices.RuntimeHelpers::AwaitAwaiterFromRuntimeAsync<valuetype [System.Runtime]System.Runtime.CompilerServices.TaskAwaiter>(!!0)
    IL_001a: pop

    IL_001b: ldloca.s 0
    IL_001d: call instance void [System.Runtime]System.Runtime.CompilerServices.TaskAwaiter::GetResult()
    IL_0022: ret
}
```

#### Await local of concrete type `Task<T>`

```cs
class C
{
    static Task<int> M();
}

var local = M();
var i = await local;
```

Translated C#:

```cs
var local = C.M();
var i =
{
    var awaiter = local.GetAwaiter();
    if (!awaiter.IsComplete)
    {
        /* Runtime-Async Call */ System.Runtime.CompilerServices.RuntimeHelpers.AwaitAwaiterFromRuntimeAsync<System.Runtime.CompilerServices.TaskAwaiter>(awaiter);
    }
    awaiter.GetResult()
};
```

```il
{
    .locals init (
        [0] valuetype [System.Runtime]System.Runtime.CompilerServices.TaskAwaiter`1<int32> awaiter
    )

    IL_0000: call class [System.Runtime]System.Threading.Tasks.Task`1<int32> C::M()
    IL_0005: callvirt instance valuetype [System.Runtime]System.Runtime.CompilerServices.TaskAwaiter`1<!0> class [System.Runtime]System.Threading.Tasks.Task`1<int32>::GetAwaiter()
    IL_000a: stloc.0
    IL_000b: ldloca.s 0
    IL_000d: call instance bool valuetype [System.Runtime]System.Runtime.CompilerServices.TaskAwaiter`1<int32>::get_IsCompleted()
    IL_0012: brtrue.s IL_001b

    IL_0014: ldloc.0
    IL_0015: call class [System.Runtime]System.Threading.Tasks.Task System.Runtime.CompilerServices.RuntimeHelpers::AwaitAwaiterFromRuntimeAsync<valuetype [System.Runtime]System.Runtime.CompilerServices.TaskAwaiter`1<int32>>(!!0)
    IL_001a: pop

    IL_001b: ldloca.s 0
    IL_001d: call instance !0 valuetype [System.Runtime]System.Runtime.CompilerServices.TaskAwaiter`1<int32>::GetResult()
    IL_0022: pop
    IL_0023: ret
}
```

#### Await a `T`-returning method

```cs
class C
{
    static T M<T>();
}

await C.M<Task>();
```

```il
TODO: https://github.com/dotnet/runtime/issues/109632
```

#### Await a generic `T` `Task<T>`-returning method

```cs
class C
{
    static Task<T> M<T>();
}

await C.M<int>();
```

```il
TODO: https://github.com/dotnet/runtime/issues/109632
```

#### Await a `Task`-returning delegate

```cs
delegate Task AsyncDelegate();

class C
{
    static Task M();
}

AsyncDelegate d = C.M;
await d;
```

```il
TODO: https://github.com/dotnet/runtime/issues/109632
```

#### Await a `T`-returning delegate

```cs

class C
{
    static Task M();
}

Func<Task> d = C.M;
await d;
```

```il
TODO: https://github.com/dotnet/runtime/issues/109632
```

#### Awaiting in a `catch` block

```cs
class C
{
    static Task M();
}

try
{
    throw new Exception();
}
catch (Exception ex)
{
    await C.M();
    throw;
}
```

Translated C#:

```cs
int pendingCatch = 0;
Exception pendingException;
try
{
    throw new Exception();
}
catch (Exception e)
{
    pendingCatch = 1;
    pendingException = e;
}

if (pendingCatch == 1)
{
    /* Runtime-Async Call */ C.M();
    throw pendingException;
}
```

```il
{
    .locals init (
        [0] int32 pendingCatch,
        [1] class [System.Runtime]System.Exception pendingException
    )

    .try
    {
        IL_0000: newobj instance void [System.Runtime]System.Exception::.ctor()
        IL_0005: throw
    }
    catch [System.Runtime]System.Exception
    {
        IL_0006: stloc.1
        IL_0007: ldc.i4.1
        IL_0008: stloc.0
        IL_0009: leave.s IL_000b
    }

    IL_000b: ldloc.0
    IL_000c: ldc.i4.1
    IL_000d: bne.un.s IL_0017

    IL_000f: ldloc.1
    IL_0010: call modreq(class [System.Runtime]System.Threading.Tasks.Task) void C::M()
    IL_0015: pop
    IL_0016: throw

    IL_0017: ret
}
```

#### Awaiting in a `finally` block

```cs
class C
{
    static Task M();
}

try
{
    throw new Exception();
}
finally
{
    await C.M();
}
```

Translated C#:

```cs
Exception pendingException;
try
{
    throw new Exception();
}
catch (Exception e)
{
    pendingException = e;
}

/* Runtime-Async Call */ C.M();

if (pendingException != null)
{
    throw pendingException;
}
```

```il
{
    .locals init (
        [0] class [System.Runtime]System.Exception pendingException
    )

    .try
    {
        IL_0000: newobj instance void [System.Runtime]System.Exception::.ctor()
        IL_0005: throw
    }
    catch [System.Runtime]System.Exception
    {
        IL_0006: stloc.0
        IL_0007: leave.s IL_0009
    }

    IL_0009: call modreq(class [System.Runtime]System.Threading.Tasks.Task) void C::M()
    IL_000e: pop
    IL_000f: ldloc.0
    IL_0010: brfalse.s IL_0014

    IL_0012: ldloc.0
    IL_0013: throw

    IL_0014: ret
}
```
