Extension Methods in the Global Namespace
=========================================

If you create a compilation in which an imported library is assigned an alias (instead of the default "global" alias), the Roslyn compilers do not automatically see extension methods in the (unnamed top-level) global namespace in that library. You would have to import the library in your code using the alias for these methods to be treated as extension methods.

Previous versions of the compiler would treat such methods as extension methods despite not being imported.

See also https://github.com/dotnet/roslyn/issues/1079.
