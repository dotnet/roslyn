# Capturing a crash dump

## Using a registry setting (recommended on Windows)

Create a registry key file (`dump.reg`) with the contents below, then execute it. The settings mean that every crash will produce a full dump (`DumpType`=2) in the folder specified by `DumpFolder`, and at most one will be kept (every subsequent crash will overwrite the file, because `DumpCount`=1).

After this key is set, repro the issue and it should produce a dump file named after the crashing process.

Use the registry editor to delete this key.

```
Windows Registry Editor Version 5.00

[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\Windows Error Reporting\LocalDumps]
"DumpFolder"="c:\\localdumps"
"DumpCount"=dword:00000001
"DumpType"=dword:00000002
```

More [information](https://msdn.microsoft.com/en-us/library/windows/desktop/bb787181(v=vs.85).aspx)

## Using environment variables (recommended on Linux)

Define the container with the [correct variables](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/collect-dumps-crash) to collect a dump on crash.

# Running the compiler with a long command line

Often times the command-line recorded by msbuild logs is very long. Simply copy/pasting it into a command window fails, because the line gets truncated.

The solution is to copy the command-line options into a test file (for instance, `repro.rsp`) then invoke the compiler with `csc.exe /noconfig @repro.rsp`. The same thing works with `vbc.exe`.

# Verifying the version of the compiler and language

If you have access to a command-line, simply running `csc.exe` will print out the version of the compiler.

For environments where you cannot use the command-line, you can include `#error version` in your program and the compiler and language versions will be printed as an error message. (Note this only works with compiler version 2.3 or later, which shipped with Visual Studio 2017 version 15.3)

# Investigating regressions and back compat issues

Those are scenarios where you need to compile the same code with different versions of the compiler.

The latest native compiler (pre-Roslyn) is part of the .NET Framework, so you can run it from `c:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe`.

For trying various Roslyn versions, you can create a new project with your code, and add a reference to [`Microsoft.Net.Compilers.Toolset`](../compilers/Compiler%20Toolset%20NuPkgs.md). By choosing the source (nuget.org or Azure DevOps) and the package version (see [versioning help](https://github.com/dotnet/roslyn/blob/main/docs/wiki/NuGet-packages.md#versioning)), you will be able to control what version of the compiler is used. Note that you need to _Build_ your project to compile the code with the desired compiler version (the IDE may show squiggles and use a different version).

# Running and debugging a test on Core on Windows
To run all Core tests on Windows, you can use `Build.cmd -testCoreClr`.

To run a specific test, here's an example of command that can be used and adjusted: 
`"C:\Program Files\dotnet\dotnet.exe" exec --depsfile D:\repos\roslyn\artifacts\bin\Microsoft.CodeAnalysis.CSharp.Symbol.UnitTests\Debug\netcoreapp3.0\\Microsoft.CodeAnalysis.CSharp.Symbol.UnitTests.deps.json --runtimeconfig D:\repos\roslyn\artifacts\bin\Microsoft.CodeAnalysis.CSharp.Symbol.UnitTests\Debug\netcoreapp3.0\\Microsoft.CodeAnalysis.CSharp.Symbol.UnitTests.runtimeconfig.json C:\Users\jcouv\.nuget\packages\dotnet-xunit\2.3.0-beta4-build3742\tools\netcoreapp2.0\xunit.console.dll D:\repos\roslyn\artifacts\bin\Microsoft.CodeAnalysis.CSharp.Symbol.UnitTests\Debug\netcoreapp3.0\\Microsoft.CodeAnalysis.CSharp.Symbol.UnitTests.dll -xml D:\repos\roslyn\artifacts\TestResults\Microsoft.CodeAnalysis.CSharp.Symbol.UnitTests.xml -method "*.MyTestMethod"`.

Such test can be debugged by opening `C:\Program Files\dotnet\dotnet.exe` as a project in Visual Studio, then configuring the debug arguments and engine (pick CoreCLR).


# Investigating squiggles in the IDE
All the compiler diagnostics that appear as IDE squiggles are channeled through the `AnalyzeXYZ`methods in [CompilationAnalyzer](http://source.roslyn.io/#Microsoft.CodeAnalysis/DiagnosticAnalyzer/CompilerDiagnosticAnalyzer.CompilationAnalyzer.cs).

# Verifying a Roslyn change in integration into the CLI
I recently had to test a [Roslyn change](https://github.com/dotnet/roslyn/pull/27349) in integration into the CLI. Here are the steps:

1. In roslyn enlistment ran: `powershell -noprofile -executionPolicy RemoteSigned -file eng\build.ps1 -bootstrap -buildcoreclr`. That puts a valid MSBuild layout into "E:\code\roslyn\Binaries\Bootstrap\Microsoft.NETCore.Compilers\42.42.42.42\tools"
1. Created a Net Core app project that had the stack overflow case. 
1. In the sample code directory 
    1. Ran `dotnet build` and verified no stack printed 
    1. Ran ` dotnet build /p:RoslynTargetsPath=E:\code\roslyn\Binaries\Bootstrap\Microsoft.NETCore.Compilers\42.42.42.42\tools` and verified the stack trace was printed

# Creating a dump file for a hung process
Using the 32-bit Task Manager (`%WINDIR%\SysWow64\TaskMgr.exe` so that SoS will work), right-click on the hung process to produce a `.dmp` file. You can then share the file with the team via some online drive (dropbox and the like).

![image](https://user-images.githubusercontent.com/12466233/42392334-4eed5286-8107-11e8-8212-26fa53383f19.png)

# Investigating build-time regressions

There are three significant candidates to investigate:
1. analyzer issue:  
  Use `/p:ReportAnalyzer=true` to add analyzer timing information to the binary log.  
  The binary log viewer can display that information.
2. compiler server issue:  
  Inspect the binary log (search for `$message CompilerServer failed` or "Error:").  
  If the compiler server is having issues, there will be many such entries.  
  In that case, use the environment variable `set RoslynCommandLineLogFile=c:\some\dir\log.txt` to enable additional logging.  
  In that log, "Keep alive" entries indicate that the compiler server restarted (which we don't expect to happen very often).
3. difference in inputs:  
  Use `/p:Features=debug-determinism` to create an additional output file that documents all the inputs to a particular compilation.  
  The file is written next to the compilation output and has a `.key` suffix.  
  Comparing those files between slow and fast runs helps detect pertinent changes (new inputs, new references, etc).  
  See [Generate a Deterministic Key File](../compilers/Deterministic%20Inputs.md#1-generate-a-deterministic-key-file) for more details.  
