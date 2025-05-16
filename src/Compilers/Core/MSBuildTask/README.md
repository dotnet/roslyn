# Microsoft.Build.Tasks.CodeAnalysis

This MSBuild tasks contains the core tasks and targets for compiling C# and VB projects.  

## Debugging

> [!NOTE] In VSCode, you can use one of the `Microsoft.Build.Tasks.CodeAnalysis.dll` launch targets.

Debugging this code requires a bit of setup because it's not an independent component.  It relies on having other parts of the toolset deployed in the same directory.  Additionally the project being debugged needs to be modified to ensure this DLL is built instead of the one that ships along side MSBuild.  

Set the startup project to Toolset.  This project properly deploys all of the necessary components and hence provides a simple F5 experience.

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

## Two Build Tasks

The compiler produces two MSBuild tasks that target .NET Framework which have the following characteristics:

| Task Assembly | Installation | Compiler Launched |
| --- | --- | --- |
| Microsoft.Build.Tasks.CodeAnalysis.dll | .NET Framework MSBuild | .NET Framework csc.exe |
| Microsoft.Build.Tasks.CodeAnalysis.Sdk.dll | .NET SDK | .NET SDK csc.dll |

There are case where both of these tasks could be loaded into a single MSBuild node. Consider that when a solution that contains both .NET SDK and non-SDK projects is built, both of these tasks will be loaded. It is possible for these to be loaded into the same MSBuild node. This means the tasks need different assembly identities to ensure the correct task is executed in each scenario. The easiest way to achieve this is to simply produce the same logical task with two different assembly names.

There are several places in the .NET SDK props / targets where the full path of the Roslyn task assembly is used. To facilitate this when the task name will change based on the scenario, the build will now ensure the following property is set: `$(RoslynTasksAssembly)`. This will be the _full path_ to the task.

The SDK will be responsible for setting this when `$(RoslynCompilerType)` is `Core`, `Framework` or `FrameworkPackage`. Packages which use `Custom` to override the compiler will need to set this to the proper location.

Note: `$(RoslynTasksAssembly)` is **not** guaranteed to be set in non-SDK scenarios as there is only a single task there.
