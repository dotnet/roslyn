# PostSharp Engineering

## Table of contents

- [PostSharp Engineering](#postsharp-engineering)
  - [Table of contents](#table-of-contents)
  - [Introduction](#introduction)
  - [Prerequisities](#prerequisities)
  - [Installation](#installation)
  - [Updating](#updating)
  - [Features](#features)
  - [Modifying](#modifying)
  - [Cloning](#cloning)
  - [Repositories with disabled symbolic links](#repositories-with-disabled-symbolic-links)

## Introduction

This repository contains common development, build and publishing scripts and configurations. It is meant to be integrated in other repositories in form of a GIT subtree.

## Prerequisities

- GIT for Windows with symlink support enabled. This is set in the installation wizzard.
> Version 2.32.0 fails to support GIT subtree.
> 
> Issue: https://github.com/git-for-windows/git/issues/3260
> 
> Version 2.31.0: https://github.com/git-for-windows/git/releases/download/v2.31.0.windows.1/Git-2.31.0-64-bit.exe
- Windows Developer Mode enabled or elevated shell. (To create symlinks.)
> New-Item CMD-let in PowerShell requires elevation to create symlinks even with Windows Developer Mode enabled.

## Installation

1. Enable symlinks in `.git/config` of your working repo.
2. Clone `Caravela.Engineering` in your `c:\src` (it must be the parent directory of the consuming repo).

    ```git clone https://postsharp@dev.azure.com/postsharp/Caravela/_git/Caravela.Engineering master --squash```

3. Execute `& c:\src\Caravela.Engineering Install.ps1`. This copies the files into the `eng\shared` directory of your repo.
4. Continue with the instructions in the following files:
    1. [Style and Formatting](style/README.md)
    1. [Build and Deployment](build/README.md)

## Updating

0. Commit all changes in the current repository.
1. From the repository root containing the `eng\shared` subtree, execute `& Build.ps1 engineering pull`.
2. Follow the steps described in [the changelog](CHANGELOG.md).
3. Commit & push. (Even if there are no changes outside the `eng\shared` subtree.)

## Features

The features provided by this repository are grouped by categories in the top-level directories. Each directory contains a `README.md` file describing the features in that category.

- [Build and Test](build/README.md)
- [Style and Formatting](style/README.md)
- [Engineering Tools](tools/README.md)

## Modifying

To share modifications in the `eng\shared` GIT subtree:

- Make sure that all documentation reflects your changes.
- Add an entry to [the changelog](CHANGELOG.md) to let others know which changes have been introduced and which actions are required when updating the `eng\shared` GIT subtree in other repositories.
- Commit your changes.
- From the repository root containing the `eng\shared` subtree, execute `& Build.ps1 engineering push`. This will copy all changes to `c:\src\Caravela.Engineering`.
- Review the changes in `c:\src\Caravela.Engineering` and commit them into a branch named `develop`. Push the branch.
- For the `Caravela.Engineering` repository, create a pull request from `develop` to `master`.

## Cloning

To clone a repository containing `eng\shared` GIT subtree, use the following command:

`git clone -c core.symlinks=true <URL>`

## Repositories with disabled symbolic links

When you have an existing repository where the symbolic links are disabled and you want to check-out changes containing symbolic links, you need to change the `symlinks` setting to `true` in `.git\config` configuration file.

Checking files out after that will properly create all symbolic links.

Changing this setting after the check-out will make the repository dirty. Reverting the "changes" will fix the links.