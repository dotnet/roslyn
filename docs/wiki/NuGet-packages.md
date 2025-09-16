## Overview

A number of NuGet packages are published from the Roslyn repo:
* Official released packages are published to [nuget.org](https://www.nuget.org/profiles/RoslynTeam), when Visual Studio releases a new RTM or Preview version.
* Pre-release packages are published daily to [Azure Devops](https://dev.azure.com/dnceng/public/_packaging?_a=feed&feed=dotnet-tools).

### Microsoft.Net.Compilers.Toolset

This package contains the C# and Visual Basic compiler toolset for .NET Desktop and .NET Core. This includes the compilers, msbuild tasks and targets files. When installed in a project this will override the compiler toolset installed in MSBuild. 

This package is primarily intended as a method for rapidly shipping hotfixes to customers. Using it as a long term solution for providing newer compilers on older MSBuild installations is explicitly not supported. That can and will break on a regular basis.

Note: this package is intended as a replacement for Microsoft.Net.Compilers (which is a Windows-only package) and Microsoft.NETCore.Compilers. Those packages are now deprecated and will be deleted in the future.

## Versioning
Starting on Roslyn versions `5.0` and above, Roslyn NuGet packages are shipped with the .NET SDK instead of VS.  New Roslyn packages will be published on every SDK preview and GA release.
   
Prior to this change, a Roslyn package was matched exactly to a VS version, and did not exactly match SDK versions.  Now, Roslyn packages will exactly align with an SDK version, but will no longer exactly match a VS version.  While generally the SDK releases ship alongside new VS versions, there may be drift in the exact commit of Roslyn when compared to VS (especially for prereleases).

### Version Schema
Generally Roslyn package versions are in the form A.B.C-D.  With the new publishing mode this will be represented as
* A = Associated with the current VS Major version (`5` = VS 18, same as before).
* B = The current VS Minor version (same as before)
* C = Our own patch version if we need to service the package for any reason, generally `0`.
* D = The VS prerelease version `-` VMR build version (if any).

We still use VS versions in our version scheme as Roslyn currently branches for releases with VS in mind.

What this actually looks like:

| SDK Version | 11.0.100-preview.4 | 11.0.100-rc.2 | 11.0.100 | 11.0.200 |
|---|---|---|---|---|
| Roslyn version | 5.6.0-2.25465.10 | 5.8.0-3.25501.82 | 5.9.0 | 5.10.0 |

SDK GA releases align with VS Minor versions.  So going from `11.0.100` to `11.0.200` means a new VS Minor version, hence incrementing the Roslyn package minor version.

However, SDK prereleases do not always correspond to new VS Minor or VS preview versions.  For example, while `11.0.100-preview.4` might produce Roslyn `5.6.0-2.25465.101`, the subsequent `11.0.100-preview.5` could produce Roslyn `5.6.0-2.25503.114` without any change to the VS minor or preview version.  In these cases, we need to include the VMR build number to differentiate between packages.  This typically occurs with early SDK versions or during extended VS preview cycles (such as preview 1 for a new VS major version).

Additionally, very early in the SDK preview cycle, Roslyn main is consumed by the new SDK preview and the prior SDK feature band.  The SDK preview releases will publish the prerelease packages for a specific version, and the feature band release will end up publishing the GA packages for that version.

| SDK Version | Roslyn Package version |
|---|---|
| 11.0.100-preview1 | 5.6.0-2.25465.10 |
| 10.0.200 | 5.6.0 |

By the later previews for .NET 11, the feature bands for 10 will be locked to an older branch of Roslyn associated with an older VS minor version, hence the GA packages for the next SDK will always have a higher minor version than any GA packages for the prior feature band release.  See also https://learn.microsoft.com/en-us/dotnet/core/porting/versioning-sdk-msbuild-vs#supported-net-versions

### Past Versions

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
- Version `3.4` includes C# 8.0 (Visual Studio 2019 version 16.4, .NET Core 3.1)
- Version `3.5` includes C# 8.0 (Visual Studio 2019 version 16.5, .NET Core 3.1)
- Version `3.6` includes C# 8.0 (Visual Studio 2019 version 16.6, .NET Core 3.1)
- Version `3.7` includes C# 8.0 (Visual Studio 2019 version 16.7, .NET Core 3.1)
- Version `3.8` includes C# 9.0 (Visual Studio 2019 version 16.8, .NET 5)
- Version `3.9` includes C# 9.0 (Visual Studio 2019 version 16.9, .NET 5)
- Version `3.10` includes C# 9.0 (Visual Studio 2019 version 16.10, .NET 5)
- Version `3.11` includes C# 9.0 (Visual Studio 2019 version 16.11, .NET 5)
- Version `4.0` includes C# 10.0 (Visual Studio 2022 version 17.0, .NET 6)
- Version `4.1` includes C# 10.0 (Visual Studio 2022 version 17.1, .NET 6)
- Version `4.2` includes C# 10.0 (Visual Studio 2022 version 17.2, .NET 6)
- Version `4.3.1` includes C# 10.0 (Visual Studio 2022 version 17.3, .NET 6)
- Version `4.4` includes C# 11.0 (Visual Studio 2022 version 17.4, .NET 7)
- Version `4.5` includes C# 11.0 (Visual Studio 2022 version 17.5, .NET 7)
- Version `4.6` includes C# 11.0 (Visual Studio 2022 version 17.6, .NET 7)
- Version `4.7` includes C# 11.0 (Visual Studio 2022 version 17.7, .NET 7)
- Version `4.8` includes C# 12.0 (Visual Studio 2022 version 17.8, .NET 8)
- Version `4.9.2` includes C# 12.0 (Visual Studio 2022 version 17.9, .NET 8)
- Version `4.10` includes C# 12.0 (Visual Studio 2022 version 17.10, .NET 8)
- Version `4.11` includes C# 12.0 (Visual Studio 2022 version 17.11, .NET 8)
- Version `4.12` includes C# 13.0 (Visual Studio 2022 version 17.12, .NET 9)
- Version `4.13` includes C# 13.0 (Visual Studio 2022 version 17.13, .NET 9)
- Version `4.14` includes C# 13.0 (Visual Studio 2022 version 17.14, .NET 9)

See the [history of C# language features](https://github.com/dotnet/csharplang/blob/main/Language-Version-History.md) for more details.

See the [.NET SDK, MSBuild, and Visual Studio versioning](https://docs.microsoft.com/dotnet/core/porting/versioning-sdk-msbuild-vs#lifecycle) docs page for details of SDK versioning. 

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
