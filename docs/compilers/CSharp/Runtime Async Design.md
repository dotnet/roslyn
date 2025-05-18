# Runtime Async Design

See also the ECMA-335 specification change for this feature: https://github.com/dotnet/runtime/blob/main/docs/design/specs/runtime-async.md. https://github.com/dotnet/runtime/issues/109632 tracks open issues for the feature in the runtime.

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

// TODO: Clarify which of these should be preferred? Should we always emit the `Unsafe` version when awaiting something that implements `ICriticalNotifyCompletion`?
namespace System.Runtime.CompilerServices;

public static class RuntimeHelpers
{
    // These methods are used to await things that cannot use the Await helpers below
    [MethodImpl(MethodImplOptions.Async)]
    public static void AwaitAwaiterFromRuntimeAsync<TAwaiter>(TAwaiter awaiter) where TAwaiter : INotifyCompletion;
    [MethodImpl(MethodImplOptions.Async)]
    public static void UnsafeAwaitAwaiterFromRuntimeAsync<TAwaiter>(TAwaiter awaiter) where TAwaiter : ICriticalNotifyCompletion;

    // These methods are used to directly await method calls
    [MethodImpl(MethodImplOptions.Async)]
    public static void Await(Task task);
    [MethodImpl(MethodImplOptions.Async)]
    public static T Await<T>(Task<T> task);
    [MethodImpl(MethodImplOptions.Async)]
    public static void Await(ValueTask task);
    [MethodImpl(MethodImplOptions.Async)]
    public static T Await<T>(ValueTask<T> task);
}
```

We presume the following `MethodImplOptions` bit is present. This is used to indicate to the JIT that it should generate an async state machine for the method. This bit is not allowed to be used manually on any method; it is added by the compiler
to an `async` method.

TODO: We may want to block directly calling `MethodImplOptions.Async` methods with non-`Task`/`ValueTask` return types.

```cs
namespace System.Runtime.CompilerServices;

public enum MethodImplOptions
{
    Async = 1024
}
```

Additionally, we use the following helper type to indicate information to the runtime. If this type is not present in the reference assemblies, we will generate it; the runtime matches by full
name, not by type identity, so we do not need to care about using the "canonical" version.

```cs
namespace System.Runtime.CompilerServices;

// Used to mark locals that should be hoisted to the generated async closure. Note that the runtime does not guarantee that all locals marked with this modreq will be hoisted; if it can prove that it
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

As mentioned previously, we try to expose as little of this to initial binding as possible. The one major exception to this is our handling of the `MethodImplOption.Async`; we do not let this be applied to
user code, and will issue an error if a user tries to do this by hand.

TODO: We may need special handling for the implementation of the `RuntimeHelpers.Await` methods in corelib to permit usage of `MethodImplOptions.Async` directly, as they will not be `async` as we think of it in C#.

Compiler generated async state machines and runtime generated async share some of the same building blocks. Both need to have `await`s with in `catch` and `finally` blocks rewritten to pend the exceptions,
perform the `await` outside of the `catch`/`finally` region, and then have the exceptions restored as necessary.

TODO: Go over `IAsyncEnumerable` and confirm that the initial rewrite to a `Task`-based method produces code that can then be implemented with runtime async, rather than a full compiler state machine.

TODO: Clarify with the debugger team where NOPs need to be inserted for debugging/ENC scenarios.
    We will likely need to insert AwaitYieldPoint and AwaitResumePoints for the scenarios where we emit calls to `RuntimeHelpers` async helpers, but can we avoid them for calls in runtime async form?

TODO: Do we need to implement clearing of locals marked with `Hoisted`, or will the runtime handle that?

### Example transformations

Below are some examples of what IL is generated for specific examples.

TODO: Include debug versions

#### General signature transformation

In general, an async method declared in C# will be transformed as follows:

```cs
async Task M()
{
    // ...
}
```

```cs
[MethodImpl(MethodImplOptions.Async), Experimental]
Task M()
{
  // ... see lowering strategy for each kind of await below ...
}
```

