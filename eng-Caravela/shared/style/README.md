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

1. Execute `& eng\shared\style\LinkConfiguration.ps1 -Create -Check` in PowerShell from the repository root. This script will link the `.editorconfig` file to the repository root and the `sln.DotSettings` next to each `.sln` solution file in the repository with a name corresponding to the solution name.

> Note: `LinkConfiguration.ps1` script needs to be executed every time a solution file name is created or its name changes.

2. Import the `CodeQuality.props` script to the `Directory.Build.props` script:

```
  <Import Project="eng\shared\style\CodeQuality.props" />
```

## Configuration

The code quality configuration is configured in the following files:

- `.editorconfig`
- `sln.DotSettings`
- `stylecop.json`
- `CodeQuality.props`

## Code style cleanup

### Installation

Create `eng\Cleanup.ps1` file. The content should look like this:

```
eng/shared/style/Cleanup.ps1 'Caravela.sln'
```

The second parameter is optional and may contain any parameter applicable to the cleanup script.

### Usage

To get the source code cleaned, execute `& eng\Cleanup.ps1` in PowerShell from the repository root.