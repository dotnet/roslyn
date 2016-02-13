Building CoreCLR Csc & Vbc
=================================================

First, build Roslyn.sln as described in https://github.com/dotnet/roslyn/wiki/Building-Testing-and-Debugging.

The Core CLR-compatible csc.exe and vbc.exe willl be placed in the core-clr subdirectory in the output directory. However, by default the executables will still run on the desktop runtime. To run on the CoreCLR console host, run the powershell script 
[src/Tools/CopyCoreClrRuntime/CopyCoreClrRuntime.ps1](../../src/Tools/CopyCoreClrRuntime/CopyCoreClrRuntime.ps1) 
with the path to the output directory as the parameter.

### Dogfooding the compiler

If you then wish to dogfood the CoreCLR compilers while developing Roslyn then set 
the BootstrapBuildPath environment variable (or at the MSBuild command line) to 
point to the core-clr directory.

> msbuild /p:BootstrapBuildPath='path-to-core-clr-dir'

