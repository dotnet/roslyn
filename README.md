<p align="center">
<img width="450" src="https://user-images.githubusercontent.com/46729679/109719841-17b7dd00-7b5e-11eb-8f5e-87eb2d4d1be9.png" alt="Roslyn logo">
</p>

<h1 align="center">The .NET Compiler Platform</h1>

<p align="center"><a href="https://gitter.im/dotnet/roslyn?utm_source=badge&amp;utm_medium=badge&amp;utm_campaign=pr-badge&amp;utm_content=badge" rel="nofollow"><img            src="https://camo.githubusercontent.com/5dbac0213da25c445bd11f168587c11a200ba153ef3014e8408e462e410169b3/68747470733a2f2f6261646765732e6769747465722e696d2f4a6f696e253230436861742e737667" alt="Join the chat at https://gitter.im/dotnet/roslyn" data-canonical-src="https://badges.gitter.im/Join%20Chat.svg" style="max-width:100%;"></a> <a href="http://aka.ms/discord-csharp-roslyn" rel="nofollow"><img src="https://camo.githubusercontent.com/1ea6a95121cbf4179d411e853681838825392a7f0ae7e6bb1e03f4ea37c8fd5d/68747470733a2f2f646973636f72646170702e636f6d2f6170692f6775696c64732f3134333836373833393238323032303335322f7769646765742e706e67" alt="Chat on Discord" data-canonical-src="https://discordapp.com/api/guilds/143867839282020352/widget.png" style="max-width:100%;"></a></p>

Roslyn is the open-source implementation of both the C# and Visual Basic compilers with an API surface for building code analysis tools.

### C# and Visual Basic Language Feature Suggestions

