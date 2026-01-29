# Roslyn Build System Overview

| | |
|---|---|
| **Last Updated** | January 29, 2026 |
| **Git SHA** | `771fe9b8443e955573725b4db6cc019685d8c2d4` |
| **Parent Doc** | [Main Overview](./main_overview.md) |

This document explains how code is built, tested, and deployed in the Roslyn codebase.

---

## Build System at a Glance

| Aspect | Technology/Approach |
|--------|---------------------|
| Primary build tool | MSBuild + .NET SDK |
| Build infrastructure | Microsoft Arcade SDK |
| CI system | Azure Pipelines |
| Distributed testing | Helix |
| Package management | NuGet (Central Package Management) |
| Artifact storage | Azure Artifacts |

---

## Core Build Tool: MSBuild with Arcade SDK

### Why This Combination?

- **MSBuild** is the standard .NET build system, deeply integrated with Visual Studio
- **Arcade SDK** provides standardized infrastructure across .NET repositories:
  - Consistent CI/CD patterns
  - Centralized version management
  - Signing and packaging
  - Helix test integration

### Basic Commands

| Task | Command |
|------|---------|
| Restore dependencies | `./eng/build.sh --restore` or `./Restore.cmd` |
| Build all | `./eng/build.sh --build` or `./Build.cmd` |
| Build compilers only | `dotnet build Compilers.slnf` |
| Build IDE only | `dotnet build Ide.slnf` |
| Run tests | `./eng/build.sh --test` or `./Test.cmd` |
| Clean | `./eng/build.sh --clean` |
| Full CI build | `./eng/build.sh --restore --build --test --pack` |

### Solution Filters

Roslyn uses solution filters (`.slnf`) to enable focused builds:

| Filter | Projects | Use Case |
|--------|----------|----------|
| `Compilers.slnf` | ~84 | Compiler-only development |
| `Ide.slnf` | ~247 | IDE feature development |
| `Roslyn.slnx` | All (~313) | Full solution |

### Key Build Configuration Files

| File | Purpose |
|------|---------|
| `Directory.Build.props` | Root MSBuild props; imports Arcade SDK |
| `Directory.Build.targets` | Root MSBuild targets |
| `Directory.Packages.props` | Central package version management |
| `eng/Versions.props` | Roslyn version and dependency versions |
| `eng/targets/Settings.props` | Project-wide settings (nullable, analyzers) |
| `eng/targets/TargetFrameworks.props` | Target framework definitions |
| `global.json` | .NET SDK version pinning |

---

## Build Phases

The build proceeds in phases, controlled by command-line arguments:

```
┌─────────┐    ┌─────────┐    ┌─────────┐    ┌─────────┐    ┌─────────┐
│ Restore │ →  │  Build  │ →  │  Test   │ →  │  Pack   │ →  │  Sign   │
└─────────┘    └─────────┘    └─────────┘    └─────────┘    └─────────┘
```

| Phase | What It Does |
|-------|--------------|
| **Restore** | NuGet package restoration |
| **Build** | Compile all projects |
| **Test** | Run unit and integration tests |
| **Pack** | Create NuGet packages |
| **Sign** | Code signing (official builds only) |
| **Publish** | Publish artifacts to feeds |

---

## Developer Workflow

### First-Time Setup

1. **Install prerequisites:**
   - .NET SDK (version in `global.json`)
   - Visual Studio 2022 (for VS integration development)

2. **Clone and restore:**
   ```bash
   git clone https://github.com/dotnet/roslyn
   cd roslyn
   ./Restore.cmd   # Windows
   ./restore.sh    # Unix
   ```

3. **Build:**
   ```bash
   ./Build.cmd     # Windows
   ./eng/build.sh  # Unix
   ```

### Daily Workflow

**For compiler work:**
```bash
# Build compilers only (faster)
dotnet build Compilers.slnf

# Run compiler tests
dotnet test src/Compilers/CSharp/Test/Syntax/CSharpSyntaxTests.csproj
```

**For IDE feature work:**
```bash
# Build IDE projects
dotnet build Ide.slnf

# Run specific feature tests
dotnet test src/Features/CSharp/Portable/Test/CSharpFeaturesTests.csproj
```

**Before committing:**
```bash
# Format code
dotnet format whitespace --folder . --include <changed-files>

# Run analyzers
./eng/build.sh --restore --build --runAnalyzers
```

### Debugging in Visual Studio

1. Open `Roslyn.slnx` (or appropriate `.slnf`)
2. Set startup project (e.g., `csc` for compiler debugging)
3. Set command-line arguments in project properties
4. F5 to debug

For debugging VS integration:
1. Set `VisualStudio.Roslyn.Development` as startup project
2. F5 launches experimental VS instance with your changes

---

## Bootstrap Build

Roslyn can build itself using a "bootstrap" process:

```bash
./eng/build.sh --bootstrap --build
```

**How it works:**
1. Build compilers using the installed SDK compiler
2. Copy built compilers to `artifacts/Bootstrap/`
3. Rebuild everything using the bootstrap compilers

**Why it matters:**
- Validates the compiler can compile itself
- Required for certain self-referential features
- Used in determinism testing

---

## CI/CD Pipeline

### Pipeline Structure

