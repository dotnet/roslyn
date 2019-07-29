## Overview

A number of NuGet packages are published from the Roslyn repo:
* Official released packages are published to [nuget.org](https://www.nuget.org/profiles/RoslynTeam), when Visual Studio releases a new RTM or Preview version.
* Pre-release packages are published daily to [myget.org](https://dotnet.myget.org/gallery/roslyn).

### Microsoft.Net.Compilers.Toolset

This package contains the C# and Visual Basic compiler toolset for .NET Desktop and .NET Core. This includes the compilers, msbuild tasks and targets files. When installed in a project this will override the compiler toolset installed in MSBuild. 

Note: this package is intended as a replacement for Microsoft.Net.Compilers (which is a Windows-only package) and Microsoft.NETCore.Compilers. Those packages are now deprecated and will be deleted in the future.

## Versioning
Below are the versions of the language available in the NuGet packages. Remember to set a specific language version (or "latest") if you want to use one that is newer than "default" (ie. latest major version).

- Versions `1.x` mean C# 6.0 and VB 14 (Visual Studio 2015 and updates). For instance, `1.3.2` corresponds to the most recent update (update 3) of Visual Studio 2015.
- Version `2.0` means C# 7.0 and VB 15 (Visual Studio 2017 version 15.0).
- Version `2.1` is still C# 7.0, but with a couple fixes (Visual Studio 2017 version 15.1).
- Version `2.2` is still C# 7.0, but with a couple more fixes (Visual Studio 2017 version 15.2). Language version "default" was updated to mean "7.0".
- Version `2.3` means C# 7.1 and VB 15.3 (Visual Studio 2017 version 15.3). For instance, `2.3.0-beta1` corresponds to Visual Studio 2017 version 15.3 (Preview 1).
- Version `2.4` is still C# 7.1 and VB 15.3, but with a couple fixes (Visual Studio 2017 version 15.4).
- Version `2.6` means C# 7.2 and VB 15.5 (Visual Studio 2017 version 15.5).
- Version `2.7` means C# 7.2 and VB 15.5, but with a number of [fixes](https://github.com/dotnet/roslyn/issues?q=is%3Aissue+is%3Aclosed+label%3AArea-Compilers+milestone%3A15.6) (Visual Studio 2017 version 15.6).
- Version `2.8` means C# 7.3 (Visual Studio 2017 version 15.7)
- Version `2.9` is still C# 7.3 and VB 15.5, but with more fixes (Visual Studio 2017 version 15.8)
- Version `2.10` is still C# 7.3 and VB 15.5, but a couple more [fixes](https://github.com/dotnet/roslyn/issues?q=is%3Aissue+milestone%3A15.9+label%3AArea-Compilers+is%3Aclosed) (Visual Studio 2017 version 15.9)
- Version `3.0` includes a preview of C# 8.0 (Visual Studio 2019 version 16.0), but `2.11` was used for preview1.
- Version `3.1` includes a preview of C# 8.0 (Visual Studio 2019 version 16.1)
- Version `3.2` includes a preview of C# 8.0 (Visual Studio 2019 version 16.2)
- Version `3.3` includes C# 8.0 (Visual Studio 2019 version 16.3, .NET Core 3.0)

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

### System.Memory

This package will contain ref-like implementations of `Span<T>` and `ReadOnlySpan<T>` that work with C# 7.2 ref features.

### Microsoft.CodeDom.Providers.DotNetCompilerPlatform

This package is produced by the ASP.NET team. You can find it [here on NuGet](https://www.nuget.org/packages/Microsoft.CodeDom.Providers.DotNetCompilerPlatform).