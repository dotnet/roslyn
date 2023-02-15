# Source Generators

## Summary

> **Warning**: Source generators implementing `ISourceGenerator` have been deprecated
> in favor of [incremental generators](incremental-generators.md).

Source generators aim to enable _compile time metaprogramming_, that is, code that can be created
at compile time and added to the compilation. Source generators will be able to read the contents of 
the compilation before running, as well as access any _additional files_, enabling generators to
introspect both user C# code and generator specific files.

> **Note**: This proposal is separate from the [previous generator design](generators.md)

### High Level Design Goals

- Generators produce one or more strings that represent C# source code to be added to the compilation.
- Explicitly _additive_ only. Generators can add new source code to a compilation but may **not** modify existing user code.
- Can produce diagnostics. When unable to generate source, the generator can inform the user of the problem.
- May access _additional files_, that is, non-C# source texts.
- Run _un-ordered_, each generator will see the same input compilation, with no access to files created by other source generators.
- A user specifies the generators to run via list of assemblies, much like analyzers.

## Implementation

At the simplest level source generators are an implementation of `Microsoft.CodeAnalysis.ISourceGenerator`

```csharp
namespace Microsoft.CodeAnalysis
{
    public interface ISourceGenerator
    {
        void Initialize(GeneratorInitializationContext context);
        void Execute(GeneratorExecutionContext context);
    }
}
```

Generator implementations are defined in external assemblies passed to the compiler
using the same `-analyzer:` option used for diagnostic analyzers. Implementations are required to
be annotated with a `Microsoft.CodeAnalysis.GeneratorAttribute` attribute.

An assembly can contain a mix of diagnostic analyzers and source generators.
Since generators are loaded from external assemblies, a generator cannot be used to build
the assembly in which it is defined.

`ISourceGenerator` has an `Initialize` method that is called by the host (either the IDE or
the command-line compiler) exactly once. `Initialize` passes an instance of `GeneratorInitializationContext`
which can be used by the generator to register a set of callbacks that affect how future generation
passes will occur.

The main generation pass occurs via the `Execute` method. `Execute` passes an instance of `GeneratorExecutionContext`
that provides access to the current `Compilation` and allows the generator to alter the resulting output `Compilation`
by adding source and reporting diagnostics.

The generator is also able to access any `AnalyzerAdditionalFiles` passed to the compiler via the `AdditionalFiles`
collection, allowing for generation decisions to based on more than just the user's C# code.

```csharp
namespace Microsoft.CodeAnalysis
{
    public readonly struct GeneratorExecutionContext
    {
        public ImmutableArray<AdditionalText> AdditionalFiles { get; }

        public CancellationToken CancellationToken { get; }

        public Compilation Compilation { get; }

        public ISyntaxReceiver? SyntaxReceiver { get; }

        public void ReportDiagnostic(Diagnostic diagnostic) { throw new NotImplementedException(); }

        public void AddSource(string fileNameHint, SourceText sourceText) { throw new NotImplementedException(); }
    }
}
```

It is assumed that some generators will want to generate more than one `SourceText`, for example in a 1:1 mapping
for additional files. The `fileNameHint` parameter of `AddSource` is intended to address this:

1. If the generated files are emitted to disk, having some ability to put some distinguishing text might be useful.
For example, if you have two `.resx` files, generating the files with simply names of `ResxGeneratedFile1.cs` and
`ResxGeneratedFile2.cs` wouldn't be terribly useful -- you'd want it to be something like
`ResxGeneratedFile-Strings.cs` and `ResxGeneratedFile-Icons.cs` if you had two `.resx` files
 named "Strings" and "Icons" respectively.

2. The IDE needs some concept of a "stable" identifier. Source generators create a couple of fun problems for the IDE:
users will want to be able to set breakpoints in a generated file, for example. If a source generator outputs multiple
files we need to know which is which so we can know which file the breakpoints go with. A source generator of course is
allowed to stop emitting a file if its inputs change (if you delete a `.resx`, then the generated file associated with it
will also go away), but this gives us some control here.

