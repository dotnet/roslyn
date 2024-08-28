# Building, Debugging and Testing on Windows

## Working with the code

Using the command line, Roslyn can be developed using the following pattern:

1. Clone https://github.com/dotnet/roslyn
1. Run Restore.cmd
1. Run Build.cmd
1. Run Test.cmd

## Recommended version of .NET Framework

The minimal required version of .NET Framework is 4.7.2.

## Developing with Visual Studio 2022

1. Use latest [Visual Studio 2022 Preview](https://visualstudio.microsoft.com/vs/preview/vs2022/)
    - Ensure Visual Studio extension development is included in the selected workloads
    - Ensure C# and Visual Basic, MSBuild, and .NET Core are included in the selected individual components
    - Ensure "Use previews of the .NET Core SDK" is checked in Tools -> Options -> Environment -> Preview Features
    - Restart Visual Studio
1. Install the [.NET 8.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) which matches the `sdk.version` property in [global.json](../../global.json#L3)
1. [PowerShell 5.0 or newer](https://docs.microsoft.com/en-us/powershell/scripting/setup/installing-windows-powershell). If you are on Windows 10, you are fine; you'll only need to upgrade if you're on earlier versions of Windows. The download link is under the ["Upgrading existing Windows PowerShell"](https://docs.microsoft.com/en-us/powershell/scripting/install/installing-windows-powershell?view=powershell-6#upgrading-existing-windows-powershell) heading.
1. Run Restore.cmd
1. Open Roslyn.sln

## Developing with Visual Studio Code

See the [Building, Debugging, and Testing on Unix](Building,%20Debugging,%20and%20Testing%20on%20Unix.md#working-in-visual-studio-code) documentation to get started developing Roslyn using Visual Studio Code.

## Running Tests

There are a number of options for running the core Roslyn unit tests:

### Command Line

The Test.cmd script will run our unit test on already built binaries. It can be passed the `-build` argument to force a new build before running tests.

1. Run the "Developer Command Prompt for VS2022" from your start menu.
2. Navigate to the directory of your Git clone.
3. Run `Test.cmd` in the command prompt.

You can more precisely control how the tests are run by running the eng/build.ps1 script directly with the relevant options. For example passing in the `-test` switch will run the tests on .NET Framework, whilst passing in the `-testCoreClr` switch will run the tests on .NET Core.

The results of the tests can be viewed in the artifacts/TestResults directory.

### Test Explorer

Tests can be run and debugged from the Test Explorer window. For best performance, we recommend the following:

1. Open **Tools &rarr; Options... &rarr; Test**
    1. Check the box for **Discover tests in real time from source files**
    2. Uncheck the box for **Additionally discover tests from build assemblies...**
2. Use the Search box of Test Explorer to narrow the scope of visible tests to the feature(s) you are working on
3. When you are not actively running tests, set the search query to `__NonExistent__` to hide all tests from the UI

#### Testing in Linux using WSL

Tests can be run and debugged under WSL. This requires a bit of setup the first time. After that, it is as easy as selecting WSL as the active environment in Test Explorer.

1. Install WSL by running `wsl --install` ([details](https://docs.microsoft.com/en-us/windows/wsl/setup/environment#get-started))
2. In the VS installer, install ".NET Debugging with WSL" as an individual component
3. In Test Explorer, go into "Configure Remote Test Environments" (under the gear icon) and uncomment the wsl/Ubuntu environment:
![image](https://user-images.githubusercontent.com/12466233/169365278-485573de-0b14-466e-a96d-b1de5e67e891.png)
![image](https://user-images.githubusercontent.com/12466233/169365165-a2bfc6bc-96ba-45fa-8705-eb8e7997df62.png)
4. Select that test environment from Test Explorer drop-down:
![image](https://user-images.githubusercontent.com/12466233/169365451-681fcbb2-0b21-46f6-bc30-de58ce01bfdc.png)
5. Run a test from Test Explorer test list. This will prompt you to install some dotnet bits into the WSL environment the first time.
6. Debug a test. This will prompt you to install some remote debugging bits into the WSL environment the first time.

[More details](https://docs.microsoft.com/en-us/visualstudio/debugger/debug-dotnet-core-in-wsl-2)

### WPF Test Runner

To debug through tests, you can right click the test project that contains your
tests and choose **Set as Startup Project**. Then press F5. This will run the
tests under the command line runner.  Some members of the team have been
working on a GUI runner that allows selection of individual tests, etc.  Grab
the source from
[xunit.runner.wpf](https://github.com/pilchie/xunit.runner.wpf), build it and
give it a try.

## Trying Your Changes in Visual Studio

### Deploying with command-line (recommended method)

You can build and deploy with the following command:
`.\Build.cmd -Restore -Configuration Release -deployExtensions -launch`.

Then you can launch the `RoslynDev` hive with `devenv /rootSuffix RoslynDev`.

### Deploying with F5

The Rosyln solution is designed to support easy debugging via F5.  Several of our
projects produce VSIX which deploy into Visual Studio during build.  The F5 operation
will start a new Visual Studio instance using those VSIX which override our installed
binaries.  This means trying out a change to the language, IDE or debugger is as
simple as hitting F5. Note that for changes to the compiler, out-of-process builds won't use the privately built version of the compiler.

The startup project needs to be set to `RoslynDeployment`.  This should be
the default but in some cases will need to be set explicitly.

Here are what is deployed with each extension, by project that builds it. If
you're working on a particular area, you probably want to set the appropriate
project as your startup project to optimize building and deploying only the relevant bits.

- **Roslyn.VisualStudio.Setup**: this project can be found inside the VisualStudio folder
  from the Solution Explorer, and builds Roslyn.VisualStudio.Setup.vsix. It
  contains the core language services that provide C# and VB editing. It also
  contains the copy of the compiler that is used to drive IntelliSense and
  semantic analysis in Visual Studio. Although this is the copy of the compiler
  that's used to generate squiggles and other information, it's not the
  compiler used to actually produce your final .exe or .dll when you do a
  build. If you're working on fixing an IDE bug, this is the project you want
  to use.
- **Roslyn.VisualStudio.InteractiveComponents**: this project can be found in the
  Interactive\Setup folder from the Solution Explorer, and builds
  Roslyn.VisualStudio.InteractiveComponents.vsix.
- **Roslyn.Compilers.Extension**: this project can be found inside the Compilers\Packages folder
  from the Solution Explorer, and builds Roslyn.Compilers.Extension.vsix.
  This deploys a copy of the command line compilers that are used to do actual
  builds in the IDE. It only affects builds triggered from the Visual Studio
  experimental instance it's installed into, so it won't affect your regular
  builds. Note that if you install just this, the IDE won't know about any
  language features included in your build. If you're regularly working on new
  language features, you may wish to consider building both the
  CompilerExtension and VisualStudioSetup projects to ensure the real build and
  live analysis are synchronized.
- **ExpressionEvaluatorPackage**: this project can be found inside the
  ExpressionEvaluator\Setup folder from the Solution Explorer, and builds
  ExpressionEvaluatorPackage.vsix. This deploys the expression evaluator and
  result providers, the components that are used by the debugger to parse and
  evaluate C# and VB expressions in the Watch window, Immediate window, and
  more. These components are only used when debugging.

The experimental instance used by Roslyn is an entirely separate instance of
Visual Studio with it's own settings and installed extensions. It's also, by
default, a separate instance than the standard "Experimental Instance" used by
other Visual Studio SDK projects. If you're familiar with the idea of Visual
Studio hives, we deploy into the RoslynDev root suffix.

### Deploying with VSIX and Nuget package

If you want to try your extension in your day-to-day use of Visual Studio, you
can find the extensions you built in your Binaries folder with the .vsix
extension. You can double-click the extension to install it into your main
Visual Studio hive. This will replace the base installed version. Once it's
installed, you'll see it marked as "Experimental" in Tools > Extensions and
Updates to indicate you're running your experimental version. You can uninstall
your version and go back to the originally installed version by choosing your
version and clicking Uninstall.

If you only install the VSIX, then the IDE will behave correctly (ie. new compiler
and IDE behavior), but the Build operation or building from the command-line won't.
To fix that, add a reference to the `Microsoft.Net.Compilers.Toolset` you built into
your csproj. As shown below, you'll want to (1) add a nuget source pointing to your local build folder,
(2) add the package reference, then (3) verify the Build Output of your project with a
`#error version` included in your program.

![image](https://user-images.githubusercontent.com/12466233/81205885-25252a80-8f80-11ea-9d75-268c7fe6f3ed.png)

![image](https://user-images.githubusercontent.com/12466233/81205974-4128cc00-8f80-11ea-93ec-641d87662b12.png)

![image](https://user-images.githubusercontent.com/12466233/81206129-7fbe8680-8f80-11ea-9438-acc0481a3585.png)

## Trying Your Changes in Visual Studio Code

See [VSCode docs](https://github.com/dotnet/vscode-csharp/blob/main/CONTRIBUTING.md#using-a-locally-developed-roslyn-server).

## Various other tips

### Referencing bootstrap compiler

If you made changes to a Roslyn compiler and want to build any projects with it, you can either
use the Visual Studio hive where your **CompilerExtension** is installed, or from
command line, run msbuild with `/p:BootstrapBuildPath=YourBootstrapBuildPath`.
`YourBootstrapBuildPath` could be any directory on your machine so long as it had
csc and vbc inside it. You can check the cibuild.cmd and see how it is used.

### Troubleshooting your setup

To confirm what version of the compiler is being used, include `#error version` in your program
and the compiler will produce a diagnostic including its own version as well as the language
version it is operating under.

You can also attach a debugger to Visual Studio and check the loaded modules, looking at the folder
where the various `CodeAnalysis` modules were loaded from (the `RoslynDev` should load them somewhere
under `AppData`, not from `Program File`).

### Testing on the [dotnet/runtime](https://github.com/dotnet/runtime) repo

1. make sure that you can build the `runtime` repo as baseline (run `build.cmd libs+libs.tests`, which should be sufficient to build all C# code, installing any prerequisites if prompted to, and perhaps `git clean -xdf` and `build.cmd -restore` initially - see [runtime repo documentation](https://github.com/dotnet/runtime/blob/main/docs/workflow/README.md) for specific prerequisites and build instructions)
2. `build.cmd -pack` on your `roslyn` repo
3. in `%userprofile%\.nuget\packages\microsoft.net.compilers.toolset` delete the version of the toolset that you just packed so that the new one will get put into the cache
4. modify your local enlistment of `runtime` as illustrated in [this commit](https://github.com/RikkiGibson/runtime/commit/da3c6d96c3764e571269b07650a374678b476384) then build again
    - add `<RestoreAdditionalProjectSources><PATH-TO-YOUR-ROSLYN-ENLISTMENT>\artifacts\packages\Debug\Shipping\</RestoreAdditionalProjectSources>` using the local path to your `roslyn` repo to `Directory.Build.props`
    - add `<UsingToolMicrosoftNetCompilers>true</UsingToolMicrosoftNetCompilers>` and `<MicrosoftNetCompilersToolsetVersion>4.1.0-dev</MicrosoftNetCompilersToolsetVersion>` with the package version you just packed (look in above artifacts folder) to `eng/Versions.props`

### Testing a VS insertion

See internal documentation for that process [here](https://microsoft.sharepoint.com/teams/managedlanguages/_layouts/OneNote.aspx?id=%2Fteams%2Fmanagedlanguages%2Ffiles%2FTeam%20Notebook%2FRoslyn%20Team%20Notebook%20-%20New&wd=target%28Infrastructure.one%7C0E3C75FE-0827-41BF-9696-F5CD7105BC9A%2FRun%20RPS%20against%20Roslyn%20PR%7C7A74A1E4-1507-41BC-84F1-F9159C853177%2F%29).

### Testing with extra IOperation validation

Run `build.cmd -testIOperation` which sets the `ROSLYN_TEST_IOPERATION` environment variable to `true` and runs the tests.
For running those tests in an IDE, the easiest is to find the `//#define ROSLYN_TEST_IOPERATION` directive and uncomment it.
See more details in the [IOperation test hook](https://github.com/dotnet/roslyn/blob/main/docs/compilers/IOperation%20Test%20Hook.md) doc.

### Replicating Failures in the Used Assemblies leg

In order to replicate test failures in that leg, there are a few options:

1. Uncomment `src/Compilers/Test/Core/Compilation/CompilationExtensions.cs:9`, which defines `ROSLYN_TEST_USEDASSEMBLIES`, and run your tests. Do _not_ check this in, as it
will enable the test hook for every test in every project and significantly slow down regular test runs.
2. Set the `ROSLYN_TEST_USEDASSEMBLIES` environment variable and restart VS with it set.
3. Set a breakpoint at the start of `CSharpTestBase.VerifyUsedAssemblyReferences` in `src/Compilers/Test/Utilities/CSharp/CSharpTestBase.cs`. When it breaks, use VS's jump to location or
drag the instruction pointer past the early check and return on `EnableVerifyUsedAssemblies`.

When a test failure is isolated, please add a _dedicated_ test for this (ie. failing even when the Used Assemblies validation isn't enabled) to make it easier to avoid future regressions.  
Preferrably, don't replicate the entire original test, just enough to hit the bug to ensure that it's protected against regressions.  
Before pushing a relevant fix to CI, you can validate locally using the `-testUsedAssemblies` command-line option for `build.cmd`. For example: `build.cmd -testCoreClr -testCompilerOnly -testUsedAssemblies`.

### Running the PublicAPI fixer

1. Install `dotnet-format` as a global tool. It does ship as part of the SDK, but a separate version can be installed as a global tool and invoked with `dotnet-format {options}`.
`C:\Source\roslyn> dotnet tool install -g dotnet-format --version "6.*" --add-source https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet6/nuget/v3/index.json`
2. Restore and build `Compilers.slnf`. This is necessary to ensure the source generator project is built and we can load the generator assembly when running `dotnet-format`.
`C:\Source\roslyn> .\restore.cmd`
`C:\Source\roslyn> .\build.cmd`
3. Invoke the `dotnet-format` global tool. Running only the analyzers subcommand and fixing only the "missing Public API signature" diagnostic. We must also pass the `--include-generated` flag to include source generated documents in the analysis.
`C:\Source\roslyn> cd ..`
`C:\Source> dotnet-format analyzers .\roslyn\Compilers.slnf --diagnostics=RS0016 --no-restore --include-generated -v diag`


## Contributing

Please see [Contributing Code](https://github.com/dotnet/roslyn/blob/main/CONTRIBUTING.md) for details on contributing changes back to the code.
