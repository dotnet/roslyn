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

# Contributing

Please see [Contributing Code](https://github.com/dotnet/wiki/Contributing-Code) for details on contributing changes back to the code.
