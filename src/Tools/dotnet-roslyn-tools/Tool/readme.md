# Microsoft.RoslynTools

`dotnet roslyn-tools` is a command line tool for performing Roslyn infrastructure tasks.

## How to Install

You can install the latest build of the tool using the following command.

```console
dotnet tool install Microsoft.RoslynTools --prerelease --add-source https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-eng/nuget/v3/index.json
```

### Global Install

You can optionally install the tool globally on your machine, rather than in a specific repository by adding the `-g` argument to the install (and uninstall) command.
When installed globally the command is run with just `roslyn-tools`, so `roslyn-tools vsbranchinfo`.

## How to Use

`dotnet roslyn-tools` functionality is broken up into subcommands.


```
Description:
  The command line tool for performing infrastructure tasks.

Usage:
  roslyn [command] [options]

Commands:
  pr-finder                          Find merged PRs between two commits
  nuget-prepare                      Prepares packages built from the Roslyn repo for validation.
  nuget-publish <roslyn|roslyn-sdk>  Publishes packages built from a Roslyn repo. [default: roslyn]
  create-release-tags                Generates git tags for VS releases in the Roslyn repo.
  vsbranchinfo                       Provides information about the state of Roslyn in one or more branches of Visual Studio.
```

For example you could run `dotnet roslyn-tools vsbranchinfo` to display information about the Roslyn package most recently inserted into Visual Studio's main branch.

## How to Uninstall

You can uninstall the tool using the following command. Include `-g` if you installed as a global tool.

```console
dotnet tool uninstall Microsoft.RoslynTools
```
