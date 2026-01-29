# Roslyn Technology Mapping

**Last Updated:** January 29, 2026

This document maps technologies to their usage across the codebase. Use it to answer "where is X used?" and "what tech does Y use?"

---

## Technology by Category

### Languages

| Language | Primary Use | Key Areas |
|----------|-------------|-----------|
| **C#** | Main implementation (~14,000 files) | All areas |
| **VB.NET** | VB compiler, VB tests (~3,600 files) | `src/Compilers/VisualBasic/`, `src/Features/VisualBasic/` |
| **XML** | MSBuild, config files | `*.csproj`, `*.props`, `*.targets` |
| **YAML** | CI/CD pipelines | `eng/pipelines/`, `.github/workflows/` |
| **JSON** | Configuration, LSP | `global.json`, LSP messages |

### Frameworks & Runtimes

| Framework | Purpose | Where Used |
|-----------|---------|------------|
| **.NET 8.0 / 10.0** | Primary runtime for tools | Most projects |
| **.NET Framework 4.7.2** | VS integration, legacy support | `src/VisualStudio/`, tests |
| **.NET Standard 2.0** | Cross-platform compatibility | Core compiler packages |
| **ASP.NET Core** | Language server hosting | `src/LanguageServer/` |

### Build System

| Technology | Purpose | Where Used |
|------------|---------|------------|
| **MSBuild** | Build orchestration | All projects |
| **Arcade SDK** | CI/CD, packaging, signing | `eng/`, `Directory.Build.props` |
| **NuGet** | Package management | `Directory.Packages.props` |
| **Solution Filters** | Focused builds | `Compilers.slnf`, `Ide.slnf` |

### IDE Integration

| Technology | Purpose | Where Used |
|------------|---------|------------|
| **MEF (v2)** | Dependency injection | All IDE services |
| **WPF** | Editor UI components | `src/EditorFeatures/` |
| **VS SDK** | VS package infrastructure | `src/VisualStudio/` |
| **LSP** | Editor-agnostic protocol | `src/LanguageServer/` |
| **ServiceHub** | Out-of-process services | `src/Workspaces/Remote/` |
| **StreamJsonRpc** | JSON-RPC communication | ServiceHub, LSP |

### Testing

| Technology | Purpose | Where Used |
|------------|---------|------------|
| **xUnit** | Unit test framework | All test projects |
| **Helix** | Distributed test execution | CI pipelines |
| **BenchmarkDotNet** | Performance benchmarks | `src/Tools/IdeBenchmarks/` |

### Protocols & Formats

| Technology | Purpose | Where Used |
|------------|---------|------------|
| **LSP (JSON-RPC)** | Language server communication | `src/LanguageServer/` |
| **PE/COFF** | Executable format | Emit pipeline |
| **PDB (Portable)** | Debug symbols | Emit pipeline |
| **Named Pipes** | Compiler server IPC | `src/Compilers/Server/` |

---

## Area-by-Area Technology Stack

### Compilers

| Layer | Technology |
|-------|------------|
| Language | C#, VB |
| Runtime | .NET 8.0, .NET Standard 2.0 |
| Output | PE/COFF, Portable PDB |
| IPC | Named Pipes (VBCSCompiler) |
| Testing | xUnit |

### Workspaces

| Layer | Technology |
|-------|------------|
| Language | C# |
| Runtime | .NET 8.0, .NET Framework 4.7.2 |
| Build Integration | MSBuild APIs |
| Remote Services | ServiceHub, StreamJsonRpc |
| Serialization | System.Text.Json |

### Features

| Layer | Technology |
|-------|------------|
| Language | C# |
| Runtime | .NET 8.0, .NET Framework 4.7.2 |
| DI | MEF v2 |
| Async | Task-based async |

### Language Server

| Layer | Technology |
|-------|------------|
| Language | C# |
| Protocol | LSP 3.17 |
| Transport | JSON-RPC over stdio/pipes |
| Framework | CLaSP (Microsoft.CommonLanguageServerProtocol.Framework) |
| Serialization | System.Text.Json |

### Editor Features

| Layer | Technology |
|-------|------------|
| Language | C# |
| Runtime | .NET Framework 4.7.2 |
| UI | WPF |
| Editor APIs | VS Editor (ITextView, ITextBuffer) |
| DI | MEF v2 |

### Visual Studio

| Layer | Technology |
|-------|------------|
| Language | C# |
| Runtime | .NET Framework 4.7.2 |
| Package | VSIX, VS SDK |
| DI | MEF v2, VS ServiceProvider |
| Deployment | VSIX manifest |

---

## Technology Usage Matrix

| Technology | Compilers | Workspaces | Features | LanguageServer | EditorFeatures | VisualStudio | Analyzers |
|------------|-----------|------------|----------|----------------|----------------|--------------|-----------|
| C# | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| .NET 8.0+ | ✓ | ✓ | ✓ | ✓ | | | ✓ |
| .NET Framework | | ✓ | ✓ | | ✓ | ✓ | |
| MSBuild | ✓ | ✓ | | | | | |
| MEF | | ✓ | ✓ | ✓ | ✓ | ✓ | |
| WPF | | | | | ✓ | ✓ | |
| LSP | | | | ✓ | | | |
| ServiceHub | | ✓ | | | | ✓ | |
| xUnit | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |

---

## Technology Notes

### MEF (Managed Extensibility Framework)

**Version:** MEF v2 (System.Composition)

**Why it's used:** Provides dependency injection for IDE services. Allows language-specific implementations to be discovered and composed without hard dependencies.

**Pattern:**
```csharp
[ExportLanguageService(typeof(IMyService), LanguageNames.CSharp), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class CSharpMyService : IMyService { }
```

**Links:** [MEF Documentation](https://docs.microsoft.com/en-us/dotnet/framework/mef/)

---

### Language Server Protocol (LSP)

**Version:** LSP 3.17

**Why it's used:** Enables Roslyn features in VS Code and other editors. Provides a standardized protocol for language features.

**Implementation:** Roslyn uses CLaSP (Common Language Server Protocol Framework) as a base, then adds Roslyn-specific handlers.

**Links:** [LSP Specification](https://microsoft.github.io/language-server-protocol/)

---

### ServiceHub

**Why it's used:** Runs expensive operations (like find references, rename) out-of-process to avoid blocking the VS UI thread.

**How it works:**
1. VS hosts ServiceHub services in separate processes
2. Services communicate via JSON-RPC (StreamJsonRpc)
3. Solution state is synchronized via checksums

**Links:** Internal Microsoft technology; see `src/Workspaces/Remote/` for implementation.

---

### Arcade SDK

**Why it's used:** Standardized build infrastructure across .NET repositories. Provides CI templates, signing, packaging, and publishing.

**Key files:**
- `Directory.Build.props` — Imports Arcade SDK
- `eng/Versions.props` — Version management
- `eng/common/` — Shared Arcade infrastructure

**Links:** [Arcade Repository](https://github.com/dotnet/arcade)

---

## Build & CI Technologies

| Technology | Purpose |
|------------|---------|
| **MSBuild** | Build orchestration |
| **Arcade SDK** | CI/CD infrastructure |
| **Azure Pipelines** | CI builds and tests |
| **Helix** | Distributed test execution |
| **NuGet** | Package publishing |
| **Authenticode** | Code signing |

---

## Deprecated Technologies

| Technology | Replacement | Migration Status |
|------------|-------------|------------------|
| MEF v1 | MEF v2 | Complete |
| project.json | MSBuild SDK-style | Complete |
| packages.config | PackageReference | Complete |
