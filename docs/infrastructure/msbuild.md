# MSBuild usage

## Supporting different MSBuilds

This repo must support the ability to build with a number of different MSBuild configurations:

- MSBuild via Visual Studio: this happens when developers open Roslyn.sln in Visual Studio and execute the build action.  This uses desktop MSBuild to drive the solution. 
- MSBuild via CLI: the cross platform portions of the repo are built via the CLI.  This is a subset of the code contained in Roslyn.sln. 
- MSBuild xcopy: an xcopyable version of MSBuild that is used to run many of our Jenkins legs.  It allows Roslyn to build and run tests on a completely fresh Windows image (no pre-reqs).  The [xcopy-msbuild](https://github.com/jaredpar/xcopy-msbuild) project is responsible for building this image. 
- BuildTools: this is a collection of tools produced by [dotnet/buildtools](https://github.com/dotnet/buildtools) which build a number of dotnet repos. 

This places a small burden on our repo to keep our build props / targets files simple to avoid any odd conflicts.  This is rarely an issue at this point.

## Picking MSBuild

Given our repo supports multiple MSBuild versions, it must pick one to use when building, restoring, etc ...  The preference list is as follows:

1. Developer command prompt: when invoked from inside a developer command prompt the associated MSBuild will be used. 
1. Machine MSBuild: when invoked on a machine with MSBuild 15.0 installed, the first mentioned instance will be used. 
1. XCopy MSBuild: fallback when no other option is available
