# Building, Testing and Debugging
_This page contains instructions for building Roslyn on Windows. For special instructions on **building Roslyn on Linux** see [here](https://github.com/dotnet/roslyn/blob/master/docs/infrastructure/cross-platform.md)._

## Known Issues
Please see the [known contributor issues](https://github.com/dotnet/roslyn/labels/Known%20Contributor%20Issue) that you might encounter contributing to Roslyn. If you issue isn't listed, please file it.

## Required Software

- Visual Studio 2015 with the [Visual Studio Extensibility and Windows 10 tools optional components](https://github.com/dotnet/roslyn/wiki/Getting-Started-on-Visual-Studio-2015) installed
- To prevent unnecessary project.json changes, turn off NuGet package restore within Visual Studio
  - Uncheck **Tools** -> **Options** -> **NuGet Package Manager** -> **General** -> **Automatically check for missing packages during build in Visual Studio**

## Getting the code

1. Open Developer Command Prompt for VS2015
2. Clone https://github.com/dotnet/roslyn
3. Run: `nuget.exe restore Roslyn.sln`
4. Open Roslyn.sln 

## Running Unit Tests
To run the unit tests:

> msbuild /v:m /m BuildAndTest.proj /p:PublicBuild=true /p:DeployExtension=false

This command will build and run all of the code / tests which are supported on the current public build of Visual Studio 2015.  

To debug suites use the *xunit.console.x86.exe* runner command which is included in the [xunit.runners](https://www.nuget.org/packages/xunit.runners) NuGet package.  Make sure to use a 2.0 version of the runner.  

> xunit.console.x86.exe [UnitTestDll] -noshadow 

## Contributing
Please see [[Contributing Code]] for details on contributing changes back to the code.

## Deploying and testing within Visual Studio
There is not a supported way to deploy and test any changes you have made within Visual Studio itself. We're currently working on adding support for this and it will be coming soon.
