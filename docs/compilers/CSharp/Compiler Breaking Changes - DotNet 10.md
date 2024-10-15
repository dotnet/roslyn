# This document lists known breaking changes in Roslyn after .NET 9 all the way to .NET 10.

## `Span<T>` and `ReadOnlySpan<T>` overloads are applicable in more scenarios in C# 14 and newer

***Introduced in Visual Studio 2022 version 17.13***

C# 14 introduces new [built-in span conversions and type inference rules](https://github.com/dotnet/csharplang/issues/7905).
This means that different overloads might be chosen compared to C# 13, and sometimes an ambiguity compile-time error
might be raised because a new overload is applicable but there is no single best overload.

The following example shows some ambiguities and possible workarounds.
Note that another workaround is for API authors to use the `OverloadResolutionPriorityAttribute`.

```cs
var x = new long[] { 1 };
Assert.Equal([2], x); // previously Assert.Equal<T>(T[], T[]), now ambiguous with Assert.Equal<T>(ReadOnlySpan<T>, Span<T>)
Assert.Equal([2], x.AsSpan()); // workaround

var y = new int[] { 1, 2 };
var s = new ArraySegment<int>(x, 1, 1);
Assert.Equal(y, s); // previously Assert.Equal<T>(T, T), now ambiguous with Assert.Equal<T>(Span<T>, Span<T>)
Assert.Equal(y.AsSpan(), s); // workaround
```

A `Span<T>` overload might be chosen in C# 14 where an overload taking an interface implemented by `T[]` (such as `IEnumerable<T>`) was chosen in C# 13,
and that can lead to an `ArrayTypeMismatchException` at runtime if used with a covariant array:

```cs
string[] s = new[] { "a" };
object[] o = s; // array variance

C.R(o); // wrote 1 previously, now crashes in Span<T> constructor with ArrayTypeMismatchException
C.R(o.AsEnumerable()); // workaround

static class C
{
    public static void R<T>(IEnumerable<T> e) => Console.Write(1);
    public static void R<T>(Span<T> s) => Console.Write(2);
    // another workaround:
    [OverloadResolutionPriority(1)]
    public static void R<T>(ReadOnlySpan<T> s) => Console.Write(3);
}
```

When using C# 14 or newer and targeting a .NET older than `net10.0`
or .NET Framework with `System.Memory` reference,
there is a breaking change with `Enumerable.Reverse` and arrays:

```cs
int[] x = new[] { 1, 2, 3 };
var y = x.Reverse(); // previously Enumerable.Reverse, now MemoryExtensions.Reverse
```

On `net10.0`, there is `Enumerable.Reverse(this T[])` which takes precedence and hence the break is avoided.
Otherwise, `MemoryExtensions.Reverse(this Span<T>)` is resolved which has different semantics
than `Enumerable.Reverse(this IEnumerable<T>)` (which used to be resolved in C# 13 and lower).
Specifically, the `Span` extension does the reversal in place and returns `void`.
As a workaround, one can define their own `Enumerable.Reverse(this T[])` or use `Enumerable.Reverse` explicitly:

```cs
int[] x = new[] { 1, 2, 3 };
var y = Enumerable.Reverse(x); // instead of 'x.Reverse();'
```
