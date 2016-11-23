# RepoUtil

## Usage

This is a tool used to manage the inputs and outputs of our repo.  In particular, it is used to manage our use of NuGet packages.  It helps to automate the otherwise tedious process of updating NuGet references by:

- Ensuring consistency in NuGet package references
- Fixing up project.json files
- Generating helper files with specified package versions. Example [Dependencies.props](https://github.com/dotnet/roslyn/blob/master/build/Targets/Dependencies.props)

The tool works by using all of our project.json files as the primary source of truth for the repo.  All files with the name pattern `*project.json` are considered to be NuGet assets and will be scanned for NuGet references.  

This will impose a minimum of requirements on our NuGet packages.  In particular the tool assumes that all references to a given NuGet package should occur at the same version.  If the tool sees a package being used with different versions it will issue an error.  

There are valid cases where a package is used at more than one version in the repo.  Typically because it's being deployed as an asset, used as a tool, etc ...  In those cases an entry can be added to [RepoData.json](https://github.com/dotnet/roslyn/blob/master/src/Tools/RepoUtil/RepoData.json)) in the fixed table to call out that usage:

``` json
"fixed" {
    "Microsoft.VSSDK.BuildTools": [ "14.3.25407", "15.0.25201-Dev15Preview2" ]
}
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
RepoUtil change
```

### verify

The `verify` command will simply analyze the state of the repo and ensure all of the NuGet dependencies are up to date:

- All packages are referenced at the same version.
- All the generated files are up to date.
- etc ... 


All packages must fall into one of the following categories:

- Normal: Packages which are expected to be updated.  
- Fixed: Package + version which are referenced at a version that should never change. 

All uses of a normal package in the repo must have the same version.  For example every use of System.Collections.Immutable.

An example of a normal package is System.Collections.Immutable.  This package changes

### consumes

The `consumes` command produces a json file describing all of the NuGet packages consumed by the repo.  It essentially aggregates all of the project.json files and adds a bit of metadata on top of them. 
# RepoUtil

