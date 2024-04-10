<p align="center">
<img width="450" src="https://github.com/postsharp/Metalama/raw/master/images/metalama-by-postsharp.svg" alt="Metalama logo">
</p>

The Metalama Compiler is a fork of [Roslyn](https://github.com/dotnet/Roslyn) that introduces extension points enabling analyzer projects to execute arbitrary transformations via the `ISourceTransformer` interface.

The Metalama Compiler is actively and professionally maintained by [PostSharp Technologies](https://www.postsharp.net/).

This repository is a part of [Metalama](https://github.com/postsharp/Metalama), a high-level meta-programming framework for C#. The first pillar of Metalama, boilerplate reduction, relies on the Metalama Compiler to implement aspect-oriented programming. Metalama utilizes several other Roslyn extension points: analyzers, diagnostic suppressors, source generators, code fix providers, and code refactoring providers. However, no extension point was previously available for transforming source code during compilation, hence the reliance on this fork.

For a detailed comparison of the modifications in the Metalama Compiler compared to the original Roslyn, please refer to [this article](docs-Metalama/Modifications.md).

## Features

The Metalama Compiler introduces the following features to Roslyn:

* **Source transformers.** This feature allows analyzer projects to execute arbitrary transformations of source code during compilation via the [ISourceTransformer](https://doc.metalama.net/api/metalama_compiler_isourcetransformer) interface.

* **Ordering.** Multiple source transformers can be ordered using the [TransformerOrderAttribute](https://doc.metalama.net/api/metalama_compiler_transformerorderattribute) assembly-level custom attribute or the `MetalamaCompilerTransformerOrder` MSBuild property (see below).

    > [!Warning]
    > Overloading a project with too many source transformers can lead to performance issues.

    Analyzers can be ordered before or after transformers using the `MetalamaSourceOnlyAnalyzers` property (see below).

* **Source mapping.** Diagnostics and PDBs are mapped to the source code by default. This behavior can be altered using one of the new MSBuild properties defined by the Metalama Compiler (see below). This is arguably the most complex feature of the Metalama Compiler.

* **Managed resources.** The `TransformerContext` argument of `ISourceTransformer.Execute` provides read-write access to managed resources, enabling the addition of resources to the reference assembly.

## Building

To build the Metalama Compiler, clone the repository and execute the following command from the command line:

```powershell
.\Build.ps1 build
```

## Documentation

### Getting started

### Step 1. Building a source transformer

To create a source transformer:

1. Establish a new .NET Standard 2.0 class library project and add a reference to the `Metalama.Compiler.Sdk` package.
2. Create a new class that implements the `ISourceTransformer` interface.
3. Add the `[Transformer]` custom attribute to this class.
4. Implement the `Execute` method. This method receives a `TransformerContext` object. Use this object to inspect the current compilation, add syntax trees, modify syntax trees, report diagnostics, or suppress diagnostics.

```cs
using Metalama.Compiler;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Metalama.SourceTransformer
{
    [Transformer]
    internal class SourceTransformer : ISourceTransformer
    {
        public void Execute( TransformerContext context )
        {

        }
    }
}
```

### Step 2. Packaging a source transformer

The packaging of your transformer should adhere to the same rules as analyzers and source generators. That is, your output `dll` should be in the `analyzers/dotnet/cs` folder, not under `lib`.

Follow [this example](https://github.com/dotnet/roslyn-sdk/blob/main/samples/CSharp/Analyzers/Analyzers.Implementation/Analyzers.CSharp.csproj) from the Roslyn documentation:

1. Define the `IncludeBuildOutput=False` property to prevent the project output from being added to the `lib` directory of the package.

2. Use the following to include the output under the `analyzers/dotnet/cs`:

    ```xml
    <ItemGroup>
     <None Include="$(OutputPath)\$(AssemblyName).dll"
           Pack="true"
           PackagePath="analyzers/dotnet/cs"
           Visible="false" />
    </ItemGroup>
    ```


### Step 3. Using a source transformer

In any project that uses the source transformer:

* Add the package containing your transformer.

    If you need a `ProjectReference` instead of a `PackageReference`, use the following code:

    ```xml
    <ItemGroup>
        <ProjectReference Include="..\PathTo\SourceGenerator.csproj"
                          OutputItemType="Analyzer"
                          ReferenceOutputAssembly="false" />
    </ItemGroup>

    ```


* Add the `Metalama.Compiler` package. You can specify `PrivateAssets="all"` if you don't want the package to flow to the projects referencing this project.

### API Documentation
See [Metalama.Compiler](https://doc.metalama.net/api/metalama_compiler)

### Architecture

The public API of the extensions introduced by the Metalama Compiler is included in the `Metalama.Compiler.Sdk` package. This package contains only interfaces and stubs.

The `Metalama.Compiler` package replaces the Roslyn compiler that comes with Visual Studio or the .NET SDK, but only for projects that reference the package. `Metalama.Compiler` is actually a fork of `Microsoft.Net.Compilers.Toolset`. It contains the implementation of `Metalama.Compiler.Sdk`.

### MSBuild properties

The Metalama Compiler can be configured using several custom MSBuild properties from the `csproj` file of a user project:

* `MetalamaEmitCompilerTransformedFiles`: Set this property to `true` to write transformed files to the `obj/Debug/metalama` or `obj/Release/metalama` directory. The default is `true` if `MetalamaDebugTransformedCode` is enabled and `false` otherwise.
* `MetalamaCompilerTransformedFilesOutputPath`: This property can be used to set the directory where transformed files are written instead of `obj/Debug`.
* `MetalamaCompilerTransformerOrder`: This property is a semicolon-separated list of namespace-qualified names of transformers. It is necessary for setting the execution order of transformers if the order has not been fully specified by the transformers using [`[TransformerOrder]`](API.md#TransformerOrderAttribute).
* `MetalamaDebugTransformedCode`: Set this property to `true` to produce diagnostics and PDB sequence points in transformed code. Otherwise, locations are attempted to be mapped to the original user code. The default is `false`.
* `MetalamaDebugCompiler`: Set this property to `true` to trigger `Debugger.Launch()`.
* `MetalamaSourceOnlyAnalyzers` contains the list of analyzers that must execute on the source code instead of the transformed code. This is a comma-separated list that can include the assembly name, an exact namespace (namespace inheritance rules do not apply), or the exact full name of an analyzer type.

> [!Note]
 > If `MetalamaDebugTransformedCode` is set to `true`, but `MetalamaEmitCompilerTransformedFiles` is explicitly set to `false` (and no custom `CompilerTransformedFilesOutputPath` is provided), then transformed sources should be used for debugging and diagnostics, but cannot be written to disk.
>
> For debugging, this means transformed sources are embedded into the PDB. For diagnostics, this means the reported locations are nonsensical and the user is warned about this.
