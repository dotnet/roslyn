# Repository Structure

Complete map of the Roslyn repository directory structure and organization.

---

## Table of Contents

- [Root Level Structure](#root-level-structure)
- [Source Code (/src/)](#source-code-src)
- [Test Organization](#test-organization)
- [Documentation (/docs/)](#documentation-docs)
- [Build Engineering (/eng/)](#build-engineering-eng)
- [Configuration Files](#configuration-files)

---

## Root Level Structure

```
roslyn/
├── .azuredevops/          # Azure DevOps pipeline configurations
├── .devcontainer/         # Development container configuration
├── .github/               # GitHub workflows and issue templates
├── .vscode/               # VS Code workspace settings
├── docs/                  # Documentation and specifications
├── eng/                   # Build engineering and configuration
├── scripts/               # Build and utility scripts
├── src/                   # All source code
├── Roslyn.slnx            # Main solution file
├── Compilers.slnf         # Solution filter: Compiler projects only
├── Ide.slnf               # Solution filter: IDE features only
├── Build.cmd              # Windows build script
├── build.sh               # Unix build script
├── Test.cmd               # Test execution script
├── Restore.cmd            # NuGet restore (Windows)
├── restore.sh             # NuGet restore (Unix)
├── global.json            # .NET SDK version specification
├── NuGet.config           # NuGet feed configuration
├── Directory.Build.props  # MSBuild properties (imported by all projects)
├── Directory.Build.targets # MSBuild targets (imported by all projects)
├── Directory.Build.rsp    # Compiler response file
├── Directory.Packages.props # Centralized package version management
├── .editorconfig          # Code style rules
└── README.md              # Project overview
```

### Key Root Files

| File | Purpose |
|------|---------|
| `Roslyn.slnx` | Main solution containing all projects |
| `Compilers.slnf` | Solution filter for compiler-focused work |
| `Ide.slnf` | Solution filter for IDE feature development |
| `global.json` | Pins .NET SDK version (currently 10.0.100-rc.2) |
| `Directory.Build.props` | Shared MSBuild properties (target frameworks, versions) |
| `Directory.Packages.props` | Central package version management (CPM) |
| `.editorconfig` | Code style and formatting rules |

---

## Source Code (/src/)

The `/src/` directory contains 18 major component directories organized by functionality.

### Complete /src/ Directory Map

```
src/
├── Analyzers/             # Built-in diagnostic analyzers and code fixes
├── CodeStyle/             # Code style analyzers (IDE0001-IDExxxx rules)
├── Compilers/             # C# and VB compilers (parser, binder, emitter)
├── Dependencies/          # Shared internal dependencies
├── Deployment/            # VSIX packaging configuration
├── EditorFeatures/        # Editor integration (80+ IDE features)
├── ExpressionEvaluator/   # Debugger expression evaluation
├── Features/              # Core IDE feature implementations
├── Interactive/           # Interactive REPL hosting
├── LanguageServer/        # Language Server Protocol implementation
├── NuGet/                 # NuGet package definitions
├── RoslynAnalyzers/       # Analyzers for Roslyn codebase quality
├── Scripting/             # C# and VB scripting APIs
├── Setup/                 # Deployment and installation
├── Test/                  # Test infrastructure and utilities
├── Tools/                 # Developer tools and utilities
├── VisualStudio/          # Visual Studio integration
└── Workspaces/            # Workspace API (solution/project/document model)
```

### Directory Purposes

#### 1. Compilers/
**Complete C# and Visual Basic compiler implementations**

```
Compilers/
├── Core/                  # Language-agnostic compiler infrastructure
│   ├── Portable/          # Cross-platform compiler code
│   │   ├── Binding/       # Semantic analysis and symbol binding
│   │   ├── Compilation/   # Compilation orchestration
│   │   ├── Diagnostic/    # Error and warning reporting
│   │   ├── Emit/          # IL emission to assemblies
│   │   ├── MetadataReader/ # Reading PE files and metadata
│   │   ├── PEWriter/      # Writing Portable Executable files
│   │   ├── Symbols/       # Symbol table and definitions
│   │   ├── Syntax/        # Syntax tree abstractions
│   │   ├── Text/          # Source text handling
│   │   ├── DiagnosticAnalyzer/ # Analyzer driver
│   │   ├── SourceGeneration/   # Source generator support
│   │   └── CommandLine/   # Command-line parsing
│   ├── CodeAnalysisTest/  # Core compiler unit tests
│   ├── MSBuildTask/       # MSBuild integration tasks
│   └── AnalyzerDriver/    # Diagnostic analyzer execution
├── CSharp/                # C# compiler implementation
│   ├── Portable/          # C# compiler (24 subdirectories)
│   │   ├── Parser/        # C# syntax parser
│   │   ├── Binder/        # C# semantic binding
│   │   ├── Symbols/       # C# symbol definitions
│   │   ├── Syntax/        # C# syntax tree nodes
│   │   ├── Compilation/   # C# compilation orchestration
│   │   ├── Emitter/       # C# IL emission
│   │   ├── Lowering/      # Desugaring (async, iterators, etc.)
│   │   ├── FlowAnalysis/  # Data flow and definite assignment
│   │   ├── BoundTree/     # Intermediate representation
│   │   ├── CodeGen/       # Code generation helpers
│   │   ├── Operations/    # IOperation API implementation
│   │   └── Errors/        # C# error definitions
│   ├── Test/              # C# compiler tests
│   │   ├── Syntax/        # Parser tests
│   │   ├── Semantic/      # Semantic analysis tests
│   │   ├── Symbol/        # Symbol tests
│   │   ├── Emit/          # IL emission tests
│   │   └── CommandLine/   # CLI tests
│   └── csc/               # C# compiler executable
│       ├── AnyCpu/        # Platform-agnostic
│       └── arm64/         # ARM64-specific
└── VisualBasic/           # VB compiler (similar structure)
    ├── Portable/
    ├── Test/
    └── vbc/               # VB compiler executable
```

**Path Examples:**
- C# parser: `/src/Compilers/CSharp/Portable/Parser/`
- C# binder: `/src/Compilers/CSharp/Portable/Binder/`
- IL emitter: `/src/Compilers/Core/Portable/Emit/`
- Diagnostics: `/src/Compilers/Core/Portable/Diagnostic/`

#### 2. Workspaces/
**Solution/project/document APIs and workspace services**

```
Workspaces/
├── Core/
│   ├── Portable/          # Core workspace API
│   │   ├── Workspace/     # Solution/project/document model
│   │   ├── CodeActions/   # Refactoring infrastructure
│   │   ├── Formatting/    # Code formatting infrastructure
│   │   ├── Rename/        # Rename refactoring
│   │   ├── FindSymbols/   # Symbol search
│   │   ├── Classification/ # Syntax classification
│   │   ├── Diagnostics/   # Diagnostic collection
│   │   ├── Options/       # Workspace options
│   │   ├── SourceGeneration/ # Source generator integration
│   │   ├── Storage/       # Persistence and caching
│   │   ├── Simplification/ # Syntax simplification
│   │   └── LanguageServices/ # Language service abstraction
│   ├── Desktop/           # Windows-specific implementations
│   ├── CoreTest/          # Workspace API tests
│   └── CoreTestUtilities/ # Test utilities
├── CSharp/                # C#-specific workspace services
├── VisualBasic/           # VB-specific workspace services
└── MSBuild/               # MSBuild project system integration
```

**Path Examples:**
- Workspace model: `/src/Workspaces/Core/Portable/Workspace/`
- Rename: `/src/Workspaces/Core/Portable/Rename/`
- Formatting: `/src/Workspaces/Core/Portable/Formatting/`

#### 3. Features/
**Language-agnostic IDE feature implementations**

```
Features/
├── Core/
│   ├── Portable/          # Core feature algorithms (language-agnostic)
│   │   ├── CodeFixes/     # Code fix infrastructure
│   │   ├── CodeRefactorings/ # Refactoring implementations
│   │   ├── CodeStyle/     # Code style analysis
│   │   ├── Completion/    # IntelliSense completion
│   │   ├── Diagnostics/   # Diagnostic infrastructure
│   │   ├── ExtractMethod/ # Extract method refactoring
│   │   ├── FindUsages/    # Find all references
│   │   ├── GoToDefinition/ # Go to definition
│   │   ├── InlineHints/   # Parameter/type hints
│   │   ├── Navigation/    # Navigation services
│   │   ├── QuickInfo/     # Hover tooltips
│   │   └── ... (40+ more)
│   └── Test/              # Core feature tests
├── CSharp/                # C#-specific feature implementations
├── VisualBasic/           # VB-specific feature implementations
├── CSharpTest/            # C# feature tests
└── VisualBasicTest/       # VB feature tests
```

**Path Examples:**
- Refactorings: `/src/Features/Core/Portable/CodeRefactorings/`
- Completion: `/src/Features/Core/Portable/Completion/`
- Go to definition: `/src/Features/Core/Portable/GoToDefinition/`

#### 4. EditorFeatures/
**Editor integration and UI for IDE features**

```
EditorFeatures/
├── Core/                  # Editor feature infrastructure (84 subdirectories)
│   ├── IntelliSense/      # IntelliSense UI
│   ├── CodeActions/       # Quick fixes and refactorings UI
│   ├── GoToDefinition/    # Navigation UI
│   ├── FindUsages/        # Find references UI
│   ├── Formatting/        # Formatting UI
│   ├── Classification/    # Syntax highlighting
│   ├── QuickInfo/         # Hover tooltip UI
│   ├── Completion/        # Completion UI
│   ├── SignatureHelp/     # Parameter hints UI
│   ├── BraceMatching/     # Brace highlighting
│   ├── InlineRename/      # Rename UI
│   ├── InlineHints/       # Inline hints UI
│   ├── EditAndContinue/   # Edit-and-continue support
│   ├── LanguageServer/    # LSP integration
│   └── ... (70+ more)
├── CSharp/                # C#-specific editor features
├── VisualBasic/           # VB-specific editor features
├── Text/                  # Text editor foundation
├── CSharpTest/            # C# editor tests
└── VisualBasicTest/       # VB editor tests
```

**Path Examples:**
- IntelliSense: `/src/EditorFeatures/Core/IntelliSense/`
- Quick actions: `/src/EditorFeatures/Core/CodeActions/`
- Classification: `/src/EditorFeatures/Core/Classification/`

#### 5. Analyzers/
**Built-in diagnostic analyzers and code fixes**

```
Analyzers/
├── Core/
│   ├── Analyzers/         # Language-agnostic analyzers
│   └── CodeFixes/         # Language-agnostic fixes
├── CSharp/
│   ├── Analyzers/         # C#-specific diagnostics
│   └── CodeFixes/         # C#-specific fixes
└── VisualBasic/
    ├── Analyzers/         # VB-specific diagnostics
    └── CodeFixes/         # VB-specific fixes
```

#### 6. CodeStyle/
**Code style analyzers (IDE rules)**

```
CodeStyle/
├── Core/
│   ├── Analyzers/         # IDE code style rules
│   └── CodeFixes/         # Code style fixes
├── CSharp/
│   ├── Analyzers/         # C# style analyzers
│   └── CodeFixes/         # C# style fixes
└── VisualBasic/
    ├── Analyzers/         # VB style analyzers
    └── CodeFixes/         # VB style fixes
```

#### 7. LanguageServer/
**Language Server Protocol implementation**

```
LanguageServer/
├── Microsoft.CodeAnalysis.LanguageServer  # Main LSP server
├── Protocol/                               # LSP protocol definitions
├── ProtocolUnitTests/                      # LSP tests
└── ExternalAccess/                         # External LSP APIs
```

#### 8. VisualStudio/
**Visual Studio integration**

```
VisualStudio/
├── Core/                  # VS core integration
├── CSharp/                # C# VS integration
├── VisualBasic/           # VB VS integration
├── CodeLens/              # CodeLens provider
├── Setup/                 # VSIX deployment
├── IntegrationTest/       # VS integration tests
├── LiveShare/             # Live Share support
└── ExternalAccess/        # External VS APIs
```

#### 9. Scripting/
**C# and VB scripting APIs**

```
Scripting/
├── Core/                  # Scripting infrastructure
├── CSharp/                # C# scripting
├── VisualBasic/           # VB scripting
└── *Test/                 # Scripting tests
```

#### 10. Interactive/
**REPL and interactive session hosting**

```
Interactive/
├── Host/                  # Interactive session host
├── HostProcess/           # Remote host process
├── csi/                   # C# Interactive executable
└── vbi/                   # VB Interactive executable
```

#### 11. ExpressionEvaluator/
**Debugger expression evaluation**

```
ExpressionEvaluator/
├── Core/                  # EE infrastructure
├── CSharp/                # C# expression evaluator
├── VisualBasic/           # VB expression evaluator
└── Package/               # Debugger integration
```

#### 12. Tools/
**Developer tools and utilities**

```
Tools/
├── ExternalAccess/        # External APIs
├── IdeBenchmarks/         # IDE performance benchmarks
├── IdeCoreBenchmarks/     # Core benchmarks
├── BuildValidator/        # Build validation
├── AnalyzerRunner/        # Analyzer execution tool
├── dotnet-format/         # Code formatter
└── Source/                # Tool source code
```

#### 13. RoslynAnalyzers/
**Analyzers for Roslyn codebase quality**

```
RoslynAnalyzers/
├── Microsoft.CodeAnalysis.Analyzers          # Roslyn API analyzers
├── PublicApiAnalyzers                        # Public API tracking
├── PerformanceSensitiveAnalyzers             # Performance analyzers
└── Roslyn.Diagnostics.Analyzers              # Roslyn diagnostics
```

#### 14. Dependencies/
**Shared internal dependencies**

```
Dependencies/
├── Collections/           # Collection abstractions
├── Contracts/             # Interface contracts
├── PooledObjects/         # Object pooling
└── Threading/             # Threading utilities
```

#### 15. NuGet/
**NuGet package definitions**

```
NuGet/
├── Microsoft.CodeAnalysis.Package
├── Microsoft.CodeAnalysis.Compilers.Package
├── Microsoft.CodeAnalysis.EditorFeatures.Package
├── Microsoft.CodeAnalysis.Scripting.Package
└── Microsoft.Net.Compilers.Toolset
```

#### 16. Setup/
**Deployment configuration**

```
Setup/
├── BuildTasks/            # Custom MSBuild tasks
├── DevDivInsertionFiles/  # DevDiv metadata
└── DevDivVsix/            # VSIX configuration
```

#### 17. Deployment/
**VSIX packaging**

```
Deployment/
└── RoslynDeployment.csproj  # VSIX manifest
```

#### 18. Test/
**Test infrastructure**

```
Test/
├── Utilities/             # Test helper utilities
└── PdbUtilities/          # PDB test helpers
```

---

## Test Organization

Tests are co-located with source code in dedicated test projects:

### Major Test Directories

```
src/
├── Compilers/
│   ├── Core/CodeAnalysisTest/            # Core compiler tests
│   ├── CSharp/Test/                      # C# compiler tests
│   │   ├── Syntax/                       # Parser tests
│   │   ├── Semantic/                     # Semantic tests
│   │   ├── Symbol/                       # Symbol tests
│   │   ├── Emit/                         # IL emission tests
│   │   └── CommandLine/                  # CLI tests
│   └── VisualBasic/Test/                 # VB compiler tests
├── EditorFeatures/
│   ├── CSharpTest/                       # C# editor tests (64 subdirs)
│   ├── VisualBasicTest/                  # VB editor tests (48 subdirs)
│   └── Test/, Test2/                     # Core editor tests
├── Features/
│   ├── Core/Test/                        # Core feature tests
│   ├── CSharpTest/                       # C# feature tests
│   └── VisualBasicTest/                  # VB feature tests (56 subdirs)
├── Workspaces/
│   ├── CoreTest/                         # Workspace API tests (22 subdirs)
│   ├── CSharpTest/                       # C# workspace tests
│   └── VisualBasicTest/                  # VB workspace tests
├── LanguageServer/
│   └── ProtocolUnitTests/                # LSP tests (35+ subdirs)
└── RoslynAnalyzers/
    └── *Test.Utilities/                  # Analyzer tests
```

### Test Framework
- **Framework:** xUnit
- **Organization:** Co-located with source code
- **Utilities:** Shared in `*TestUtilities/` directories
- **Count:** 39+ major test directories

---

## Documentation (/docs/)

```
docs/
├── specs/                 # Specifications
│   ├── PortablePdb-Metadata.md
│   └── CSharp 6/
├── compilers/             # Compiler documentation
│   ├── Design/            # Design documents
│   │   ├── Parser.md
│   │   ├── Bound Node Design.md
│   │   └── Closure Conversion.md
│   ├── CSharp/
│   └── Visual Basic/
├── features/              # Feature documentation
│   ├── source-generators.md
│   ├── nullable-reference-types.md
│   ├── interceptors.md
│   └── ... (30+ feature docs)
├── ide/                   # IDE documentation
│   ├── api-designs/
│   ├── specs/
│   └── test-plans/
├── contributing/          # Contributor guides
│   ├── Building, Debugging, and Testing on Windows.md
│   ├── Building, Debugging, and Testing on Unix.md
│   ├── Compiler Test Plan.md
│   └── Developing a Language Feature.md
├── infrastructure/        # Build/CI documentation
├── roslyn-analyzers/      # Analyzer documentation
└── wiki/                  # Wiki documentation
```

---

## Build Engineering (/eng/)

```
eng/
├── common/                # Shared Arcade SDK infrastructure
│   ├── BuildConfiguration/
│   ├── core-templates/    # Azure Pipelines templates
│   ├── post-build/        # Post-build verification
│   ├── sdl/               # Security checks
│   └── loc/               # Localization
├── config/                # Configuration
│   ├── globalconfigs/     # EditorConfig files
│   ├── test/              # Test configuration
│   └── guardian/          # Security scanning
├── targets/               # MSBuild targets (23 files)
│   ├── Settings.props     # Build settings
│   ├── Services.props     # Service configuration
│   ├── TargetFrameworks.props
│   ├── Imports.targets
│   ├── VisualStudio.targets
│   └── XUnit.targets
└── pipelines/             # Azure Pipelines YAML
```

---

## Configuration Files

### Build Configuration
- `Directory.Build.props` - Global MSBuild properties
- `Directory.Build.targets` - Global MSBuild targets
- `Directory.Build.rsp` - Compiler response file
- `Directory.Packages.props` - Package version management

### Development Configuration
- `.editorconfig` - Code style rules
- `NuGet.config` - NuGet feed configuration
- `global.json` - .NET SDK version

### CI/CD Configuration
- `azure-pipelines.yml` - Main CI pipeline
- `azure-pipelines-pr-validation.yml` - PR validation
- `azure-pipelines-official.yml` - Official builds
- `azure-pipelines-integration.yml` - Integration tests

---

## Quick Navigation

### To find...

| What | Where |
|------|-------|
| C# compiler | `/src/Compilers/CSharp/Portable/` |
| VB compiler | `/src/Compilers/VisualBasic/Portable/` |
| Workspace APIs | `/src/Workspaces/Core/Portable/` |
| IDE features | `/src/Features/Core/Portable/` |
| Editor integration | `/src/EditorFeatures/Core/` |
| Analyzers | `/src/Analyzers/` |
| Code style | `/src/CodeStyle/` |
| LSP server | `/src/LanguageServer/` |
| VS integration | `/src/VisualStudio/` |
| Scripting | `/src/Scripting/` |
| Tests | Co-located with source in `*Test/` dirs |
| Documentation | `/docs/` |
| Build config | `/eng/` |

---

**Next:** [Component Guide](02-component-guide.md) - Detailed component documentation