If you want to suggest a new feature for the C# or Visual Basic languages go here:
- [dotnet/csharplang](https://github.com/dotnet/csharplang) for C# specific issues
- [dotnet/vblang](https://github.com/dotnet/vblang) for VB-specific features
- [dotnet/csharplang](https://github.com/dotnet/csharplang) for features that affect both languages

### Contributing

All work on the C# and Visual Basic compiler happens directly on [GitHub](https://github.com/dotnet/roslyn). Both core team members and external contributors send pull requests which go through the same review process.

If you are interested in fixing issues and contributing directly to the code base, a great way to get started is to ask some questions on [GitHub Discussions](https://github.com/dotnet/roslyn/discussions)! Then check out our [contributing guide](https://github.com/dotnet/roslyn/blob/main/docs/contributing/Building%2C%20Debugging%2C%20and%20Testing%20on%20Windows.md) which covers the following:

- [Coding guidelines](https://github.com/dotnet/roslyn/blob/main/docs/wiki/Contributing-Code.md)
- [The development workflow, including debugging and running tests](https://github.com/dotnet/roslyn/blob/main/docs/contributing/Building%2C%20Debugging%2C%20and%20Testing%20on%20Windows.md)
- [Submitting pull requests](https://github.com/dotnet/roslyn/blob/main/CONTRIBUTING.md)
- Finding a bug to fix in the [IDE](https://aka.ms/roslyn-ide-bugs-help-wanted) or [Compiler](https://aka.ms/roslyn-compiler-bugs-help-wanted)
- Finding a feature to implement in the [IDE](https://aka.ms/roslyn-ide-feature-help-wanted) or [Compiler](https://aka.ms/roslyn-compiler-feature-help-wanted)
- Roslyn API suggestions should go through the [API review process](<docs/contributing/API Review Process.md>)

### Community

The Roslyn community can be found on [GitHub Discussions](https://github.com/dotnet/roslyn/discussions), where you can ask questions, voice ideas, and share your projects.

To chat with other community members, you can join the Roslyn [Discord](https://discord.com/invite/tGJvv88) or [Gitter](https://gitter.im/dotnet/roslyn).

Our [Code of Conduct](CODE-OF-CONDUCT.md) applies to all Roslyn community channels and has adopted the [.NET Foundation Code of Conduct](https://dotnetfoundation.org/code-of-conduct).

### Documentation

Visit [Roslyn Architecture Overview](https://docs.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/compiler-api-model) to get started with Roslyn’s API’s.

### NuGet Feeds

**The latest pre-release builds** are available from the following public NuGet feeds: 
- [Compiler](https://dev.azure.com/dnceng/public/_packaging?_a=feed&feed=dotnet-tools): `https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-tools/nuget/v3/index.json`
- [IDE Services](https://dev.azure.com/azure-public/vside/_packaging?_a=feed&feed=vssdk): `https://pkgs.dev.azure.com/azure-public/vside/_packaging/vssdk/nuget/v3/index.json`
- [.NET SDK](https://dev.azure.com/dnceng/public/_packaging?_a=feed&feed=dotnet5): `https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet5/nuget/v3/index.json`

[//]: # (Begin current test results)

### Continuous Integration status

#### Builds

|Branch|Windows Debug|Windows Release|Unix Debug|
|:--:|:--:|:--:|:--:|
**main**|[![Build Status](https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/roslyn/roslyn-CI?branchname=main&jobname=Build_Windows_Debug&configuration=Build_Windows_Debug&label=build)](https://dev.azure.com/dnceng/public/_build/latest?definitionId=15&branchname=main&view=logs)|[![Build Status](https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/roslyn/roslyn-CI?branchname=main&jobname=Build_Windows_Release&configuration=Build_Windows_Release&label=build)](https://dev.azure.com/dnceng/public/_build/latest?definitionId=15&branchname=main&view=logs)|[![Build Status](https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/roslyn/roslyn-CI?branchname=main&jobname=Build_Unix_Debug&configuration=Build_Unix_Debug&label=build)](https://dev.azure.com/dnceng/public/_build/latest?definitionId=15&branchname=main&view=logs)|
**main-vs-deps**|[![Build Status](https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/roslyn/roslyn-CI?branchname=main-vs-deps&jobname=Build_Windows_Debug&configuration=Build_Windows_Debug&label=build)](https://dev.azure.com/dnceng/public/_build/latest?definitionId=15&branchname=main-vs-deps&view=logs)|[![Build Status](https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/roslyn/roslyn-CI?branchname=main-vs-deps&jobname=Build_Windows_Release&configuration=Build_Windows_Release&label=build)](https://dev.azure.com/dnceng/public/_build/latest?definitionId=15&branchname=main-vs-deps&view=logs)|[![Build Status](https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/roslyn/roslyn-CI?branchname=main-vs-deps&jobname=Build_Unix_Debug&configuration=Build_Unix_Debug&label=build)](https://dev.azure.com/dnceng/public/_build/latest?definitionId=15&branchname=main-vs-deps&view=logs)|

#### Desktop Unit Tests

|Branch|Debug x86|Debug x64|Release x86|Release x64|
|:--:|:--:|:--:|:--:|:--:|
**main**|[![Build Status](https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/roslyn/roslyn-CI?branchname=main&jobname=Test_Windows_Desktop_Debug_32&configuration=Test_Windows_Desktop_Debug_32&label=build)](https://dev.azure.com/dnceng/public/_build/latest?definitionId=15&branchname=main&view=logs)|[![Build Status](https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/roslyn/roslyn-CI?branchname=main&jobname=Test_Windows_Desktop_Debug_64&configuration=Test_Windows_Desktop_Debug_64&label=build)](https://dev.azure.com/dnceng/public/_build/latest?definitionId=15&branchname=main&view=logs)|[![Build Status](https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/roslyn/roslyn-CI?branchname=main&jobname=Test_Windows_Desktop_Release_32&configuration=Test_Windows_Desktop_Release_32&label=build)](https://dev.azure.com/dnceng/public/_build/latest?definitionId=15&branchname=main&view=logs)|[![Build Status](https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/roslyn/roslyn-CI?branchname=main&jobname=Test_Windows_Desktop_Release_64&configuration=Test_Windows_Desktop_Release_64&label=build)](https://dev.azure.com/dnceng/public/_build/latest?definitionId=15&branchname=main&view=logs)|
**main-vs-deps**|[![Build Status](https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/roslyn/roslyn-CI?branchname=main-vs-deps&jobname=Test_Windows_Desktop_Debug_32&configuration=Test_Windows_Desktop_Debug_32&label=build)](https://dev.azure.com/dnceng/public/_build/latest?definitionId=15&branchname=main-vs-deps&view=logs)|[![Build Status](https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/roslyn/roslyn-CI?branchname=main-vs-deps&jobname=Test_Windows_Desktop_Debug_64&configuration=Test_Windows_Desktop_Debug_64&label=build)](https://dev.azure.com/dnceng/public/_build/latest?definitionId=15&branchname=main-vs-deps&view=logs)|[![Build Status](https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/roslyn/roslyn-CI?branchname=main-vs-deps&jobname=Test_Windows_Desktop_Release_32&configuration=Test_Windows_Desktop_Release_32&label=build)](https://dev.azure.com/dnceng/public/_build/latest?definitionId=15&branchname=main-vs-deps&view=logs)|[![Build Status](https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/roslyn/roslyn-CI?branchname=main-vs-deps&jobname=Test_Windows_Desktop_Release_64&configuration=Test_Windows_Desktop_Release_64&label=build)](https://dev.azure.com/dnceng/public/_build/latest?definitionId=15&branchname=main-vs-deps&view=logs)|

#### CoreClr Unit Tests

|Branch|Windows Debug|Windows Release|Linux|
|:--:|:--:|:--:|:--:|
**main**|[![Build Status](https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/roslyn/roslyn-CI?branchname=main&jobname=Test_Windows_CoreClr_Debug&configuration=Test_Windows_CoreClr_Debug&label=build)](https://dev.azure.com/dnceng/public/_build/latest?definitionId=15&branchname=main&view=logs)|[![Build Status](https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/roslyn/roslyn-CI?branchname=main&jobname=Test_Windows_CoreClr_Release&configuration=Test_Windows_CoreClr_Release&label=build)](https://dev.azure.com/dnceng/public/_build/latest?definitionId=15&branchname=main&view=logs)|[![Build Status](https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/roslyn/roslyn-CI?branchname=main&jobname=Test_Linux_Debug&configuration=Test_Linux_Debug&label=build)](https://dev.azure.com/dnceng/public/_build/latest?definitionId=15&branchname=main&view=logs)|
**main-vs-deps**|[![Build Status](https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/roslyn/roslyn-CI?branchname=main-vs-deps&jobname=Test_Windows_CoreClr_Debug&configuration=Test_Windows_CoreClr_Debug&label=build)](https://dev.azure.com/dnceng/public/_build/latest?definitionId=15&branchname=main-vs-deps&view=logs)|[![Build Status](https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/roslyn/roslyn-CI?branchname=main-vs-deps&jobname=Test_Windows_CoreClr_Release&configuration=Test_Windows_CoreClr_Release&label=build)](https://dev.azure.com/dnceng/public/_build/latest?definitionId=15&branchname=main-vs-deps&view=logs)|[![Build Status](https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/roslyn/roslyn-CI?branchname=main-vs-deps&jobname=Test_Linux_Debug&configuration=Test_Linux_Debug&label=build)](https://dev.azure.com/dnceng/public/_build/latest?definitionId=15&branchname=main-vs-deps&view=logs)|

#### Integration Tests

|Branch|Debug x86|Debug x64|Release x86|Release x64
|:--:|:--:|:--:|:--:|:--:|
**main**|[![Build Status](https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/roslyn/roslyn-integration-CI?branchname=main&jobname=VS_Integration&configuration=VS_Integration%20debug_32&label=build)](https://dev.azure.com/dnceng/public/_build/latest?definitionId=245&branchname=main&view=logs)|[![Build Status](https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/roslyn/roslyn-integration-CI?branchname=main&jobname=VS_Integration&configuration=VS_Integration%20debug_64&label=build)](https://dev.azure.com/dnceng/public/_build/latest?definitionId=245&branchname=main&view=logs)|[![Build Status](https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/roslyn/roslyn-integration-CI?branchname=main&jobname=VS_Integration&configuration=VS_Integration%20release_32&label=build)](https://dev.azure.com/dnceng/public/_build/latest?definitionId=245&branchname=main&view=logs)|[![Build Status](https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/roslyn/roslyn-integration-CI?branchname=main&jobname=VS_Integration&configuration=VS_Integration%20release_64&label=build)](https://dev.azure.com/dnceng/public/_build/latest?definitionId=245&branchname=main&view=logs)|
**main-vs-deps**|[![Build Status](https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/roslyn/roslyn-integration-CI?branchname=main-vs-deps&jobname=VS_Integration&configuration=VS_Integration%20debug_32&label=build)](https://dev.azure.com/dnceng/public/_build/latest?definitionId=245&branchname=main-vs-deps&view=logs)|[![Build Status](https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/roslyn/roslyn-integration-CI?branchname=main-vs-deps&jobname=VS_Integration&configuration=VS_Integration%20debug_64&label=build)](https://dev.azure.com/dnceng/public/_build/latest?definitionId=245&branchname=main-vs-deps&view=logs)|[![Build Status](https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/roslyn/roslyn-integration-CI?branchname=main-vs-deps&jobname=VS_Integration&configuration=VS_Integration%20release_32&label=build)](https://dev.azure.com/dnceng/public/_build/latest?definitionId=245&branchname=main-vs-deps&view=logs)|[![Build Status](https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/roslyn/roslyn-integration-CI?branchname=main-vs-deps&jobname=VS_Integration&configuration=VS_Integration%20release_64&label=build)](https://dev.azure.com/dnceng/public/_build/latest?definitionId=245&branchname=main-vs-deps&view=logs)|

#### Misc Tests

|Branch|Determinism|Build Correctness|Source build|Spanish|MacOS|
|:--:|:--:|:--|:--:|:--:|:--:|
**main**|[![Build Status](https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/roslyn/roslyn-CI?branchname=main&jobname=Correctness_Determinism&configuration=Correctness_Determinism&label=build)](https://dev.azure.com/dnceng/public/_build/latest?definitionId=15&branchname=main&view=logs)|[![Build Status](https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/roslyn/roslyn-CI?branchname=main&jobname=Correctness_Build&configuration=Correctness_Build&label=build)](https://dev.azure.com/dnceng/public/_build/latest?definitionId=15&branchname=main&view=logs)|[![Build Status](https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/roslyn/roslyn-CI?branchname=main&jobname=Correctness_SourceBuild&configuration=Correctness_SourceBuild&label=build)](https://dev.azure.com/dnceng/public/_build/latest?definitionId=15&branchname=main&view=logs)|[![Build Status](https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/roslyn/roslyn-CI?branchname=main&jobname=Test_Windows_Desktop_Spanish_Release_32&configuration=Test_Windows_Desktop_Spanish_Release_32&label=build)](https://dev.azure.com/dnceng/public/_build/latest?definitionId=15&branchname=main&view=logs)|[![Build Status](https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/roslyn/roslyn-CI?branchname=main&jobname=Test_macOS_Debug&configuration=Test_macOS_Debug&label=build)](https://dev.azure.com/dnceng/public/_build/latest?definitionId=15&branchname=main&view=logs)|
**main-vs-deps**|[![Build Status](https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/roslyn/roslyn-CI?branchname=main-vs-deps&jobname=Correctness_Determinism&configuration=Correctness_Determinism&label=build)](https://dev.azure.com/dnceng/public/_build/latest?definitionId=15&branchname=main-vs-deps&view=logs)|[![Build Status](https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/roslyn/roslyn-CI?branchname=main-vs-deps&jobname=Correctness_Build&configuration=Correctness_Build&label=build)](https://dev.azure.com/dnceng/public/_build/latest?definitionId=15&branchname=main-vs-deps&view=logs)|[![Build Status](https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/roslyn/roslyn-CI?branchname=main-vs-deps&jobname=Correctness_SourceBuild&configuration=Correctness_SourceBuild&label=build)](https://dev.azure.com/dnceng/public/_build/latest?definitionId=15&branchname=main-vs-deps&view=logs)|[![Build Status](https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/roslyn/roslyn-CI?branchname=main-vs-deps&jobname=Test_Windows_Desktop_Spanish_Release_32&configuration=Test_Windows_Desktop_Spanish_Release_32&label=build)](https://dev.azure.com/dnceng/public/_build/latest?definitionId=15&branchname=main-vs-deps&view=logs)|[![Build Status](https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/roslyn/roslyn-CI?branchname=main-vs-deps&jobname=Test_macOS_Debug&configuration=Test_macOS_Debug&label=build)](https://dev.azure.com/dnceng/public/_build/latest?definitionId=15&branchname=main-vs-deps&view=logs)|

[//]: # (End current test results)

### .NET Foundation

This project is part of the [.NET Foundation](http://www.dotnetfoundation.org/projects) along with other
projects like [the .NET Runtime](https://github.com/dotnet/runtime/).
