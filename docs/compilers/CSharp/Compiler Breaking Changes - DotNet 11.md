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

## Dynamic evaluation of `&&`/`||` operators is not allowed with the left operand statically typed as an interface.

***Introduced in Visual Studio 2026 version 18.3***

The C# compiler now reports an error when an interface type is used as the left operand of
a logical `&&` or `||` operator with a `dynamic` right operand.
Previously, code would compile for an interface type with `true`/`false` operators,
but fail at runtime with a `RuntimeBinderException` because the runtime binder cannot
invoke operators defined on interfaces.

This change prevents a runtime error by reporting it at compile time instead. The error message is:

> error CS7083: Expression must be implicitly convertible to Boolean or its type 'I1' must not be an interface and must define operator 'false'.

```cs
interface I1
{
    static bool operator true(I1 x) => false;
    static bool operator false(I1 x) => false;
}

class C1 : I1
{
    public static C1 operator &(C1 x, C1 y) => x;
    public static bool operator true(C1 x) => false;
    public static bool operator false(C1 x) => false;
}

void M()
{
    I1 x = new C1();
    dynamic y = new C1();
    _ = x && y; // error CS7083: Expression must be implicitly convertible to Boolean or its type 'I1' must not be an interface and must define operator 'false'.
}
```

If your code is impacted by this breaking change, consider changing the static type of the left operand from an interface type to a concrete class type,
or to `dynamic` type:

```cs
void M()
{
    I1 x = new C1();
    dynamic y = new C1();
    _ = (C1)x && y; // Valid - uses operators defined on C1
    _ = (dynamic)x && y; // Valid - uses operators defined on C1
}
```

See also https://github.com/dotnet/roslyn/issues/80954.

## Parsing of 'with' within a switch-expression-arm

***Introduced in Visual Studio 2026 version 18.4***

See https://github.com/dotnet/roslyn/issues/81837 and https://github.com/dotnet/roslyn/pull/81863

Previously, when seeing the following, the compiler would treat `(X.Y)when` as a cast-expression.  Specifically,
casting the contextual identifier `when` to `(X.Y)`:  

```c#
x switch
{
    (X.Y) when
}
```

This was undesirable, and meant a simple `when` check of the pattern (like `(X.Y) when a > b =>`) would not
parse properly.  Now, this is treated as a constant pattern `(X.Y)` followed by a `when clause`.

## `with()` as a collection expression element is treated as collection construction *arguments*

***Introduced in Visual Studio 2026 version 18.4***

`with(...)` when used as an element in a collection expression, and when the LangVersion is set to 'Preview', is bound as arguments passed to constructor or
factory method used to create the collection, rather than as an invocation expression of a method named `with`.

To bind to a method named `with`, use `@with` instead.

```cs
object x, y, z = ...;
object[] items;

items = [with(x, y), z];  // C#14: call to with() method; 'Preview': error args not supported for object[]
items = [@with(x, y), z]; // call to with() method
object with(object a, object b) { ... }
```
