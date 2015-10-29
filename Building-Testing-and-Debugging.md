# Building, Testing and Debugging
_This page contains instructions for building Roslyn on Windows. For special instructions on **building Roslyn on Linux** see [here](https://github.com/dotnet/roslyn/blob/master/docs/infrastructure/cross-platform.md)._

## Known Issues
Please see the [known contributor issues](https://github.com/dotnet/roslyn/labels/Contributor%20Pain) that you might encounter contributing to Roslyn. If you issue isn't listed, please file it.

## Required Software

- Visual Studio 2015 with the [Visual Studio Extensibility and Windows 10 tools optional components](https://github.com/dotnet/roslyn/wiki/Getting-Started-on-Visual-Studio-2015) installed
- To prevent unnecessary project.json changes and slow builds, turn off NuGet package restore within Visual Studio
  - Uncheck **Tools** -> **Options** -> **NuGet Package Manager** -> **General** -> **Automatically check for missing packages during build in Visual Studio**

## Getting the code

1. Open Developer Command Prompt for VS2015
2. Clone https://github.com/dotnet/roslyn
3. Run: `Restore.cmd`
4. Due to [Issue #5876](https://github.com/dotnet/roslyn/issues/5876), you should build on the command line before opening in Visual Studio.  Run: `msbuild /v:m /m Roslyn.sln`
5. Open _Roslyn.sln_

## Solutions

There are three solutions in the Roslyn tree:

__Compilers.sln__: Contains just C# and VB compilers, and the compiler APIs.

__Roslyn.sln__: Contains entirety of Roslyn including the C# and VB compilers and the compiler APIs, workspace APIs, features layer, language service, and other Visual Studio integration pieces. This is the solution most contributors should open.

__CrossPlatform.sln__: Represents all the projects that we build across Linux, Windows and Mac. This solution is changing regularly as we bring more and more code to Linux and Mac.

## Running Tests
Tests cannot be run via Test Explorer due to some Visual Studio limitations.

__To run tests:__

From a Visual Studio Command Prompt:

```
msbuild /v:m /m BuildAndTest.proj
```

This will build and run all tests which are supported on Visual Studio 2015.

__To debug tests:__

1. Right-click on the test project you want to debug and choose __Set as Start Project__
2. Press _F5_ to start debugging

Alternatively, some members of the team have been working on a WPF runner that allows selection of individual tests, etc.  Grab the source from [xunit.runner.wpf](https://github.com/pilchie/xunit.runner.wpf), build it and give it a try.

## Contributing
Please see [[Contributing Code]] for details on contributing changes back to the code.

## Deploying and testing within Visual Studio
There is not a supported way to deploy and test any changes you have made within Visual Studio itself. We're currently working on adding support for this and it will be coming soon.