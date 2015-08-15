Building CoreCLR Csc & Vbc
=================================================

The compiler projects included in all the solution files currently target only
the desktop framework. If you want to build a version of each exe compatible
with CoreCLR, there are two separate projects:
* [CscCore](../../src/Compilers/CSharp/CscCore/CscCore.csproj)
* [VbcCore](../../src/Compilers/VisualBasic/VbcCore/VbcCore.csproj)

To build these projects, first execute `powershell .nuget\NuGetRestore.ps1`
from the root of your clone to restore NuGet packages, and then execute `msbuild`
on the command line with the path to each of the projects.

When you build CscCore or VbcCore the binaries will be output to the core-clr
subdirectory in the output directory. However, by default the executables will
still run on the desktop runtime. To run on the CoreCLR console host, run the
powershell script 
[src/Tools/CopyCoreClrRuntime/CopyCoreClrRuntime.ps1](../../src/Tools/CopyCoreClrRuntime/CopyCoreClrRuntime.ps1) 
with the path to the output directory as the parameter.

### Dogfooding the compiler

If you then wish to dogfood the CoreCLR copmilers while developing Roslyn then set 
the BootstrapBuildPath environment variable (or at the MSBuild command line) to 
point to the core-clr directory.

> msbuild /p:BootstrapBuildPath=<path-to-core-clr-dir>

