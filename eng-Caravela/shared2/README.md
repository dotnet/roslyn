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

1. Enable symlinks in .git/config.
2. Add the `eng\shared` subtree:

`git subtree add --prefix eng/shared https://postsharp@dev.azure.com/postsharp/Caravela/_git/Caravela.Engineering master --squash`

3. Check `README.md` in each directory in the `eng\shared` subtree for further installation steps.

## Updating

1. From the repository root containing the `eng\shared` subtree, execute `& eng\shared\PullEngineering.ps1`.
2. Follow the steps described in [the changelog](CHANGELOG.md).
3. Commit & push. (Even if there are no changes outside the `eng\shared` subtree.)

## Features

The features provided by this repository are grouped by categories in the top-level directories. Each directory contains a `README.md` file describing the features in that category.

- [Build and Test](build/README.md)
- [Deploy](deploy/README.md)
- [Style and Formatting](style/README.md)
- [Engineering Tools](tools/README.md)

## Modifying

To share modifications in the `eng\shared` GIT subtree:

- Make sure that all documentation reflects your changes.
- Add an entry to [the changelog](CHANGELOG.md) to let others know which changes have been introduced and which actions are required when updating the `eng\shared` GIT subtree in other repositories.
- Commit your changes.
- From the repository root containing the `eng\shared` subtree, execute `& eng\shared\PushEngineering.ps1`.
- Follow the [Updating](#updating) section in the other repositories containing the `eng\shared` GIT subtree.

## Cloning

To clone a repository containing `eng\shared` GIT subtree, use the following command:

`git clone -c core.symlinks=true <URL>`

## Repositories with disabled symbolic links

When you have an existing repository where the symbolic links are disabled and you want to check-out changes containing symbolic links, you need to change the `symlinks` setting to `true` in `.git\config` configuration file.

Checking files out after that will properly create all symbolic links.

Changing this setting after the check-out will make the repository dirty. Reverting the "changes" will fix the links.