# Roslyn Codebase Overview

**Last Updated:** January 29, 2026  
**Purpose:** High-level map of the Roslyn codebase for engineers new to the project

---

## Table of Contents

1. [Quick Orientation](#quick-orientation)
2. [Product Areas](#product-areas)
3. [How to Use This Documentation](#how-to-use-this-documentation)

---

## Quick Orientation

### What is Roslyn?

Roslyn is the .NET Compiler Platform—the open-source implementation of both the C# and Visual Basic compilers. But Roslyn is much more than a traditional compiler: it exposes rich code analysis APIs that enable building sophisticated code analysis tools, refactoring engines, and IDE features.

The name "Roslyn" was the original codename for the project. While the official name is ".NET Compiler Platform," the codebase and community still commonly use "Roslyn."

Roslyn powers:
- **The C# and VB compilers** (`csc.exe`, `vbc.exe`) shipped with the .NET SDK
- **Visual Studio's language services** for C# and VB (IntelliSense, refactoring, diagnostics)
- **The C# Dev Kit** for VS Code (via Language Server Protocol)
- **Analyzers and code fixes** for code quality and style enforcement
- **C# scripting** (`.csx` files) and the Interactive Window

What makes Roslyn technically interesting is its "compiler as a service" architecture: instead of being a black box that takes source code and produces binaries, Roslyn exposes every phase of compilation through APIs—syntax trees, semantic models, symbols, and emit infrastructure. This enables tools to understand code at the same level the compiler does.

### Codebase at a Glance

| Metric | Value |
|------------------------|---------------------------|
| Top-level directories | 18 in `src/` |
| Primary languages | C# (~14,000 files), VB (~3,600 files) |
| Build system | MSBuild + Arcade SDK |
| Repo type | Monorepo |
| Lines of code | ~2.5 million |

### Architecture Layers

Roslyn follows a strict layering model, from low-level compiler infrastructure to high-level IDE integration:

```
┌─────────────────────────────────────────────────────────────┐
│                    Visual Studio                            │
│              (VS packages, VSIX deployment)                 │
└──────────────────────┬──────────────────────────────────────┘
                       │
┌──────────────────────▼──────────────────────────────────────┐
│               Editor Features / Language Server             │
│        (WPF editor integration, LSP implementation)         │
└──────────────────────┬──────────────────────────────────────┘
                       │
┌──────────────────────▼──────────────────────────────────────┐
│                      Features                               │
│    (Code completion, refactoring, diagnostics, navigation)  │
└──────────────────────┬──────────────────────────────────────┘
                       │
┌──────────────────────▼──────────────────────────────────────┐
│                     Workspaces                              │
│         (Solution/Project model, document management)       │
└──────────────────────┬──────────────────────────────────────┘
                       │
┌──────────────────────▼──────────────────────────────────────┐
│                     Compilers                               │
│   (Lexer, Parser, Binder, Semantic Model, Emit, Symbols)    │
└─────────────────────────────────────────────────────────────┘
```

### Core Design Pattern

A fundamental pattern throughout Roslyn is **language-agnostic core with language-specific specializations**:

- **Core/Portable** components contain shared abstractions and the majority of logic
- **CSharp** and **VisualBasic** components extend the core with language-specific implementations
- This allows most features to be written once and work for both languages

This pattern appears in Compilers, Features, Analyzers, and most other areas.

### Key Technologies

- **C# / .NET** — Primary implementation language; targets .NET 8.0+ and .NET Framework
- **MSBuild** — Build system; integrates with Arcade SDK for CI
- **MEF (Managed Extensibility Framework)** — Dependency injection for IDE services
- **Language Server Protocol (LSP)** — Protocol for VS Code and other editors
- **ServiceHub** — Out-of-process communication for VS remote services
- **Azure Pipelines** — CI/CD with Helix for distributed testing

---

## Product Areas

### Compilers

**Purpose:** The C# and Visual Basic compilers with full API exposure

**Why it matters:** This is the foundation of everything. The compiler APIs (`Compilation`, `SyntaxTree`, `SemanticModel`, `Symbol`) are used by every other layer.

**Key Directories:**
- `src/Compilers/Core/` — Shared compiler infrastructure
- `src/Compilers/CSharp/` — C# compiler implementation
- `src/Compilers/VisualBasic/` — VB compiler implementation
- `src/Compilers/Server/` — Compiler server daemon (VBCSCompiler)

**Documentation:**
- [Product Overview](./compilers/product_overview.md) — Why it exists, compilation pipeline
- [Codebase Overview](./compilers/codebase_overview.md) — Architecture, components, patterns

---

### Workspaces

**Purpose:** Solution/project model and document management layer

**Why it matters:** Workspaces provide the "IDE view" of code—managing multiple projects, tracking document changes, and coordinating compilation across a solution.

**Key Directories:**
- `src/Workspaces/Core/` — Core workspace model
- `src/Workspaces/MSBuild/` — MSBuild integration for loading projects
- `src/Workspaces/Remote/` — Out-of-process workspace for ServiceHub

**Documentation:**
- [Product Overview](./workspaces/product_overview.md) — Why it exists, workspace model
- [Codebase Overview](./workspaces/codebase_overview.md) — Architecture, state management

---

### Features

**Purpose:** Language-agnostic IDE features

**Why it matters:** This is where the "smart" IDE features live—code completion, refactoring, diagnostics, navigation. Features are written once and work for both C# and VB.

**Key Directories:**
- `src/Features/Core/Portable/` — Shared feature implementations
- `src/Features/CSharp/` — C#-specific features
- `src/Features/VisualBasic/` — VB-specific features

**Documentation:**
- [Product Overview](./features/product_overview.md) — IDE features and their purposes
- [Codebase Overview](./features/codebase_overview.md) — Architecture, provider patterns

---

### Language Server

**Purpose:** Language Server Protocol (LSP) implementation for VS Code

**Why it matters:** This enables Roslyn features in any LSP-compatible editor, most notably VS Code via the C# Dev Kit.

**Key Directories:**
- `src/LanguageServer/Protocol/` — LSP handlers and server
- `src/LanguageServer/Microsoft.CommonLanguageServerProtocol.Framework/` — CLaSP framework

**Documentation:**
- [Product Overview](./language_server/product_overview.md) — LSP architecture
- [Codebase Overview](./language_server/codebase_overview.md) — Handlers, integration

---

### Editor Integration

**Purpose:** Visual Studio editor integration and VS-specific services

**Why it matters:** This layer bridges Roslyn's abstract features to concrete editor experiences—tagging, adornments, command handling, and VS package infrastructure.

**Key Directories:**
- `src/EditorFeatures/` — WPF text editor integration
- `src/VisualStudio/` — VS packages and language services

**Documentation:**
- [Product Overview](./editor_integration/product_overview.md) — Editor integration
- [Codebase Overview](./editor_integration/codebase_overview.md) — Layers, MEF composition

---

### Analyzers

**Purpose:** Static analysis, code style enforcement, and code fixes

**Why it matters:** Analyzers run at compile-time and in the IDE to catch issues early. Roslyn includes ~150 built-in analyzers for code style and quality.

**Key Directories:**
- `src/Analyzers/` — Built-in IDE analyzers
- `src/RoslynAnalyzers/` — Packaged analyzers (for external use)

**Documentation:**
- [Product Overview](./analyzers/product_overview.md) — Analyzer types and purposes
- [Codebase Overview](./analyzers/codebase_overview.md) — API patterns, registration

---

### Supporting Areas

| Area | Purpose | Key Directories |
|------|---------|-----------------|
| **ExpressionEvaluator** | Debugger expression evaluation | `src/ExpressionEvaluator/` |
| **Scripting** | C#/VB scripting (`.csx`, `.vbx`) | `src/Scripting/`, `src/Interactive/` |
| **Tools** | Build tools, code generators | `src/Tools/` |
| **Dependencies** | Shared collections, utilities | `src/Dependencies/` |

---

## How to Use This Documentation

### If you're brand new

1. Read this overview to understand the landscape
2. Check the [Glossary](./glossary.md) for unfamiliar terms
3. Read the [Compilers Product Overview](./compilers/product_overview.md) to understand the foundation
4. Pick an area relevant to your work and read its Product Overview first

### If you're looking for something specific

- **"What is X?"** → Check the [Glossary](./glossary.md)
- **"Where is code for Y?"** → Check the relevant area's Codebase Overview
- **"What technology does Z use?"** → Check [Technology Mapping](./technology_mapping.md)
- **"How do I build?"** → Check [Build System Overview](./build_system_overview.md)

### If you're debugging

- Start with the area's Codebase Overview for architecture context
- Understand the layering: Compilers → Workspaces → Features → Editor
- Check how services are composed via MEF

---

## Documentation Status

### Documented Areas

| Area | Product Overview | Codebase Overview | Detail Level |
|------|------------------|-------------------|--------------|
| Compilers | ✓ | ✓ | Detailed |
| Workspaces | ✓ | ✓ | Detailed |
| Features | ✓ | ✓ | Detailed |
| Language Server | ✓ | ✓ | High-level |
| Editor Integration | ✓ | ✓ | High-level |
| Analyzers | ✓ | ✓ | High-level |

### Areas for Future Documentation

- **ExpressionEvaluator** — Complex debugger integration; would benefit from detailed flow docs
- **Scripting/Interactive** — Scripting API and REPL architecture
- **Remote Services** — ServiceHub communication patterns
- **Build System** — Arcade SDK integration details

---

## Related Documents

**In This Overview:**
- [Glossary](./glossary.md) — All internal terms and acronyms
- [Technology Mapping](./technology_mapping.md) — What tech is used where
- [Build System Overview](./build_system_overview.md) — How code is built

---

## Existing Codebase Documentation

The Roslyn repository contains extensive documentation. This AI-generated overview complements but does not replace it.

- [Main README](../../README.md) — Project overview
- [Roslyn Overview](../wiki/Roslyn-Overview.md) — Official architecture deep-dive
- [Samples and Walkthroughs](../wiki/Samples-and-Walkthroughs.md) — Getting started guides
- [Contributing Code](../wiki/Contributing-Code.md) — Development guidelines
- [Building, Testing, and Debugging](../wiki/Building-Testing-and-Debugging.md) — Setup instructions
- [Official Microsoft Docs](https://docs.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/compiler-api-model) — External documentation

---

## Documentation Scope

This documentation provides a high-level map of the Roslyn codebase. Individual area documents cover architecture and purpose but do not exhaustively document implementation details.

**What's covered:** Major areas, architecture overview, key patterns, terminology

**What's not covered:** Implementation details, all configuration options, edge cases

**To expand coverage:** Start a new AI session using the [Expanding Documentation Prompt](https://github.com/CyrusNajmabadi/codebase-explorer/blob/main/LOADER.md#expanding-documentation-prompt). Example requests:
- "Drill deeper into the Binder component"
- "Trace a compilation through the system"
- "Document the async/await lowering internals"

**Methodology:** This documentation was created using the [Codebase Explorer methodology](https://github.com/CyrusNajmabadi/codebase-explorer).
