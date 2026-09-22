# Breaking changes in Roslyn after .NET 11.0.100 through .NET 12.0.100

This document lists known breaking changes in Roslyn after .NET 11 general release (.NET SDK version 11.0.100) through .NET 12 general release (.NET SDK version 12.0.100).

## `extension` declaration recognition is used consistently in modifier lookahead

***Introduced in Visual Studio 2026 version 18.11***

Parser modifier lookahead now uses the same type-declaration recognition for `record`, `union`, and
`extension`. Roslyn has recognized `extension` followed by a type parameter list as an extension
declaration in every language version since
[dotnet/roslyn#80206](https://github.com/dotnet/roslyn/pull/80206). This change applies that existing
recognition when looking past modifiers such as `ref` and `partial`.

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
