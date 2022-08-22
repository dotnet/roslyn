# Your Library

***An awesome template for your awesome library***

![NuGet package](https://img.shields.io/badge/nuget-your--package--here-yellow.svg)

[![Azure Pipelines status](https://dev.azure.com/andrewarnott/OSS/_apis/build/status/AArnott.Library.Template?branchName=main)](https://dev.azure.com/andrewarnott/OSS/_build/latest?definitionId=29&branchName=main)
![GitHub Actions status](https://github.com/aarnott/Library.Template/workflows/CI/badge.svg)
[![codecov](https://codecov.io/gh/aarnott/library.template/branch/main/graph/badge.svg)](https://codecov.io/gh/aarnott/library.template)

## Features

* Follow the best and simplest patterns of build, pack and test with dotnet CLI.
* Init script that installs prerequisites and auth helpers, supporting both non-elevation and elevation modes.
* Static analyzers: [FxCop](https://docs.microsoft.com/en-us/visualstudio/code-quality/fxcop-analyzers?view=vs-2019) and [StyleCop](https://github.com/DotNetAnalyzers/StyleCopAnalyzers)
* Read-only source tree (builds to top-level bin/obj folders)
* Auto-versioning (via [Nerdbank.GitVersioning](https://github.com/dotnet/nerdbank.gitversioning))
* Builds with a "pinned" .NET Core SDK to ensure reproducible builds across machines and across time.
* Automatically pack the library and publish it as an artifact, and even push it to some NuGet feed for consumption.
* Testing
  * Testing on .NET Framework, multiple .NET Core versions
  * Testing on Windows, Linux and OSX
  * Tests that crash or hang in Azure Pipelines automatically collect dumps and publish as a pipeline artifact for later investigation.
* Cloud build support
  * YAML based build for long-term serviceability, and PR review opportunities for any changes.
  * Azure Pipelines and GitHub Action support
  * Emphasis on PowerShell scripts over reliance on tasks for a more locally reproducible build.
  * Code coverage published to Azure Pipelines
  * Code coverage published to codecov.io so GitHub PRs get code coverage results added as a PR comment

## Consumption

Once you've expanded this template for your own use, you should **run the `Expand-Template.ps1` script** to customize the template for your own project.

Further customize your repo by:

1. Verify the license is suitable for your goal as it appears in the LICENSE and stylecop.json files and the Directory.Build.props file's `PackageLicenseExpression` property.
1. Reset or replace the badges at the top of this file.

### Maintaining your repo based on this template

The best way to keep your repo in sync with this template's evolving features and best practices is to periodically merge the template into your repo:

```ps1
git checkout main          # your default branch
git pull                   # make sure you're at tip
git fetch libtemplate      # fetch latest Library.Template
git merge libtemplate/main
```

There will frequently be merge conflicts to work out, but they will be easier to resolve than running the `Apply-Template.ps1` script every time, which simply blows away all your local changes with the latest from the template.

If you do not already have Library.Template history in your repo or have never completed a merge before, the above steps may produce errors.
To get it working the first time, follow these steps:

```ps1
git remote add libtemplate https://github.com/AArnott/Library.Template.git
git fetch libtemplate
```

If the `git merge` step described earlier still fails for you, you may need to artificially create your first merge.
First, you must have a local clone of Library.Template on your box:

```ps1
git clone https://github.com/AArnott/Library.Template.git
```

Make sure you have either `main` checked out in that clone, as appropriate to match.
Use `git rev-parse HEAD` within the Library.Template repo and record the resulting commit as we'll use it later.

Run the `Apply-Template.ps1` script, passing in the path to your own Library.Template-based repo. This will blow away most customizations you may have made to your repo's build authoring. You should *carefully* review all changes to your repo, staging those changes that you want to keep and reverting those that remove customizations you made.

Now it's time to commit your changes. We do this in a very low-level way in order to have git record this as a *merge* commit even though it didn't start as a merge.
By doing this, git will allow future merges from `libtemplate/main` and only new changes will be brought down, which will be much easier than the `Apply-Template.ps1` script you just ran.
We create the merge commit with these commands:

1. Be sure to have staged or reverted all the changes in your repo.
1. Run `git write-tree` within your repo. This will print out a git tree hash.
1. Run `git commit-tree -p HEAD -p A B -m "Merged latest Library.Template"`, where `A` is the output from `git rev-parse HEAD` that you recorded earlier, and `B` is the output from your prior `git write-tree` command.
1. Run `git merge X` where `X` is the output of the `git commit-tree` command.

**IMPORTANT**: If using a pull request to get your changes into your repo, you must *merge* your PR. If you *squash* your PR, history will be lost and you will have to repeatedly resolve the same merge conflicts at the next Library.Template update.

**CAUTION**: when merging this for the first time, a github-hosted repo may close issues in your repo with the same number as issues that this repo closed in git commit messages.
Verify after completing your PR by visiting your github closed issues, sorted by recently updated, and reactivate any that were inadvertently closed by this merge.
This shouldn't be a recurring issue because going forward, we will avoid referencing github issues with simple `#123` syntax in this repo's history.

Congratulations. You're all done.
Next time you want to sync to latest from Library.Template, you can the simple `git merge` steps given at the start of this section.
