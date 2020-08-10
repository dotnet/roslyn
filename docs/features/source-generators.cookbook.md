# Source Generators Cookbook

## Summary

> **Note**: The design for the source generator proposal is still under review. This document uses only one possible syntax, and
> it is expected to change without notice as the feature evolves.

This document aims to be a guide to help the creation of source generators by providing a series of guidelines for common patterns.
It also aims to set out what types of generators are possible under the current design, and what is expected to be explicitly out 
of scope in the final design of the shipping feature.

**This document expands on the details in the [full design document](source-generators.md), please ensure you have read that first.**

## Proposal

As a reminder, the high level design goals of source generators are:

- Generators produce one or more strings that represent C# source code to be added to the compilation.
- Explicitly _additive_ only. Generators can add new source code to a compilation but may **not** modify existing user code.
- Can produce diagnostics. When unable to generate source, the generator can inform the user of the problem.
- May access _additional files_, that is, non-C# source texts.
- Run _un-ordered_, each generator will see the same input compilation, with no access to files created by other source generators.
- A user specifies the generators to run via list of assemblies, much like analyzers.

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

## Conventions

TODO: List a set of general conventions that apply to all designs below. E.g. Re-using namespaces, generated file names etc.

## Designs

This section is broken down by user scenarios, with general solutions listed first, and more specific examples later on.

### Generated class

**User scenario:** As a generator author I want to be able to add a type to the compilation, that can be referenced by the user's code.

**Solution:** Have the user write the code as if the type was already present. Generate the missing type based on information available in the compilation.

**Example:**

Given the following user code:

```csharp
public partial class UserClass
{
    public void UserMethod()
    {
        // call into a generated method
        GeneratedNamespace.GeneratedClass.GeneratedMethod();
    }
}
```

Create a generator that will create the missing type when run:

```csharp
[Generator]
public class CustomGenerator : ISourceGenerator
{
    public void Initialize(InitializationContext context) {}

    public void Execute(SourceGeneratorContext context)
    {
        context.AddSource("myGeneratedFile.cs", SourceText.From(@"
namespace GeneratedNamespace
{
    public class GeneratedClass
    {
        public static void GeneratedMethod()
        {
            // generated code
        }
    }
}", Encoding.UTF8));
    }
}
```

### Additional file transformation

**User scenario:** As a generator author I want to be able to transform an external non-C# file into an equivalent C# representation.

**Solution:** Use the additional files property of the `SourceGeneratorContext` to retrieve the contents of the file, convert it to the C# representation and return it.

**Example:**

```csharp
[Generator]
public class FileTransformGenerator : ISourceGenerator
{
    public void Initialize(InitializationContext context) {}

    public void Execute(SourceGeneratorContext context)
    {
        // find anything that matches our files
        var myFiles = context.AnalyzerOptions.AdditionalFiles.Where(at => at.Path.EndsWith(".xml"));
        foreach (var file in myFiles)
        {
            var content = file.GetText(context.CancellationToken);

            // do some transforms based on the file context
            string output = MyXmlToCSharpCompiler.Compile(content);

            var sourceText = SourceText.From(output, Encoding.UTF8);

            context.AddSource($"{file.Name}generated.cs", sourceText);
        }
    }
}
```

### Augment user code

**User scenario:** As a generator author I want to be able to inspect and augment a user's code with new functionality.

**Solution:** Require the user to make the class you want to augment be a `partial class`, and mark it with e.g. a unique attribute, or name.
Register a `SyntaxReceiver` that looks for any classes marked for generation and records them. Retrieve the populated `SyntaxReceiver`
during the generation phase and use the recorded information to generate a matching `partial class` that
contains the additional functionality.

**Example:**

```csharp
public partial class UserClass
{
    public void UserMethod()
    {
        // call into a generated method inside the class
        this.GeneratedMethod();
    }
}
```

