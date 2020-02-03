**This document lists known breaking changes in Roslyn 3.0 (Visual Studio 2019) from Roslyn 2.* (Visual Studio 2017)

<!--
*Breaks are formatted with a monotonically increasing numbered list to allow them to referenced via shorthand (i.e., "known break #1").
Each entry should include a short description of the break, followed by either a link to the issue describing the full details of the break or the full details of the break inline.*
-->

1. Previously, reference assemblies were emitted including embedded resources. In Visual Studio 2019, embedded resources are no longer emitted into ref assemblies.
  See https://github.com/dotnet/roslyn/issues/31197
