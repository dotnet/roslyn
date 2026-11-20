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
