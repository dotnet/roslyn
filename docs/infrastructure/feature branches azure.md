# Creating Feature Branches
This document describes the process for setting up CI on a feature branch of roslyn.

## Push the branch
The first step is to create the branch seeded with the initial change on roslyn. This branch should have the name `features/<feature name>`. For example: `features/mono` for working on mono work. 

Assuming the branch should start with the contents of `master` the branch can be created by doing the following:

Note: these steps assume the remote `origin` points to the official [roslyn repository](https://github.com/dotnet/roslyn).

``` cmd
> git fetch origin
> git checkout -B init origin/master
> git push origin init:features/mono
```

## Adding branch to Azure Pipelines
The following files need to be edited in order for GitHub to trigger Azure Pipelines Test runs on PRs:

- [azure-pipelines.yml](https://github.com/dotnet/roslyn/blob/master/azure-pipelines.yml)
- [azure-pipelines-integration.yml](https://github.com/dotnet/roslyn/blob/master/azure-pipelines-integration.yml)

Under the `pr` section in the file add your branch name. 

``` yaml
pr:
- master
- master-vs-deps
- ...
- features/mono
```

