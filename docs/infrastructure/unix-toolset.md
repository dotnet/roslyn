Building a new Unix toolset
====
This document describes building a new toolset for use in Mac or Linux.
Because the toolsets contain various targets and reference assemblies that
only exist on Windows, the toolsets currently must be built on Windows.

### Building Roslyn Toolset
The new *toolset name* will be chosen as one of the follownig:

- Linux: roslyn.linux.`<version number>`
- Mac: roslyn.mac.`<version number>`

The value of *version number* will simply be the one number higher than the current version number of the toolset.  

To build the toolset do the following:

- If necessary, make modifications to the dependencies in the 
 `build/MSBuildToolset/project.json` file to bring in anything new.
- Run the `build/MSBuildToolset/build-toolset.ps1` file.
- The script produces two zip files in bin\Debug\netcoreapp1.0 subdirectory:
    - Rename `osx.10.10-x64.zip` to roslyn.mac.`<version number>.zip`
    - Rename `ubuntu.14.04-x64.zip` to roslyn.linux.`<version number>.zip`
- Upload the files to the Azure in the dotnetci storage account in the roslyn container:

```
azcopy /Pattern:*.zip /Source:build\MSBuildToolset\bin\Debug\netcoreapp1.0 /Dest:https://dotnetci.blob.core.windows.net/roslyn /DestKey:<<key>>
```

- Send a PR to change [Makefile](https://github.com/dotnet/roslyn/blob/master/Makefile) to use the new toolset.  
