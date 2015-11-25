# Required Software

1. [Visual Studio 2015 with Update 1](http://go.microsoft.com/fwlink/?LinkId=691129). _You need Update 1_.
2. Visual Studio 2015 Extensibility Tools. If you already installed Visual Studio, choose "Modify" from the Programs and Features control panel, and check "Visual Studio Extensibility".

# Getting the Code

1. Clone https://github.com/dotnet/roslyn
2. Run the "Developer Command Prompt for VS2015" from your start menu.
3. Run `Restore.cmd` in the command prompt to restore NuGet packages.
4. Due to [Issue #5876](https://github.com/dotnet/roslyn/issues/5876), you should build on the command line before opening in Visual Studio. Run `msbuild /v:m /m Roslyn.sln`
5. Open _Roslyn.sln_

# Running Tests

Tests cannot be run via Test Explorer due to some Visual Studio limitations.

1. Run the "Developer Command Prompt for VS2015" from your start menu.
2. Run `msbuild /v:m /m BuildAndTest.proj` in the command prompt.

# Trying Your Changes in Visual Studio

Starting with Update 1, it is now possible to run your changes inside Visual
Studio to try them out. Some projects in Roslyn.sln, listed below, build Visual
Studio extensions. When you build those projects, they automatically deploy
into an experimental instance of Visual Studio. The first time you clone, you
should first do a full build of Roslyn.sln to make sure everything is primed.
Then, you can run Visual Studio by right clicking the appropriate project in
Visual Studio, setting it as a startup project, and pressing F5. You can also
run Visual Studio after building by running a "Developer Command Prompt for
VS2015" and then running `devenv /rootsuffix RoslynDev`.

Here are what is deployed with each extension, by project that builds it. If
you're working on a particular area, you probably want to set the appropriate
project as your startup project to ensure the right things are built and
deployed.

- **VisualStudioSetup**: this project builds Roslyn.VisualStudio.Setup.vsix. It
  contains the core language services that provide C# and VB editing. It also
  contains the copy of the compiler that is used to drive IntelliSense and
  semantic analysis in Visual Studio. Although this is the copy of the compiler
  that's used to generate squiggles and other information, it's not the
  compiler used to actually produce your final .exe or .dll when you do a
  build. If you're working on fixing an IDE bug, this is the project you want
  to use.
- **CompilerExtension**: this project builds Roslyn.Compilers.Extension.vsix.
  This deploys a copy of the command line compilers that are used to do actual
  builds in the IDE. It only affects builds triggered from the Visual Studio
  experimental instance it's installed into, so it won't affect your regular
  builds. Note that if you install just this, the IDE won't know about any
  language features included in your build. If you're regularly working on new
  language features, you may wish to consider building both the
  CompilerExtension and VisualStudioSetup projects to ensure the real build and
  live analysis are synchronized.
- **ExpressionEvaluatorPackage**: this deploys the expression evaluator and
  result providers, the components that are used by the debugger to parse and
  evaluate C# and VB expressions in the watch window, immediate window, and
  more. These components are only used when debugging.
- **VisualStudioInteractiveWindow**: this deploys the "base" interactive window
  experience that is shared by Roslyn, Python, and other languages. This code
  is core support only and doesn't include any language specific logic.
- **VisualStudioInteractiveSetup**: this deploys the Roslyn (i.e. C# and VB)
  specific parts of the interactive window. If you're working on the
  interactive experience, this the project you want to use as your startup
  project.

The experimental instance used by Roslyn is an entirely separate instance of
Visual Studio with it's own settings and installed extensions. It's also, by
default, a separate instance than the standard "Experimental Instance" used by
other Visual Studio SDK projects. If you're familiar with the idea of Visual
Studio hives, we deploy into the RoslynDev root suffix.

If you want to try your extension in your day-to-day use of Visual Studio, you
can find the extensions you built in your Binaries folder with the .vsix extension.
You can double-click the extension to install it into your
main Visual Studio hive. This will replace the base installed version. You can
uninstall your version and go back to the "real" version by going Tools >
Extensions and Updates, finding your extension, and choosing Uninstall. Your
extension should be marked with the "Experimental" flag.

# Contributing

Please see [Contributing Code](https://github.com/dotnet/wiki/Contributing-Code) for details on contributing changes back to the code.
