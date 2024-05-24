# Incremental Generators Cookbook

## Summary

This document aims to be a guide to help the creation of source generators by providing a series of guidelines for common patterns.
It also aims to set out what types of generators are possible under the current design, and what is expected to be explicitly out 
of scope in the final design of the shipping feature.

**This document expands on the details in the [full design document](incremental-generators.md), please ensure you have read that first.**

## Table of contents

- [Incremental Generators Cookbook](#incremental-generators-cookbook)
  - [Summary](#summary)
  - [Table of contents](#table-of-contents)
  - [Proposal](#proposal)
  - [Out of scope designs](#out-of-scope-designs)
    - [Language features](#language-features)
    - [Code rewriting](#code-rewriting)
  - [Conventions](#conventions)
    - [Pipeline model design](#pipeline-model-design)
    - [Use `ForAttributeWithMetadataName`](#use-forattributewithmetadataname)
    - [Use an indented text writer, not `SyntaxNode`s, for generation](#use-an-indented-text-writer-not-syntaxnodes-for-generation)
  - [Designs](#designs)
    - [Generated class](#generated-class)
    - [Additional file transformation](#additional-file-transformation)
    - [Augment user code](#augment-user-code)
    - [Issue Diagnostics](#issue-diagnostics)
    - [INotifyPropertyChanged](#inotifypropertychanged)
    - [Package a generator as a NuGet package](#package-a-generator-as-a-nuget-package)
    - [Use functionality from NuGet packages](#use-functionality-from-nuget-packages)
    - [Access Analyzer Config properties](#access-analyzer-config-properties)
    - [Consume MSBuild properties and metadata](#consume-msbuild-properties-and-metadata)
    - [Unit Testing of Generators](#unit-testing-of-generators)
    - [Auto interface implementation](#auto-interface-implementation)
  - [Breaking Changes:](#breaking-changes)
  - [Open Issues](#open-issues)

## Proposal

As a reminder, the high level design goals of source generators are:

- Generators produce one or more strings that represent C# source code to be added to the compilation.
- Explicitly _additive_ only. Generators can add new source code to a compilation but may **not** modify existing user code.
- May access _additional files_, that is, non-C# source texts.
- Run _un-ordered_, each generator will see the same input compilation, with no access to files created by other source generators.
- A user specifies the generators to run via list of assemblies, much like analyzers.
- Generators create a pipeline, starting from base input sources and mapping them to the output they wish to produce. The more exposed,
  properly equatable states exist, the earlier the compiler will be able to cut off changes and reuse the same output.

## Out of scope designs

We will briefly look at the non-solvable problems as examples of the kind of problems source generators are *not* designed to solve:

### Language features

Source generators are not designed to replace new language features: for instance one could imagine [records](records.md) being implemented as a source generator
that converts the specified syntax to a compilable C# representation.

We explicitly consider this to be an anti-pattern; the language will continue to evolve and add new features, and we don't expect source generators to be
a way to enable this. Doing so would create new 'dialects' of C# that are incompatible with the compiler without generators. Further, because generators,
by design, cannot interact with each other, language features implemented in this way would quickly become incompatible with other additions to the language.

### Code rewriting

There are many post-processing tasks that users perform on their assemblies today, which here we define broadly as 'code rewriting'. These include, but
are not limited to:

- Optimization
- Logging injection
- IL Weaving
- Call site re-writing

While these techniques have many valuable use cases, they do not fit into the idea of *source generation*. They are, by definition, code altering operations
which are explicitly ruled out by the source generators proposal.

There are already well supported tools and techniques for achieving these kinds of operations, and the source generators proposal is not aimed at replacing them.
We are exploring approaches for call site rewriting (see [interceptors.md](interceptors.md)), but those features are experimental and may change significantly
or even be removed.

## Conventions

### Pipeline model design

As a general guideline, source generator pipelines need to pass along models that are _value equatable_. This is critical to the incrementality of an
`IIncrementalGenerator`; as soon as a pipeline step returns the same information that it returned in the previous run, the generator driver can stop running
the generator and reuse the same cached data that the generator produced in the previous run. Most times a generator is triggered (particularly generators that
need to look at type or method definitions, using `ForAttributeWithMetadataName`) the edit that triggered the generator will not actually have affected the
things that your generator is looking at. However, because semantics can change on basically any edit, the generator driver _must_ rerun your generator again
to ensure that this is the case. If your generator then produces a model with the same values as it did previously, this short-circuits the pipeline and allows
us to avoid a lot of work. Here are some general guidelines around designing your models to ensure that you maintain this equality:

* Use `record`s, rather than `class`es, so that value equality is generated for you.
* Symbols (`ISymbol` and anything that inherits from that interface) are never equatable, and including them in your model can potentially root old compilations
  and force Roslyn to hold onto lots of memory that it could otherwise free. Never put these in your model types. Instead, extract the information you need from
  the symbols you inspect to an equatable representation: `string`s often work quite well here.
* `SyntaxNode`s are also usually not equatable between runs. They're not as strongly discouraged from the initial stages of a pipeline as symbols, and an example
  [later down](#access-analyzer-config-properties) shows a case where you will need to include a `SyntaxNode` in model. They also don't potentially root as much
  memory as symbols will. However, any edit in a file will ensure that all `SyntaxNode`s from that file are no longer equatable, so they should be removed from
  your models as soon as possible.
* The previous bullet applies to `Location`s as well.
* Be careful of collection types in your models. Most built-in collection types in .NET do not do value equality by default. Arrays, `ImmutableArray<T>` and
  `List<T>`, for example, use reference equality, not value equality. We suggest that most generator authors use a wrapper type around arrays to augment them
  with value-based equality.

### Use `ForAttributeWithMetadataName`

We highly recommend that all generator authors that need to inspect syntax do so by using a marker attribute to indicate the types or members that need to be
inspected. This has multiple benefits, both for you as an author, and also for your users:

* As an author, you can use `SyntaxProvider.ForAttributeWithMetadataName`. This utility method is at least 99x more efficient than `SyntaxProvider.CreateSyntaxProvider`,
  and in many cases even more efficient. This will help you avoid causing performance issues for your users in editors.
* Your users can clearly indicate that they _intend_ to use your source generator. This intention is extremely helpful for designing a good user experience; it
  means that you can author Roslyn analyzers to help your users when they intended to use your generator but violated your rules in some fashion. For example, if
  you are generating some method body, and your generator requires that the user return a specific type, the presence of a `GenerateMe` attribute means you can
  write an analyzer to tell the user if their method declaration returns something that it shouldn't.

### Use an indented text writer, not `SyntaxNode`s, for generation

We do not recommend generating `SyntaxNode`s when generating syntax for `AddSource`. Doing so can be complex, and it can be difficult to format it well; calling
`NormalizeWhitespace` is often quite expensive, and the API is not really designed for this use-case. Additionally, to ensure immutability guarantees, `AddSource` does
not accept `SyntaxNode`s. It instead requires getting the `string` representation and putting that into a `SourceText`. Instead of `SyntaxNode`, we recommend using a
wrapper around `StringBuilder` that will keep track of indent level and prepend the right amount of indentation when `AppendLine` is called. See
[this](https://github.com/dotnet/roslyn/issues/52914#issuecomment-1732680995) conversation on the performance of `NormalizeWhitespace` for more examples, performance
measurements, and discussion on why we don't believe that `SyntaxNode`s are a good abstraction for this use case.

## Designs

This section is broken down by user scenarios, with general solutions listed first, and more specific examples later on.

### Generated class

**User scenario:** As a generator author I want to be able to add a type to the compilation, that can be referenced by the user's code. Common use cases include
creating an attribute that will be used to drive other source generator steps.

**Solution:** Have the user write the code as if the type was already present. Generate the missing type based on information available in the compilation using
the `RegisterPostInitializationOutput` step.

**Example:**

Given the following user code:

```csharp
public partial class UserClass
{
    [GeneratedNamespace.GeneratedAttribute]
    public partial void UserMethod();
}
```

Create a generator that will create the missing type when run:

```csharp
[Generator]
public class CustomGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(static postInitializationContext => {
            postInitializationContext.AddSource("myGeneratedFile.cs", SourceText.From("""
                using System;

                namespace GeneratedNamespace
                {
                    internal sealed class GeneratedAttribute : Attribute
                    {
                    }
                }
                """, Encoding.UTF8));
        });
    }
}
```

**Alternative Solution**: If you are also providing a library to your users, in addition to a source generator, simply have that library include the
attribute definition.

### Additional file transformation

**User scenario:** As a generator author I want to be able to transform an external non-C# file into an equivalent C# representation.

**Solution:** Use the `AdditionalTextsProvider` to filter for and retrieve your files. Transform them into the code you care about, then
register that code with the solution.

**Example:**

```csharp
[Generator]
public class FileTransformGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var pipeline = context.AdditionalTextsProvider
            .Where(static (text) => text.Path.EndsWith(".xml"))
            .Select(static (text, cancellationToken) =>
            {
                var name = Path.GetFileName(text.Path);
                var code = MyXmlToCSharpCompiler.Compile(text.GetText(cancellationToken));
                return (name, code);
            });

        context.RegisterSourceOutput(pipeline,
            static (context, pair) => 
                // Note: this AddSource is simplified. You will likely want to include the path in the name of the file to avoid
                // issues with duplicate file names in different paths in the same project.
                context.AddSource($"{pair.name}generated.cs", SourceText.From(pair.code, Encoding.UTF8)));
    }
}
```

### Augment user code

**User scenario:** As a generator author I want to be able to inspect and augment a user's code with new functionality.

**Solution:** Require the user to make the class you want to augment be a `partial class`, and mark it with a unique attribute.
Provide that attribute in a `RegisterPostInitializationOutput` step. Register for callbacks on that attribute with
`ForAttributeWithMetadataName` to collect the information needed to generate code, and use tuples (or create an equatable model)
 to pass along that information. That information should be extracted from syntax and symbols; **do not put syntax or symbols into
 your models**.

**Example:**

```csharp
public partial class UserClass
{
    [Generate]
    public partial void UserMethod();
}
```

```csharp
[Generator]
public class AugmentingGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(static postInitializationContext =>
            postInitializationContext.AddSource("myGeneratedFile.cs", SourceText.From("""
                using System;
                namespace GeneratedNamespace
                {
                    [AttributeUsage(AttributeTargets.Method)]
                    internal sealed class GeneratedAttribute : Attribute
                    {
                    }
                }
                """, Encoding.UTF8));

        var pipeline = context.SyntaxProvider.ForAttributeWithMetadataName(
            fullyQualifiedMetadataName: "GeneratedNamespace.GeneratedAttribute",
            predicate: static (syntaxNode, cancellationToken) => syntaxNode is BaseMethodDeclarationSyntax,
            transform: static (context, cancellationToken) =>
            {
                var containingClass = context.TargetSymbol.ContainingType;
                return new Model(
                    // Note: this is a simplified example. You will also need to handle the case where the type is in a global namespace, nested, etc.
                    Namespace: containingClass.ContainingNamespace?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted)),
                    ClassName: containingClass.Name,
                    MethodName: context.TargetSymbol.Name);
            }
        );

        context.RegisterSourceOutput(pipeline, static (context, model) =>
        {
            var sourceText = SourceText.From($$"""
                namespace {{model.Namespace}};
                partial class {{model.ClassName}}
                {
                    partial void {{model.MethodName}}()
                    {
                        // generated code
                    }
                }
                """, Encoding.UTF8);

            context.AddSource($"{model.ClassName}_{model.MethodName}.g.cs", sourceText);
        });
    }

    private record Model(string Namespace, string ClassName, string MethodName);
}
```

### Issue Diagnostics

**User Scenario:** As a generator author I want to be able to add diagnostics to the user's compilation.

**Solution:** We do not recommend issuing diagnostics within generators. It is possible, but doing so without breaking incrementality is advanced topic
beyond the scope of this cookbook. Instead, we suggest writing a separate analyzer for reporting diagnostics.

### INotifyPropertyChanged

**User scenario:** As a generator author I want to be able to implement the `INotifyPropertyChanged` pattern automatically for a user.

**Solution:** The design tenant 'Explicitly additive only' seems to be at direct odds with the ability to implement this, and appears to call for user code modification.
However we can instead take advantage of explicit fields and instead of *editing* the users properties, directly provide them for listed fields.

**Example:**

Given a user class such as:

```csharp
using AutoNotify;

public partial class UserClass
{
    [AutoNotify]
    private bool _boolProp;

    [AutoNotify(PropertyName = "Count")]
    private int _intProp;
}
```

A generator could produce the following:

```csharp
using System;
using System.ComponentModel;

namespace AutoNotify
{
    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    sealed class AutoNotifyAttribute : Attribute
    {
        public AutoNotifyAttribute()
        {
        }
        public string PropertyName { get; set; }
    }
}


public partial class UserClass : INotifyPropertyChanged
{
    public bool BoolProp
    {
        get => _boolProp;
        set
        {
            _boolProp = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("UserBool"));
        }
    }

    public int Count
    {
        get => _intProp;
        set
        {
            _intProp = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Count"));
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;
}
```

### Package a generator as a NuGet package

**User scenario**: As a generator author I want to package my generator as a NuGet package for consumption.

**Solution:** Generators can be packaged using the same method as an Analyzer would.
Ensure the generator is placed in the `analyzers\dotnet\cs` folder of the package for it to be automatically added to the users project on install.

For example, to turn your generator project into a NuGet package at build, add the following to your project file:

```xml
  <PropertyGroup>
    <GeneratePackageOnBuild>true</GeneratePackageOnBuild> <!-- Generates a package at build -->
    <IncludeBuildOutput>false</IncludeBuildOutput> <!-- Do not include the generator as a lib dependency -->
  </PropertyGroup>

  <ItemGroup>
    <!-- Package the generator in the analyzer directory of the nuget package -->
    <None Include="$(OutputPath)\$(AssemblyName).dll" Pack="true" PackagePath="analyzers/dotnet/cs" Visible="false" />
  </ItemGroup>
```

### Use functionality from NuGet packages

**User Scenario:** As a generator author I want to rely on functionality provided in NuGet packages inside my generator.

**Solution:** It is possible to depend on NuGet packages inside of a generator, but special consideration has to be taken for distribution.

Any *runtime* dependencies, that is, code that the end users program will need to rely on, can simply be added as a dependency of the generator NuGet package via the usual referencing mechanism.

For example, consider a generator that creates code that relies on `Newtonsoft.Json`. The generator does not directly use the dependency, it just emits code that relies on the library being referenced in the users compilation. The author would add a reference to `Newtonsoft.Json` as a public dependency, and when the user adds the generator package it will referenced automatically.

```xml
<Project>
  <PropertyGroup>
    <GeneratePackageOnBuild>true</GeneratePackageOnBuild> <!-- Generates a package at build -->
    <IncludeBuildOutput>false</IncludeBuildOutput> <!-- Do not include the generator as a lib dependency -->
  </PropertyGroup>

  <ItemGroup>
    <!-- Take a public dependency on Json.Net. Consumers of this generator will get a reference to this package -->
    <PackageReference Include="Newtonsoft.Json" Version="12.0.1" />

    <!-- Package the generator in the analyzer directory of the nuget package -->
    <None Include="$(OutputPath)\$(AssemblyName).dll" Pack="true" PackagePath="analyzers/dotnet/cs" Visible="false" />
  </ItemGroup>
</Project>
```

However, any *generation-time* dependencies, that is, used by the generator while it is is running and generating code, must be packaged directly alongside the generator assembly inside the generator NuGet package. There are no automatic facilities for this, and you will need to manually specify the dependencies to include.

Consider a generator that uses `Newtonsoft.Json` to encode something to json during the generation pass, but does not emit any code the relies on it being present at runtime. The author would add a reference to `Newtonsoft.Json` but make all of its assets *private*; this ensures the consumer of the generator does not inherit a dependency on the library.

The author would then have to package the `Newtonsoft.Json` library alongside the generator inside of the NuGet package. This can be achieved in the following way: set the dependency to generate a path property by adding `GeneratePathProperty="true"`. This will create a new MSBuild property of the format `PKG<PackageName>` where `<PackageName>` is the package name with `.` replaced by `_`. In our example there would be an MSBuild property called `PKGNewtonsoft_Json` with a value that points to the path on disk of the binary contents of the NuGet files. We can then use that to add the binaries to the resulting NuGet package as we do with the generator itself:

```xml
<Project>
  <PropertyGroup>
    <GeneratePackageOnBuild>true</GeneratePackageOnBuild> <!-- Generates a package at build -->
    <IncludeBuildOutput>false</IncludeBuildOutput> <!-- Do not include the generator as a lib dependency -->
  </PropertyGroup>

  <ItemGroup>
    <!-- Take a private dependency on Newtonsoft.Json (PrivateAssets=all) Consumers of this generator will not reference it.
         Set GeneratePathProperty=true so we can reference the binaries via the PKGNewtonsoft_Json property -->
    <PackageReference Include="Newtonsoft.Json" Version="12.0.1" PrivateAssets="all" GeneratePathProperty="true" />

    <!-- Package the generator in the analyzer directory of the nuget package -->
    <None Include="$(OutputPath)\$(AssemblyName).dll" Pack="true" PackagePath="analyzers/dotnet/cs" Visible="false" />

    <!-- Package the Newtonsoft.Json dependency alongside the generator assembly -->
    <None Include="$(PkgNewtonsoft_Json)\lib\netstandard2.0\*.dll" Pack="true" PackagePath="analyzers/dotnet/cs" Visible="false" />
  </ItemGroup>
</Project>
```

```C#
[Generator]
public class JsonUsingGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var pipeline = context.AdditionalTextsProvider.Select(static (text, cancellationToken) =>
        {
            if (!text.Path.EndsWith("*.json"))
            {
                return default;
            }

            return (Name: Path.GetFileName(text.Path), Value: Newtonsoft.Json.JsonConvert.DeserializeObject<MyObject>(text.GetText(cancellationToken).ToString()));
        })
        .Where((pair) => pair is not ((_, null) or (null, _)));

        context.RegisterSourceOutput(pipeline, static (context, pair) =>
        {
            var sourceText = SourceText.From($$"""
                namespace GeneratedNamespace
                {
                    internal sealed class GeneratedClass
                    {
                        public static const (int A, int B) SerializedContent = ({{pair.A}}, {{pair.B}});
                    }
                }
                """, Encoding.UTF8);

            context.AddSource($"{pair.Name}generated.cs", sourceText)
        });
    }

    record MyObject(int A, int B);
}
```

### Access Analyzer Config properties

**User Scenarios:**

- As a generator author I want to access the analyzer config properties for a syntax tree or additional file.
- As a generator author I want to access key-value pairs that customize the generator output.
- As a user of a generator I want to be able to customize the generated code and override defaults.

**Solution**: Generators can access analyzer config values via the `AnalyzerConfigOptionsProvider`. Analyzer config values can either be accessed in the context of a `SyntaxTree`, `AdditionalFile` or globally via `GlobalOptions`. Global options are 'ambient' in that they don't apply to any specific context, but will be included when requesting option within a specific context.

Note that this is one of the few cases that it is necessary to put a `SyntaxNode` into a pipeline, as you need the tree in order to get the generator option.
Try to get the `SyntaxNode` out of the pipeline as fast as possible to avoid making the model not correctly equatable.

A generator is free to use a global option to customize its output. For example, consider a generator that can optionally emit logging. The author may choose to check the value of a global analyzer config value in order to control whether or not to emit the logging code. A user can then choose to enable the setting per project via an `.globalconfig` file:

```.globalconfig
mygenerator_emit_logging = true
```

```csharp
[Generator]
public class MyGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {

        var userCodePipeline = context.SyntaxProvider.ForAttributeWithMetadataName(... /* collect user code info */);
        var emitLoggingPipeline = context.AnalyzerConfigOptionsProvider.Select(static (options, cancellationToken) =>
            options.GlobalOptions.TryGetValue("mygenerator_emit_logging", out var emitLoggingSwitch)
                ? emitLoggingSwitch.Equals("true", StringComparison.InvariantCultureIgnoreCase)
                : false); // Default

        context.RegisterSourceOutput(userCodePipeline.Combine(emitLoggingPipeline), (context, pair) => /* emit code */);
    }
}
```

### Consume MSBuild properties and metadata

**User Scenarios:**

- As a generator author I want to make decisions based on the values contained in the project file
- As a user of a generator I want to be able to customize the generated code and override defaults.

**Solution:** MSBuild will automatically translate specified properties and metadata into a global analyzer config that can be read by a generator. A generator author specifies the properties and metadata they want to make available by adding items to the `CompilerVisibleProperty` and `CompilerVisibleItemMetadata` item groups. These can be added via a props or targets file when packaging the generator as a NuGet package.

For example, consider a generator that creates source based on additional files, and wants to allow a user to enable or disable logging via the project file. The author would specify in their props file that they want to make the specified MSBuild property visible to the compiler:

```xml
<ItemGroup>
    <CompilerVisibleProperty Include="MyGenerator_EnableLogging" />
</ItemGroup>
```

The value of `MyGenerator_EnableLogging` property will then be emitted to a generated analyzer config file before build, with a name of `build_property.MyGenerator_EnableLogging`. The generator is then able read this property from via the `GlobalOptions` property of the `AnalyzerConfigOptionsProvider` pipeline:

```c#
context.AnalyzerConfigOptionsProvider.Select((provider, ct) =>
    provider.GlobalOptions.TryGetValue("build_property.MyGenerator_EnableLogging", out var emitLoggingSwitch)
        ? emitLoggingSwitch.Equals("true", StringComparison.InvariantCultureIgnoreCase) : false);
```

A user can thus enable, or disable logging, by setting a property in their project file.

Now, consider that the generator author wants to optionally allow opting in/out of logging on a per-additional file basis. The author can request that MSBuild emit the value of metadata for the specified file, by adding to the `CompilerVisibleItemMetadata` item group. The author specifies both the MSBuild itemType they want to read the metadata from, in this case `AdditionalFiles`, and the name of the metadata that they want to retrieve for them.

```xml
<ItemGroup>
    <CompilerVisibleItemMetadata Include="AdditionalFiles" MetadataName="MyGenerator_EnableLogging" />
</ItemGroup>
```

This value of `MyGenerator_EnableLogging` will be emitted to a generated analyzer config file, for each of the additional files in the compilation, with an item name of `build_metadata.AdditionalFiles.MyGenerator_EnableLogging`. The generator can read this value in the context of each additional file:

```cs
context.AdditionalFilesProvider
       .Combine(context.AnalyzerConfigOptionsProvider)
       .Select((pair, ctx) =>
           pair.Right.GetOptions(pair.Left).TryGetValue("build_metadata.AdditionalFiles.MyGenerator_EnableLogging", out var perFileLoggingSwitch)
               ? perFileLoggingSwitch : false);
```

In the users project file, the user can now annotate the individual additional files to say whether or not they want to enable logging:

```xml
<ItemGroup>
    <AdditionalFiles Include="file1.txt" />  <!-- logging will be controlled by default, or global value -->
    <AdditionalFiles Include="file2.txt" MyGenerator_EnableLogging="true" />  <!-- always enable logging for this file -->
    <AdditionalFiles Include="file3.txt" MyGenerator_EnableLogging="false" /> <!-- never enable logging for this file -->
</ItemGroup>
```

**Full Example:**

MyGenerator.props:

```xml
<Project>
    <ItemGroup>
        <CompilerVisibleProperty Include="MyGenerator_EnableLogging" />
        <CompilerVisibleItemMetadata Include="AdditionalFiles" MetadataName="MyGenerator_EnableLogging" />
    </ItemGroup>
</Project>
```

MyGenerator.csproj:

```xml
<Project>
  <PropertyGroup>
    <GeneratePackageOnBuild>true</GeneratePackageOnBuild> <!-- Generates a package at build -->
    <IncludeBuildOutput>false</IncludeBuildOutput> <!-- Do not include the generator as a lib dependency -->
  </PropertyGroup>

  <ItemGroup>
    <!-- Package the generator in the analyzer directory of the nuget package -->
    <None Include="$(OutputPath)\$(AssemblyName).dll" Pack="true" PackagePath="analyzers/dotnet/cs" Visible="false" />

    <!-- Package the props file -->
    <None Include="MyGenerator.props" Pack="true" PackagePath="build" Visible="false" />
  </ItemGroup>
</Project>
```

MyGenerator.cs:

```csharp
[Generator]
public class MyGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var emitLoggingPipeline = context.AdditionalTextsProvider
            .Combine(context.AnalyzerConfigOptionsProvider)
            .Select((pair, ctx) =>
                pair.Right.GetOptions(pair.Left).TryGetValue("build_metadata.AdditionalFiles.MyGenerator_EnableLogging", out var perFileLoggingSwitch)
                ? perFileLoggingSwitch.Equals("true", StringComparison.OrdinalIgnoreCase)
                : pair.Right.GlobalOptions.TryGetValue("build_property.MyGenerator_EnableLogging", out var emitLoggingSwitch)
                  ? emitLoggingSwitch.Equals("true", StringComparison.OrdinalIgnoreCase)
                  : false);

        var sourcePipeline = context.AdditionalTextsProvider.Select((file, ctx) => /* Gather build info */);

        context.RegisterSourceOutput(sourcePipeline.Combine(emitLoggingPipeline), (context, pair) => /* Add source */);
    }
}
```

### Unit Testing of Generators

**User scenario**: As a generator author, I want to be able to unit test my generators to make development easier and ensure correctness.

**Solution A**:

The recommended approach is to use [Microsoft.CodeAnalysis.Testing](https://github.com/dotnet/roslyn-sdk/tree/main/src/Microsoft.CodeAnalysis.Testing#microsoftcodeanalysistesting) packages:

- `Microsoft.CodeAnalysis.CSharp.SourceGenerators.Testing.MSTest`
- `Microsoft.CodeAnalysis.VisualBasic.SourceGenerators.Testing.MSTest`
- `Microsoft.CodeAnalysis.CSharp.SourceGenerators.Testing.NUnit`
- `Microsoft.CodeAnalysis.VisualBasic.SourceGenerators.Testing.NUnit`
- `Microsoft.CodeAnalysis.CSharp.SourceGenerators.Testing.XUnit`
- `Microsoft.CodeAnalysis.VisualBasic.SourceGenerators.Testing.XUnit`

TODO: https://github.com/dotnet/roslyn/issues/72149

### Auto interface implementation

TODO: https://github.com/dotnet/roslyn/issues/72149

## Breaking Changes:

* None currently

## Open Issues

This section track other miscellaneous TODO items:

**Framework targets**: May want to mention if we have framework requirements for the generators, e.g. they must target netstandard2.0 or similar.

**Conventions**: (See TODO in [conventions](#conventions) section above). What standard conventions are we suggesting to users?

**Feature detection**: Show how to create a generator that relies on specific target framework features, without depending on the TargetFramework property.
