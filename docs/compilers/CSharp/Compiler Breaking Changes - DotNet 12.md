# Breaking changes in Roslyn after .NET 11.0.100 through .NET 12.0.100

This document lists known breaking changes in Roslyn after .NET 11 general release (.NET SDK version 11.0.100) through .NET 12 general release (.NET SDK version 12.0.100).

## `safe` requires `/unsafe`

***Introduced in Visual Studio 2026 version 18.13***

Using the `safe` modifier requires `/unsafe` (`AllowUnsafeBlocks` in MSBuild), just like `unsafe`.

## Embedded statements introduce a nested ref-safety context

***Introduced in Visual Studio 2026 version 18.14***

Ref-safety analysis treats an embedded statement without braces as a nested context, just as it treats an explicit block.
This applies to the bodies of `if`/`else`, `while`, `do`, `for`, `foreach`, `using`, `lock`, and `fixed` statements,
including an `if` statement embedded in an `else` clause.

Previously, omitting braces could allow a reference to a temporary defensive copy to escape its context.
Such code now reports the same diagnostic with or without braces. For example:

```cs
using System.Diagnostics.CodeAnalysis;

struct S
{
    [UnscopedRef] public ref S Self => ref this;
}

class C
{
    static void M(in S s, bool condition)
    {
        ref S r = ref s.Self;
        if (condition)
            r = ref s.Self; // CS8374: 's.Self' has a narrower escape scope than 'r'.
    }
}
```

The non-readonly property access on the readonly receiver `s` creates a defensive copy.
The second access creates a copy in the embedded statement's nested context, which cannot escape to `r`.

If affected, evaluate the member access in the enclosing context and reuse the resulting reference:

```cs
ref S r = ref s.Self;
ref S replacement = ref s.Self;
if (condition)
    r = ref replacement;
```

## Type parameter inference uses generic constraints

***Introduced in Visual Studio 2026 version 18.14***

In C# 16, method type inference can infer type parameters that occur only in the constraints of other inferred type parameters.
As a result, a generic method that was previously excluded from overload resolution may become applicable.
This can change the selected overload, introduce an ambiguity, or produce a more specific diagnostic from the newly applicable candidate.
Constraint inference can also change type arguments that were already inferable from method arguments.

For example, the following call previously selected the non-generic overload because `U` could not be inferred.
It now selects the generic overload because `T` is fixed to `string` and `U` is then inferred as `char` from the `IEnumerable<U>` constraint.

```cs
using System;
using System.Collections.Generic;

class Program
{
    static void M(object value) => Console.WriteLine("non-generic");
    static void M<T, U>(T value) where T : IEnumerable<U> => Console.WriteLine("generic");

    static void Main()
    {
        M("test"); // C# 15: "non-generic"; C# 16: "generic"
    }
}
```

To preserve the previous overload selection, explicitly convert the argument to the intended parameter type:

```cs
M((object)"test"); // "non-generic"
```

Even when there is only one method and its type arguments were already inferable, constraint inference can change the inferred types.
This can change behavior or cause a previously valid call to fail a constraint check.
For example:

```cs
using System;

static Type M<T, U>(T t, U u) where T : I<U> where U : B => typeof(U);

Console.WriteLine(M(new Holder(), new B()).Name); // C# 15: "B"; C# 16: CS0311

interface I<in T> { }
class A { }
class B : A { }
class Holder : I<A> { }
```

Previously, `U` was inferred as `B` from the second argument.
It is now inferred as `A` after inference through the contravariant `I<U>` constraint, violating the `where U : B` constraint.
To preserve the previous type arguments, specify them explicitly:

```cs
Console.WriteLine(M<Holder, B>(new Holder(), new B()).Name); // "B"
```

The inferred types can also change without producing a diagnostic, including when a type argument is inferred from a lambda's return type:

```cs
using System;

static Type M<T, U, V>(T t, V v, Func<V, U> f) where T : I<U> => typeof(U);

Console.WriteLine(M(new Holder(), 0, x => new B()).Name); // C# 15: "B"; C# 16: "A"

interface I<in T> { }
class A { }
class B : A { }
class Holder : I<A> { }
```

Previously, `U` was inferred as `B` from the lambda's return type.
Constraint inference now contributes `A` through the `where T : I<U>` constraint, and `U` is inferred as `A`.
The lambda can still return a `B`, so the call remains valid, but the method executes with a different type argument.
To preserve the previous behavior, specify the type arguments explicitly:

```cs
Console.WriteLine(M<Holder, B, int>(new Holder(), 0, x => new B()).Name); // "B"
```

See [the type parameter inference from constraints proposal](https://github.com/dotnet/csharplang/issues/9453).
