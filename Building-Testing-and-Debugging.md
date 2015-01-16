
# Building, Testing, and Debugging

## Required Software
**Microsoft Visual Studio Ultimate 2015 Preview**

The Roslyn source code currently targets prerelease builds of Visual Studio 2015", the latest preview release can be downloaded free from [http://www.visualstudio.com/en-us/downloads/visual-studio-2015-downloads-vs](http://www.visualstudio.com/en-us/downloads/visual-studio-2015-downloads-vs).

**Microsoft Visual Studio 2015 Preview SDK**

The Visual Studio SDK is used to extend Visual Studio 2015".  This can be downloaded free from a [url:http://www.visualstudio.com/en-us/downloads/visual-studio-14-ctp-vs].

**NuGet package manager**

We use [NuGet](http://nuget.org) with package restore for our dependencies.  We have seen some issues with package restore in some versions of the NuGet Package Manager, so we recommend making sure you have at least NuGet 2.8.1 installed.  Go to Tools\Extensions and Updates and click on the Updates tab to see if you are up to date.

**Latest Visual Studio 2015 Preview is recommended**

When using a Preview build you should ensure you select the repo branch that matches your installed Visual Studio preview, E.g releases/Dev14Preview. Roslyn is being developed at the same time as other core Visual Studio Components, APIs Roslyn is using may change during this preview phase, if you select the matching branch, then the source code you build will match the installed core Visual Studio components. 

## Getting the code

1. Clone (http://github.com/dotnet/roslyn)
2. Switch to the "releases/Dev14Preview" branch

## Using Visual Studio Preview releases 

There are API differences between "master" and "releases/build-preview". If you build "master" you will not be able to test your changes in Visual Studio. 

> Use git branch --list from the command line to see the possible branches, E.g:

```
git branch --list --all
  master
  remotes/origin/HEAD -> origin/master
  remotes/origin/releases/Dev14Preview
```

> Select the branch that matches your Visual Studio preview release, E.g:

```
git checkout releases/Dev14Preview 
```

## Strong Name Verification
Roslyn binaries are configured to be delay signed using the Microsoft strong name key.  We are using a new technique to allow these assemblies to be loaded - currently 'fakesign' - these assemblies do not need to have strong name signing disabled to be loaded.  However, they cannot be installed in the GAC neither can they be loaded from a partially trusted AppDomain.

In order to test changes in Visual Studio without affecting the normal development environment, Visual Studio can be run using an isolated registry hive and AppData directories via the /rootSuffix Roslyn command line option.  When Roslyn is built it creates and populates this hive with the necessary packages.

```
"%devenvdir%"\devenv.exe /rootSuffix Roslyn
```

## Download NuGet Packages
From the command prompt, change directory to `<clone dir>` and run `Src\.nuget\nuget restore Src\Roslyn.sln`

This ensures that all of the references and tools needed to build Roslyn are present on the computer.  Because we use toolset packages, it's important to do this before opening the solution.

## Building the command line compilers
In order to build the command line compilers, you can simply open "Src\Roslyn.sln" from the directory where you created your git clone.  Alternatively, you can build from the command line using `msbuild Src\Roslyn.sln`.  If you want to debug the C# compiler, you should set the “Compilers\CSharp\csc” project as the startup project.  For the Visual Basic compiler, it’s the "Compilers\VisualBasic\vbc" project.

Note that in most situations the compilers will NOT be invoked through csc and vbc for performance reasons, but they are the simplest way to debug.  Other entry points include:

* csc2.exe and vbc2.exe.  These are extremely small native executables that simply start or connect to a VBCSCompiler.exe process and send command line arguments to it.  This allows the VBCSCompiler.exe process to reuse loaded assemblies for multiple projects.
* MSBuild tasks. In the Compilers\Core\MSBuildTasks project there are MSBuild tasks that also connect to VBCSCompiler.exe and send the arguments across.
* Custom hosts of the API.  This includes the Visual Studio IDE experience calling into the compiler, as well as any other application written using the SDK.  Debugging the binaries used by Visual Studio is covered below.

NOTE: By default VBCSCompiler.exe will run for 3 hours after starting.  If you are making changes and rebuilding it often, you can change the timeout by editing Src\Compilers\Core\VBCSCompiler\App.config 

## Deploying changes to Visual Studio
When you build using VS 2015 the updates will be deployed to the Roslyn Hive and ready for debugging.

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

# Running Unit Tests
To run the unit tests:

* Close the solution in Visual Studio (to ensure files are not locked)
* Open a Developer Command Prompt (administrator rights are not required)
* Navigate to the root of your clone
* Run `msbuild /m BuildAndTest.proj /p:DeployExtension=false`

This will first build all sources, and then run all unit tests using the msbuild runner from the [xUnit 2.0 runners NuGet Package](https://www.nuget.org/packages/xunit.runners/2.0.0-alpha-build2576).  Results will be placed in a file named <clone dir>\UnitTestResults.html.  Several warnings are expected as we have a policy of adding skipped unit tests to represent bugs that are not yet fixed.

**Debugging unit test failures**
You can debug a unit test project using *xunit.console.clr4.x86.exe* which is part of the package above:

* Open project properties for the unit test project containing the test you want to debug
* Select the Debug tab
* Change the "Start Action" to "Debug External Program"
* Set the path to the program to be "<clone directory>\Src\packages\xunit.runners.2.0.0-alpha-build2576\tools\xunit.console.x86.exe"
* Enter the full path of the unit test assembly, followed by "-noshadow" to the "Command Line Arguments" text box
* Set a breakpoint in the body of the test you want to run
* Start Debugging (F5)

## Contributing
Please see [How to Contribute] for details on contributing changes back to the code.

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

# Walkthroughs
## Install and build Roslyn

1. Install the latest Visual Studio 14 preview from (http://www.visualstudio.com/en-us/downloads/visual-studio-14-ctp-vs.aspx)
2. Install the corresponding  VSSDK from the same page 
3. Clone the repo using: git clone https://github.com/dotnet/roslyn.git
4. Checkout the branch that matches the downloaded preview E.g: Git checkout releases/Dev14Preview
5. Start VS, Load the Roslyn Solution 
6. Set the Tools\OpenSourceDebug project to the default project 
7. Use the menu to build the solution 

## Build and Debug Roslyn with Visual Studio

1. Install and Configure VS as above
2. Start VS, Load the Roslyn Solution 
3. Press F5 to start debugging 
4. The solution will build and start a new instance of Visual Studio 
5. In the new instance of VS create a new C# or VB project 
6. In the VS with the Roslyn solution open, add a breakpoint to the file: Workspaces\workspace\workspace.cs at line 142 
7. Add an interface to the project created earlier and see the breakpoint hit.

## Build Project with OSS compilers within Visual Studio

1. Install and Configure VS as above
2. Press F5 to start a new instance of VS
3. Create a new C# or VB project
4. Use Tools/Options/Projects and Solutions/Build and Run to set the 'Build Project Verbosity' to 'Normal' so that we can ensure the correct compiler was used
5. Build the solution you built above.
6. Look in the build 0utput window and observe the compiler used is similar to: `%USERPROFILE%\APPDATA\LOCAL\MICROSOFT\VISUALSTUDIO\14.0ROSLYN\EXTENSIONS\MSOPENTECH\OPENSOURCEDEBUG\1.0\csc.exe`

## Build with OSS Roslyn compilers using MSBUILD

1.  In a Visual Studio Command Shell, type the following command:
2.  MSBUILD ConsoleApplication01.csproj /t:Rebuild /p:RoslynHive=VisualStudio\14.0Roslyn
3.  In the build output from this command observe a compiler command line similar to: `%USERPROFILE%\APPDATA\LOCAL\MICROSOFT\VISUALSTUDIO\14.0ROSLYN\EXTENSIONS\MSOPENTECH\OPENSOURCEDEBUG\1.0\csc.exe`

