# Breaking changes in Roslyn after .NET 11.0.100 through .NET 12.0.100

This document lists known breaking changes in Roslyn after .NET 11 general release (.NET SDK version 11.0.100) through .NET 12 general release (.NET SDK version 12.0.100).

## Type parameter inference considers generic constraints

***Introduced in .NET 12***

In C# 16, method type inference can infer type parameters that occur only in the constraints of other inferred type parameters. As a result, a generic method that was previously excluded from overload resolution may become applicable. This can change the selected overload, introduce an ambiguity, or produce a more specific diagnostic from the newly applicable candidate.

For example, the following call previously selected the non-generic overload because `U` could not be inferred. It now selects the generic overload because `T` is fixed to `string` and `U` is then inferred as `char` from the `IEnumerable<U>` constraint.

```cs
using System;
using System.Collections.Generic;

static void M(object value) => Console.WriteLine("non-generic");
static void M<T, U>(T value) where T : IEnumerable<U> => Console.WriteLine("generic");

M("test"); // C# 15: "non-generic"; C# 16: "generic"
```

To preserve the previous overload selection, explicitly convert the argument to the intended parameter type:

```cs
M((object)"test"); // "non-generic"
```

See [the type parameter inference from constraints proposal](https://github.com/dotnet/csharplang/issues/9453) and [the Roslyn implementation](https://github.com/dotnet/roslyn/pull/84655).
