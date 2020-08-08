## Welcome to the .NET Compiler Platform ("Roslyn")

[![Join the chat at https://gitter.im/dotnet/roslyn](https://badges.gitter.im/Join%20Chat.svg)](https://gitter.im/dotnet/roslyn?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge) [![Chat on Discord](https://discordapp.com/api/guilds/143867839282020352/widget.png)](http://aka.ms/discord-csharp-roslyn)

Roslyn provides open-source C# and Visual Basic compilers with rich code analysis APIs.  It enables building code analysis tools with the same APIs that are used by Visual Studio.

### Language Design Discussion

We are now taking language feature discussion in other repositories:
- https://github.com/dotnet/csharplang for C# specific issues
- https://github.com/dotnet/vblang for VB-specific features
- https://github.com/dotnet/csharplang for features that affect both languages

Discussion about the transition of language design to the new repos is at https://github.com/dotnet/roslyn/issues/18002.

### Download C# and Visual Basic

Want to start developing in C# and Visual Basic? Download [Visual Studio 2019](https://www.visualstudio.com/downloads/), which has the latest features built-in. There are 
also [prebuilt Azure VM images](https://azuremarketplace.microsoft.com/en-us/marketplace/apps/category/compute?search=visual%20studio%202019) available with 
Visual Studio 2019 already installed.

To install the latest release without Visual Studio, download the [.NET SDK nightlies](https://github.com/dotnet/installer/blob/master/README.md#installers-and-binaries).

See [what's new with the C# and VB compilers](https://github.com/dotnet/roslyn/wiki/Changelog-for-C%23-and-VB-compilers).

**Pre-release builds** are available from the following public NuGet feeds: 
- [Compiler](https://dev.azure.com/dnceng/public/_packaging?_a=feed&feed=dotnet-tools): `https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-tools/nuget/v3/index.json`
- [IDE Services](https://devdiv.visualstudio.com/DevDiv/_packaging?_a=feed&feed=vssdk): `https://devdiv.pkgs.visualstudio.com/_packaging/vssdk/nuget/v3/index.json` 
- [.NET SDK](https://dev.azure.com/dnceng/public/_packaging?_a=feed&feed=dotnet5): `https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet5/nuget/v3/index.json`

### Source code

* Clone the sources: `git clone https://github.com/dotnet/roslyn.git`
* [Enhanced source view](http://sourceroslyn.io/), powered by Roslyn 
* [Building, testing and debugging the sources](https://github.com/dotnet/roslyn/wiki/Building%20Testing%20and%20Debugging)

### Get started

* Tutorial articles by Alex Turner in MSDN Magazine
  - [Use Roslyn to Write a Live Code Analyzer for Your API](https://msdn.microsoft.com/en-us/magazine/dn879356)
  - [Adding a Code Fix to your Roslyn Analyzer](https://msdn.microsoft.com/en-us/magazine/dn904670.aspx)
* [Roslyn Overview](https://github.com/dotnet/roslyn/wiki/Roslyn%20Overview) 
* [API Changes between CTP 6 and RC](https://github.com/dotnet/roslyn/wiki/VS-2015-RC-API-Changes)
* [Samples and Walkthroughs](https://github.com/dotnet/roslyn/wiki/Samples-and-Walkthroughs)
* [Documentation](https://github.com/dotnet/roslyn/tree/master/docs)
* [Analyzer documentation](https://github.com/dotnet/roslyn/tree/master/docs/analyzers)
* [Syntax Visualizer Tool](https://github.com/dotnet/roslyn/wiki/Syntax%20Visualizer)
* [Syntax Quoter Tool](http://roslynquoter.azurewebsites.net)
* [Roadmap](https://github.com/dotnet/roslyn/wiki/Roadmap) 
* [Language Design Notes](https://github.com/dotnet/roslyn/issues?q=label%3A%22Design+Notes%22+)
* [FAQ](https://github.com/dotnet/roslyn/wiki/FAQ)
* Also take a look at our [Wiki](https://github.com/dotnet/roslyn/wiki) for more information on how to contribute, what the labels on issue mean, etc.

### Contribute!

Some of the best ways to contribute are to try things out, file bugs, and join in design conversations. 

* [How to Contribute](https://github.com/dotnet/roslyn/wiki/Contributing-Code)
* [Pull requests](https://github.com/dotnet/roslyn/pulls): [Open](https://github.com/dotnet/roslyn/pulls?q=is%3Aopen+is%3Apr)/[Closed](https://github.com/dotnet/roslyn/pulls?q=is%3Apr+is%3Aclosed)

Looking for something to work on? The list of [up for grabs issues](https://github.com/dotnet/roslyn/labels/help%20wanted) is a great place to start.

This [project](CODE-OF-CONDUCT.md) has adopted the [.NET Foundation Code of Conduct](https://dotnetfoundation.org/code-of-conduct).

### .NET Foundation

This project is part of the [.NET Foundation](http://www.dotnetfoundation.org/projects) along with other
projects like [the .NET Runtime](https://github.com/dotnet/runtime/).

[//]: # (Begin current test results)

### Continuous Integration status

#### Desktop Unit Tests
|Branch|Debug x86|Debug x64|Release x86|Release x64|
|:--:|:--:|:--:|:--:|:--:|
**master**|[![Build Status](https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/roslyn/roslyn-CI?branchname=master&jobname=Windows_Desktop_Unit_Tests&configuration=Windows_Desktop_Unit_Tests%20debug_32&label=build)](https://dev.azure.com/dnceng/public/_build/latest?definitionId=15&branchname=master&view=logs)|[![Build Status](https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/roslyn/roslyn-CI?branchname=master&jobname=Windows_Desktop_Unit_Tests&configuration=Windows_Desktop_Unit_Tests%20debug_64&label=build)](https://dev.azure.com/dnceng/public/_build/latest?definitionId=15&branchname=master&view=logs)|[![Build Status](https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/roslyn/roslyn-CI?branchname=master&jobname=Windows_Desktop_Unit_Tests&configuration=Windows_Desktop_Unit_Tests%20release_32&label=build)](https://dev.azure.com/dnceng/public/_build/latest?definitionId=15&branchname=master&view=logs)|[![Build Status](https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/roslyn/roslyn-CI?branchname=master&jobname=Windows_Desktop_Unit_Tests&configuration=Windows_Desktop_Unit_Tests%20release_64&label=build)](https://dev.azure.com/dnceng/public/_build/latest?definitionId=15&branchname=master&view=logs)|
**master-vs-deps**|[![Build Status](https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/roslyn/roslyn-CI?branchname=master-vs-deps&jobname=Windows_Desktop_Unit_Tests&configuration=Windows_Desktop_Unit_Tests%20debug_32&label=build)](https://dev.azure.com/dnceng/public/_build/latest?definitionId=15&branchname=master-vs-deps&view=logs)|[![Build Status](https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/roslyn/roslyn-CI?branchname=master-vs-deps&jobname=Windows_Desktop_Unit_Tests&configuration=Windows_Desktop_Unit_Tests%20debug_64&label=build)](https://dev.azure.com/dnceng/public/_build/latest?definitionId=15&branchname=master-vs-deps&view=logs)|[![Build Status](https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/roslyn/roslyn-CI?branchname=master-vs-deps&jobname=Windows_Desktop_Unit_Tests&configuration=Windows_Desktop_Unit_Tests%20release_32&label=build)](https://dev.azure.com/dnceng/public/_build/latest?definitionId=15&branchname=master-vs-deps&view=logs)|[![Build Status](https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/roslyn/roslyn-CI?branchname=master-vs-deps&jobname=Windows_Desktop_Unit_Tests&configuration=Windows_Desktop_Unit_Tests%20release_64&label=build)](https://dev.azure.com/dnceng/public/_build/latest?definitionId=15&branchname=master-vs-deps&view=logs)|

#### CoreClr Unit Tests
|Branch|Windows Debug|Windows Release|Linux|
|:--:|:--:|:--:|:--:|
**master**|[![Build Status](https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/roslyn/roslyn-CI?branchname=master&jobname=Windows_CoreClr_Unit_Tests&configuration=Windows_CoreClr_Unit_Tests%20debug&label=build)](https://dev.azure.com/dnceng/public/_build/latest?definitionId=15&branchname=master&view=logs)|[![Build Status](https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/roslyn/roslyn-CI?branchname=master&jobname=Windows_CoreClr_Unit_Tests&configuration=Windows_CoreClr_Unit_Tests%20release&label=build)](https://dev.azure.com/dnceng/public/_build/latest?definitionId=15&branchname=master&view=logs)|[![Build Status](https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/roslyn/roslyn-CI?branchname=master&jobname=Linux_Test&configuration=Linux_Test%20coreclr&label=build)](https://dev.azure.com/dnceng/public/_build/latest?definitionId=15&branchname=master&view=logs)|
**master-vs-deps**|[![Build Status](https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/roslyn/roslyn-CI?branchname=master-vs-deps&jobname=Windows_CoreClr_Unit_Tests&configuration=Windows_CoreClr_Unit_Tests%20debug&label=build)](https://dev.azure.com/dnceng/public/_build/latest?definitionId=15&branchname=master-vs-deps&view=logs)|[![Build Status](https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/roslyn/roslyn-CI?branchname=master-vs-deps&jobname=Windows_CoreClr_Unit_Tests&configuration=Windows_CoreClr_Unit_Tests%20release&label=build)](https://dev.azure.com/dnceng/public/_build/latest?definitionId=15&branchname=master-vs-deps&view=logs)|[![Build Status](https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/roslyn/roslyn-CI?branchname=master-vs-deps&jobname=Linux_Test&configuration=Linux_Test%20coreclr&label=build)](https://dev.azure.com/dnceng/public/_build/latest?definitionId=15&branchname=master-vs-deps&view=logs)|

#### Integration Tests
|Branch|Debug|Release
|:--:|:--:|:--:|
**master**|[![Build Status](https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/roslyn/roslyn-integration-CI?branchname=master&jobname=VS_Integration&configuration=VS_Integration%20debug_async&label=build)](https://dev.azure.com/dnceng/public/_build/latest?definitionId=245&branchname=master&view=logs)|[![Build Status](https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/roslyn/roslyn-integration-CI?branchname=master&jobname=VS_Integration&configuration=VS_Integration%20release_async&label=build)](https://dev.azure.com/dnceng/public/_build/latest?definitionId=245&branchname=master&view=logs)|
**master-vs-deps**|[![Build Status](https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/roslyn/roslyn-integration-CI?branchname=master-vs-deps&jobname=VS_Integration&configuration=VS_Integration%20debug_async&label=build)](https://dev.azure.com/dnceng/public/_build/latest?definitionId=245&branchname=master-vs-deps&view=logs)|[![Build Status](https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/roslyn/roslyn-integration-CI?branchname=master-vs-deps&jobname=VS_Integration&configuration=VS_Integration%20release_async&label=build)](https://dev.azure.com/dnceng/public/_build/latest?definitionId=245&branchname=master-vs-deps&view=logs)|

#### Misc Tests
|Branch|Determinism|Build Correctness|Spanish|Mono|
|:--:|:--:|:--:|:--:|:--:|
**master**|[![Build Status](https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/roslyn/roslyn-CI?branchname=master&jobname=Windows_Determinism_Test&configuration=Windows_Determinism_Test&label=build)](https://dev.azure.com/dnceng/public/_build/latest?definitionId=15&branchname=master&view=logs)|[![Build Status](https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/roslyn/roslyn-CI?branchname=master&jobname=Windows_Correctness_Test&configuration=Windows_Correctness_Test&label=build)](https://dev.azure.com/dnceng/public/_build/latest?definitionId=15&branchname=master&view=logs)|[![Build Status](https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/roslyn/roslyn-CI?branchname=master&jobname=Windows_Desktop_Spanish_Unit_Tests&configuration=Windows_Desktop_Spanish_Unit_Tests&label=build)](https://dev.azure.com/dnceng/public/_build/latest?definitionId=15&branchname=master&view=logs)|[![Build Status](https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/roslyn/roslyn-CI?branchname=master&jobname=Linux_Test&configuration=Linux_Test%20mono&label=build)](https://dev.azure.com/dnceng/public/_build/latest?definitionId=15&branchname=master&view=logs)|
**master-vs-deps**|[![Build Status](https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/roslyn/roslyn-CI?branchname=master-vs-deps&jobname=Windows_Determinism_Test&configuration=Windows_Determinism_Test&label=build)](https://dev.azure.com/dnceng/public/_build/latest?definitionId=15&branchname=master-vs-deps&view=logs)|[![Build Status](https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/roslyn/roslyn-CI?branchname=master-vs-deps&jobname=Windows_Correctness_Test&configuration=Windows_Correctness_Test&label=build)](https://dev.azure.com/dnceng/public/_build/latest?definitionId=15&branchname=master-vs-deps&view=logs)|[![Build Status](https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/roslyn/roslyn-CI?branchname=master-vs-deps&jobname=Windows_Desktop_Spanish_Unit_Tests&configuration=Windows_Desktop_Spanish_Unit_Tests&label=build)](https://dev.azure.com/dnceng/public/_build/latest?definitionId=15&branchname=master-vs-deps&view=logs)|[![Build Status](https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/roslyn/roslyn-CI?branchname=master-vs-deps&jobname=Linux_Test&configuration=Linux_Test%20mono&label=build)](https://dev.azure.com/dnceng/public/_build/latest?definitionId=15&branchname=master-vs-deps&view=logs)|

[//]: # (End current test results)
