# Building, Debugging and Testing on Windows

## Working with the code

Using the command line Roslyn can be developed using the following pattern:

1. Clone https://github.com/dotnet/roslyn
1. Run Restore.cmd
1. Run Build.cmd
1. Run Test.cmd

## Recommended version of .NET Framework

The minimal required version of .NET Framework is 4.6, however 4.7.2 is recommended for best developer experience.

The projects in this repository are configured to build with Portable PDBs, which are supported in stack traces starting with .NET Framework 4.7.2.
If a stack trace is displayed on .NET Framework older than 4.7.2 (e.g. by xUnit when a test fails) it won't contain source and line information.

.NET Framework 4.7.2 is included in [Windows 10 April 2018 Update](https://blogs.windows.com/windowsexperience/2018/04/30/how-to-get-the-windows-10-april-2018-update/). It can also be installed from the [Microsoft Download Center](https://www.microsoft.com/net/download/dotnet-framework-runtime).

## Developing with Visual Studio 2017

1. [Visual Studio 2017 Version 15.7](https://www.visualstudio.com/vs/preview/)
    - Ensure C#, VB, MSBuild, .NET Core and Visual Studio Extensibility are included in the selected work loads
    - Ensure Visual Studio is on Version "15.7" or greater
1. [.NET Core SDK 2.1.401](https://www.microsoft.com/net/download/core) (the installers are: [Windows x64 installer](https://dotnetcli.blob.core.windows.net/dotnet/Sdk/2.1.401/dotnet-sdk-2.1.401-win-x64.exe), [Windows x86 installer](https://dotnetcli.blob.core.windows.net/dotnet/Sdk/2.1.401/dotnet-sdk-2.1.401-win-x86.exe))
1. [PowerShell 3.0 or newer](https://docs.microsoft.com/en-us/powershell/scripting/setup/installing-windows-powershell). If you are on Windows 10, you are fine; you'll only need to upgrade if you're on Windows 7. The download link is under the "upgrading existing Windows PowerShell" heading.
1. Run Restore.cmd
1. Open Roslyn.sln

If you already installed Visual Studio and need to add the necessary work loads or move to version 15.7:
do the following:

- Run the Visual Studio Installer from your start menu. You can just search for "Visual Studio Installer". If you can't find it, it's typically located at "C:\Program Files (x86)\Microsoft Visual Studio\Installer\vs_installer.exe"
- The Visual Studio installation will be listed under the Installed section
- Click on the menu icon (three horizontal lines), click Modify
- Choose the workloads listed above and click Modify

## Running Tests

There are a number of options for running the core Roslyn unit tests:

### Command Line

The Test.cmd script will run our unit test on already built binaries.  It can be passed the -build arguments to force a new build before running tests.

1. Run the "Developer Command Prompt for VS2017" from your start menu.
2. Navigate to the directory of your Git clone.
3. Run `msbuild /v:m /m /nodereuse:false BuildAndTest.proj` in the command prompt.

### Test Explorer

Tests can be run and debugged from the Test Explorer window. For best performance, we recommend the following:

1. Open **Tools &rarr; Options... &rarr; Test**
    1. Check the box for **Discover tests in real time from source files**
    2. Uncheck the box for **Additionally discover tests from build assemblies...**
2. Use the Search box of Test Explorer to narrow the scope of visible tests to the feature(s) you are working on
3. When you are not actively running tests, set the search query to `__NonExistent__` to hide all tests from the UI

### WPF Test Runner

To debug through tests, you can right click the test project that contains your
tests and choose **Set as Startup Project**. Then press F5. This will run the
tests under the command line runner.  Some members of the team have been
working on a GUI runner that allows selection of individual tests, etc.  Grab
the source from
[xunit.runner.wpf](https://github.com/pilchie/xunit.runner.wpf), build it and
give it a try.

## Trying Your Changes in Visual Studio

The Rosyln solution is designed to support easy debugging via F5.  Several of our
projects produce VSIX which deploy into Visual Studio during build.  The F5 operation
will start a new Visual Studio instance using those VSIX which override our installed
binaries.  This means trying out a change to the language, IDE or debugger is as
simple as hitting F5.

The startup project needs to be set to `RoslynDeployment`.  This should be
the default but in same cases will need to be set explicitly.

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

If you want to try your extension in your day-to-day use of Visual Studio, you
can find the extensions you built in your Binaries folder with the .vsix
extension. You can double-click the extension to install it into your main
Visual Studio hive. This will replace the base installed version. Once it's
installed, you'll see it marked as "Experimental" in Tools > Extensions and
Updates to indicate you're running your experimental version. You can uninstall
your version and go back to the originally installed version by choosing your
version and clicking Uninstall.

If you made changes to a Roslyn compiler and want to build any projects with it, you can either
use the Visual Studio hive where your **CompilerExtension** is installed, or from
command line, run msbuild with `/p:BootstrapBuildPath=YourBootstrapBuildPath`.
`YourBootstrapBuildPath` could be any directory on your machine so long as it had
csc and vbc inside it. You can check the cibuild.cmd and see how it is used.

## Contributing

Please see [Contributing Code](https://github.com/dotnet/roslyn/blob/master/CONTRIBUTING.md) for details on contributing changes back to the code.