```csharp
[Generator]
public class AugmentingGenerator : ISourceGenerator
{
    public void Initialize(InitializationContext context)
    {
        // Register a factory that can create our custom syntax receiver
        context.RegisterForSyntaxNotifications(() => new MySyntaxReceiver());
    }

    public void Execute(SourceGeneratorContext context)
    {
        // the generator infrastructure will create a receiver and populate it
        // we can retrieve the populated instance via the context
        MySyntaxReceiver syntaxReceiver = (MySyntaxReceiver)context.SyntaxReceiver;

        // get the recorded user class
        ClassDeclarationSyntax userClass = syntaxReceiver.ClassToAugment;
        if (userClass is null)
        {
            // if we didn't find the user class, there is nothing to do
            return;
        }

        // add the generated implementation to the compilation
        SourceText sourceText = SourceText.From($@"
public partial class {userClass.Identifier}
{{
    private void GeneratedMethod()
    {{
        // generated code
    }}
}", Encoding.UTF8);
        context.AddSource("UserClass.Generated.cs", sourceText);
    }

    class MySyntaxReceiver : ISyntaxReceiver
    {
        public ClassDeclarationSyntax ClassToAugment { get; private set; }

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            // Business logic to decide what we're interested in goes here
            if (syntaxNode is ClassDeclarationSyntax cds &&
                cds.Identifier.ValueText == "UserClass")
            {
                ClassToAugment = cds;
            }
        }
    }
}
```

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

The generator can check the compilation for the presence of the `Newtonsoft.Json` assembly and issue a warning or error if not present.

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

```C#
using System.Linq;

[Generator]
public class SerializingGenerator : ISourceGenerator
{
    public void Execute(SourceGeneratorContext context)
    {
        // check that the users compilation references the expected library 
        if (!context.Compilation.ReferencedAssemblyNames.Any(ai => ai.Name.Equals("Newtonsoft.Json", StringComparison.OrdinalIgnoreCase)))
        {
            context.ReportDiagnostic(/*error or warning*/);
        }
    }

    public void Initialize(InitializationContext context)
    {
    }
}
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
public class JsonUsingGenerator : ISourceGenerator
{
    public void Execute(SourceGeneratorContext context)
    {
        // use the newtonsoft.json library, but don't add any source code that depends on it

        var serializedContent = Newtonsoft.Json.JsonConvert.SerializeObject(new { a = "a", b = 4 });

        context.AddSource("myGeneratedFile.cs", SourceText.From($@"
namespace GeneratedNamespace
{{
    public class GeneratedClass
    {{
        public static const SerializedContent = {serializedContent};
    }}
}}", Encoding.UTF8));

    }

    public void Initialize(InitializationContext context)
    {
    }
}
```

### Access Analyzer Config properties

**Implementation status:** Available from VS 16.7 preview3.

**User Scenarios:**

- As a generator author I want to access the analyzer config properties for a syntax tree or additional file.
- As a generator author I want to access key-value pairs that customize the generator output.
- As a user of a generator I want to be able to customize the generated code and override defaults.

**Solution**: Generators can access analyzer config values via the `AnalyzerConfigOptions` property of the `SourceGeneratorContext`. Analyzer config values can either be accessed in the context of a `SyntaxTree`, `AdditionalFile` or globally via `GlobalOptions`. Global options are 'ambient' in that they don't apply to any specific context, but will be included when requesting option within a specific context.

A generator is free to use a global option to customize its output. For example, consider a generator that can optionally emit logging. The author may choose to check the value of a global analyzer config value in order to control whether or not to emit the logging code. A user can then choose to enable the setting per project via an `.editorconfig` file:

```.editorconfig
mygenerator_emit_logging = true
```

```csharp
[Generator]
public class MyGenerator : ISourceGenerator
{
    public void Execute(SourceGeneratorContext context)
    {
        // control logging via analyzerconfig
        bool emitLogging = false;
        if (context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("mygenerator_emit_logging", out var emitLoggingSwitch))
        {
            emitLogging = emitLoggingSwitch.Equals("true", StringComparison.OrdinalIgnoreCase);
        }

        // add the source with or without logging...
    }

    public void Initialize(InitializationContext context)
    {
    }
}
```

### Consume MSBuild properties and metadata

**Implementation status:** Available from VS 16.7 preview3.

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

The value of `MyGenerator_EnableLogging` property will then be emitted to a generated analyzer config file before build, with a name of `build_property.MyGenerator_EnableLogging`. The generator is then able read this property from via the `AnalyzerConfigOptions` property of the `SourceGeneratorContext`:

