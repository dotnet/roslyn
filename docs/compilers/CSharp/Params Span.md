# Stack allocation of `params Span<T>`

## Background
A `params` array parameter allows callers to pass an arbitrary length list of arguments to the method without explicitly creating an array. Instead, the compiler is responsible for allocating and initializing an array for the trailing arguments.

For instance, a call to `Console.WriteLine(fmt, x, y, z, w);` is emitted as:
```csharp
// public static void WriteLine(string format, params object?[]? arg)
Console.WriteLine(fmt, new object[4] { x, y, z, w });
```
However, the implicit array is a heap allocation.

To avoid the array allocation overhead, some commonly used methods such as `Console.WriteLine()` include overloads with fixed numbers of arguments in addition to the `params` array overload.
```csharp
public static void WriteLine(string format, object? arg0);
public static void WriteLine(string format, object? arg0, object? arg1);
public static void WriteLine(string format, object? arg0, object? arg1, object? arg2);
public static void WriteLine(string format, params object?[]? arg);
```

## Proposal
The runtime and C# compiler will support stack allocation of the implicit `params` array.

The runtime will provide a method for allocating an array on the stack as a `Span<T>`.
```csharp
namespace System.Runtime.CompilerServices
{
    public static class RuntimeHelpers
    {
        public static Span<T> StackAlloc<T>(int length);
    }
}
```
The compiler will support parameters declared as `params ReadOnlySpan<T>` and `params Span<T>`.
If a call in _expanded_ form binds to such a method, the compiler will emit the call with a `Span<T>` allocated from the stack.

For instance, the call to `Console.WriteLine(fmt, x, y, z, w);` will be emitted as:
```csharp
Span<object> arg = RuntimeHelpers.StackAlloc<object>(length: 4);
arg[0] = x;
arg[1] = y;
arg[2] = z;
arg[3] = w;
// public static void WriteLine(string format, params ReadOnlySpan<object?> arg)
Console.WriteLine(fmt, (ReadOnlySpan<object>)arg);
```
No changes are needed at the call-site other than recompiling with a version of the library with the additional `params ReadOnlySpan<object>` overload.

## Prototype implementation
Overload resolution continues to prefer overloads that are applicable in _normal_ form rather than _expanded_ form.
But for overloads that are applicable in _expanded_ form, overload resolution prefers `params ReadOnlySpan<T>` or `params Span<T>` over `params T[]`.
```csharp
Console.WriteLine(fmt, x, y);       // WriteLine(string, object?, object?)
Console.WriteLine(fmt, x, y, z, w); // WriteLine(string, params ReadOnlySpan<object?>)
```

To avoid repeated calls within loops, all compiler-generated calls to `StackAlloc<T>(int length)` are moved to the start of the method.
No attempt is made currently to reuse spans across distinct _call-sites_ in the method however.

For a span to be reused across repeated calls, the compiler needs to ensure there are no aliases to the span.
To do that, the compiler reports an error if the method with the `params Span<T>` parameter returns the parameter value to the caller.

## Remaining work
`RuntimeHelpers.StackAlloc<T>(int length)` allocates an array of `T[length]` on the stack unconditionally.
The C# compiler may need heuristics to decide when to allocate the array from the _stack_ rather than the _heap_: perhaps a single _stack_ allocation of no more than `N` bytes; or total _stack_ allocations for a method of no more than `M` bytes.

The compiler should reuse spans aggressively within a method. Ideally a single span should be reused across calls that do not overlap if the types are the same size and the span is large enough, so the compiler should allocate the minimum number of spans to satisfy all calls within the method.

There should be a mechanism for the caller to opt-out of implicit stack allocation of `params` spans.
The caller can opt-out for a particular call by explicitly allocating the `params` argument:
```csharp
Console.WriteLine(fmt, new object[] { x, y, z, w }); // WriteLine(string, params object?[]?)
```
To opt-out for multiple calls, we could support a `[MethodImpl(MethodImplOptions.NoStackAlloc)]` attribute applied to the calling method, or perhaps a similar attribute applied to the containing type or assembly.
