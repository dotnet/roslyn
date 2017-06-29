This page documents some best practices for adding new BCL types to be used by the compiler.

New **attributes** should be marked with an `Obsolete` attribute (with `error` set to `true`, so they are not allowed in source) and `EditorBrowsable(EditorBrowsableState.Never)`, if layering permits. 

For instance, although we didn't do it, the `TupleElementNames` attribute would have been best marked this way. So when using Visual Studio 2015 to implement a C# 7.0 interface with tuple names, the IDE would not introduce `TupleElementNames` in source (which causes problems once you update to Visual Studio 2017).

New **types** are best added to corlib (CoreCLR, CoreRT, Mono, Desktop) first. Then a standalone package can be offered with a downlevel implementation (for older targets) and type-forwards for new targets.

Trying to offer a package first, then migrating the types down to corlib as a second step creates headaches, windows of ambiguities and opportunities for mistakes. See the [log](https://github.com/dotnet/roslyn/issues/13177)of ValueTuple library work.

## Open issues:
- Verify how the IDE behaves today when an attribute is marked as obsolete and the IDE tries to copy it from the method you're overriding.

