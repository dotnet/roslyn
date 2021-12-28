This page documents some best practices for adding new BCL types to be used by the compiler.

## Attributes

New **attributes** should be marked with an `Obsolete` attribute (with `error` set to `true`, so they are not allowed in source) and `EditorBrowsable(EditorBrowsableState.Never)`, if layering permits. 

For instance, although we didn't do it, the `TupleElementNames` attribute would have been best marked this way. So when using Visual Studio 2015 to implement a C# 7.0 interface with tuple names, the IDE would not introduce `TupleElementNames` in source (which causes problems once you update to Visual Studio 2017).

## Types

New **types** are best added to corlib (CoreCLR, CoreRT, Mono, Desktop) first. Then a standalone package can be offered with a downlevel implementation (for older targets) and type-forwards for new targets.

Trying to offer a package first, then migrating the types down to corlib as a second step creates headaches, windows of ambiguities and opportunities for mistakes. See the [log](https://github.com/dotnet/roslyn/issues/13177) of ValueTuple library work. If we choose to do that, the package should be updated and marked as *release* (not pre-release) as soon as possible.

## Round-tripping with older compiler

There is a general problem of using a new library (produced with new compiler, with new metadata) from an old client project (using earlier version of the compiler). Each feature that introduces such metadata needs to make a choice between:

1. poisoning the metadata (so that the older compiler rejects it)
2. exposing the client to a break (when updating the project to use the newer compiler)
3. ensuring that the new compiler maintains the behavior of an old compiler encountering this metadata it didn't understand

The `ref readonly` feature chose option (1).

There is discussion to adjust the compiler's tolerance to missing tuple names (adopting the third approach, instead of the second one): https://github.com/dotnet/roslyn/issues/20528.

## History:
- Changes in [.NET Framework 4.7.1](https://blogs.msdn.microsoft.com/dotnet/2017/09/28/net-framework-4-7-1-runtime-and-compiler-features/) (adding attributes for ref readonly and ref struct, making `ValueTuple` types serializable, adding ITuple, adding `RuntimeFeature` API)
- Changes in [.NET Framework 4.7](https://blogs.msdn.microsoft.com/dotnet/2017/04/05/announcing-the-net-framework-4-7/) (adding `ValueTuple` types)

## Open issues:
- Verify how the IDE behaves today when an attribute is marked as obsolete and the IDE tries to copy it from the method you're overriding.

