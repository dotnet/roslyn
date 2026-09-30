---
coverage: How the Correctness_Rebuild leg validates deterministic rebuilds, and the reference-resolution rules BuildValidator relies on
---

# Rebuild validation (BuildValidator)

The `Correctness_Rebuild` CI leg runs `eng/test-rebuild.cmd -ci -configuration Release -bootstrap`,
which does a bootstrap Release build and then runs `artifacts/bin/BuildValidator/Release/net10.0/BuildValidator.exe`
over `artifacts/obj`. For each included assembly, BuildValidator reconstructs a compilation from
metadata in its PDB and compares the emitted PE bytes with the original.

## Reference resolution

`src/Tools/BuildValidator/LocalReferenceResolver.cs` indexes candidate files by file name across
`--assembliesPath artifacts/obj`, the NuGet cache, `--referencesPath artifacts/bin`, and the SDK
`packs` directory. For each requested file name, it caches the first non-ReadyToRun candidate
encountered for each MVID that is not already in the global `_mvidMap`, then considers
ReadyToRun candidates as fallbacks. The first cached entry for an MVID is never replaced.

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

`eng/targets/XUnit.targets` does this for .NET Framework xUnit v3 tests in the
`CreateXunitV3AppHost` target: it copies the test assembly to the `.exe` app host launched by the
VSTest adapter, then applies the `SetPEStackReserve` task to that copy. It does not patch the
primary test assembly or its intermediate. The task changes the 32-bit/AnyCPU app host from its
1 MB reserve to 4 MB; a 64-bit app host already has a 4 MB reserve and is left unchanged.

## Investigating failures

- `Program.ValidateFiles` retains each `CompilationDiff` until the run ends. A binary-difference
  result retains both PE images and the rebuilt `Compilation`, so many mismatched outputs can
  consume substantial memory and lead to `OutOfMemoryException` or `Insufficient memory` messages.
  This accumulation is one possible cause of memory exhaustion; it does not establish that every
  out-of-memory failure is just a symptom of mass output differences.
- On failure CI publishes the `BuildValidator_DebugOut` artifact from `artifacts/BuildValidator`,
  which contains per-assembly diffs.
- To reproduce locally without a bootstrap build, publish the language server for one RID
  (`dotnet publish src/LanguageServer/Microsoft.CodeAnalysis.LanguageServer -c Release -r win-x64`)
  and then run `eng/test-rebuild.cmd -configuration Release`. Without the bootstrap compiler some
  assemblies fail for unrelated reasons, so compare failure sets between two runs rather than
  expecting zero failures.
- `eng/test-rebuild.ps1` holds the list of assemblies excluded from validation, grouped by reason.
