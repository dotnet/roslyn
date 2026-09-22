# Breaking changes in Roslyn after .NET 11.0.100 through .NET 12.0.100

This document lists known breaking changes in Roslyn after .NET 11 general release (.NET SDK version 11.0.100) through .NET 12 general release (.NET SDK version 12.0.100).

## `extension` is recognized consistently in contextual type declaration lookahead

***Introduced in Visual Studio 2026 version 18.11***

Parser lookahead now uses the same type-declaration recognition for `record`, `union`, and `extension`.
As a result, `extension` followed by a type parameter list is recognized as the start of an extension
declaration in ambiguous declaration contexts, including in language versions before extension
declarations were introduced.

For example, the following C# 13 program previously parsed `extension<int>` as the return type of `M`.
It is now parsed as the start of an extension declaration and produces syntax errors:

```cs
class extension<T> { }

interface I
{
    ref extension<int> M();
}
```

Escape the type name to preserve the previous interpretation:

```cs
class extension<T> { }

interface I
{
    ref @extension<int> M();
}
```

The same declaration lookahead can affect ambiguous uses of `extension` in newer language versions,
such as `ref extension M()` and `partial extension M()`.
