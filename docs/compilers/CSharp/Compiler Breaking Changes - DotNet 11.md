# Breaking changes in Roslyn after .NET 10.0.100 through .NET 11.0.100

This document lists known breaking changes in Roslyn after .NET 10 general release (.NET SDK version 10.0.100) through .NET 11 general release (.NET SDK version 11.0.100).

## The *safe-context* of a collection expression of Span/ReadOnlySpan type is now *declaration-block*

***Introduced in Visual Studio 2026 version 18.3***

The C# compiler made a breaking change in order to properly adhere to the [ref safety rules](https://github.com/dotnet/csharplang/blob/main/proposals/csharp-12.0/collection-expressions.md#ref-safety) in the *collection expressions* feature specification. Specifically, the following clause:

> * If the target type is a *span type* `System.Span<T>` or `System.ReadOnlySpan<T>`, the safe-context of the collection expression is the *declaration-block*.

Previously, the compiler used safe-context *function-member* in this situation. We have now made a change to use *declaration-block* per the specification. This can cause new errors to appear in existing code, such as in the scenario below:

```cs
scoped Span<int> items1 = default;
scoped Span<int> items2 = default;
foreach (var x in new[] { 1, 2 })
{
    Span<int> items = [x];
    if (x == 1)
        items1 = items; // previously allowed, now an error

    if (x == 2)
        items2 = items; // previously allowed, now an error
}
```

If your code is impacted by this breaking change, consider using an array type for the relevant collection expressions instead:

```cs
scoped Span<int> items1 = default;
scoped Span<int> items2 = default;
foreach (var x in new[] { 1, 2 })
{
    int[] items = [x];
    if (x == 1)
        items1 = items; // ok, using 'int[]' conversion to 'Span<int>'

    if (x == 2)
        items2 = items; // ok
}
```

Alternatively, move the collection-expression to a scope where the assignment is permitted:
```cs
scoped Span<int> items1 = default;
scoped Span<int> items2 = default;
Span<int> items = [0];
foreach (var x in new[] { 1, 2 })
{
    items[0] = x;
    if (x == 1)
        items1 = items; // ok

    if (x == 2)
        items2 = items; // ok
}
```

See also https://github.com/dotnet/csharplang/issues/9750.


## Scenarios requiring compiler to synthesize a `ref readonly` returning delegate now require availability of `System.Runtime.InteropServices.InAttribute` type.

***Introduced in Visual Studio 2026 version 18.3***

The C# compiler made a breaking change in order to properly emit metadata for `ref readonly` returning
[synthesized delegates](https://github.com/dotnet/csharplang/blob/main/proposals/csharp-10.0/lambda-improvements.md#delegate-types)

This can cause an "error CS0518: Predefined type 'System.Runtime.InteropServices.InAttribute' is not defined or imported"
to appear in existing code, such as in the scenarios below:

```cs
var d = this.MethodWithRefReadonlyReturn;
```

```cs
var d = ref readonly int () => ref x;
```

If your code is impacted by this breaking change, consider adding a reference to an assembly defining `System.Runtime.InteropServices.InAttribute` 
to your project.

## Scenarios utilizing `ref readonly` local functions now require availability of `System.Runtime.InteropServices.InAttribute` type.

***Introduced in Visual Studio 2026 version 18.3***

The C# compiler made a breaking change in order to properly emit metadata for `ref readonly` returning local functions.

This can cause an "error CS0518: Predefined type 'System.Runtime.InteropServices.InAttribute' is not defined or imported"
to appear in existing code, such as in the scenario below:

```cs
void Method()
{
    ...
    ref readonly int local() => ref x;
    ...
}
```

If your code is impacted by this breaking change, consider adding a reference to an assembly defining `System.Runtime.InteropServices.InAttribute` 
to your project.

