---
title: Razor compiler breaking changes since .NET 11
description: Learn about breaking changes in Razor between .NET 11 and .NET 12.
---
# Breaking changes in Razor after .NET 11.0.100 through .NET 12.0.100

This document lists known breaking changes in Razor after .NET 11 general release (.NET SDK version 11.0.100) through .NET 12 general release (.NET SDK version 12.0.100).

## Ambiguous namespace-scoped component matches

***Introduced in Razor 12***

When a tag matches multiple components in the current namespace or an enclosing namespace, Razor 12 tries to select a component or reports an ambiguity error. In Razor 11, if none of the matching namespaces were explicitly imported, Razor did not select a component or report an ambiguity error.

For example, consider these components:

```csharp
using Microsoft.AspNetCore.Components;

namespace Components.Library
{
    public class Widget : ComponentBase { }
}

namespace Components.Library.Nested
{
    public class Widget : ComponentBase { }
}
```

In a Razor file with the following markup, both components are in scope:

```razor
@namespace Components.Library.Nested

<Widget />
```

Razor 11 did not select either component. Razor 12 reports `RZ9985` because it cannot choose between them. To select a component, use its fully qualified name:

```razor
<Components.Library.Nested.Widget />
```

If the candidates differ by generic arity, Razor 12 may instead select a component. For example, if only `Widget` and `Widget<TItem>` match, `<Widget />` selects the non-generic component. This can change the rendered output without producing an error.
