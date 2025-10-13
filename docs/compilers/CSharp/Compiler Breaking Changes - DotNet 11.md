# Breaking changes in Roslyn after .NET 10.0.100 through .NET 11.0.100

This document lists known breaking changes in Roslyn after .NET 10 general release (.NET SDK version 10.0.100) through .NET 11 general release (.NET SDK version 11.0.100).

## The *safe-context* of a collection expression of Span/ReadOnlySpan type is now *declaration-block*

***Introduced in Visual Studio 2026 version 18.0***

The C# compiler made a breaking change in order to properly adhere to the [ref safety rules](https://github.com/dotnet/csharplang/blob/main/proposals/csharp-12.0/collection-expressions.md#ref-safety) in the *collection expressions* feature specification. Specifically, the following clause:

> * If the target type is a *span type* `System.Span<T>` or `System.ReadOnlySpan<T>` and the collection expression is not a constant expression,
>   the *safe-context* is the *declaration-block* (that is, the collection expression value cannot be returned from the enclosing method or lambda, nor can it escape to an outer statement).

Previously, the compiler incorrectly allowed collection expressions with Span/ReadOnlySpan type to escape to any scope within the current method.
This could lead to correctness issues where the contents of spans are aliased unexpectedly.

For example, the following code previously compiled without errors, but now produces compilation errors:

```csharp
scoped Span<int> items1 = default;
scoped Span<int> items2 = default;
foreach (var x in new[] { 1, 2 })
{
    Span<int> items = [x];
    if (x == 1)
        items1 = items; // error CS8352: Cannot use variable 'items' in this context because it may expose referenced variables outside of their declaration scope

    if (x == 2)
        items2 = items; // error CS8352: Cannot use variable 'items' in this context because it may expose referenced variables outside of their declaration scope
}
```

The issue is that `items` is a collection expression created within the `foreach` loop body (a nested scope), but was being incorrectly allowed to escape to the outer scope and be assigned to `items1` and `items2`.

To fix this error, avoid assigning span collection expressions to variables in outer scopes. Instead, create the collection expression directly in the scope where it's needed, or use arrays which don't have the same restrictions:

```csharp
// Option 1: Create collection expression in the scope where it's needed
scoped Span<int> items1;
scoped Span<int> items2;
foreach (var x in new[] { 1, 2 })
{
    if (x == 1)
        items1 = [x]; // OK - collection expression created at assignment

    if (x == 2)
        items2 = [x]; // OK - collection expression created at assignment
}

// Option 2: Use arrays instead
int[] items1 = default;
int[] items2 = default;
foreach (var x in new[] { 1, 2 })
{
    int[] items = [x];
    if (x == 1)
        items1 = items; // OK - arrays can escape

    if (x == 2)
        items2 = items; // OK - arrays can escape
}
```
