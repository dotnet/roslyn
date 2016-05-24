**This document lists known breaking changes in Roslyn (VS2015+) from the native C# compiler (VS2013 and previous).**

*Breaks are formatted with a monotonically increasing numbered list to allow them to referenced via shorthand (i.e., "known break #1").
Each entry should include a short description of the break, followed by either a link to the issue describing the full details of the break or the full details of the break inline.*

1. In some cases, due to a bug in the native compiler, programs with pointers to structs with one or more
   type parameters compiled without error. All such programs should now produce errors in Roslyn. See
   [#5712](https://github.com/dotnet/roslyn/issues/5712) for examples and details.
2. When calling a method group with only instance methods in a static context with dynamic arguments, the
   native compiler generated no warnings/errors and emitted code that would always throw when executed.
   Roslyn produces an error in this situation. See [#11341](https://github.com/dotnet/roslyn/pull/11341) for when this decision was made,
   [#11256](https://github.com/dotnet/roslyn/pull/11256) for when it was discovered, and [#10463](https://github.com/dotnet/roslyn/issues/10463) for the original issue that led to this.
