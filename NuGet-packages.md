## Overview

A number of [NuGet packages](https://www.nuget.org/profiles/RoslynTeam) are published from the Roslyn repo.
New packages are published when Visual Studio releases a new RTM or Preview version.

### Microsoft.Net.Compilers

This package not only includes the C# and Visual Basic compilers, it also modifies MSBuild targets so that the included compiler versions are used rather than any system-installed versions.
Once installed, this package requires Microsoft Build Tools 2015.

## Versioning

- Versions `1.x` mean C# 6.0 (Visual Studio 2015 and updates). For instance, `1.3.2` corresponds to the most recent update (update 3) of Visual Studio 2015.
- Version `2.0` means C# 7.0 and VB 15 (Visual Studio 2017 version 15.0).
- Version `2.1` is still C# 7.0, but with a couple fixes (Visual Studio 2017 version 15.1).
- Version `2.2` is still C# 7.0, but with a couple more fixes (Visual Studio 2017 version 15.2).
- Version `2.3` means C# 7.1 and VB 15.3 (Visual Studio 2017 version 15.3). For instance, `2.3.0-beta1` corresponds to Visual Studio 2017 version 15.3 (Preview 1).
- Version `2.4` is still C# 7.1 and VB 15.3, but with a couple fixes (Visual Studio 2017 version 15.4).
- Version `2.6` means C# 7.2 and VB 15.5 (Visual Studio 2017 version 15.5).

See the [history of C# language features](https://github.com/dotnet/csharplang/blob/master/Language-Version-History.md) for more details.

## Other packages

A few other packages are relevant or related to Roslyn, but are not produced from the Roslyn repo.

### ValueTuple

To facilitate adoption of C# 7.0 and VB 15 tuples, the required underlying types were made available as a standalone package (see [ValueTuple](https://www.nuget.org/packages/System.ValueTuple) on NuGet). But those types were progressively built into newer versions of the different .NET frameworks.

|                        | Version that includes ValueTuple |
|------------------------|----------------------------------|
| Full/desktop framework | .NET Framework 4.7 and Windows 10 Creators Edition Update (RS2) | 
| Core | .NET Core 2.0 | 
| Mono | Mono 5.0 | 
| .Net Standard | .Net Standard 2.0 | 

The package supports multiple target frameworks, providing an implementation for older targets including PCL (moniker `portable_net40+sl4+win8+wp8`, where `ValueTuple.dll` only depends on `mscorlib`) and .NET Standard 1.0 (`netstandard1.0`).
For newer targets such as `net47`, `netstandard2.0`, `netcoreapp2.0`, the package provides type forwards to the in-box implementation.

The above describes version 4.4.0 of the ValueTuple package. The package is produced from the corefx repo.

### Microsoft.CodeDom.Providers.DotNetCompilerPlatform

This package is produced by the ASP.NET team. You can find it [here on NuGet](https://www.nuget.org/packages/Microsoft.CodeDom.Providers.DotNetCompilerPlatform).