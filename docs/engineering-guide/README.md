# Roslyn Engineering Guide

**Comprehensive repository map for Microsoft engineers working on the .NET Compiler Platform (Roslyn)**

This guide provides a complete map of the Roslyn repository, showing exactly where to find and modify components, features, and functionality. It's designed for engineers who need to quickly locate code, understand the architecture, and make changes effectively.

---

## What is Roslyn?

Roslyn is the open-source implementation of the C# and Visual Basic compilers with rich code analysis APIs. It provides:

- **Complete C# and VB compilers** with full language support
- **Workspace APIs** for solution/project/document manipulation
- **80+ IDE features** including IntelliSense, refactorings, navigation, and formatting
- **Code analysis infrastructure** for diagnostics and analyzers
- **Language Server Protocol (LSP)** implementation
- **Scripting and interactive** execution (REPL)
- **Debugger integration** (expression evaluation, Edit and Continue)

---

## Documentation Index

This engineering guide is organized into the following documents:

### 1. [Repository Structure](01-repository-structure.md)
Complete map of the repository directory structure, including:
- Top-level directories and their purposes
- Source code organization (`/src/`)
- Test organization
- Build and engineering directories (`/eng/`)
- Documentation and specifications (`/docs/`)

### 2. [Component Guide](02-component-guide.md)
Detailed guide to major components and where to find them:
- **Compilers** (C# and VB parser, binder, emitter)
- **Workspaces** (solution/project APIs)
- **IDE Features** (IntelliSense, refactorings, navigation)
- **Analyzers** (diagnostics and code fixes)
- **Language Server** (LSP implementation)
- **Visual Studio Integration**
- **Scripting and Interactive**
- **Expression Evaluator** (debugger)

### 3. [Feature Location Guide](03-feature-location-guide.md)
Quick reference for finding specific features and functionality:
- Where to modify IntelliSense behavior
- Where to add new refactorings
- Where to implement new language features
- Where to add diagnostics and code fixes
- Where to modify formatting
- Where to enhance navigation features

### 4. [Architecture and Design Patterns](04-architecture.md)
Understanding Roslyn's architecture:
- Compiler pipeline architecture
- Syntax tree design
- Symbol and binding system
- Workspace model
- Service architecture (MEF composition)
- Common design patterns
- Performance considerations

### 5. [Developer Guide](05-developer-guide.md)
Practical guide for common development tasks:
- Building and testing locally
- Adding a new language feature
- Creating a diagnostic analyzer
- Adding a code refactoring
- Implementing IDE features
- Working with the workspace API
- Testing strategies
- Debugging tips

---

## Quick Start: Common Scenarios

### I want to...

#### Modify the C# Compiler
→ See [Component Guide - Compilers](02-component-guide.md#compilers) for compiler structure
→ `/src/Compilers/CSharp/Portable/` contains all C# compiler code

#### Add a New Refactoring
→ See [Feature Location Guide - Refactorings](03-feature-location-guide.md#refactorings)
→ `/src/Features/` for the logic, `/src/EditorFeatures/` for the UI

#### Create a Diagnostic Analyzer
→ See [Developer Guide - Analyzers](05-developer-guide.md#creating-analyzers)
→ `/src/Analyzers/` for built-in analyzers

#### Modify IntelliSense
→ See [Feature Location Guide - IntelliSense](03-feature-location-guide.md#intellisense)
→ `/src/EditorFeatures/Core/IntelliSense/` for completion infrastructure

#### Add IDE Feature (Go to Definition, Find References, etc.)
→ See [Component Guide - IDE Features](02-component-guide.md#ide-features)
→ `/src/Features/` and `/src/EditorFeatures/`

#### Work with Language Server Protocol
→ See [Component Guide - Language Server](02-component-guide.md#language-server)
→ `/src/LanguageServer/`

#### Modify Code Formatting
→ See [Feature Location Guide - Formatting](03-feature-location-guide.md#formatting)
→ `/src/Workspaces/Core/Portable/Formatting/` and `/src/Features/Core/Portable/Formatting/`

---

## Repository Statistics

- **Languages:** C# (primary), Visual Basic (compiler target)
- **Project Count:** 295+ projects
- **Major Components:** 18 component directories
- **Test Projects:** 39+ major test directories
- **Lines of Code:** ~3M+ lines
- **Supported Platforms:** Windows, macOS, Linux (x86/x64/ARM64)
- **NuGet Packages:** 50+ published packages

---

## Key Technologies

- **.NET:** 10.0.100-rc.2 (with multi-targeting)
- **Build System:** MSBuild + Arcade SDK
- **CI/CD:** Azure Pipelines
- **Testing:** xUnit
- **Composition:** MEF (Managed Extensibility Framework)
- **IDE Integration:** Visual Studio, VS Code (via LSP), other editors

---

## Repository Layout (Quick Reference)

```
roslyn/
├── src/                           # All source code
│   ├── Compilers/                 # C# and VB compilers
│   ├── Workspaces/                # Workspace APIs
│   ├── Features/                  # IDE feature implementations
│   ├── EditorFeatures/            # Editor integration
│   ├── Analyzers/                 # Built-in diagnostics
│   ├── CodeStyle/                 # Code style analyzers
│   ├── LanguageServer/            # LSP implementation
│   ├── VisualStudio/              # VS integration
│   ├── Scripting/                 # Scripting APIs
│   ├── Interactive/               # REPL
│   ├── ExpressionEvaluator/       # Debugger
│   └── Tools/                     # Utilities
├── docs/                          # Specifications and documentation
├── eng/                           # Build configuration
├── scripts/                       # Build scripts
└── azure-pipelines.yml            # CI definition
```

---

## Solution Files

The repository provides different solution filters for focused development:

- **Roslyn.slnx** - Complete solution (all projects)
- **Compilers.slnf** - Compiler-only projects
- **Ide.slnf** - IDE features and editor projects

Use solution filters to reduce load times and focus on your area of work.

---

## Getting Help

- **Build Issues:** See `/docs/contributing/Building, Debugging, and Testing on Windows.md`
- **Feature Development:** See [Developer Guide](05-developer-guide.md)
- **Architecture Questions:** See [Architecture Guide](04-architecture.md)
- **API Documentation:** See `/docs/` directory

---

## Additional Resources

- [Roslyn GitHub Repository](https://github.com/dotnet/roslyn)
- [Roslyn API Documentation](https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/)
- [Language Specifications](../specs/)
- [Compiler Design Docs](../compilers/)
- [Feature Documentation](../features/)

---

**Last Updated:** 2025-11-21
**Maintained By:** Microsoft Roslyn Engineering Team
