## Component diagram

![Component diagram of the Caravela compiler](Caravela.Compiler.png)

The diagram above shows relationships between the components of the Caravela compiler, along with a sample transformer project and an application using it. Most of this information are implementation details and are not necessary to understand to author source transformers or to use them.

### Caravela.Compiler

#### Caravela.Compiler.Interface.dll

The Caravela.Compiler.Interface.dll assembly contains the 4 types described in [the API](API.md), referencing the official stable version of Microsoft.CodeAnalysis.CSharp.

Because there is a cyclic dependency between Caravela.Compiler.Interface and the Caravela.Compiler version of Microsoft.CodeAnalysis.dll, the relevant code is actually in the shared source project Caravela.Compiler.Shared, which is included into the Caravela.Compiler.Sdk version of Caravela.Compiler.Interface.dll and the Caravela.Compiler version of Microsoft.CodeAnalysis.dll (more on the distinction between Sdk and the base package below). To make assembly identity work, Caravela.Compiler also contains a version of Caravela.Compiler.Interface.dll which uses `[TypeForwardedTo]` to Microsoft.CodeAnalysis.dll.

#### Microsoft.CodeAnalysis.dll

Microsoft.CodeAnalysis.dll contains many of the modifications of Roslyn code needed for Caravela (others are in Microsoft.CodeAnalysis.CSharp.dll).

These modifications come in three forms:

1. As mentioned above, the Caravela.Compiler API, code for which comes from the shared source project Caravela.Compiler.Shared.
2. Direct modifications of existing code in Microsoft.CodeAnalysis.dll.
3. Code from the shared source project Caravela.Compiler.CodeAnalysis, which contains new types introduced for Caravela.Compiler. This isn't strictly speaking necessary, but exists for better separation of Caravela.Compiler code from Roslyn code.

#### Caravela.Compiler.Sdk.pkg

Caravela.Compiler.Sdk is the NuGet package used as a reference by transformer projects (in practice, that is just Caravela proper). It contains reference assembly Caravela.Compiler.Interface.dll, along with MSBuild targets necessary for producing transformer packages. That includes editing the nuspec file of the transformer project so that the produced package depends on Caravela.Compiler instead of Caravela.Compiler.Sdk.

#### Caravela.Compiler.pkg

Caravela.Compiler is a NuGet package used as a dependency of transformer packages. It includes the Caravela.Compiler fork of the C# compiler along with MSBuild files that make sure this is the compiler used at build time.

It is effectively a renamed Microsoft.Net.Compilers.Toolset.

### Caravela.Compiler.Samples

Note that these projects are just for illustration, they don't actually exist in the repository.

#### Caravela.Compiler.Samples.Virtuosity

A transformer project, which references Caravela.Compiler.Sdk. The built package then depends on Caravela.Compiler.

#### Caravela.Compiler.Samples.Virtuosity.Test

A sample application that depends on the Caravela.Compiler.Samples.Virtuosity transformer package. This means it also indirectly depends on Caravela.Compiler, which means compiling this project will use the Caravela compiler.