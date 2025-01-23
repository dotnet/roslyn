# Breaking changes in Roslyn after .NET 9.0.100 through .NET 10.0.100

This document lists known breaking changes in Roslyn after .NET 9 general release (.NET SDK version 9.0.100) through .NET 10 general release (.NET SDK version 10.0.100).

## `scoped` in a lambda parameter list is now always a modifier.

***Introduced in Visual Studio 2022 version 17.13***

C# 14 introduces the ability to write a lambda with parameter modifiers, without having to specify a parameter type:
https://github.com/dotnet/csharplang/blob/main/proposals/simple-lambda-parameters-with-modifiers.md

As part of this work, a breaking change was accepted where `scoped` will always be treated as a modifier
in a lambda parameter, even where it might have been accepted as a type name in the past.  For example:

```c#
var v = (scoped scoped s) => { ... };

ref struct @scoped { }
```

In C# 14 this will be an error as both `scoped` tokens are treated as modifiers.  The workaround is to
use `@` in the type name position like so:

```c#
var v = (scoped @scoped s) => { ... };

ref struct @scoped { }
```

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
var s = new ArraySegment<int>(y, 1, 1);
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
    public static void R<T>(ReadOnlySpan<T> s) => Console.Write(3);
}
```

For that reason, `ReadOnlySpan<T>` is generally preferred over `Span<T>` by overload resolution in C# 14.
In some cases, that might lead to compilation breaks,
for example when there are overloads for both `Span<T>` and `ReadOnlySpan<T>`, both taking and returning the same span type:

```cs
double[] x = new double[0];
Span<ulong> y = MemoryMarshal.Cast<double, ulong>(x); // previously worked, now compilation error
Span<ulong> z = MemoryMarshal.Cast<double, ulong>(x.AsSpan()); // workaround

static class MemoryMarshal
{
    public static ReadOnlySpan<TTo> Cast<TFrom, TTo>(ReadOnlySpan<TFrom> span) => default;
    public static Span<TTo> Cast<TFrom, TTo>(Span<TFrom> span) => default;
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

## Diagnostics now reported for pattern-based disposal method in `foreach`

***Introduced in Visual Studio 2022 version 17.13***

For instance, an obsolete `DisposeAsync` method is now reported in `await foreach`.
```csharp
await foreach (var i in new C()) { } // 'C.AsyncEnumerator.DisposeAsync()' is obsolete

class C
{
    public AsyncEnumerator GetAsyncEnumerator(System.Threading.CancellationToken token = default)
    {
        throw null;
    }

    public sealed class AsyncEnumerator : System.IAsyncDisposable
    {
        public int Current { get => throw null; }
        public Task<bool> MoveNextAsync() => throw null;

        [System.Obsolete]
        public ValueTask DisposeAsync() => throw null;
    }
}
```

## Set state of enumerator object to "after" during disposal

***Introduced in Visual Studio 2022 version 17.13***

The state machine for enumerators incorrectly allowed resuming execution after the enumerator was disposed.  
Now, `MoveNext()` on a disposed enumerator properly returns `false` without executing any more user code.

```csharp
var enumerator = C.GetEnumerator();

Console.Write(enumerator.MoveNext()); // prints True
Console.Write(enumerator.Current); // prints 1

enumerator.Dispose();

Console.Write(enumerator.MoveNext()); // now prints False

class C
{
    public static IEnumerator<int> GetEnumerator()
    {
        yield return 1;
        Console.Write("not executed after disposal")
        yield return 2;
    }
}
```

## `UnscopedRefAttribute` cannot be used with old ref safety rules

***Introduced in Visual Studio 2022 version 17.13***

The `UnscopedRefAttribute` unintentionally affected code compiled by new Roslyn compiler versions
even when the code was compiled in the context of the earlier ref safety rules
(i.e., targeting C# 10 or earlier with net6.0 or earlier).
However, the attribute should not have an effect in that context, and that is now fixed.

Code that previously did not report any errors in C# 10 or earlier with net6.0 or earlier can now fail to compile:

```cs
using System.Diagnostics.CodeAnalysis;
struct S
{
    public int F;

    // previously allowed in C# 10 with net6.0
    // now fails with the same error as if the [UnscopedRef] wasn't there:
    // error CS8170: Struct members cannot return 'this' or other instance members by reference
    [UnscopedRef] public ref int Ref() => ref F;
}
```

To prevent misunderstanding (thinking the attribute has an effect
but it actually does not because your code is compiled with the earlier ref safety rules),
a warning is reported when the attribute is used in C# 10 or earlier with net6.0 or earlier:

```cs
using System.Diagnostics.CodeAnalysis;
struct S
{
    // both are errors in C# 10 with net6.0:
    // warning CS9269: UnscopedRefAttribute is only valid in C# 11 or later or when targeting net7.0 or later.
    [UnscopedRef] public ref int Ref() => throw null!;
    public static void M([UnscopedRef] ref int x) { }
}
```

## `Microsoft.CodeAnalysis.EmbeddedAttribute` is validated on declaration

***Introduced in Visual Studio 2022 version 17.13***

The compiler now validates the shape of `Microsoft.CodeAnalysis.EmbeddedAttribute` when declared in source. Previously, the compiler
would allow user-defined declarations of this attribute, but only when it didn't need to generate one itself. We now validate that:

1. It must be internal
2. It must be a class
3. It must be sealed
4. It must be non-static
5. It must have an internal or public parameterless constructor
6. It must inherit from System.Attribute.
7. It must be allowed on any type declaration (class, struct, interface, enum, or delegate)

```cs
namespace Microsoft.CodeAnalysis;

// Previously, sometimes allowed. Now, CS9271
public class EmbeddedAttribute : Attribute {}
```

## Expression `field` in a property accessor refers to synthesized backing field

***Introduced in Visual Studio 2022 version 17.12***

The expression `field`, when used within a property accessor, refers to a synthesized backing field for the property.

The warning CS9258 is reported when the identifier would have bound to a different symbol with language version 13 or earlier.

To avoid generating a synthesized backing field, and to refer to the existing member, use 'this.field' or '@field' instead.
Alternatively, rename the existing member and the reference to that member to avoid a conflict with `field`.

```csharp
class MyClass
{
    private int field = 0;

    public object Property
    {
        get
        {
            // warning CS9258: The 'field' keyword binds to a synthesized backing field for the property.
            // To avoid generating a synthesized backing field, and to refer to the existing member,
            // use 'this.field' or '@field' instead.
            return field;
        }
    }
}
```

## Variable named `field` disallowed in a property accessor

***Introduced in Visual Studio 2022 version 17.14***

The expression `field`, when used within a property accessor, refers to a synthesized backing field for the property.

The error CS9272 is reported when a local, or a parameter of a nested function, with the name `field` is declared in a property accessor.

To avoid the error, rename the variable, or use `@field` in the declaration.

```csharp
class MyClass
{
    public object Property
    {
        get
        {
            // error CS9272: 'field' is a keyword within a property accessor.
            // Rename the variable or use the identifier '@field' instead.
            int field = 0;
            return @field;
        }
    }
}
```

## `record` and `record struct` types cannot define pointer type members, even when providing their own Equals implementations

***Introduced in Visual Studio 2022 version 17.14***

The specification for `record class` and `record struct` types indicated that any pointer types are disallowed as instance fields.
However, this was not enforced correctly when the `record class` or `record struct` type defined its own `Equals` implementation.

The compiler now correctly forbids this.

```cs
unsafe record struct R(
    int* P // Previously fine, now CS8908
)
{
    public bool Equals(R other) => true;
}
```
