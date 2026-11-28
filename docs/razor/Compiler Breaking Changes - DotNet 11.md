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

## Ambiguous generic component matches

***Introduced in Razor 12***

When multiple imported generic components have the same type parameter names, Razor 11 could select the first matching component. Razor 12 reports `RZ9985` instead. For example, if both `First` and `Second` contain `Widget<TItem>`, this tag is ambiguous:

```razor
@using First
@using Second

<Widget TItem="string" />
```

Use the fully qualified tag name to select a component, such as `<First.Widget TItem="string" />`. When exactly one component's complete set of type parameters matches the supplied type-parameter attributes, Razor 12 selects it even if other components partially match and need more type parameters.

## Component aliases take precedence over same-named tags

***Introduced in Razor 12***

Component aliases now take precedence over ordinary component matches with the same tag name. For example, given components named `Library.Button` and `OtherLibrary.Button`, this Razor file renders `OtherLibrary.Button` in Razor 12:

```razor
@namespace Library
@using Button = OtherLibrary.Button

<Button />
```

In Razor 11, the alias did not affect component binding, so `<Button />` rendered `Library.Button`. In Razor 12, the alias can also prevent a same-named component from binding when its target is not a component; the tag is then treated as markup. To keep using the ordinary component, use its fully qualified tag name or rename the alias.
