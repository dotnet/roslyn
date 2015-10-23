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
3. Run: `nuget.exe restore Roslyn.sln`
4. Due to [Issue #5876](https://github.com/dotnet/roslyn/issues/5876), you should build on the command line before opening in Visual Studio.  Run: `msbuild /v:m /m Roslyn.sln`
5. Open Roslyn.sln 

## Running Unit Tests
To run the unit tests:

> msbuild /v:m /m BuildAndTest.proj

This command will build and run all of the code / tests which are supported on the current public build of Visual Studio 2015.  

To debug suites use the *xunit.console.x86.exe* runner command which is included in the [xunit.runner.console](https://www.nuget.org/packages/xunit.runner.console) NuGet package.  Make sure to use version 2.1 of the runner.

> xunit.console.x86.exe [UnitTestDll] -noshadow 

Alternatively, some members of the team have been working on a WPF runner that allows selection of individual tests, etc.  Grab the source from [xunit.runner.wpf](https://github.com/pilchie/xunit.runner.wpf), build it and give it a try.

## Contributing
Please see [[Contributing Code]] for details on contributing changes back to the code.

## Deploying and testing within Visual Studio
There is not a supported way to deploy and test any changes you have made within Visual Studio itself. We're currently working on adding support for this and it will be coming soon.