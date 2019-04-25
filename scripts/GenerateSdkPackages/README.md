# Generate SDK Packages

This is a collection of tools for generating a set of NuGet packages for the VS SDK and updating our repo to consume 
them.  This is a temporary solution until we work with the VS SDK team to help address a couple of issues with how their 
packages are produced.

- make-all.ps1: Generates all of the NuGet packages we need for the VS SDK
- change-all.ps1: Changes all our project.json files to reference a new VS SDK version 

## Example workflow

Here is an example of building, testing and uploading the packages for the 26418.00 build of d15prerel.  First step is 
to make the packages for the build.

``` powershell 
> .\make-all.ps1 -version "26418.00" -branch "d15prerel"  -outpath c:\users\jaredpar\temp\nuget
```

This will create all of the packages with the version string 15.0.26418-alpha.

When building packages from a non-release branch (e.g. vsucorediag) use ```-versionSuffix branch-name``` to avoid potential 
conflicts with release branch versions.

Next the build needs to be updated to reflect this change in version for the packages we are consuming. 

``` powershell
> .\change-all.ps1 -version "26418.00" 
```

Before uploading the packages to myget please do a local build to validate the changes.  In order to do this the 
following line needs to be added to NuGet.config.  Do not merge this change, it is for testing only.  

``` xml
<add key="DO NOT MERGE" value="c:\users\jaredpar\temp\nuget" />
```

Given this entry we can quickly run the following developer flow to validate the changes:

``` cmd
> cd <roslyn root>
> Restore.cmd
> Build.cmd
> Test.cmd
```

Assuming this all passes then revert the change to NuGet.config, upload the packages to the roslyn-tools feed of 
myget and submit the result of `change-all.ps1` as a PR. 





