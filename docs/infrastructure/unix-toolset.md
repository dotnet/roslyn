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
- In the `build/MSBuildToolset/bin/Debug/dnxcore50` directory:
    - Rename and bzip the `osx.10.10-x64/publish` directory as roslyn.mac.`<version number>`
    - Rename and bzip the `ubuntu.14.04-x65/publish` directory as roslyn.linux.`<version number>`
- Upload the file to the Azure in the dotnetci storage account in the roslyn container.  
- Send a PR to change [Makefile](https://github.com/dotnet/roslyn/blob/master/Makefile) to use the new toolset.  
