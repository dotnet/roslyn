# Generate SDK Packages

This is a tool for generating a set of NuGet packages for the VS SDK.  This is a temporary solution until we work with the VS SDK team to help address a couple of issues with how their 
packages are produced.

- make-all.ps1: Generates all of the NuGet packages we need for the VS SDK

## Example workflow

Here is an example of building, testing and uploading the packages for the 26418.00 build of d15prerel.  First step is 
to make the packages for the build.

``` powershell 
> .\make-all.ps1 -version "26418.00" -branch "d15prerel"  -outpath c:\users\jaredpar\temp\nuget
```

This will create all of the packages with the version string 15.0.26418-alpha.

When building packages from a non-release branch (e.g. vsucorediag) use ```-versionSuffix branch-name``` to avoid potential 
conflicts with release branch versions.

Before uploading the packages to Azure DevOps please do a local build to validate the changes.  In order to do this the 
following line needs to be added to NuGet.config.  Do not merge this change, it is for testing only.  

``` xml
<add key="DO NOT MERGE" value="c:\users\jaredpar\temp\nuget" />
```

Then update the version in `Versions.props` to match the newly generated version.

Given this entry we can quickly run the following developer flow to validate the changes:

``` cmd
> cd <roslyn root>
> Restore.cmd
> Build.cmd
> Test.cmd
```

Assuming this all passes then revert the change to NuGet.config, upload the packages to the [public vs-impl feed](https://dev.azure.com/azure-public/vside/_packaging?_a=feed&feed=vs-impl) 
and submit the result as a PR.





