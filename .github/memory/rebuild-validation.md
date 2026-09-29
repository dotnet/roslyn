---
coverage: How the Correctness_Rebuild leg validates deterministic rebuilds, and the reference-resolution rules BuildValidator relies on
---

# Rebuild validation (BuildValidator)

The `Correctness_Rebuild` CI leg runs `eng/test-rebuild.cmd -ci -configuration Release -bootstrap`,
which does a bootstrap Release build and then runs `artifacts/bin/BuildValidator/Release/net10.0/BuildValidator.exe`
over `artifacts/obj`. BuildValidator recompiles each assembly from the compilation metadata stored in
its PDB and requires the result to be byte-identical to the original.

## Reference resolution

`src/Tools/BuildValidator/LocalReferenceResolver.cs` indexes candidate files by file name across
`--assembliesPath artifacts/obj`, the NuGet cache, `--referencesPath artifacts/bin`, and the SDK
`packs` directory, then maps MVID → the first matching file (`_mvidMap`, first wins).

Two properties of that mapping matter:

- The PDB records more than the MVID for each reference. `MetadataReferenceInfo` in
  `src/Compilers/Core/Rebuild/Records.cs` also carries the reference's COFF `Timestamp` and
  `ImageSize`, and those values are re-emitted into the rebuilt PDB. Resolving to a different file
  with the same MVID therefore still produces a binary difference.
- Crossgen2 keeps the MVID of the IL assembly it compiled, so a ReadyToRun image is an MVID match
  for its IL twin while differing in timestamp and image size. `EnsureCachePopulated` registers
  ReadyToRun images only after IL assemblies, so an IL twin always wins; ReadyToRun images remain
  registered as a fallback because some references (for example
  `Microsoft.AspNetCore.Razor.Runtime.dll` from the ASP.NET Core runtime packs) are published only
  in that form.

ReadyToRun images under `artifacts/obj/Microsoft.CodeAnalysis.LanguageServer/Release/net10.0/<rid>/R2R/`
come from the platform-specific Release publish configured by
`src/LanguageServer/LanguageServerPublish.props`. They only exist after a Release publish with a
runtime identifier, so a plain local build will not exercise ReadyToRun reference resolution.

## Post-compile modification of intermediate assemblies

Anything that rewrites `@(IntermediateAssembly)` after `CoreCompile` breaks validation for every
affected project, because BuildValidator compares its rebuild against the file in `artifacts/obj`.
A single changed header byte is enough. Post-process the copies in `artifacts/bin` instead, and
restore the original last write time so incremental copies keep skipping the patched files.
CI builds pass `ROSLYNUSEHARDLINKS=true`, so bin copies are hard links to `artifacts/obj` files:
delete and recreate the bin file before writing, or the edit also changes the intermediate assembly.

`eng/targets/XUnit.targets` does this for .NET Framework test executables: `SetTestAssemblyStackReserve`
raises the PE `SizeOfStackReserve` to 4 MB on the bin assembly after `CopyFilesToOutputDirectory`,
and `CreateXunitV3AppHost` does the same for the `.exe` app host that xunit.v3's VSTest adapter
launches. Note that csc already emits a 4 MB reserve for 64-bit images, so only the 32-bit/AnyCPU
output is actually changed.

## Investigating failures

- Every rebuild diff is retained for the whole run (`Program.ValidateFiles` accumulates
  `CompilationDiff` values that hold both PE images and the rebuild `Compilation`), so a large
  number of failures can end the run with `Insufficient memory` messages and an
  `OutOfMemoryException`. Treat memory exhaustion as a symptom of mass failures, not a separate bug.
- On failure CI publishes the `BuildValidator_DebugOut` artifact from `artifacts/BuildValidator`,
  which contains per-assembly diffs.
- To reproduce locally without a bootstrap build, publish the language server for one RID
  (`dotnet publish src/LanguageServer/Microsoft.CodeAnalysis.LanguageServer -c Release -r win-x64`)
  and then run `eng/test-rebuild.cmd -configuration Release`. Without the bootstrap compiler some
  assemblies fail for unrelated reasons, so compare failure sets between two runs rather than
  expecting zero failures.
- `eng/test-rebuild.ps1` holds the list of assemblies excluded from validation, grouped by reason.