This was called "hint" in that the compiler is implicitly allowed to control the filename in however it ultimately
needs, and if two source generators give the same "hint" it can still distinguish them with any sort of
prefix/suffix as necessary.

### IDE Integration

One of the more complicated aspects of supporting generators is enabling a high-fidelity
experience in Visual Studio. For the purposes of determining code correctness, it is
expected that all generators will have had to be run. Obviously, it is impractical to run
every generator on every keystroke, and still maintain an acceptable level of performance
within the IDE.

#### Progressive complexity opt-in

It is expected instead that source generators would work on an 'opt-in' approach to IDE
enablement.

By default, a generator implementing only `ISourceGenerator` would see no IDE integration
and only be correct at build time. Based on conversations with 1st party customers,
there are several cases where this would be enough.

However, for scenarios such as code first gRPC, and in particular Razor and Blazor,
the IDE will need to be able to generate code on-the-fly as those file types are
edited and reflect the changes back to other files in the IDE in near real-time.

The proposal is to have a set of advanced callbacks that can be optionally implemented,
that would allow the IDE to query the generator to decide what needs to be run in the case
of any particular edit.

For example an extension that would cause generation to run after saving a third party
file might look something like:

```csharp
namespace Microsoft.CodeAnalysis
{
    public struct GeneratorInitializationContext
    {
        public void RegisterForAdditionalFileChanges(EditCallback<AdditionalFileEdit> callback){ }
    }
}
```

This would allow the generator to register a callback during initialization that would be invoked
every time an additional file changes.

It is expected that there will be various levels of opt in, that can be added to a generator
in order to achieve the specific level of performance required of it.

What these exact APIs will look like remains an open question, and it's expected that we will
need to prototype some real-world generators before knowing what their precise shape will be.

### Output files

It is desirable that the generated source texts be available for inspection after generation,
either as part of creating a generator or seeing what code was generated by a third party
generator.

By default, generated texts will be persisted to a `GeneratedFiles/{GeneratorAssemblyName}` 
sub-folder within `CommandLineArguments.OutputDirectory`. The `fileNameHint` from 
`GeneratorExecutionContext.AddSource` will be used to create a unique name, with appropriate
collision renaming applied if required. For instance, on Windows a call to 
`AddSource("MyCode", ...);` from `MyGenerator.dll` for a C# project might be 
persisted as `obj/debug/GeneratedFiles/MyGenerator.dll/MyCode.cs`.

File output is not required for the correct function of either command line or IDE based 
generation, and can be completely disabled, if required. The IDE will work on in-memory 
copies of the generated source texts (for 'Find all references', breakpoints etc.) and 
periodically flush any changes to disk. 

To support the use case where a user wishes to generate the source text, then commit 
the generated files to source control, we will allow changing the location of the 
generated files via an appropriate command line switch, and matching MSBuild property 
(naming still to be determined).

In these cases it will be up to the user if they wish to generate over the files again
in the future (in which case they would still be generated, but output to a 
source controlled location), or remove the generators and perform the action as a one 
time step. 

It is currently an open question how for example, the action of setting a breakpoint in
a disk-based generated file will function. 

TK: how do we save PDBs/Source link etc?

### Editing experiences for third party languages

One of the interesting scenarios that source generators will enable is essentially 
the 'embedding' of C# within other languages (and vice versa). This is how Razor 
works today, and the Razor team maintains a significant language service investment 
in Visual Studio to enable it. 

A possible goal of this project would be to find a generic way to represent this:
that would allow the Razor team to reduce their tooling investment, while allowing
third parties the opportunity to enable the same sort of experiences 
(including 'Go to definition', 'Find all references' etc.) relatively cheaply.

The current thinking is to have some form of 'side-channel' available to
the generator. As the generator emits source text, it would indicate where 
in the original document this was generated from. This would allow the 
compiler API to track e.g. a generated `Symbol` as having an `OriginalDefinition` 
that represents a span of third party source text (such as a Razor tag in a
`.cshtml` file). 

