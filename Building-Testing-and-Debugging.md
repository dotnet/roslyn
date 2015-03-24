# Building, Testing, and Debugging

## Required Software

The Roslyn source code targets the latest public build of Visual Studio 2015.  At this time that is CTP6.  In order to edit, build and test the source code both the latest public drop and the SDK will need to be installed:

- [Visual Studio 2015 CTP6](http://go.microsoft.com/?linkid=9875137&clcid=0x409&wt.mc_id=o~msft~vscom~download-body~dn906891&campaign=o~msft~vscom~download-body~dn906891)
- [Visual Studio 2015 SDK](http://go.microsoft.com/?linkid=9875738)

## Getting the code

1. Clone https://github.com/dotnet/roslyn
2. Open RoslynLight.sln 

## Running Unit Tests
To run the unit tests:

> msbuild /v:m /m BuildAndTest.proj /p:PublicBuild=true

This command will build and run all of the code / tests which are supported on the current public build of Visual Studio 2015.  

To debug suites use the *xunit.console.x86.exe* runner command which is included in the [xunit.runners](https://www.nuget.org/packages/xunit.runners) NuGet package.  Make sure to use a 2.0 version of the runner.  

> xunit.console.x86.exe [UnitTestDll] -noshadow 

## Contributing
Please see [[Contributing Code]] for details on contributing changes back to the code.

## Using earlier versions of Visual Studio 2015 

The Roslyn build depends on Visual Studio APIs that change from release to release.  In order to use an older version of Visual Studio to load the source code you will need to use the appropriate branch.

> Use git branch --list from the command line to see the possible branches, E.g:

```
git branch --list --all
  master
  remotes/origin/HEAD -> origin/master
  remotes/origin/releases/Dev14CTP5
```

> Select the branch that matches your Visual Studio preview release, E.g:

```
git checkout releases/Dev14CTP5 
```

Note: PRs will only be accepted from the master branch.  

## Strong Name Verification
Roslyn binaries are configured to be delay signed using the Microsoft strong name key.  We are using a new technique to allow these assemblies to be loaded - currently 'fakesign' - these assemblies do not need to have strong name signing disabled to be loaded.  However, they cannot be installed in the GAC neither can they be loaded from a partially trusted AppDomain.

In order to test changes in Visual Studio without affecting the normal development environment, Visual Studio can be run using an isolated registry hive and AppData directories via the /rootSuffix Roslyn command line option.  When Roslyn is built it creates and populates this hive with the necessary packages.

```
"%devenvdir%"\devenv.exe /rootSuffix Roslyn
```

## Download NuGet Packages
From the command prompt, change directory to `<clone dir>` and run `Src\.nuget\nuget restore Src\Roslyn.sln`

This ensures that all of the references and tools needed to build Roslyn are present on the computer.  Because we use toolset packages, it's important to do this before opening the solution.

## Deploying changes to Visual Studio
When you build using VS 2015 the updates will be deployed to the Roslyn Hive and ready for debugging.  Note that only the components up to the Workspaces layer will be deployed.  

**Debugging Visual Studio**

To begin

* In Solution Explorer, right click the project "OpenSourceDebug" and choose "Set as Startup Project"
* Choose Debug\Start Debugging (F5)

At this point, you will be able to debug the code that you changed.  Note that not all aspects of the compiler are executed inside the Visual Studio process, so you may not hit all breakpoints.  Visual Studio will call many of the APIs in your built binaries to power its own features, but if you invoke a build inside your target Visual Studio, that will launch a new instance of csc.exe and vbc.exe.  This technique does not execute vbcscompiler.exe.  csc.exe and vbc.exe are a separate process and are not retained in memory after compilation is complete.

If you have installed and built Roslyn with a previous build of Visual Studio, you may get an InvalidCast out of VS MEF, this is because the VS MEFCache is out of date, I cured this by re-creating the roslyn hive by running this command at a Dos Prompt:

`"%VSSDK140Install%\VisualStudioIntegration\Tools\Bin\CreateExpInstance.exe" /Create /VSInstance=14.0 /RootSuffix=Roslyn`

## To Build a project using OSS built compilers
At the command line type: 

```
set RoslynHive=VisualStudio\14.0Roslyn
MSBuild someproj.vbproj
```

**Removing the code**
To remove the code

* Start Task Manager and end all "VBCSCompiler.exe" processes
* Delete the directory containing your local git clone

**Uninstalling the End User Preview (optional)**

You are welcome to continue to use the End User Preview to provide feedback on the potential new language and IDE features it contains, but if you want to uninstall it, you can do so by following these steps:

* Start Task Manager and end all “VBCSCompiler.exe” processes
* Start Visual Studio
* Go to Tools\Extensions and Updates
* Select "Roslyn Preview" and click “Uninstall”