The same holds for methods that return `Task<T>`, `ValueTask`, and `ValueTask<T>`. Any method returning a different `Task`-like type is not transformed to runtime async form and uses a C#-generated state machine.

`await`s within the body will either be transformed to Runtime-Async call format (as detailed in the runtime specification), or we will use one of the `RuntimeHelpers` methods to do the `await`. Specifics
for given scenarios are elaborated in more detail below.

`Experimental` will be removed when the full feature is ready to ship, likely not before .NET 11.

TODO: Async iterators (returning `IAsyncEnumerable<T>`)

#### `Task`, `Task<T>`, `ValueTask`, `ValueTask<T>` Scenarios

For any lvalue of one of these types, we'll generally rewrite `await expr` into `System.Runtime.CompilerServices.RuntimeHelpers.Await(expr)`. A number of different example scenarios for this are covered below. The
main interesting deviations are when `struct` rvalues need to be hoisted across an `await`, and exception handling rewriting.

##### Await `Task`-returning method

```cs
class C
{
    static Task M();
}

await C.M();
```

Translated C#:

```cs
System.Runtime.CompilerServices.RuntimeHelpers.Await(C.M());
```

```il
call [System.Runtime]System.Threading.Tasks.Task C::M()
call void [System.Runtime]System.Runtime.CompilerServices.RuntimeHelpers::Await(class [System.Runtime]System.Threading.Tasks.Task)
```

---------------------------

```cs
var c = new C();
await c.M();

class C
{
    Task M();
}
```

Translated C#:

```cs
var c = new C();
System.Runtime.CompilerServices.RuntimeHelpers.Await(c.M());
```

```il
newobj instance void C::.ctor()
callvirt instance class [System.Runtime]System.Threading.Tasks.Task C::M()
call void [System.Runtime]System.Runtime.CompilerServices.RuntimeHelpers::Await(class [System.Runtime]System.Threading.Tasks.Task)
```

<details>
<summary>Extended examples of further variations on the simple `await expr` scenario</summary>

##### Await a concrete `T` `Task<T>`-returning method

```cs
int i = await C.M();

class C
{
    static Task<int> M();
}
```

Translated C#:

```cs
int i = System.Runtime.CompilerServices.RuntimeHelpers.Await<int>(C.M());
```

```il
call class [System.Runtime]System.Threading.Tasks.Task`1<int32> C::M()
call int32 [System.Runtime]System.Runtime.CompilerServices.RuntimeHelpers::Await<int32>(class [System.Runtime]System.Threading.Tasks.Task`1<int32>)
stloc.0
```

---------------------------

```cs
var c = new C();
int i = await c.M();

class C
{
    Task<int> M();
}
```

Translated C#:

```cs
var c = new C();
int i = System.Runtime.CompilerServices.RuntimeHelpers.Await<int>(c.M());
```

```il
newobj instance void C::.ctor()
callvirt instance class [System.Runtime]System.Threading.Tasks.Task`1<int32> C::M()
call int32 [System.Runtime]System.Runtime.CompilerServices.RuntimeHelpers::Await<int32>(class [System.Runtime]System.Threading.Tasks.Task`1<int32>)
stloc.0
```

##### Await local of type `Task`

```cs
var local = M();
await local;

class C
{
    static Task M();
}
```

Translated C#:

```cs
var local = C.M();
System.Runtime.CompilerServices.RuntimeHelpers.Await(local);
```

```il
{
    .locals init (
        [0] valuetype [System.Runtime]System.Runtime.CompilerServices.TaskAwaiter awaiter
    )

    IL_0000: call class [System.Runtime]System.Threading.Tasks.Task C::M()
    IL_0005: stloc.0
    IL_0006: ldloc.0
    IL_0007: call void [System.Runtime]System.Runtime.CompilerServices.RuntimeHelpers::Await(class [System.Runtime]System.Threading.Tasks.Task)
    IL_000c: ret
}
```

##### Await local of concrete type `Task<T>`

```cs
var local = M();
var i = await local;

class C
{
    static Task<int> M();
}
```

Translated C#:

```cs
var local = C.M();
var i = System.Runtime.CompilerServices.RuntimeHelpers.Await<int>(local);
```

```il
{
    .locals init (
        [0] class [System.Runtime]System.Threading.Tasks.Task`1<int32> local,
        [1] int32 i
    )

    IL_0000: call class [System.Runtime]System.Threading.Tasks.Task`1<int32> C::M()
    IL_0005: stloc.0
    IL_0006: ldloc.0
    IL_0007: call !!0 [System.Runtime]System.Runtime.CompilerServices.RuntimeHelpers::Await<int32>(class [System.Runtime]System.Threading.Tasks.Task`1<!!0>)
    IL_000c: stloc.1
    IL_000d: ret
}
```

##### Await a `T`-returning method

```cs
await C.M<Task>();

