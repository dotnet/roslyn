**This document lists known breaking changes in Roslyn (VS2015+) from the native VB compiler (VS2013 and previous).**

*Breaks are formatted with a monotonically increasing numbered list to allow them to referenced via shorthand (i.e., "known break #1").
Each entry should include a short description of the break, followed by either a link to the issue describing the full details of the break or the full details of the break inline.*

1. When specifying the output extension as one of the known output types (example /out:foo.dll), but specifying a different target type
   (example /t:exe) the compiler will now keep the specified extension and won't add the target extension (example foo.dll.exe). See
   [#13681](https://github.com/dotnet/roslyn/issues/13681) for examples and details.