We discussed embedding this directly in the source text via `#pragma` but 
this would require language changes and limit the feature to a specific version
of C#. Other considerations could be specially formed comments or `#if FALSE --`
blocks. In general a 'side-channel' approach seems preferable to specially crafted
grammar in the generated text. 

This is not necessarily a goal required for the success of Source Generators;
Razor’s language service can be updated to work with source generators 
if it proves to be infeasible, but it certainly something we want to consider 
as part of the work.

### MSBuild Integration

It is expected that generators will need some form of configuration system, and we intend to allow 
certain properties to flow through from MSBuild to facilitate this.

> **Note**: This is still under design and open to change.


### Performance targets

Ultimately, the performance of the feature is going to be somewhat dependent on the performance of the 
generators written by customers. Progressive opt-in, and build-time only by-default will allow the IDE
to mitigate many of the potential performance problems posed by third party generators. However, there
is still a risk that third-party generators will cause unacceptable performance problems for the IDE, 
and the design of the feature will need to keep this in mind.

For 1st party generators, especially Razor and Blazor, we aim at a minimum to match the existing
performance seen by users today. It is expected that even naïve generator-based implementations 
will perform significantly faster than the existing tooling, due to less communication overhead
and duplicated work, but improving the speed of these experiences is not a primary goal of this project.

### Language Changes

This design does not currently propose altering the language, it is purely a compiler feature. 
The previous design for source generators introduced the `replace` and `original` keywords. 
This proposal removes these, as the source generated is purely additional and so there is no
need for them. We expect that most scenarios are possible with the existing use of `partial` 
definitions; as a V1 we expect to ship in this state. If concrete scenarios are later shown
that can’t be achieved with the V1 approach we would consider allowing modification as a V2. 

## Use cases

We've identified several first and third party candidates that would benefit from source generators:

- ASP.Net: Improve startup time 
- Blazor and Razor: Massively reduce tooling burden 
- Azure Functions: regex compilation during startup
- Azure SDK
- [gRPC](https://docs.microsoft.com/en-us/aspnet/core/grpc/?view=aspnetcore-3.1)
- Resx file generation
- [System.CommandLine](https://github.com/dotnet/command-line-api)
- Serializers
- [SWIG](http://www.swig.org/)


## Discussion / Open Issues / TODOs:

**Interface vs Class for ISourceGenerator**: 

We discussed about this being an interface or class. Analyzers chose to have a abstract base class,
but we weren't sure what we'd end up a need since ultimately we only had one method on this.
Keeping it an interface also was more natural since we have other interfaces that 
implement this interface as well for optional light-up.

**IDependsOnCompilationGenerator**:

We did discuss if there should be an IDependsOnCompilationGenerator to formally state 
that you actually use a compilation. After all, if you don't use the compilation 
then we know your performance in the IDE is greatly simplified. However every 
scenario we've had for reading additional files has also needed the compilation, 
so we simply weren't sure what that was going to bring.

**Breakpoints in generated files**: 

Do we map this back to the in-memory file?

**Should generators be push or pull**:

Source generators are pull-based, analyzers are push-based (registration based). Should
we use a push-based model for generators as well?

- If we go down the push-based model, walking the tree should make sure to continue
to produce events for as many nodes as possible, even with errors, as generators
will often work in the presence 

- The events that we use today for analyzers may require may more work to produce,
since we expect analyzers to run during full compilation, while generators may
not want to even construct the symbol table

- The progressive-performance-opt-in model may work better in a push-based model,
since you would only register for the things you care about

**Should we share more with the analyzer type hierarchy?**:

We would still need to differentiate analyzers from generators, since
they would be generated at different times (generator diagnostics only on
the first compilation, analyzer diagnostics only on the second compilation)

**Can we predict how often some of our sample customers (Razor?) will have to run the generators?**:

They can't predict that right now, and the incorporation of timers into their
current generation makes it very difficult to predict the consequences of
only event-based generation

**Do we have a priority list of the most important customers?**:

No, we should work out priority in order to prioritize features.

**Security Review**:

Do generators create any new security risks not already posed via analyzers and nuget?
