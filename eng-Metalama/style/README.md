# PostSharp Engineering: Code Style Features

Make sure you have read and understood [PostSharp Engineering](../README.md) before reading this doc.

## Table of contents

- [PostSharp Engineering: Code Style Features](#postsharp-engineering-code-style-features)
  - [Table of contents](#table-of-contents)
  - [Introduction](#introduction)
  - [Installation](#installation)
  - [Configuration](#configuration)
  - [Code style cleanup](#code-style-cleanup)

## Introduction

This directory contains centralized code-style configuration and scripts.

## Installation

1. Copy the `PostSharp.Engineering.CodeStyle` repo to your own repo into `eng/style` using `PostSharp.Engineering.BuildTools`, with the command:

      ```
      .\Build.ps1 codestyle pull
      ```

    This tool will create a symlink for `.editorconfig`.

2. Enable symlinks for your repo (edit `.git/config`).

3. For each solution, in Rider, open Settings, choose "Manage Layers", select the team-shared layer, click on the `+` icon and then on "Open Settings File", then choose `eng/style/CommonStyle.DotSettings`.
  This step is required for code formatting using `Build.ps1 reformat`, even if you are otherwise not using Rider.

## Configuration

The code quality configuration is configured in the following files:

- `.editorconfig`
- `CommonStyle.DotSettings` (used by JetBrains tools)
- `stylecop.json`

## Code style cleanup

1. Commit all your changes. You cannot reformat a repo with uncommitted changes.
2. Do `.\Build.ps1 reformat` from the repo root (see `PostSharp.Engineering`).