class C
{
    static T M<T>();
}
```

Translated C#:

```cs
System.Runtime.CompilerServices.RuntimeHelpers.Await(C.M<Task>());
```

```il
{
    IL_0000: call !!0 C::M<class [System.Runtime]System.Threading.Tasks.Task>()
    IL_0005: call void [System.Runtime]System.Runtime.CompilerServices.RuntimeHelpers::Await(class [System.Runtime]System.Threading.Tasks.Task)
    IL_000a: ret
}
```

##### Await a generic `T` `Task<T>`-returning method

```cs
int i = await C.M<int>();

class C
{
    static Task<T> M<T>();
}
```

Translated C#:

```cs
int i = System.Runtime.CompilerServices.RuntimeHelpers.Await<int>(C.M<int>());
```

```il
{
    IL_0000: call class [System.Runtime]System.Threading.Tasks.Task`1<!!0> C::M<int32>()
    IL_0005: call !!0 [System.Runtime]System.Runtime.CompilerServices.RuntimeHelpers::Await<int32>(class [System.Runtime]System.Threading.Tasks.Task`1<!!0>)
    IL_000a: stloc.0
    IL_000b: ret
}
```

##### Await a `Task`-returning delegate

```cs
AsyncDelegate d = C.M;
await d();

delegate Task AsyncDelegate();

class C
{
    static Task M();
}
```

Translated C#

```cs
AsyncDelegate d = C.M;
System.Runtime.CompilerServices.RuntimeHelpers.Await(d());
```

```il
{
    IL_0000: ldsfld class AsyncDelegate Program/'<>O'::'<0>__M'
    IL_0005: dup
    IL_0006: brtrue.s IL_001b

    IL_0008: pop
    IL_0009: ldnull
    IL_000a: ldftn class [System.Runtime]System.Threading.Tasks.Task C::M()
    IL_0010: newobj instance void AsyncDelegate::.ctor(object, native int)
    IL_0015: dup
    IL_0016: stsfld class AsyncDelegate Program/'<>O'::'<0>__M'

    IL_001b: callvirt instance class [System.Runtime]System.Threading.Tasks.Task AsyncDelegate::Invoke()
    IL_0020: call void [System.Runtime]System.Runtime.CompilerServices.RuntimeHelpers::Await(class [System.Runtime]System.Threading.Tasks.Task)
    IL_0025: ret
}
```

##### Await a `T`-returning delegate where `T` becomes `Task`

```cs
Func<Task> d = C.M;
await d();