```
┌──────────────────────────────────────────────────────────────────┐
│                    Azure Pipelines                               │
│                                                                  │
│  ┌──────────┐    ┌──────────┐    ┌──────────┐    ┌──────────┐  │
│  │ Windows  │    │  Linux   │    │  macOS   │    │  VS Int. │  │
│  │  Build   │    │  Build   │    │  Build   │    │  Tests   │  │
│  └────┬─────┘    └────┬─────┘    └────┬─────┘    └────┬─────┘  │
│       │               │               │               │         │
│       └───────────────┴───────────────┴───────────────┘         │
│                            │                                    │
│                     ┌──────▼──────┐                            │
│                     │   Helix    │                             │
│                     │   Tests    │                             │
│                     └─────────────┘                            │
└──────────────────────────────────────────────────────────────────┘
```

### Build Configurations

| Job | Platform | Purpose |
|-----|----------|---------|
| Build_Windows_Debug | Windows | Debug build validation |
| Build_Windows_Release | Windows | Release build |
| Build_Unix_Debug | Linux | Cross-platform validation |
| Test_Windows_Desktop_Debug | Windows | .NET Framework tests |
| Test_Windows_CoreClr_Debug | Windows | .NET Core tests |
| Test_Linux_Debug | Linux | Linux test execution |
| VS_Integration_Debug | Windows | VS integration tests |
| Correctness_Determinism | Windows | Build determinism check |

### Helix Integration

Tests run on Helix for:
- Distributed execution across many machines
- Historical test data and flakiness tracking
- Support for specialized hardware/OS configurations

---

## Test Infrastructure

### Test Organization

| Pattern | Purpose |
|---------|---------|
| `*.UnitTests.csproj` | Fast, isolated unit tests |
| `*.IntegrationTests.csproj` | Full system integration tests |
| `Test.Utilities` | Shared test infrastructure |

### Running Tests

```bash
# All tests
./eng/build.sh --test

# Compiler tests only
./eng/build.sh --testCoreClr --testCompilerOnly

# Specific test project
dotnet test src/Compilers/CSharp/Test/Syntax/CSharpSyntaxTests.csproj

# With filter
dotnet test --filter "FullyQualifiedName~ParseTest"
```

### Test Patterns

Roslyn tests typically:
1. Inherit from base classes (`CSharpTestBase`, `VisualBasicTestBase`, `WorkspaceTestBase`)
2. Use `[UseExportProvider]` for MEF-dependent tests
3. Use raw string literals for test source code
4. Attribute tests fixing issues with `[WorkItem("https://github.com/dotnet/roslyn/issues/123")]`

---

## Build Performance

### Caching Strategies

| Cache | Purpose | Location |
|-------|---------|----------|
| NuGet cache | Package caching | `~/.nuget/packages` |
| MSBuild cache | Incremental builds | `artifacts/obj/` |
| Bootstrap cache | Compiler reuse | `artifacts/Bootstrap/` |

### Optimization Tips

- Use solution filters (`Compilers.slnf`, `Ide.slnf`) for faster builds
- Enable parallel builds: `dotnet build -m`
- Use binary log for build investigation: `dotnet build -bl`
- Skip documentation: `--skipDocumentation` (saves significant time)

---

## Troubleshooting

### "Build is slow"

1. Check you're using the right solution filter
2. Ensure NuGet cache is populated
3. Try `dotnet build --no-restore` after initial restore
4. Check for anti-virus interference on `artifacts/` directory

### "Build fails with missing SDK"

Ensure you have the correct .NET SDK version from `global.json`:
```bash
dotnet --version
# Should match version in global.json
```

### "Tests pass locally but fail in CI"

1. Check for test ordering dependencies
2. Verify all test resources are properly embedded
3. Check for hardcoded paths or timezones
4. CI runs with different locale settings (e.g., Spanish)

### "MSBuild error MSB4025"

Usually caused by corrupted NuGet cache:
```bash
dotnet nuget locals all --clear
./Restore.cmd
```

---

## Key Directories

| Directory | Purpose |
|-----------|---------|
| `eng/` | Build engineering (scripts, config, pipelines) |
| `eng/common/` | Shared Arcade infrastructure |
| `eng/pipelines/` | Azure Pipeline definitions |
| `eng/targets/` | MSBuild targets and props |
| `artifacts/bin/` | Build output |
| `artifacts/obj/` | Intermediate build files |
| `artifacts/packages/` | NuGet packages |
| `artifacts/Bootstrap/` | Bootstrap compiler |
| `artifacts/log/` | Build logs |

---

## Related Documentation

**In This Overview:**
- [Technology Mapping](./technology_mapping.md) — Technologies used across codebase
- [Main Overview](./main_overview.md) — Full codebase map

**Existing Codebase Docs:**
- [Building, Testing, and Debugging](../wiki/Building-Testing-and-Debugging.md)
- [Contributing Code](../wiki/Contributing-Code.md)
- [Official Contributing Guide](https://github.com/dotnet/roslyn/blob/main/CONTRIBUTING.md)

---

## Documentation Scope

This document provides a high-level overview of the build system. It covers common commands and CI/CD but does not exhaustively document all build options.

**What's covered:** Build commands, CI pipeline, developer workflow, troubleshooting

**What's not covered:** All MSBuild properties, Arcade SDK internals, all CI job configurations

**To go deeper:** Start a new AI session using the [Expanding Documentation Prompt](https://github.com/CyrusNajmabadi/codebase-explorer/blob/main/LOADER.md#expanding-documentation-prompt).

**Methodology:** This documentation was created using the [Codebase Explorer methodology](https://github.com/CyrusNajmabadi/codebase-explorer).
