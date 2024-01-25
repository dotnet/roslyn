# Artifact Outputs for Incremental Generators

## Summary

This proposal adds a mechanism that allows [incremental generators](incremental-generators.md) to output non-source artifacts. This would allow for text or binary files to be emitted to disk alongside the compiled assembly.

### Rationale

There exists a desire for incremental source generators be able to write out a set of accompanying files, alongside the generated sources that they add to the compilation. An example of this is JavaScript interop with .NET code. When exposing a .NET method to JavaScript today it is possible to generate the C# interop source code that enables the call to be made from JavaScript, but the user must still hand author the corresponding JavaScript code. Instead, the incremental generator could emit the required C# code alongside a JavaScript file that provides the equivalent JavaScript interop code.

### High Level Design Goals

- Allow Incremental Generators to emit text or binary files to disk alongside the emitted .NET assembly
- The user can control where the generated artifacts are emitted with a suitable default
- Be optimal in terms of file writes / copies and ensure we keep the correct incremental build semantics for MSBuild
- Do not have to be run as part of the IDE experience. Files are only emitted to disk as part of command line compilation

## Implementation

The feature is exposed to incremental generator authors via a new output API:

```csharp
namespace Microsoft.CodeAnalysis;

public readonly partial struct IncrementalGeneratorInitializationContext
{
    public void RegisterArtifactOutput<TSource>(IncrementalValueProvider<TSource> source, Action<ArtifactProductionContext, TSource> action);
    public void RegisterArtifactOutput<TSource>(IncrementalValuesProvider<TSource> source, Action<ArtifactProductionContext, TSource> action);
}

public readonly struct ArtifactProductionContext
{
    public CancellationToken CancellationToken { get; }

    public void AddFile(string hintName, Action<System.IO.Stream, CancellationToken> callback);
}
```

This can be used by a generator author like so:

```csharp
internal sealed class Generator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var compilationSource = context.CompilationProvider;

        context.RegisterArtifactOutput(compilationSource, (artifactOutputContext, item) =>
        {
            artifactOutputContext.AddFile("interop.js", (stream, cancellationToken) =>
            {
                using var writer = new StreamWriter(stream);
                writer.WriteLine("//output");
            });
        });
    }
}
```

Note how the call to `AddFile` takes a callback that provides a stream. This allows the host of the generator driver to control how the stream is created and what kind of stream is created. For instance in the compiler it could be a `FileStream` that writes directly to disk, but in unit tests it could be a `MemoryStream` that can be inspected as part of the test. It also takes a `hintName`, the same way that source files do, rather than an explicit file name or path. This is explained in more detail below.

In order to control whether this output is used or not, a new entry is added to the `IncrementalGeneratorOutputKind` enum:

```diff
[Flags]
public enum IncrementalGeneratorOutputKind
{
    None = 0,
    Source = 0b1,
    PostInit = 0b10,
+    Artifact = 0b100
}
```

A host such as the IDE may disable non-source outputs when constructing the Generator Driver.

In terms of the compilation and inputs that an Artifact can be based on, artifact outputs are treated the same as source outputs; all generation is based on the state of the compilation *before* any other generation has happened, and an artifact can't use the output of another generator (outside of itself) to generate artifacts.

### File names and output locations

The call to `AddFile` takes a `hintName` for the output file rather than an explicit file name or path. This allows the host of the generator driver the freedom to decide where precisely the file should go (if it is indeed going to e.g. the disk). Here we propose a set of rules that the command line compiler will follow. Other hosts may choose a different scheme.

The compiler will take a new command line option `/generatedartifactsout:` that will specify the _default_ location of any generated artifacts. By default, using the compiler MSBuild targets, this will be set to the `$(OutDir)` property (i.e. `/bin/`). Note that this is different to `OutputAssembly` and `GeneratedFilesOutputPath` which typically use the `$(IntermediateOutputPath)` (i.e. `/obj/`). This ensures that artifacts are written to their final location a single time, rather than being written to an intermediate directory and having to be copied, optimizing the disk usage.

By default the supplied `hintName` will be used as the filename. We already have conditions in place for source texts to prevent hint names from being invalid filenames, and we will reuse this logic for artifact names. If two generators attempt to write a file with the same name, the generator name, including namespace, will be prepended to the name of the files to ensure there are no conflicts.

### Customizing output locations

While it may be acceptable for the majority of cases to simply place the artifacts in the bin directory, there are known cases (such as the Javascript interop case mentioned at the beginning) where it would be beneficial to place generated files in a more specific location (such as `wwwroot`). This proposal provides a mechanism to override the location of artifacts in two ways: on a per-generator basis, and on an individual artifact basis. Both of these mechanisms are implemented via `.globalconfig` options, and exposed with `MSBuild` properties to enable easy configuration by both Generator authors and consumers.

The generator driver will recognize the following `.globalconfig` options:

- `compiler_artifact.<generatorname>.output_dir = <dir>`
- `compiler_artifact.<artifactname>.output_dir = <dir>`

