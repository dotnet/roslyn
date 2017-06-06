## Overview

A number of [NuGet packages](https://www.nuget.org/profiles/RoslynTeam) are published from the Roslyn repo.
New packages are published when Visual Studio releases a new RTM or Preview version.

### Microsoft.Net.Compilers

This package not only includes the C# and Visual Basic compilers, it also modifies MSBuild targets so that the included compiler versions are used rather than any system-installed versions.

## Versioning

- Versions `1.x` mean C# 6.0 (Visual Studio 2015 and updates). For instance, `1.3.2` corresponds to the most recent update (update 3) of Visual Studio 2015.
- Version `2.0` means C# 7.0 and VB 15 (Visual Studio 2017 version 15.0).
- Version `2.1` is still C# 7.0, but with a couple fixes (Visual Studio 2017 version 15.1).
- Version `2.2` is still C# 7.0, but with a couple more fixes (Visual Studio 2017 version 15.2).
- Version `2.3` means C# 7.1 and VB 15.3 (Visual Studio 2017 version 15.3). For instance, `2.3.0-beta1` corresponds to Visual Studio 2017 version 15.3 (Preview 1).