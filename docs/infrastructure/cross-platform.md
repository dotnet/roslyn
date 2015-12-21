# Cross Platform Instructions

## Caveats

Linux and Mac support for developing Roslyn is very much a work in progress.  Not everything is supported at the moment and the steps detailed on this page will change very frequently.  If this is an area you are interested in then please check back frequently for updates.

## Building using a pre-made toolset

Right now Roslyn builds on *nix using a mix of Mono and CoreCLR. Patching the right Mono version and acquiring all the tools
can be very difficult, so we've saved pre-built versions on Azure.

Running `make` should download all these toolset binaries and kick off a build using MSBuild running on Mono.

## Using the compiler

After building there should be at least two versions of `csc.exe` in your output directory.

The first is in the `Binaries/Debug` directory. This is the "full .NET framework" version. That means it expects to run on a
full .NET framework, like either the Windows .NET framework or Mono. You would run this like you run other mono programs, i.e.
`mono csc.exe`.

The second copy is in the `Binaries/Debug/csccore` directory. This is a version running directly on CoreCLR -- no Mono necessary.
Just run `csc` in that directory. Note that this version includes a copy of CoreCLR in the output directory, so it is not portable.
The version of CoreCLR copied is specific to whatever machine you built with, so if you're running OS X, this will only run on OS X.
Similarly with Linux (and whatever distro you're using). 


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
