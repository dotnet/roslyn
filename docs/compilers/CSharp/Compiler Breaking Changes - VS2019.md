**This document lists known breaking changes in Roslyn 3.0 (Visual Studio 2019) from Roslyn 2.* (Visual Studio 2017)

*Breaks are formatted with a monotonically increasing numbered list to allow them to referenced via shorthand (i.e., "known break #1").
Each entry should include a short description of the break, followed by either a link to the issue describing the full details of the break or the full details of the break inline.*

1. Previously, we allowed the `Microsoft.CodeAnalysis.EmbeddedAttribute` to be referenced from a source module.
    In Visual Studio 2019, this produces a collision with the injected declaration of that type.

2. Previously, we allowed a user-defined `System.Runtime.CompilerServices.NonNullTypesAttribute` to be referenced from metadata (assembly or module).
    In Visual Studio 2019, the type from assembly is ignored in favor of the injected declaration of that type, and the type from an added module produces a collision with the injected declaration of that type.
