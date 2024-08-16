<p align="center">
<img width="450" src="https://user-images.githubusercontent.com/46729679/109719841-17b7dd00-7b5e-11eb-8f5e-87eb2d4d1be9.png" alt="Roslyn logo">
</p>

<h1 align="center">The .NET Compiler Platform</h1>

<p align="center"><a href="http://aka.ms/discord-csharp-roslyn" rel="nofollow"><img title="Chat on Discord" src="docs/img/discord-mark-white.png" /></a></p>

Roslyn is the open-source implementation of both the C# and Visual Basic compilers with an API surface for building code analysis tools.

### C# and Visual Basic Language Feature Suggestions

If you want to suggest a new feature for the C# or Visual Basic languages go here:
- [dotnet/csharplang](https://github.com/dotnet/csharplang) for C# specific issues
- [dotnet/vblang](https://github.com/dotnet/vblang) for VB-specific features
- [dotnet/csharplang](https://github.com/dotnet/csharplang) for features that affect both languages

### Contributing

All work on the C# and Visual Basic compiler happens directly on [GitHub](https://github.com/dotnet/roslyn). Both core team members and external contributors send pull requests which go through the same review process.

If you are interested in fixing issues and contributing directly to the code base, a great way to get started is to ask some questions on [GitHub Discussions](https://github.com/dotnet/roslyn/discussions)! Then check out our [contributing guide](https://github.com/dotnet/roslyn/blob/main/CONTRIBUTING.md) which covers the following:

- [Coding guidelines](https://github.com/dotnet/roslyn/blob/main/docs/wiki/Contributing-Code.md)
- [The development workflow, including debugging and running tests](https://github.com/dotnet/roslyn/blob/main/docs/contributing/Building%2C%20Debugging%2C%20and%20Testing%20on%20Windows.md)
- [Submitting pull requests](<https://github.com/dotnet/roslyn/blob/main/CONTRIBUTING.md#How-to-submit-a-PR>)
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
**main**|[![Build Status](https://dev.azure.com/dnceng-public/public/_apis/build/status/dotnet/roslyn/roslyn-CI?branchname=main&jobname=Build_Windows_Debug&configuration=Build_Windows_Debug&label=build)](https://dev.azure.com/dnceng-public/public/_build/latest?definitionId=95&branchname=main&view=logs)|[![Build Status](https://dev.azure.com/dnceng-public/public/_apis/build/status/dotnet/roslyn/roslyn-CI?branchname=main&jobname=Build_Windows_Release&configuration=Build_Windows_Release&label=build)](https://dev.azure.com/dnceng-public/public/_build/latest?definitionId=95&branchname=main&view=logs)|[![Build Status](https://dev.azure.com/dnceng-public/public/_apis/build/status/dotnet/roslyn/roslyn-CI?branchname=main&jobname=Build_Unix_Debug&configuration=Build_Unix_Debug&label=build)](https://dev.azure.com/dnceng-public/public/_build/latest?definitionId=95&branchname=main&view=logs)|

#### Desktop Unit Tests

|Branch|Debug x86|Debug x64|Release x86|Release x64|
|:--:|:--:|:--:|:--:|:--:|
**main**|[![Build Status](https://dev.azure.com/dnceng-public/public/_apis/build/status/dotnet/roslyn/roslyn-CI?branchname=main&jobname=Test_Windows_Desktop_Debug_32&configuration=Test_Windows_Desktop_Debug_32&label=build)](https://dev.azure.com/dnceng-public/public/_build/latest?definitionId=95&branchname=main&view=logs)|[![Build Status](https://dev.azure.com/dnceng-public/public/_apis/build/status/dotnet/roslyn/roslyn-CI?branchname=main&jobname=Test_Windows_Desktop_Debug_64&configuration=Test_Windows_Desktop_Debug_64&label=build)](https://dev.azure.com/dnceng-public/public/_build/latest?definitionId=95&branchname=main&view=logs)|[![Build Status](https://dev.azure.com/dnceng-public/public/_apis/build/status/dotnet/roslyn/roslyn-CI?branchname=main&jobname=Test_Windows_Desktop_Release_32&configuration=Test_Windows_Desktop_Release_32&label=build)](https://dev.azure.com/dnceng-public/public/_build/latest?definitionId=95&branchname=main&view=logs)|[![Build Status](https://dev.azure.com/dnceng-public/public/_apis/build/status/dotnet/roslyn/roslyn-CI?branchname=main&jobname=Test_Windows_Desktop_Release_64&configuration=Test_Windows_Desktop_Release_64&label=build)](https://dev.azure.com/dnceng-public/public/_build/latest?definitionId=95&branchname=main&view=logs)|

#### CoreClr Unit Tests

|Branch|Windows Debug|Windows Release|Linux|
|:--:|:--:|:--:|:--:|
**main**|[![Build Status](https://dev.azure.com/dnceng-public/public/_apis/build/status/dotnet/roslyn/roslyn-CI?branchname=main&jobname=Test_Windows_CoreClr_Debug&configuration=Test_Windows_CoreClr_Debug&label=build)](https://dev.azure.com/dnceng-public/public/_build/latest?definitionId=95&branchname=main&view=logs)|[![Build Status](https://dev.azure.com/dnceng-public/public/_apis/build/status/dotnet/roslyn/roslyn-CI?branchname=main&jobname=Test_Windows_CoreClr_Release&configuration=Test_Windows_CoreClr_Release&label=build)](https://dev.azure.com/dnceng-public/public/_build/latest?definitionId=95&branchname=main&view=logs)|[![Build Status](https://dev.azure.com/dnceng-public/public/_apis/build/status/dotnet/roslyn/roslyn-CI?branchname=main&jobname=Test_Linux_Debug&configuration=Test_Linux_Debug&label=build)](https://dev.azure.com/dnceng-public/public/_build/latest?definitionId=95&branchname=main&view=logs)|

#### Integration Tests

|Branch|Debug x86|Debug x64|Release x86|Release x64
|:--:|:--:|:--:|:--:|:--:|
**main**|[![Build Status](https://dev.azure.com/dnceng-public/public/_apis/build/status/dotnet/roslyn/roslyn-integration-CI?branchname=main&jobname=VS_Integration_Debug_32&configuration=VS_Integration_Debug_32&label=build)](https://dev.azure.com/dnceng-public/public/_build/latest?definitionId=96&branchname=main&view=logs)|[![Build Status](https://dev.azure.com/dnceng-public/public/_apis/build/status/dotnet/roslyn/roslyn-integration-CI?branchname=main&jobname=VS_Integration_Debug_64&configuration=VS_Integration_Debug_64&label=build)](https://dev.azure.com/dnceng-public/public/_build/latest?definitionId=96&branchname=main&view=logs)|[![Build Status](https://dev.azure.com/dnceng-public/public/_apis/build/status/dotnet/roslyn/roslyn-integration-CI?branchname=main&jobname=VS_Integration_Release_32&configuration=VS_Integration_Release_32&label=build)](https://dev.azure.com/dnceng-public/public/_build/latest?definitionId=96&branchname=main&view=logs)|[![Build Status](https://dev.azure.com/dnceng-public/public/_apis/build/status/dotnet/roslyn/roslyn-integration-CI?branchname=main&jobname=VS_Integration_Release_64&configuration=VS_Integration_Release_64&label=build)](https://dev.azure.com/dnceng-public/public/_build/latest?definitionId=96&branchname=main&view=logs)|

#### Misc Tests

|Branch|Determinism|Analyzers|Build Correctness|Source build|TODO/Prototype|Spanish|MacOS|
|:--:|:--:|:--:|:--:|:--:|:--:|:--:|:--:|
**main**|[![Build Status](https://dev.azure.com/dnceng-public/public/_apis/build/status/dotnet/roslyn/roslyn-CI?branchname=main&jobname=Correctness_Determinism&configuration=Correctness_Determinism&label=build)](https://dev.azure.com/dnceng-public/public/_build/latest?definitionId=95&branchname=main&view=logs)|[![Build Status](https://dev.azure.com/dnceng-public/public/_apis/build/status/dotnet/roslyn/roslyn-CI?branchname=main&jobname=Correctness_Analyzers&configuration=Correctness_Analyzers&label=build)](https://dev.azure.com/dnceng-public/public/_build/latest?definitionId=95&branchname=main&view=logs)|[![Build Status](https://dev.azure.com/dnceng-public/public/_apis/build/status/dotnet/roslyn/roslyn-CI?branchname=main&jobname=Correctness_Build_Artifacts&configuration=Correctness_Build_Artifacts&label=build)](https://dev.azure.com/dnceng-public/public/_build/latest?definitionId=95&branchname=main&view=logs)|[![Build Status](https://dev.azure.com/dnceng-public/public/_apis/build/status/dotnet/roslyn/roslyn-CI?branchname=main&jobname=Source-Build+(Managed)&configuration=Source-Build+(Managed)&label=build)](https://dev.azure.com/dnceng-public/public/_build/latest?definitionId=95&branchname=main&view=logs)|[![Build Status](https://dev.azure.com/dnceng-public/public/_apis/build/status/dotnet/roslyn/roslyn-CI?branchname=main&jobname=Correctness_TodoCheck&configuration=Correctness_TodoCheck&label=build)](https://dev.azure.com/dnceng-public/public/_build/latest?definitionId=95&branchname=main&view=logs)|[![Build Status](https://dev.azure.com/dnceng-public/public/_apis/build/status/dotnet/roslyn/roslyn-CI?branchname=main&jobname=Test_Windows_Desktop_Spanish_Release_64&configuration=Test_Windows_Desktop_Spanish_Release_64&label=build)](https://dev.azure.com/dnceng-public/public/_build/latest?definitionId=95&branchname=main&view=logs)|[![Build Status](https://dev.azure.com/dnceng-public/public/_apis/build/status/dotnet/roslyn/roslyn-CI?branchname=main&jobname=Test_macOS_Debug&configuration=Test_macOS_Debug&label=build)](https://dev.azure.com/dnceng-public/public/_build/latest?definitionId=95&branchname=main&view=logs)|


[//]: # (End current test results)

### .NET Foundation

This project is part of the [.NET Foundation](http://www.dotnetfoundation.org/projects) along with other projects like [the .NET Runtime](https://github.com/dotnet/runtime/).
