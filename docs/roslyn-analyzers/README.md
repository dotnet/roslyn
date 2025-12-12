# Roslyn Analyzers

## Microsoft.CodeAnalysis.Analyzers

*Latest stable version:* <sub>[![NuGet](https://img.shields.io/nuget/v/Microsoft.CodeAnalysis.Analyzers.svg)](https://www.nuget.org/packages/Microsoft.CodeAnalysis.Analyzers)</sub>

*Latest pre-release version:* [here](https://dev.azure.com/dnceng/public/_artifacts/feed/dotnet7/NuGet/Microsoft.CodeAnalysis.Analyzers/versions)

This package contains rules for correct usage of APIs from the [Microsoft.CodeAnalysis](https://www.nuget.org/packages/Microsoft.CodeAnalysis) NuGet package, i.e. .NET Compiler Platform ("Roslyn") APIs. These are primarily aimed towards helping authors of diagnostic analyzers and code fix providers to invoke the Microsoft.CodeAnalysis APIs in a recommended manner. [More info about rules in this package](../../src/RoslynAnalyzers/Microsoft.CodeAnalysis.Analyzers/Microsoft.CodeAnalysis.Analyzers.md)

## Roslyn.Diagnostics.Analyzers

*Latest stable version:* <sub>[![NuGet](https://img.shields.io/nuget/v/Roslyn.Diagnostics.Analyzers.svg)](https://www.nuget.org/packages/Roslyn.Diagnostics.Analyzers)</sub>

*Latest pre-release version:* [here](https://dev.azure.com/dnceng/public/_artifacts/feed/dotnet7/NuGet/Roslyn.Diagnostics.Analyzers/versions)

This package contains rules that are very specific to the .NET Compiler Platform ("Roslyn") project, i.e. [dotnet/roslyn](https://github.com/dotnet/roslyn) repo. This analyzer package is *not intended for general consumption* outside the Roslyn repo. [More info about rules in this package](../../src/RoslynAnalyzers/Roslyn.Diagnostics.Analyzers/Roslyn.Diagnostics.Analyzers.md)

## Microsoft.CodeAnalysis.BannedApiAnalyzers

*Latest stable version:* <sub>[![NuGet](https://img.shields.io/nuget/v/Microsoft.CodeAnalysis.BannedApiAnalyzers.svg)](https://www.nuget.org/packages/Microsoft.CodeAnalysis.BannedApiAnalyzers)</sub>

*Latest pre-release version:* [here](https://dev.azure.com/dnceng/public/_artifacts/feed/dotnet7/NuGet/Microsoft.CodeAnalysis.BannedApiAnalyzers/versions)

This package contains customizable rules for identifying references to banned APIs. [More info about rules in this package](../../src/RoslynAnalyzers/Microsoft.CodeAnalysis.BannedApiAnalyzers/Microsoft.CodeAnalysis.BannedApiAnalyzers.md)

For instructions on using this analyzer, see [Instructions](../../src/RoslynAnalyzers/Microsoft.CodeAnalysis.BannedApiAnalyzers/BannedApiAnalyzers.Help.md).

## Microsoft.CodeAnalysis.PublicApiAnalyzers

*Latest stable version:* <sub>[![NuGet](https://img.shields.io/nuget/v/Microsoft.CodeAnalysis.PublicApiAnalyzers.svg)](https://www.nuget.org/packages/Microsoft.CodeAnalysis.PublicApiAnalyzers)</sub>

*Latest pre-release version:* [here](https://dev.azure.com/dnceng/public/_artifacts/feed/dotnet7/NuGet/Microsoft.CodeAnalysis.PublicApiAnalyzers/versions)

This package contains rules to help library authors monitoring change to their public APIs. [More info about rules in this package](../../src/RoslynAnalyzers/PublicApiAnalyzers/Microsoft.CodeAnalysis.PublicApiAnalyzers.md)

For instructions on using this analyzer, see [Instructions](../../src/RoslynAnalyzers/PublicApiAnalyzers/PublicApiAnalyzers.Help.md).