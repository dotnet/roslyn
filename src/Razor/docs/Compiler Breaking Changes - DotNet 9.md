---
title: Razor compiler breaking changes since .NET 9
description: Learn about any breaking changes since the initial release of .NET 9.
author: wadepickett
ms.author: wpickett
ms.date: 11/12/2024
---
# Breaking changes in Razor after .NET 9.0.100 through .NET 10.0.100

This document lists known breaking changes in Razor after .NET 9 general release (.NET SDK version 9.0.100) through .NET 10 general release (.NET SDK version 10.0.100).

## Preprocessor directive parsing breaks

***Introduced in VS 17.13p1 and .NET 9.0.200***

A new lexing mode was introduced for understanding the C# sections in razor files that brings increased compatibility with how C# is natively lexed. However, this
also brings some breaking changes to the Razor compiler's understanding of C# preprocessing directives, which previously did not work consistently. Directives are
now required to start at the beginning of a line in Razor files (only whitespace is allowed before them). Additionally, disabled sections are now properly disabled
by the Razor compiler when `#if` preprocessor blocks are considered inactive.

### Preprocessor blocks are required to start at the beginning of a line

```razor
@{ #if DEBUG /* Previously allowed, now triggers RZ1043 */ }
<div>test</div>
@{ #endif /* Previously allowed, now triggers RZ1043 */ }
```

To fix, move the directives to a new line. Only whitespace is allowed before the directive.

```razor
@{
#if DEBUG /* This is allowed */
}
<div>test</div>
@{
    #endif /* This is allowed */
}
```

### Disabled blocks are now considered properly in the Razor compiler

Disabled blocks are now considered completely disabled by the Razor compiler, and no attempt to understand the block is made. When combined with the previous break,
this means that if an `#else`, `#elif`, or `#endif` was not at the start of a line (modulo whitespace), a larger section of the file will be considered disabled than
in older versions of the Razor compiler. To help diagnose potential breaks here, the Razor compiler will scan disabled text sections for potential misplaced preprocessor
directives and report a warning if one is encountered.

```razor
@{
#if false
}

This area is now properly considered disabled by the razor compiler, and no attempt to understand it as either C# or HTML is made. This
can cause changes to how the output is rendered from previous versions of the Razor compiler.

@{ #else
    In previous versions of the Razor compiler, this directive would have been picked up. It is no longer picked up because it is not at
    the start of a line. The Razor compiler will report a warning, RZ1044, to help diagnose any potential breaks in this area.
}

@{
#endif
}
```