When the second part of the key matches the name of a loaded generator, any artifacts produced by that generator will be placed in the specified `<dir>` instead of the default directory. If the key fails to match a generator, individual artifacts are checked: if the hint name of an artifact matches it will be placed in the specified `<dir>`.

While these properties can be set manually in a `.globalconfig` it's expected that they will instead be passed from `MSBuild` as project items. The compiler already has a mechanism via `.MSBuildGeneratedEditorConfig` that allows the mapping of `MSBuild` information into `.globalconfig` format, and this proposal expands on that to support the compiler artifact mapping. The mapping will recognize the `<CompilerArtifactMapping` item, and will map the `include` value into the key name and the `Location` attribute as the key value (`<dir>`)

```xml
<ItemGroup>
    <!-- Map all of the outputs of the razor source generator -->
    <CompilerArtifactMapping Include="Microsoft.CodeAnalysis.Razor.SourceGenerators.SourceGenerator" Location="$(OutDir)\Razor" />

    <!-- Map an individual artifact -->
    <CompilerArtifactMapping Include="ArtifactName" Location="c:\dir" />
<ItemGroup>
```

Would produce the following in the generated editor config:

```ini
compiler_artifact.Microsoft.CodeAnalysis.Razor.SourceGenerators.SourceGenerator.output_dir = bin\net8.0\Razor
compiler_artifact.ArtifactName.output_dir = c:\dir
```

This means that generator authors are free to specify a mapping alongside their generator via the standard `NuGet` props and targets mechanism, and consumers of the generators are able to override the location in a consistent way.

## MSBuild semantics

Today, it's not possible to dynamically provide the list of outputs to an `MSBuild` target. This proposal allows the compiler to effectively emit an unbound number of files, none of which are tracked via `MSBuild` meaning that incremental compilation can be broken. (Note this is actually true today thanks to `EmitCompilerGeneratedFiles` but they don't have a bearing on the correctness of build as they are not inputs to any other task or target).

In order to correctly participate in the build process the compiler and targets are updated to emit a new file with the list of all files that it wrote as part of a compilation. A new target is added before `CoreCompile` that runs unconditionally and looks for the existence of the output list file; if the list is present and anything in the list is missing or out of date, the target touches a 'sentinel file'. The 'sentinel' file is passed as an input to `CoreCompile`. This ensures that anytime an output from the compiler is modified outside of the compilation process, `CoreCompile` will be correctly invoked and not skipped as being up to date. The 'sentinel' file effectively stands in for all of the outputs produced by the compiler in the `MSBuild` up to date checks.

In order to decide if a file is modified the new target will require a task that can decide if a file has been modified since the previous build. This logic is fairly simple, as it just has to compare the timestamp of the output list file with the items listed within, but it's worth calling out that this does duplicate some of the `MSBuild` logic in the compiler tasks. The concept of dynamic inputs/outputs is proposed by `MSBuild` but not currently implemented. If this feature is added we could remove this duplication.

## Open Questions

- Naming: This proposal originally used the term `NonSource` but that's not particularly descriptive and perhaps odd in that it's not actually the mirror of a source output in that it's not going into the compilation. `Artifact` seems like a better term? Or simply `File`?
- Do we want custom mapping for the `compiler_artifact.` scheme, or could we just use the existing `build_metadata.<ItemType>.<Metadata>` mechanism and recognize `CompilerMappedArtifact` as a special key?
  - This is doable, but does tie the MSBuild item type name directly into the compiler, rather than having a level of indirection.
- File mapping: should we just let the generators party on the file system and allow absolute paths and `.`, `..` etc, rather than having the host control the output location?
  - An issue with this approach is that a user can control their project layout via MSBuild, and the generator won't know that it has changed, meaning you might end up with files in odd places
  - It would be possible to route though e.g. `OutDir` either automatically, or by the generator author specifying it as a `CompilerVisibleProperty` but this relies on the author doing the correct thing (and multiple authors having to copy the same logic)
  - It seems better to write to a 'virtual' location, and let the host re-direct that as appropriate.
- File mapping: within a hint name should we allow sub-directories? So that a given generator can write to e.g. `<base>/css` and `<base>/js` and just map the `<base>` directory to wherever is appropriate?
- File mapping: can we just have the compiler write them to the obj directory (or the `GeneratedFilesOut` directory) and use MSBuild to do the copying? Either by default or require the author to supply targets to do the copying after build?
  - This makes things for the compiler simpler, but doesn't optimize for disk hits or consistency. A given generator may not even _know_ what its outputs are going to be ahead of time, as the artifact name may be derived from a user input, making it almost impossible for the generator to provide the copy
    - We could say that each generators outputs go to a specific folder, like the generated files do, so that it could just copy `<dir>/*` to the location that matters.
- File mapping: do we really need the ability to map individual artifacts? Maybe per-generator is enough, and a user can always write `MSBuild` logic to move the individual artifacts if required?
- User control: do we need a way for the user to disable artifact production? Should this be per generator or just all artifacts?