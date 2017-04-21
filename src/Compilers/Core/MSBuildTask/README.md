# Microsoft.Build.Tasks.CodeAnalysis

This MSBuild tasks contains the core tasks and targets for compiling C# and VB projects.  

## Debugging

Debugging this code requires a bit of setup because it's not an independent component.  It relies on having other parts of the toolset deployed in the same directory.  Additionally the project being debugged needs to be modified to ensure this DLL is built instead of the one that ships along side MSBuild.  

Set the startup project to Toolset.  This project properly delpoys all of the necessary components and hence provides a simple F5 experience.

Next modify the Debug settings for Toolset.  The startup program needs to be MSBuild.exe.  Set the "start external program" field to the full path of MSBuild:

> C:\Program Files (x86)\MSBuild\14.0\Bin\MSBuild.exe

Any version 14.0 or high is fine.  Pass the path of the target project as the command line arguments.

The target project itself needs to be modified so that it will load the freshly built binaries.  Otherwise it will load the binaries deployed with MSBuild.  Open up the project file and add the following lines **before** the first `<Import>` declaration in the file:

``` xml
<UsingTask TaskName="Microsoft.CodeAnalysis.BuildTasks.Csc"
         AssemblyFile="E:\dd\roslyn\Binaries\Debug\Exes\Toolset\Microsoft.Build.Tasks.CodeAnalysis.dll" />
<UsingTask TaskName="Microsoft.CodeAnalysis.BuildTasks.Vbc"
         AssemblyFile="E:\dd\roslyn\Binaries\Debug\Exes\Toolset\Microsoft.Build.Tasks.CodeAnalysis.dll" />
```

Replace `e:\dd\roslyn` with the path to your Roslyn enlistment.

Once that is all done you should be able to F5 the Toolset project and debug the MSBuild task directly.