```c#
context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.MyGenerator_EnableLogging", out var emitLoggingSwitch);
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
foreach (var file in context.AdditionalFiles)
{
    context.AnalyzerConfigOptions.GetOptions(file).TryGetValue("build_metadata.AdditionalFiles.MyGenerator_EnableLogging", out var perFileLoggingSwitch);
}
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
public class MyGenerator : ISourceGenerator
{
    public void Execute(SourceGeneratorContext context)
    {
        // global logging from project file
        bool emitLoggingGlobal = false;
        if(context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.MyGenerator_EnableLogging", out var emitLoggingSwitch))
        {
            emitLoggingGlobal = emitLoggingSwitch.Equals("true", StringComparison.OrdinalIgnoreCase);
        }

        foreach (var file in context.AdditionalFiles)
        {
            // allow the user to override the global logging on a per-file basis
            bool emitLogging = emitLoggingGlobal;
            if (context.AnalyzerConfigOptions.GetOptions(file).TryGetValue("build_metadata.AdditionalFiles.MyGenerator_EnableLogging", out var perFileLoggingSwitch))
            {
                emitLogging = perFileLoggingSwitch.Equals("true", StringComparison.OrdinalIgnoreCase);
            }

            // add the source with or without logging...
        }
    }

    public void Initialize(InitializationContext context)
    {
    }
}
```

### Participate in the IDE experience

**Implementation Status**: Not Implemented.

**User scenario:** As a generator author I want to be able to interactively regenerate code as the user is editing files.

**Solution:** We expect there to be an opt-in set of interactive callbacks that can be implemented to allow for progressively more complex generation strategies.
It is anticipated there will be a mechanism for providing symbol mapping for lighting up features such as 'Find all references'.

```csharp
[Generator]
public class InteractiveGenerator : ISourceGenerator
{
    public void Initialize(InitializationContext context)
    {
        // Register for additional file callbacks
        context.RegisterForAdditionalFileChanges(OnAdditionalFilesChanged);
    }

    public void Execute(SourceGeneratorContext context)
    {
        // generators must always support a total generation pass
    }

    public void OnAdditionalFilesChanged(AdditionalFilesChangedContext context)
    {
        // determine which file changed, and if it affects this generator
        // regenerate only the parts that are affected by this change.
    }
}
```

### Serialization

**User Scenario**

Serialization is often implemented using _dynamic analysis_, i.e. serializers often
use reflection to examine the runtime state of a given type and generate serialization
logic. This can be expensive and brittle. If the compile-time type and the runtime-type
are similar, it could be useful to move much of the cost to compile-time, instead of
run-time.

Source generators provide a way to do this. Since source generators can be delivered via
NuGet the same way analyzers can, we anticipate this would be a use-case for a source
generator library, as opposed to everyone building their own.

**Solution**

To start, the generator will need some way to discover which types are meant
to be serializable. One indicator could be an attribute, e.g.

```C#
[GeneratorSerializable]
partial class MyRecord
{
    public string Item1 { get; }
    public int Item2 { get; }
}
```

