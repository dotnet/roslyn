**This document lists known breaking changes in Roslyn 3.0 (Visual Studio 2019) from Roslyn 2.* (Visual Studio 2017)

*Breaks are formatted with a monotonically increasing numbered list to allow them to referenced via shorthand (i.e., "known break #1").
Each entry should include a short description of the break, followed by either a link to the issue describing the full details of the break or the full details of the break inline.*

1. Previously, we allowed adding a module with `Microsoft.CodeAnalysis.EmbeddedAttribute` or `System.Runtime.CompilerServices.NonNullTypesAttribute` types declared in it.
    In Visual Studio 2019, this produces a collision error with the injected declarations of those types.

2. Previously, you could refer to a `System.Runtime.CompilerServices.NonNullTypesAttribute` type declared in a referenced assembly.
    In Visual Studio 2019, the type from assembly is ignored in favor of the injected declaration of that type.
