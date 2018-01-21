# Cross Platform Instructions

## Caveats

Linux and Mac support for developing Roslyn is very much a work in progress.  Not everything is supported at the moment and the steps detailed on this page will change very frequently.  If this is an area you are interested in then please check back frequently for updates.

## Building

Build all cross-platform projects with: 

```
cd <roslyn-git-directory>
./build.sh --restore
./build.sh --build
```

If you do not have a system-wide `dotnet` install, or it does not match the required version, the build script will install a compatible version of the CLI to `./Binaries/Tools/dotnet` and use that version for all build tasks.

## Using the compiler

After building, there will be a `csc` in the `Binaries/Debug/Exes/csc/netcoreapp2.0` directory.

### Known issues when running `csc.exe`

##### Output:

  ```
  Microsoft (R) Visual C# Compiler version 42.42.42.42
 Copyright (C) Microsoft Corporation. All rights reserved.

error CS0006: Metadata file 'System.Deployment.dll' could not be found
error CS0006: Metadata file 'System.Web.Mobile.dll' could not be found
error CS0006: Metadata file 'System.Web.RegularExpressions.dll' could not be found
error CS0006: Metadata file 'System.Workflow.Activities.dll' could not be found
error CS0006: Metadata file 'System.Workflow.ComponentModel.dll' could not be found
error CS0006: Metadata file 'System.Workflow.Runtime.dll' could not be found
```

##### Fix: 

  This is because `csc.exe` by default references the `csc.rsp` file next to it. This is the Windows response file, so not all
  assemblies are present when running on Mono. Pass the `-noconfig` option to ignore this response file.
  
##### Output:

```
error CS0041: Unexpected error writing debug information -- 'The requested feature is not implemented
```

##### Fix:

  The compiler is defaulting to writing full PDBs, which are not supported outside of Windows. Use the `/debug:portable` flag
  to generate a portable PDB instead.
