# Microsoft.RoslynTools

`dotnet roslyn-tools` is a command line tool for performing infrastructure tasks.

## How to Install

### Local Install

You can install the latest build of the tool using the following command.

```console
dotnet tool install Microsoft.RoslynTools --prerelease --add-source https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-eng/nuget/v3/index.json
```

### Global Install

You can optionally install the tool globally on your machine. When installed globally the command is run with just `roslyn-tools`, so `roslyn-tools vsbranchinfo`.

```console
dotnet tool install -g Microsoft.RoslynTools --prerelease --add-source https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-eng/nuget/v3/index.json
```

## How to Use

`dotnet roslyn-tools` functionality is broken up into subcommands. Some commands require that the `authenticate` command be run first.

```
Description:
  The command line tool for performing infrastructure tasks.

Usage:
  roslyn-tools [command] [options]

Commands:
  authenticate                       Stores the AzDO and GitHub tokens required for remote operations.
  pr-finder                          Find merged PRs between two commits
  nuget-dependencies                 Lists dependencies that are missing or out of date for a folder of .nupkg files.
  nuget-prepare                      Prepares packages built from the Roslyn repo for validation.
  nuget-publish <roslyn|roslyn-sdk>  Publishes packages built from a Roslyn repo. [default: roslyn]
  create-release-tags                Generates git tags for VS releases in the Roslyn repo.
  vsbranchinfo                       Provides information about the state of Roslyn in one or more branches of Visual Studio.
```

For example you could run `dotnet roslyn-tools vsbranchinfo` to display information about the Roslyn package most recently inserted into Visual Studio's main branch.

## How to Build from Source

You can build and package the tool using the following commands. The instructions assume that you are in the root of the repository.

```console
.\build.cmd -pack
# One of the final lines from the build will read something like
# Successfully created package '.\artifacts\packages\Debug\NonShipping\Microsoft.RoslynTools.1.1.0-dev.nupkg'..
# Use the value that is in the form `1.1.0-dev` as the version in the next command.
dotnet tool install --add-source .\artifacts\packages\Debug\NonShipping -g Microsoft.RoslynTools --version <version>
roslyn-tools
```

> Note: On macOS and Linux, `.\build.cmd` should be replaced with `./build.sh` and `.\artifacts` will need be switched to `./artifacts` to accommodate for the different slash directions.

## How to Uninstall

You can uninstall the tool using the following command. Include `-g` if you installed as a global tool.

```console
dotnet tool uninstall Microsoft.RoslynTools
```