This attribute could also be used for [Participate in the IDE experience](#participate-in-the-ide-experience),
when the full scope of that feature is fully designed. In that scenario,
instead of the generator finding every type marked with the given attribute,
the compiler would notify the generator of every type marked with the given
attribute. For now we'll assume that the types are provided to us.

The first task is to decide what we want our serialization to return. Let's say
we do a simple JSON serialization that produces a string like the following

```json
{
    "Item1": "abc",
    "Item2": 11,
}
```

For that we could add a `Serialize` method to our record type like the following:

```C#
public string Serialize()
{
    var sb = new StringBuilder();
    sb.AppendLine("{");
    int indent = 8;

    // Body
    addWithIndent($"\"Item1\": \"{this.Item1.ToString()}\",");
    addWithIndent($"\"Item2\": {this.Item2.ToString()},");

    sb.AppendLine("}");

    return sb.ToString();

    void addWithIndent(string s)
    {
        sb.Append(' ', indent);
        sb.AppendLine(s);
    }
}
```

Obviously this is heavily simplified -- this example only handles the `string` and `int`
types properly, adds a trailing comma to the json output and has no error recovery, but
it should serve to demonstrate the kind of code a source generator could add to a compilation.

Our next task is design a generator to generate the above code, since the
above code is itself customized in the `// Body` section according to the
actual properties in the class. In other words, we need to generate the code
which will generate the JSON format. This is a generator-generator.

Let's start with a basic template. We are adding a full source generator, so we'll need
to generate a class with the same name as the input class, with a public method called
`Serialize`, and a filler area where we write out the properties.

```C#
string template = @"
using System.Text;
partial class {0}
{{
    public string Serialize()
    {{
        var sb = new StringBuilder();
        sb.AppendLine(""{{"");
        int indent = 8;

        // Body
{1}

        sb.AppendLine(""}}"");

        return sb.ToString();

        void addWithIndent(string s)
        {{
            sb.Append(' ', indent);
            sb.AppendLine(s);
        }}
    }}
}}";
```

Now that we know the general structure of the code, we need to examine the input
type and find all the right info to fill in. This information is all available in
a C# SyntaxTree in our example. Let's say we were given a `ClassDeclarationSyntax`
that was confirmed to have a generation attribute attached. Then we could grab the
name of the class and the name of it's properties as follows:

```C#
private static string Generate(ClassDeclarationSyntax c)
{
    var className = c.Identifier.ToString();
    var propertyNames = new List<string>();
    foreach (var member in c.Members)
    {
        if (member is PropertyDeclarationSyntax p)
        {
            propertyNames.Add(p.Identifier.ToString());
        }
    }
}
```

This is really all we need. If the serialized values of the properties
is their string value, the generated code just needs to call `ToString()` on
them. The only remaining question is what `using`s to put at the top of the file.
Since our template uses a string builder, we'll need `System.Text` for that, but
all other types appear to be primitives, so that's all we'll need. Putting it all
together:

```C#
private static string Generate(ClassDeclarationSyntax c)
{
    var sb = new StringBuilder();
    int indent = 8;
    foreach (var member in c.Members)
    {
        if (member is PropertyDeclarationSyntax p)
        {
            var name = p.Identifier.ToString();
            appendWithIndent($"addWithIndent($\"\\\"{name}\\\": ");
            if (p.Type.ToString() != "int")
            {
                sb.Append("\\\"");
            }
            sb.Append($"{{this.{name}.ToString()}}");
            if (p.Type.ToString() != "int")
            {
                sb.Append("\\\"");
            }
            sb.AppendLine(",\");");
            break;
        }
    }

    return $@"
using System.Text;
partial class {c.Identifier.ToString()}
{{
    public string Serialize()
    {{
        var sb = new StringBuilder();
        sb.AppendLine(""{{"");
        int indent = 8;

        // Body
{sb.ToString()}

        sb.AppendLine(""}}"");

        return sb.ToString();

        void addWithIndent(string s)
        {{
            sb.Append(' ', indent);
            sb.AppendLine(s);
        }}
    }}
}}";
    void appendWithIndent(string s)
    {
        sb.Append(' ', indent);
        sb.Append(s);
    }
}
```

This ties cleanly into the other serialization examples. By finding all the
appropriate class declarations in the Compilation's SyntaxTrees and passing
them to the above Generate method we can build new partial classes for each
type that opt-ed in to generated serialization. Unlike other technologies,
this serialization mechanism happens entirely at compile time and can be
specialized exactly to what was written in the user class.

### Auto interface implementation

TODO:

## Open Issues

This section track other miscellaneous TODO items:

**Framework targets**: May want to mention if we have framework requirements for the generators, e.g. they must target netstandard2.0 or similar.

**Conventions**: (See TODO in [conventions](#conventions) section above). What standard conventions are we suggesting to users?

**Partial methods**: Should we provide a scenario that includes partial methods? Reasons:

- Control of name. The developer can control the name of the member
- Generation is optional/depending on other state. Based on other information, generator might decide that the method isn't needed.

**Feature detection**: Show how to create a generator that relies on specific target framework features, without depending on the TargetFramework property.