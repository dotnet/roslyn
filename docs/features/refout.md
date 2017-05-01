# Reference assemblies

Reference assemblies are metadata-only assemblies with the minimum amount of metadata to preserve the compile-time behavior of consumers (diagnostics may be affected, though).

## Scenarios
There are 4 scenarios:

1. The traditional one, where an assembly is emitted as the primary output (`/out` command-line parameter, or `peStream` parameter in `Compilation.Emit` APIs).
2. The IDE scenario, where the metadata-only assembly is emitted (via `Emit` API), still as the primary output. Later on, the IDE is interested to get metadata-only assemblies even when there are errors in the compilation.
3. The CoreFX scenario, where only the ref assembly is emitted, still as the primary output (`/refonly` command-line parameter) 
4. The MSBuild scenario, which is the new scenario, where both a real assembly is emitted as the primary output, and a ref assembly is emitted as the secondary output (`/refout` command-line parameter, or `metadataPeStream` parameter in `Emit`).


## Definition of ref assemblies
Metadata-only assembly have their method bodies replaced with a single `throw null` body, but include all members except anonymous types. The reason for using `throw null` bodies (as opposed to no bodies) is so that PEVerify could run and pass (thus validating the completeness of the metadata).
Ref assemblies further remove metadata (private members) from metadata-only assemblies.

A reference assembly will only have references for what it needs in the API surface. The real assembly may have additional references related to specific implementations. For instance, the reference assembly for `class C { private void M() { dynamic .... } }` will not have any references for `dynamic` types.

Private function-members (methods, properties and events) will be removed. If there are no `InternalsVisibleTo` attributes, do the same for internal function-members

All types (including private or nested types) must be kept in reference assemblies.

All fields of a struct will be kept (in C# 7.1 timeframe), but this can later be refined. There are three cases to consider (ref case, struct case, generic case), where we could possibly substitute the fields with adequate placeholders that would minimize the rate of change of the ref assembly.

Reference assemblies will include an assembly-level `ReferenceAssembly` attribute. If such an attribute is found in source, we won't need to synthesize it. Because of this attribute, runtimes will refuse to load reference assemblies.

## API changes

### Command-line
Two mutually exclusive command-line parameters will be added to `csc.exe` and `vbc.exe`:
- `/refout`
- `/refonly`

The `/refout` parameter specifies a file path where the ref assembly should be output. This translates to `metadataPeStream` in the `Emit` API (see details below). The filename for the ref assembly should generally match that of the primary assembly, but it can be in a different folder.

The `/refonly` parameter is a flag that indicates that a ref assembly should be output instead of an implementation assembly. 
The `/refonly` parameter is not allowed together with the `/refout` parameter, as it doesn't make sense to have both the primary and secondary outputs be ref assemblies. Also, the `/refonly` parameter silently disables outputting PDBs, as ref assemblies cannot be executed. 
The `/refonly` parameter translates to `EmitMetadataOnly` being `true`, and `IncludePrivateMembers` being `false` in the `Emit` API (see details below).
Neither `/refonly` nor `/refout` are permitted with `/target:module` or `/addmodule` options.

When the compiler produces documentation, the contents produced will match the APIs that go into the primary output. In other words, the documentation will be filtered down when using the `/refonly` parameter.

The compilation from the command-line will either produce both assemblies (implementation and ref) or neither. There is no "partial success" scenario.

### CscTask/CoreCompile
The `CoreCompile` target will support a new output, called `IntermediateRefAssembly`, which parallels the existing `IntermediateAssembly`.
The `Csc` task will support a new output, called `OutputRefAssembly`, which parallels the existing `OutputAssembly`.
Both of those basically map to the `/refout` command-line parameter.

An additional task, called `CopyRefAssembly`, will be provided along with the existing `Csc` task. It takes a `SourcePath` and a `DestinationPath` and generally copies the file from the source over to the destination. But if it can determine that the contents of those two files match (by comparing their MVIDs, see details below), then the destination file is left untouched.

### CodeAnalysis APIs
It is already possible to produce metadata-only assemblies by using `EmitOptions.EmitMetadataOnly`, which is used in IDE scenarios with cross-language dependencies.
The compiler will be updated to honour the `EmitOptions.IncludePrivateMembers` flag as well. When combined with `EmitMetadataOnly` or a `metadataPeStream` in `Emit`, a ref assembly will be produced.
The diagnostic check for emitting methods lacking a body (`void M();`) will be filtered from declaration diagnostics, so that code will successfully emit with `EmitMetadataOnly`.
Later on, the `EmitOptions.TolerateErrors` flag will allow emitting error types as well.
`Emit` is also modified to produce a new PE section called ".mvid" containing a copy of the MVID, when producing ref assemblies. This makes it easy for `CopyRefAssembly` to extract and compare MVIDs from ref assemblies.

Going back to the 4 driving scenarios:
1. For a regular compilation, `EmitMetadataOnly` is left to `false` and no `metadataPeStream` is passed into `Emit`.
2. For the IDE scenario, `EmitMetadataOnly` is set to `true`, but `IncludePrivateMembers` is left to `true`.
3. For the CoreFX scenario, ref assembly source code is used as input, `EmitMetadataOnly` is set to `true`, and `IncludePrivateMembers` is set to `false`.
4. For the MSBuild scenario, `EmitMetadataOnly` is left to `false`, a `metadataPeStream` is passed in and `IncludePrivateMembers` is set to `false`.

## Future
As mentioned above, there may be further refinements after C# 7.1:
- controlling internals (producing public ref assemblies)
- produce ref assemblies even when there are errors outside method bodies (emitting error types when `EmitOptions.TolerateErrors` is set)

## Open questions
- should explicit method implementations be included in ref assemblies?
- Non-public attributes on public APIs (emit attribute based on accessibility rule)
- ref assemblies and NoPia

## Related issues
- Produce ref assemblies from command-line and msbuild (https://github.com/dotnet/roslyn/issues/2184)
- Refine what is in reference assemblies and what diagnostics prevent generating one (https://github.com/dotnet/roslyn/issues/17612)
- [Are private members part of the API surface?](http://blog.paranoidcoding.com/2016/02/15/are-private-members-api-surface.html)
