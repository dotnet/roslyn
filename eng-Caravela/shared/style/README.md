# PostSharp Engineering: Code Style Features

Make sure you have read and understood [PostSharp Engineering](../README.md) before reading this doc.

## Table of contents

- [PostSharp Engineering: Code Style Features](#postsharp-engineering-code-style-features)
  - [Table of contents](#table-of-contents)
  - [Introduction](#introduction)
  - [Installation](#installation)
  - [Configuration](#configuration)
  - [Code style cleanup](#code-style-cleanup)
    - [Installation](#installation-1)
    - [Usage](#usage)

## Introduction

This directory contains centralized code-style configuration and scripts.

## Installation

To install the common code style configuration:

1. Execute `& eng\shared\style\Install.ps1 -Create -Check` in PowerShell from the repository root. This script will link the `.editorconfig` file to the repository root.

2. Import the `CodeQuality.props` script to the `Directory.Build.props` script:

```
  <Import Project="eng\shared\style\CodeQuality.props" />
```

3. For each solution, in Rider, open Settings, choose "Manage Layers", select the team-shared layer, click on the `+` icon and then on "Open Settings File", then choose `eng/shared/style/CommonStyle.DotSettings`.
  This step is required for code formatting using `Build.ps1 reformat`, even if you are otherwise not using Rider.

## Configuration

The code quality configuration is configured in the following files:

- `.editorconfig`
- `CommonStyle.DotSettings` (used by JetBrains tools)
- `stylecop.json`
- `CodeQuality.props`

## Code style cleanup

1. Commit all your changes. You cannot reformat a repo with uncommitted changes.
2. Do `.\Build.ps1 reformat` from the repo root.