# RepoUtil

## Usage

This is a tool that manages the use of NuGet packages in the repo:

- Ensuring we consistently use the same package version in our projects.  
- Ensuring we aren't referencing stale / deleted package versions in configuration.
- Generating helper files with specified package versions. Example [Dependencies.props](https://github.com/dotnet/roslyn/blob/master/build/Targets/Dependencies.props)
- Simple way to upgrade package versions in the repo.

The primary goal of the tool is to ensure consistency in our NuGet package usage.  Given the large number of project.json files in our repo and the variety of ship vehicles it's easy for packages to get out of sync.  For instance referencing two versions of System.Collection.Immutable.  Doing so is is incorrect, potentially invalidates our testing and potentially breaks insertions. 

The tool ensures that by default given package is only referenced at a single version for the entire repo.  That is we can only use a single version of System.Collections.Immutable in our shipping code.  Deviatons are allowed but they must be explicitly added to the [config file](https://github.com/dotnet/roslyn/blob/master/build/config/RepoUtilData.json) as a "fixed" package.  There is nothing inherently wrong with adding a fixed package so long as it's not used in shipping code.  It's perfectly fine for tools, assets, etc ... 

``` json
"fixed" {
    "Microsoft.VSSDK.BuildTools": [ "14.3.25407", "15.0.25201-Dev15Preview2" ]
}
```

The tool operates by using all of our project.json files as the primary source of truth for the repo.  All files with the name pattern `*project.json` are considered to be NuGet assets and will be scanned for NuGet references.  To test the tool out locally run the following command:

``` cmd
> Binaries\Debug\Exes\RepoUtil\RepoUtil.exe verify 
```

## Code generation

This tool is also designed to generate helper files which attach strong names to the version numbers.  This allows MSBuild files to reference `$(SystemConsoleVersion)` instead of hard coding the current value of 4.0.0.

The `verify` command will ensure that all generated files are consistent with the current state of project.json files in the repo.  Hence there is no danger of referencing an outdated version of a package using these files.

The generated files are all listed under the `generate` section of the config file:

``` json
  "generate": {
        "msbuild": {
            "path": "build\\Targets\\Dependencies.props",
            "values": [
                "Microsoft.Dia.*",
                "System.*",
            ]
        }
    },
```

Each generated file kind has two properties:

1. The path of the file to generate the contents to. 
2. A collection of Regex.  Packages which match any of the regexes will have their values included in the generated file.

## Commands

This tool operates by having a set of sub commands that it executes

### change

The `change` command has two main purposes:

1. Update a NuGet package reference to a new version 
2. Regenerate all of the supporting files

In order to change a single reference simply pass the new package as an argument:

``` cmd
RepoUtil change "System.Collections.Immutable 1.3.0"
```

For large number of packages a file can be used to list out all of the packages.  

``` cmd
RepoUtil change -version e:\path\to\file.txt
```

To regenerate all of the supporting files without updating any packages just use `change` without any arguments

``` cmd
> Binaries\Debug\Exes\RepoUtil\RepoUtil.exe change
```

### verify

The `verify` command will simply analyze the state of the repo and ensure all of the NuGet dependencies are up to date:

- All packages are referenced at the same version.
- All the generated files are up to date.
- etc ... 

All packages must fall into one of the following categories:

- Normal: Packages which are expected to be updated.  
- Fixed: Package + version which are referenced at a version that should never change. 

All uses of a normal package in the repo must have the same version.  For example every use of System.Collections.Immutable in shipping code should be the same version.  This is important as we only deploy a single version.  Hence to ensure our our testing, build and deployment logic reflect our shipping state the code must unify on a single version.

### consumes

The `consumes` command produces a json file describing all of the NuGet packages consumed by the repo.  It essentially aggregates all of the project.json files and adds a bit of metadata on top of them. 


