# Microsoft.CodeAnalysis.Analyzers

Contains rules for correct usage of APIs from the [Microsoft.CodeAnalysis](https://www.nuget.org/packages/Microsoft.CodeAnalysis) NuGet package, i.e. .NET Compiler Platform ("Roslyn") APIs. These are primarily aimed towards helping authors of diagnostic analyzers, code fix providers and other tools built on top of Microsoft.CodeAnalysis to invoke the Microsoft.CodeAnalysis APIs in a recommended manner. This package is included as a development dependency of [Microsoft.CodeAnalysis](https://www.nuget.org/packages/Microsoft.CodeAnalysis) NuGet package, and does not need to be installed separately if you are referencing Microsoft.CodeAnalysis NuGet package.

[More info about rules in this package](./Microsoft.CodeAnalysis.Analyzers.md)
