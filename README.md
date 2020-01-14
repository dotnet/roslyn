# Your Library

***An awesome template for your awesome library***

![NuGet package](https://img.shields.io/badge/nuget-your--package--here-yellow.svg)

[![Azure Pipelines status](https://dev.azure.com/andrewarnott/OSS/_apis/build/status/AArnott.Library.Template?branchName=master)](https://dev.azure.com/andrewarnott/OSS/_build/latest?definitionId=29&branchName=master)
![GitHub Actions status](https://github.com/aarnott/Library.Template/workflows/CI/badge.svg)
[![codecov](https://codecov.io/gh/aarnott/library.template/branch/master/graph/badge.svg)](https://codecov.io/gh/aarnott/library.template)

## Features

* Follow the best and simplest patterns of build, pack and test with dotnet CLI.
* Static analyzers: [FxCop](https://docs.microsoft.com/en-us/visualstudio/code-quality/fxcop-analyzers?view=vs-2019) and [StyleCop](https://github.com/DotNetAnalyzers/StyleCopAnalyzers)
* Read-only source tree (builds to top-level bin/obj folders)
* Auto-versioning (via [Nerdbank.GitVersioning](https://github.com/aarnott/nerdbank.gitversioning))
* Azure Pipeline via YAML with all dependencies declared for long-term serviceability.
* Testing on .NET Framework, multiple .NET Core versions
* Testing on Windows, Linux and OSX
* Code coverage published to Azure Pipelines
* Code coverage published to codecov.io so GitHub PRs get code coverage results added as a PR comment

## Consumption

Once you've expanded this template for your own use, you should **run the `Expand-Template.ps1` script** to customize the template for your own project.

Further customize your repo by:

1. Verify the license is suitable for your goal as it appears in the LICENSE and stylecop.json files and the Directory.Build.props file's `PackageLicenseExpression` property.
1. Reset or replace the badges at the top of this file.