class C
{
    static Task M();
}
```

Translated C#:

```cs
Func<Task> d = C.M;
System.Runtime.CompilerServices.RuntimeHelpers.Await(d());
```

```il
{
    IL_0000: ldsfld class [System.Runtime]System.Func`1<class [System.Runtime]System.Threading.Tasks.Task> Program/'<>O'::'<0>__M'
    IL_0005: dup
    IL_0006: brtrue.s IL_001b

    IL_0008: pop
    IL_0009: ldnull
    IL_000a: ldftn class [System.Runtime]System.Threading.Tasks.Task C::M()
    IL_0010: newobj instance void class [System.Runtime]System.Func`1<class [System.Runtime]System.Threading.Tasks.Task>::.ctor(object, native int)
    IL_0015: dup
    IL_0016: stsfld class [System.Runtime]System.Func`1<class [System.Runtime]System.Threading.Tasks.Task> Program/'<>O'::'<0>__M'

    IL_001b: callvirt instance !0 class [System.Runtime]System.Func`1<class [System.Runtime]System.Threading.Tasks.Task>::Invoke()
    IL_0020: call void [System.Runtime]System.Runtime.CompilerServices.RuntimeHelpers::Await(class [System.Runtime]System.Threading.Tasks.Task)
    IL_0025: ret
}
```

</details>

##### Awaiting in a `catch` block

```cs
try
{
    throw new Exception();
}
catch (Exception ex)
{
    await C.M();
    throw;
}

class C
{
    static Task M();
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
    System.Runtime.CompilerServices.RuntimeHelpers.Await(C.M());
    throw pendingException;
}
```

```il
{
    .locals init (
        [0] int32 pendingCatch,
        [1] class [System.Runtime]System.Exception pendingException
    )

    IL_0000: ldc.i4.0
    IL_0001: stloc.0
    .try
    {
        IL_0002: newobj instance void [System.Runtime]System.Exception::.ctor()
        IL_0007: throw
    } // end .try
    catch [System.Runtime]System.Exception
    {
        IL_0008: ldc.i4.1
        IL_0009: stloc.0
        IL_000a: stloc.1
        IL_000b: leave.s IL_000d
    } // end handler

    IL_000d: ldloc.0
    IL_000e: ldc.i4.1
    IL_000f: bne.un.s IL_001d

    IL_0011: call class [System.Runtime]System.Threading.Tasks.Task C::M()
    IL_0016: call void [System.Runtime]System.Runtime.CompilerServices.RuntimeHelpers::Await(class [System.Runtime]System.Threading.Tasks.Task)
    IL_001b: ldloc.1
    IL_001c: throw

    IL_001d: ret
}
```

##### Awaiting in a `finally` block

```cs
try
{
    throw new Exception();
}
finally
{
    await C.M();
}

class C
{
    static Task M();
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

System.Runtime.CompilerServices.RuntimeHelpers.Await(C.M());

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
    } // end .try
    catch [System.Runtime]System.Exception
    {
        IL_0006: stloc.0
        IL_0007: leave.s IL_0009
    } // end handler

    IL_0009: call class [System.Runtime]System.Threading.Tasks.Task C::M()
    IL_000e: call void [System.Runtime]System.Runtime.CompilerServices.RuntimeHelpers::Await(class [System.Runtime]System.Threading.Tasks.Task)
    IL_0013: ldloc.0
    IL_0014: brfalse.s IL_0018

    IL_0016: ldloc.0
    IL_0017: throw

    IL_0018: ret
}
```

##### Preserving compound assignments

```cs
int[] a = new int[] { };
a[C.M2()] += await C.M1();

