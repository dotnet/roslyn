---
title: Razor compiler breaking changes since .NET 8
description: Learn about any breaking changes since the initial release of .NET 8.
author: wadepickett
ms.author: wpickett
ms.date: 11/12/2024
---
# Breaking changes in Razor after .NET 8.0.100 through .NET 9.0.100

This document lists known breaking changes in Razor after .NET 8 general release (.NET SDK version 8.0.100) through .NET 9 general release (.NET SDK version 9.0.100).

## Parsing of `@` identifiers was unified

***Introduced in VS 17.10 and .NET 8.0.300***

In https://github.com/dotnet/razor/pull/10232, we adjusted the behavior of how an identifier is parsed following an `@` to be more consistent across Razor.
This resulted in a few scenarios that have different behavior, listed below.

### Verbatim interpolated strings

Strings of the form `@$"ticket-{i}.png"` are no longer recognized. This will be fixed in a later release by changing to a new lexer; until then, use `$@` to work around the issue.

### C# preprocessor directives followed by HTML are not parsed correctly

1. The preprocessor directive is directly before HTML. This flavor looks something like this:
```razor
@{
    #region R
    <h3>@ViewData["Title"]</h3>
    #endregion
}
```
2. There is valid C# between the preprocessor directive and the html, but it doesn't have a character that tells the parser to end parsing before the HTML. This is a variation of 1, and can occur with things like `switch` statements:
```razor
@{
    switch (true)
    {
        #region R
        case true:
            <div>@(1 + 1)</div>
            break;
    }
}
```

Previously, C# preprocessor directives followed by HTML would sometimes be parsed correctly if the HTML had an `@` transition in it. It is now consistently parsed
incorrectly. This will be resolved in a later release by changing to a new lexer. Until then, there are available workarounds to get this to compile.

#### Surround the HTML in a block

The HTML can be surrounded with braces.

```razor
@{
    #if  DEBUG
    {
        <h3>@ViewData["Title"]</h3>
    }
    #endif
}
```

#### Add a semicolon to the directive

Directives such as `#region` and `#endregion` allow putting a semicolon after the directive. This will effectively work around the issue.

```razor
@{
    #region R ;
    <h3>@ViewData["Title"]</h3>
    #endregion
}
```

#### Add a semicolon after the directive

Directives such as `#if` and `#endif` do not allow semicolons after the directive condition, but one can be placed on the next line to make an empty statement.

```razor
@{
    #if  DEBUG
    ;
    <h3>@ViewData["Title"]</h3>
    #endif
}
```
