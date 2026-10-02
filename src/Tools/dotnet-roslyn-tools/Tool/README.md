# Microsoft.RoslynTools

`dotnet roslyn-tools` is a command-line tool for performing Roslyn infrastructure tasks.

## Install

Install the latest published build:

```console
dotnet tool install Microsoft.RoslynTools --prerelease --add-source https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-eng/nuget/v3/index.json
```

To install globally, add `-g`. A global installation is invoked as `roslyn-tools`.

## Commands

Some commands require `authenticate` to be run first.

```text
authenticate                       Stores the AzDO and GitHub tokens required for remote operations.
pr-finder                          Finds merged PRs between two commits.
pr-tagger                          Tags PRs inserted in a given VS build.
nuget-dependencies                 Lists missing or out-of-date package dependencies.
nuget-prepare                      Prepares Roslyn packages for validation.
nuget-publish <roslyn|roslyn-sdk>  Publishes packages built from the Roslyn repo.
create-release-tags                Generates git tags for VS and SDK releases.
vsbranchinfo                       Reports Roslyn insertion information for VS branches.
dart-test                          Runs the DartLab pipeline for a PR.
pr-val                             Runs the PR validation pipeline.
pr-suite                           Runs the PR validation and DartLab pipelines.
create-insertion                   Creates a Visual Studio insertion PR.
update-insertion                   Updates an existing Visual Studio insertion PR.
```

For example:

```console
dotnet roslyn-tools vsbranchinfo
```

## Build from source

From the Roslyn repository root:

```console
dotnet build src/Tools/dotnet-roslyn-tools/Tool/Microsoft.RoslynTools.csproj
dotnet pack src/Tools/dotnet-roslyn-tools/Tool/Microsoft.RoslynTools.csproj
```

Install the resulting package from `artifacts/packages/Debug/NonShipping`:

```console
dotnet tool install Microsoft.RoslynTools \
  --tool-path .tools \
  --add-source artifacts/packages/Debug/NonShipping \
  --version <version>
```

## Uninstall

```console
dotnet tool uninstall Microsoft.RoslynTools
```

Include `-g` when uninstalling a global installation.