class C
{
    public static Task<int> M1();
    public static int M2();
}
```

Translated C#:

```cs
int[] a = new int[] { };
int _tmp1 = C.M2();
int _tmp2 = a[_tmp1];
int _tmp3 = System.Runtime.CompilerServices.RuntimeHelpers.Await(C.M1());
a[_tmp1] = _tmp2 + _tmp3;
```

```il
{
    .locals init (
        [0] int32 _tmp1,
        [1] int32 _tmp2,
        [2] int32 _tmp3
    )

    IL_0000: ldc.i4.0
    IL_0001: newarr [System.Runtime]System.Int32
    IL_0006: call int32 C::M2()
    IL_000b: stloc.0
    IL_000c: dup
    IL_000d: ldloc.0
    IL_000e: ldelem.i4
    IL_000f: stloc.1
    IL_0010: call class [System.Runtime]System.Threading.Tasks.Task`1<int32> C::M1()
    IL_0015: call !!0 [System.Runtime]System.Runtime.CompilerServices.RuntimeHelpers::Await<int32>(class [System.Runtime]System.Threading.Tasks.Task`1<!!0>)
    IL_001a: stloc.2
    IL_001b: ldloc.0
    IL_001c: ldloc.1
    IL_001d: ldloc.2
    IL_001e: add
    IL_001f: stelem.i4
    IL_0020: ret
}
```

#### Await a non-Task/ValueTask

For anything that isn't a `Task`, `Task<T>`, `ValueTask`, and `ValueTask<T>`, we instead use `System.Runtime.CompilerServices.RuntimeHelpers.AwaitAwaiterFromRuntimeAsync` or
`System.Runtime.CompilerServices.RuntimeHelpers.UnsafeAwaitAwaiterFromRuntimeAsync`. These are covered below.

##### Implementor of ICriticalNotifyCompletion

`ICriticalNotifyCompletion` lowering is always preferred over `INotifyCompletion` lowering, when we statically know `ICriticalNotifyCompletion` is implemented by the expression.

```cs
var c = new C();
await c;

class C
{
    public class Awaiter : ICriticalNotifyCompletion
    {
        public void OnCompleted(Action continuation) { }
        public void UnsafeOnCompleted(Action continuation) { }
        public bool IsCompleted => true;
        public void GetResult() { }
    }

    public Awaiter GetAwaiter() => new Awaiter();
}
```

Translated C#:

```cs
var c = new C();
_ = {
    var awaiter = c.GetAwaiter();
    if (!awaiter.IsCompleted)
    {
        System.Runtime.CompilerServices.RuntimeHelpers.UnsafeAwaitAwaiterFromRuntimeAsync<C.Awaiter>(awaiter);
    }
    awaiter.GetResult()
};
```

```il
{
    .locals init (
        [0] class C/Awaiter awaiter
    )

    IL_0000: newobj instance void C::.ctor()
    IL_0005: callvirt instance class C/Awaiter C::GetAwaiter()
    IL_000a: stloc.0
    IL_000b: ldloc.0
    IL_000c: callvirt instance bool C/Awaiter::get_IsCompleted()
    IL_0011: brtrue.s IL_0019

    IL_0013: ldloc.0
    IL_0014: call void [System.Runtime]System.Runtime.CompilerServices.RuntimeHelpers::UnsafeAwaitAwaiterFromRuntimeAsync<class C/Awaiter>(!!0)

    IL_0019: ldloc.0
    IL_001a: callvirt instance void C/Awaiter::GetResult()
    IL_001f: ret
}
```

##### Implementor of INotifyCompletion

```cs
var c = new C();
await c;

class C
{
    public class Awaiter : INotifyCompletion
    {
        public void OnCompleted(Action continuation) { }
        public bool IsCompleted => true;
        public void GetResult() { }
    }

    public Awaiter GetAwaiter() => new Awaiter();
}
```

Translated C#:

```cs
var c = new C();
_ = {
    var awaiter = c.GetAwaiter();
    if (!awaiter.IsCompleted)
    {
        System.Runtime.CompilerServices.RuntimeHelpers.AwaitAwaiterFromRuntimeAsync<C.Awaiter>(awaiter);
    }
    awaiter.GetResult()
};
```

```il
{
    .locals init (
        [0] class C/Awaiter awaiter
    )

    IL_0000: newobj instance void C::.ctor()
    IL_0005: callvirt instance class C/Awaiter C::GetAwaiter()
    IL_000a: stloc.0
    IL_000b: ldloc.0
    IL_000c: callvirt instance bool C/Awaiter::get_IsCompleted()
    IL_0011: brtrue.s IL_0019

    IL_0013: ldloc.0
    IL_0014: call void [System.Runtime]System.Runtime.CompilerServices.RuntimeHelpers::AwaitAwaiterFromRuntimeAsync<class C/Awaiter>(!!0)

    IL_0019: ldloc.0
    IL_001a: callvirt instance void C/Awaiter::GetResult()
    IL_001f: ret
} 
```
