**This document lists known breaking changes in Roslyn (VS2015+) from the native C# compiler (VS2013 and previous).**

*Breaks are formatted with a monotonically increasing numbered list to allow them to referenced via shorthand (i.e., "known break #1").
Each entry should include a short description of the break, followed by either a link to the issue describing the full details of the break or the full details of the break inline.*

1. In some cases, due to a bug in the native compiler, programs with pointers to structs with one or more
   type parameters compiled without error. All such programs should now produce errors in Roslyn. See
   [#5712](https://github.com/dotnet/roslyn/issues/5712) for examples and details.
