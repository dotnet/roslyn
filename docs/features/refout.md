# Reference assemblies

Reference assemblies are metadata-only assemblies with the minimum amount of metadata to preserve the compile-time behavior of consumers (diagnostics may be affected, though).

The compiler may choose to remove more metadata in later versions, if it is determined to be safe (ie. respects the principle above).

## Scenarios
There are 4 scenarios:

1. The traditional one, where an assembly is emitted as the primary output (`/out` command-line parameter, or `peStream` parameter in `Compilation.Emit` APIs).
2. The IDE scenario, where the metadata-only assembly is emitted (via `Emit` API), still as the primary output. Later on (after C# 7.1), the IDE is interested to get metadata-only assemblies even when there are errors in the compilation.
3. The CoreFX scenario, where only the ref assembly is emitted, still as the primary output (`/refonly` command-line parameter) 
4. The MSBuild scenario, which is the new scenario, where both a real assembly is emitted as the primary output, and a ref assembly is emitted as the secondary output (`/refout` command-line parameter, or `metadataPeStream` parameter in `Emit`).


## Definition of ref assemblies
Metadata-only assembly have their method bodies replaced with a single `throw null` body, but include all members except anonymous types. The reason for using `throw null` bodies (as opposed to no bodies) is so that PEVerify could run and pass (thus validating the completeness of the metadata).

Ref assemblies include an assembly-level `ReferenceAssembly` attribute. This attribute may be specified in source (then we won't need to synthesize it). Because of this attribute, runtimes will refuse to load ref assemblies for execution (but they can still be loaded in reflection-only mode). Some tools may be affected and will need to be updated (for example, `sgen.exe`).

Ref assemblies further remove metadata (private members) from metadata-only assemblies:

- A ref assembly only has references for what it needs in the API surface. The real assembly may have additional references related to specific implementations. For instance, the ref assembly for `class C { private void M() { dynamic d = 1; ... } }` does not reference any types required for `dynamic`.
- Private function-members (methods, properties and events) are removed. If there are no `InternalsVisibleTo` attributes, do the same for internal function-members.
- But all types (including private or nested types) are kept in ref assemblies. All attributes are kept (even internal ones), as well as their (internal) constructors.
- All virtual methods are kept. Explicit interface implementations are kept. Explicitly-implemented properties and events are kept, as their accessors are virtual (and are therefore kept).
- All fields of a struct are kept. (This is a candidate for post-C#-7.1 refinement)
- Any resources included on the command-line are not emitted into ref assemblies (produced either with `/refout` or `/refonly`). (This was fixed in dev16)

## API changes

### Command-line
Two mutually exclusive command-line parameters were added to `csc.exe` and `vbc.exe`:
- `/refout`
- `/refonly`

The `/refout` parameter specifies a file path where the ref assembly should be output. This translates to `metadataPeStream` in the `Emit` API (see details below). The filename for the ref assembly should generally match that of the primary assembly. The recommended convention (used by MSBuild) is to place the ref assembly in a "ref/" sub-folder relative to the primary assembly.

The `/refonly` parameter is a flag that indicates that a ref assembly should be output instead of an implementation assembly, as the primary output.

The `/refonly` parameter is not allowed together with the `/refout` parameter, as it doesn't make sense to have both the primary and secondary outputs be ref assemblies. Also, the `/refonly` parameter silently disables outputting PDBs, as ref assemblies cannot be executed. 

The `/refonly` parameter translates to `EmitMetadataOnly` being `true`, and `IncludePrivateMembers` being `false` in the `Emit` API (see details below).

Neither `/refonly` nor `/refout` are permitted with net modules (`/target:module`, `/addmodule` options).

The compilation from the command-line either produces both assemblies (implementation and ref) or neither. There is no "partial success" scenario.

When the compiler produces documentation, it is un-affected by either the `/refonly` or `/refout` parameters. This may change in the future.

The main purpose of the `/refout` option is to speed up incremental build scenarios, so it is acceptable for the current implementation for this flag to produce a ref assembly with more metadata than `/refonly` does (for instance, anonymous types). This is a candidate for post-C#-7.1 refinement.

### CscTask/CoreCompile
The `CoreCompile` target supports a new output, called `IntermediateRefAssembly`, which parallels the existing `IntermediateAssembly`.

The `Csc` task supports a new output, called `OutputRefAssembly`, which parallels the existing `OutputAssembly`.
Both of those basically map to the `/refout` command-line parameter.

An additional task, called `CopyRefAssembly`, is provided along with the existing `Csc` task. It takes a `SourcePath` and a `DestinationPath` and generally copies the file from the source over to the destination. But if it can determine that the contents of those two files match (by comparing their MVIDs, see details below), then the destination file is left untouched.

As a side-note, `CopyRefAssembly` uses the same assembly resolution/redirection trick as `Csc` and `Vbc`, to avoid type loading problems with `System.IO.FileSystem`.

### CodeAnalysis APIs
Prior to C# 7.1, it was already possible to produce metadata-only assemblies by using `EmitOptions.EmitMetadataOnly`, which is used in IDE scenarios with cross-language dependencies.  

With C# 7.1, the compiler now honours the `EmitOptions.IncludePrivateMembers` flag as well. When combined with `EmitMetadataOnly` or a `metadataPeStream` in `Emit`, a ref assembly is produced.  

Method bodies aren't compiled when using `EmitMetadataOnly`. Even the diagnostic check for emitting methods lacking a body (`void M();`) is filtered from declaration diagnostics, so such code will successfully emit with `EmitMetadataOnly`.  

Later on, the `EmitOptions.TolerateErrors` flag will allow emitting error types as well.  
`Emit` was modified to produce a new PE section called ".mvid" containing a copy of the MVID, when emitting ref assemblies. This makes it easy for `CopyRefAssembly` to extract and compare MVIDs from ref assemblies.

Going back to the 4 driving scenarios:
1. For a regular compilation, `EmitMetadataOnly` is left to `false` and no `metadataPeStream` is passed into `Emit`.
2. For the IDE scenario, `EmitMetadataOnly` is set to `true`, but `IncludePrivateMembers` is left to `true`.
3. For the CoreFX scenario, ref assembly source code is used as input, `EmitMetadataOnly` is set to `true`, and `IncludePrivateMembers` is set to `false`.
4. For the MSBuild scenario, `EmitMetadataOnly` is left to `false`, a `metadataPeStream` is passed in and `IncludePrivateMembers` is set to `false`.

### Determinism

We recommend that you always use determinism with reference assemblies. This minimizes the rate of change for the ref assembly, thereby maximizing the benefits they realize.

That said, even if determinism isn't set, compilation of ref assemblies is [largely deterministic](http://blog.paranoidcoding.com/2016/04/05/deterministic-builds-in-roslyn.html) by default. The main exception is when using `AssemblyVersionAttribute` with a wildcard (for example, `[assembly: System.Reflection.AssemblyVersion("1.0.*")]`). In such case, the compilation is necessarily non-deterministic and therefore ref assemblies don't provide any benefits.

## MSBuild

* `ProduceReferenceAssembly` (boolean) controls whether to create the item passed to the compiler task (and thus pass `/refout:`). It requires opt-in. It is recommended that `Deterministic` also be set for best result (see details above).  Cannot be used in conjunction with `ProduceOnlyReferenceAssembly`
* `ProduceOnlyReferenceAssembly` (boolean) controls whether to pass `/refonly` to the compiler. It requires opt-in. Cannot be used in conjunction with `ProduceReferenceAssembly`
* If you encounter a problem using reference assemblies, you can set the boolean property `CompileUsingReferenceAssemblies` to `false` to avoid using ref assemblies even if the projects you reference produce them. This is unset by default and only ever checked against `false`. It is only there to provide an emergency escape hatch; a customer who hits a bug can set it to `false` and avoid the new codepaths.

## Future
As mentioned above, there may be further refinements after C# 7.1:
- Further reduce the metadata in ref assemblies produced by `/refout`, to match those produced by `/refonly`.
- Controlling internals so they are not included despite `InternalsVisibleTo` (producing public ref assemblies)
- Produce ref assemblies even when there are errors outside method bodies (emitting error types when `EmitOptions.TolerateErrors` is set)
- When the compiler produces documentation, the contents produced could be filtered down to match the APIs that go into the primary output. In other words, the documentation could be filtered down when using the `/refonly` parameter.

## Open questions

## Related issues
- Produce ref assemblies from command-line and msbuild (https://github.com/dotnet/roslyn/issues/2184)
- Refine what is in reference assemblies and what diagnostics prevent generating one (https://github.com/dotnet/roslyn/issues/17612)
- [Are private members part of the API surface?](http://blog.paranoidcoding.com/2016/02/15/are-private-members-api-surface.html)
- MSBuild work items and design notes (https://github.com/Microsoft/msbuild/issues/1986)
- Fast up-to-date check in project-system (https://github.com/dotnet/project-system/issues/2254)
