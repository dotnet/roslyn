# Component Guide

Detailed guide to Roslyn's major components and where to find specific functionality.

---

## Table of Contents

- [Compilers](#compilers)
- [Workspaces](#workspaces)
- [IDE Features](#ide-features)
- [Editor Features](#editor-features)
- [Analyzers and Code Style](#analyzers-and-code-style)
- [Language Server](#language-server)
- [Visual Studio Integration](#visual-studio-integration)
- [Scripting and Interactive](#scripting-and-interactive)
- [Expression Evaluator](#expression-evaluator)
- [Tools and Utilities](#tools-and-utilities)

---

## Compilers

**Location:** `/src/Compilers/`

The compiler component provides complete C# and Visual Basic compiler implementations.

### Compiler Architecture

```
Source Code
    ↓
Lexical Analysis (Lexer/Scanner)
    ↓
Syntax Analysis (Parser) → Syntax Tree
    ↓
Semantic Analysis (Binder) → Bound Tree + Symbols
    ↓
Lowering (Desugar) → Lowered Bound Tree
    ↓
IL Emission → Assembly (.dll/.exe)
```

### Core Compiler (`/src/Compilers/Core/Portable/`)

Language-agnostic compiler infrastructure shared by C# and VB.

#### Key Subdirectories

| Directory | Purpose | Key Classes |
|-----------|---------|-------------|
| `Binding/` | Symbol resolution, overload resolution | `Binder`, `BinderFactory` |
| `Compilation/` | Compilation orchestration | `Compilation`, `CompilationOptions` |
| `Diagnostic/` | Error and warning reporting | `Diagnostic`, `DiagnosticBag` |
| `Emit/` | IL emission to assemblies | `PEBuilder`, `EmitContext` |
| `MetadataReader/` | PE file and metadata reading | `PEReader`, `MetadataReader` |
| `PEWriter/` | Portable Executable writing | `PEBuilder`, `PEWriter` |
| `Symbols/` | Symbol definitions (types, methods, etc.) | `ISymbol`, `ITypeSymbol`, `IMethodSymbol` |
| `Syntax/` | Syntax tree abstractions | `SyntaxNode`, `SyntaxToken`, `SyntaxTree` |
| `Text/` | Source text management | `SourceText`, `TextSpan`, `LinePosition` |
| `DiagnosticAnalyzer/` | Analyzer infrastructure | `DiagnosticAnalyzer`, `AnalysisContext` |
| `SourceGeneration/` | Source generator support | `ISourceGenerator`, `GeneratorDriver` |
| `CommandLine/` | Command-line parsing | `CommandLineParser`, `CommandLineArguments` |

#### Important APIs

**Compilation API:**
```csharp
// Location: /src/Compilers/Core/Portable/Compilation/Compilation.cs
Compilation compilation = ...;
var diagnostics = compilation.GetDiagnostics();
var emitResult = compilation.Emit(stream);
var semanticModel = compilation.GetSemanticModel(tree);
```

**Symbols API:**
```csharp
// Location: /src/Compilers/Core/Portable/Symbols/
ISymbol symbol = semanticModel.GetSymbolInfo(node).Symbol;
ITypeSymbol type = semanticModel.GetTypeInfo(expression).Type;
```

**Diagnostic API:**
```csharp
// Location: /src/Compilers/Core/Portable/Diagnostic/Diagnostic.cs
Diagnostic diagnostic = Diagnostic.Create(descriptor, location, args);
```

### C# Compiler (`/src/Compilers/CSharp/Portable/`)

C#-specific compiler implementation.

#### Key Subdirectories

| Directory | Purpose | Entry Points |
|-----------|---------|--------------|
| `Parser/` | C# syntax parsing | `LanguageParser.cs` |
| `Binder/` | C# semantic binding | `Binder.cs`, `BinderFactory.cs` |
| `Symbols/` | C# symbol definitions | `SourceNamedTypeSymbol.cs` |
| `Syntax/` | C# syntax tree nodes | `CSharpSyntaxNode.cs` |
| `Compilation/` | C# compilation | `CSharpCompilation.cs` |
| `Emitter/` | C# IL emission | `PEModuleBuilder.cs` |
| `Lowering/` | Syntactic sugar lowering | `LocalRewriter.cs` |
| `FlowAnalysis/` | Data flow analysis | `DataFlowPass.cs`, `NullableWalker.cs` |
| `BoundTree/` | Intermediate representation | `BoundNode.cs`, `BoundExpression.cs` |
| `CodeGen/` | Code generation | `ILBuilder.cs` |
| `Operations/` | IOperation implementation | `CSharpOperationFactory.cs` |
| `Errors/` | C# error definitions | `ErrorCode.cs` |

#### C# Compiler Pipeline Files

**Parser:** `/src/Compilers/CSharp/Portable/Parser/LanguageParser.cs`
- Converts tokens to syntax trees
- Handles all C# syntax constructs
- Error recovery during parsing

**Binder:** `/src/Compilers/CSharp/Portable/Binder/Binder.cs`
- Semantic analysis
- Symbol resolution
- Type checking
- Overload resolution

**Lowering:** `/src/Compilers/CSharp/Portable/Lowering/LocalRewriter.cs`
- Desugars language features:
  - `async`/`await` → state machines
  - `foreach` → while loops
  - `using` → try/finally
  - LINQ query syntax → method calls
  - Properties → get/set methods
  - Events → add/remove methods

**Flow Analysis:**
- `/src/Compilers/CSharp/Portable/FlowAnalysis/DataFlowPass.cs` - Data flow analysis
- `/src/Compilers/CSharp/Portable/FlowAnalysis/NullableWalker.cs` - Nullable analysis
- `/src/Compilers/CSharp/Portable/FlowAnalysis/DefiniteAssignment.cs` - Definite assignment

**Emitter:** `/src/Compilers/CSharp/Portable/Emitter/Model/PEModuleBuilder.cs`
- Generates IL opcodes
- Writes metadata
- Creates assembly

### Visual Basic Compiler (`/src/Compilers/VisualBasic/Portable/`)

Similar structure to C# compiler:
- `Scanner/` - Lexical analysis
- `Parser/` - Syntax parsing
- `Binding/` - Semantic analysis
- `Symbols/` - Symbol definitions
- `Semantics/` - Semantic model
- `CodeGen/` - IL generation

### Compiler Executables

**C# Compiler:**
- `/src/Compilers/CSharp/csc/` - csc.exe (C# compiler)

**VB Compiler:**
- `/src/Compilers/VisualBasic/vbc/` - vbc.exe (VB compiler)

### MSBuild Integration

**Location:** `/src/Compilers/Core/MSBuildTask/`

Provides MSBuild tasks for compiling C# and VB projects:
- `Csc.cs` - C# compilation task
- `Vbc.cs` - VB compilation task

---

## Workspaces

**Location:** `/src/Workspaces/`

Provides APIs for working with solutions, projects, and documents.

### Workspace Model

```
Workspace
├── Solution
    ├── Project (C#)
    │   ├── Document (.cs file)
    │   ├── AdditionalDocument (config files)
    │   ├── AnalyzerConfigDocument (.editorconfig)
    │   └── Compilation
    ├── Project (VB)
    └── ProjectReference
```

### Core Workspace (`/src/Workspaces/Core/Portable/`)

#### Key Subdirectories

| Directory | Purpose | Key Classes |
|-----------|---------|-------------|
| `Workspace/` | Solution/project/document model | `Workspace`, `Solution`, `Project`, `Document` |
| `CodeActions/` | Refactoring infrastructure | `CodeAction`, `CodeActionProvider` |
| `Formatting/` | Code formatting | `Formatter`, `FormattingOptions` |
| `Rename/` | Symbol renaming | `Renamer`, `RenameOptions` |
| `FindSymbols/` | Symbol search | `SymbolFinder` |
| `Classification/` | Syntax classification | `Classifier` |
| `Diagnostics/` | Diagnostic collection | `DiagnosticAnalyzer`, `DiagnosticData` |
| `Options/` | Workspace options | `OptionKey`, `OptionSet` |
| `Simplification/` | Syntax simplification | `Simplifier` |
| `LanguageServices/` | Language service abstraction | `ILanguageService` |
| `CodeCleanup/` | Code cleanup | `CodeCleaner` |
| `ChangeNamespace/` | Namespace refactoring | `ChangeNamespaceService` |
| `ExtractMethod/` | Extract method | `ExtractMethodService` |
| `Storage/` | Persistence | `IPersistentStorage` |

#### Important APIs

**Workspace API:**
```csharp
// Location: /src/Workspaces/Core/Portable/Workspace/Workspace.cs
Workspace workspace = ...;
Solution solution = workspace.CurrentSolution;
Project project = solution.Projects.First();
Document document = project.Documents.First();
```

**Document Manipulation:**
```csharp
// Get syntax tree
SyntaxNode root = await document.GetSyntaxRootAsync();

// Get semantic model
SemanticModel model = await document.GetSemanticModelAsync();

// Update document
Document newDocument = document.WithSyntaxRoot(newRoot);
```

**Formatting API:**
```csharp
// Location: /src/Workspaces/Core/Portable/Formatting/Formatter.cs
Document formatted = await Formatter.FormatAsync(document);
SyntaxNode formatted = await Formatter.FormatAsync(root, workspace);
```

**Rename API:**
```csharp
// Location: /src/Workspaces/Core/Portable/Rename/Renamer.cs
Solution newSolution = await Renamer.RenameSymbolAsync(
    solution, symbol, newName, options);
```

**Symbol Finding:**
```csharp
// Location: /src/Workspaces/Core/Portable/FindSymbols/SymbolFinder.cs
var references = await SymbolFinder.FindReferencesAsync(symbol, solution);
var implementations = await SymbolFinder.FindImplementationsAsync(symbol, solution);
```

### MSBuild Integration (`/src/Workspaces/MSBuild/`)

Loads MSBuild projects into the workspace:
```csharp
// Location: /src/Workspaces/MSBuild/MSBuild/MSBuildWorkspace.cs
var workspace = MSBuildWorkspace.Create();
var project = await workspace.OpenProjectAsync(projectPath);
var solution = await workspace.OpenSolutionAsync(solutionPath);
```

---

## IDE Features

**Location:** `/src/Features/`

Language-agnostic implementations of IDE features.

### Core Features (`/src/Features/Core/Portable/`)

#### Navigation Features

| Feature | Location | Purpose |
|---------|----------|---------|
| Go to Definition | `GoToDefinition/` | Navigate to symbol definition |
| Find References | `FindUsages/` | Find all references to a symbol |
| Navigate To | `NavigateTo/` | Search and navigate to symbols |
| Go to Base | `GoToBase/` | Navigate to base types/members |
| Go to Implementation | `GoToImplementation/` | Navigate to implementations |

**Key Files:**
- `/src/Features/Core/Portable/GoToDefinition/AbstractGoToDefinitionService.cs`
- `/src/Features/Core/Portable/FindUsages/AbstractFindUsagesService.cs`

#### IntelliSense Features

| Feature | Location | Purpose |
|---------|----------|---------|
| Completion | `Completion/` | Code completion (IntelliSense) |
| Quick Info | `QuickInfo/` | Hover tooltips |
| Signature Help | `SignatureHelp/` | Parameter hints |
| Inline Hints | `InlineHints/` | Parameter name hints, type hints |

**Completion Providers:**
- `/src/Features/CSharp/Portable/Completion/CompletionProviders/` - 30+ providers

#### Refactoring Features

| Feature | Location | Purpose |
|---------|----------|---------|
| Extract Method | `ExtractMethod/` | Extract code into method |
| Extract Interface | `ExtractInterface/` | Extract interface from class |
| Inline Method | `InlineMethod/` | Inline method body |
| Move Type to File | `MoveToNamespace/` | Move type to namespace |
| Rename | (in Workspaces) | Rename symbols |

**Refactoring Files:**
- `/src/Features/Core/Portable/CodeRefactorings/` - Base infrastructure
- `/src/Features/CSharp/Portable/CodeRefactorings/` - 50+ C# refactorings

#### Code Fix Features

| Feature | Location | Purpose |
|---------|----------|---------|
| Code Fixes | `CodeFixes/` | Quick fixes for diagnostics |
| Fix All | `CodeFixes/FixAllOccurrences/` | Fix all instances |
| Suppression | `CodeFixes/Suppression/` | Suppress diagnostics |

**Code Fix Providers:**
- `/src/Features/CSharp/Portable/CodeFixes/` - 100+ C# code fixes

#### Formatting Features

| Feature | Location | Purpose |
|---------|----------|---------|
| Formatting | `Formatting/` | Code formatting |
| Indentation | `Indentation/` | Smart indentation |
| Brace Completion | `BraceCompletion/` | Auto-close braces |

**Key Files:**
- `/src/Features/Core/Portable/Formatting/FormattingCodeFixProvider.cs`
- `/src/Features/CSharp/Portable/Formatting/CSharpFormattingOptions.cs`

---

## Editor Features

**Location:** `/src/EditorFeatures/`

UI and editor integration for IDE features.

### Core Editor Features (`/src/EditorFeatures/Core/`)

#### Classification and Highlighting

| Feature | Location | Purpose |
|---------|----------|---------|
| Classification | `Classification/` | Syntax coloring |
| Brace Matching | `BraceMatching/` | Highlight matching braces |
| Keyword Highlighting | `KeywordHighlighting/` | Highlight keywords |
| Reference Highlighting | `ReferenceHighlighting/` | Highlight symbol references |

#### Interactive Features

| Feature | Location | Purpose |
|---------|----------|---------|
| IntelliSense | `IntelliSense/` | IntelliSense UI |
| Quick Info | `QuickInfo/` | Hover tooltip UI |
| Completion | `Completion/` | Completion UI |
| Signature Help | `SignatureHelp/` | Parameter hint UI |

#### Editing Features

| Feature | Location | Purpose |
|---------|----------|---------|
| Inline Rename | `InlineRename/` | Rename UI |
| Code Actions | `CodeActions/` | Light bulb menu |
| Inline Hints | `InlineHints/` | Inline parameter/type hints |
| Line Separators | `LineSeparators/` | Visual line separators |

#### Navigation Features

| Feature | Location | Purpose |
|---------|----------|---------|
| Go to Definition | `GoToDefinition/` | Navigation to definitions |
| Find Usages | `FindUsages/` | Find references UI |
| Navigate To | `NavigateTo/` | Symbol search UI |
| Navigation Bar | `NavigationBar/` | Dropdowns at top of editor |

#### Advanced Features

| Feature | Location | Purpose |
|---------|----------|---------|
| Edit and Continue | `EditAndContinue/` | Debugger edit support |
| CodeLens | `CodeLens/` | CodeLens references |
| Diagnostics | `Diagnostics/` | Error/warning display |
| Structure Guide Lines | `StructureGuideLines/` | Visual structure guides |

**Key Files:**
- `/src/EditorFeatures/Core/IntelliSense/AsyncCompletion/` - Async completion
- `/src/EditorFeatures/Core/CodeActions/CodeActionEditHandlerService.cs` - Code action execution

---

## Analyzers and Code Style

### Analyzers (`/src/Analyzers/`)

Built-in diagnostic analyzers and code fixes.

#### Structure

```
Analyzers/
├── Core/
│   ├── Analyzers/           # Language-agnostic analyzers
│   └── CodeFixes/           # Language-agnostic fixes
├── CSharp/
│   ├── Analyzers/           # C#-specific diagnostics
│   └── CodeFixes/           # C#-specific fixes
└── VisualBasic/
    ├── Analyzers/           # VB-specific diagnostics
    └── CodeFixes/           # VB-specific fixes
```

#### Common Analyzers

| Analyzer | Location | Purpose |
|----------|----------|---------|
| Naming | `Analyzers/Core/Analyzers/Naming/` | Naming conventions |
| Performance | `Analyzers/Core/Analyzers/Performance/` | Performance issues |
| Simplification | `Analyzers/Core/Analyzers/Simplification/` | Code simplification |

**C# Analyzers:**
- `/src/Analyzers/CSharp/Analyzers/` - 30+ C# diagnostic analyzers

### Code Style (`/src/CodeStyle/`)

Code style analyzers (IDE rules: IDE0001-IDExxxx).

#### Structure

```
CodeStyle/
├── Core/
│   ├── Analyzers/           # IDE style rules
│   └── CodeFixes/           # Style fixes
├── CSharp/
│   ├── Analyzers/           # C# style rules
│   └── CodeFixes/           # C# style fixes
└── VisualBasic/
    ├── Analyzers/           # VB style rules
    └── CodeFixes/           # VB style fixes
```

#### Common Style Rules

- Expression preferences (IDE0001-IDE0099)
- Pattern matching preferences
- `var` preferences
- Null checking preferences
- Code block preferences

**Key Files:**
- `/src/CodeStyle/Core/Analyzers/AbstractBuiltInCodeStyleDiagnosticAnalyzer.cs`
- `/src/CodeStyle/CSharp/Analyzers/` - C# style analyzers

---

## Language Server

**Location:** `/src/LanguageServer/`

Language Server Protocol (LSP) implementation for cross-editor support.

### Structure

```
LanguageServer/
├── Microsoft.CodeAnalysis.LanguageServer/  # Main LSP server
├── Protocol/                                # LSP protocol definitions
├── ProtocolUnitTests/                       # LSP tests
└── ExternalAccess/                          # External LSP APIs
```

### LSP Server (`/src/LanguageServer/Microsoft.CodeAnalysis.LanguageServer/`)

Implements LSP protocol for:
- **Diagnostics:** Error and warning publishing
- **Completion:** IntelliSense over LSP
- **Hover:** Quick info tooltips
- **Go to Definition:** Navigation
- **Find References:** Reference finding
- **Code Actions:** Refactorings and fixes
- **Document Symbols:** Outline view
- **Workspace Symbols:** Symbol search

**Key Files:**
- `/src/LanguageServer/Microsoft.CodeAnalysis.LanguageServer/LanguageServer.cs` - Main server
- `/src/LanguageServer/Protocol/Protocol/` - LSP message definitions

---

## Visual Studio Integration

**Location:** `/src/VisualStudio/`

Visual Studio-specific integration.

### Structure

```
VisualStudio/
├── Core/                  # VS integration core
├── CSharp/                # C# VS integration
├── VisualBasic/           # VB VS integration
├── CodeLens/              # CodeLens provider
├── Setup/                 # VSIX deployment
├── IntegrationTest/       # VS integration tests
└── LiveShare/             # Live Share support
```

### Key Components

| Component | Location | Purpose |
|-----------|----------|---------|
| Project System | `Core/Def/ProjectSystem/` | VS project system integration |
| Workspace | `Core/Impl/Workspace/` | VS workspace implementation |
| CodeLens | `CodeLens/` | CodeLens provider |
| Debugger | `Core/Impl/Debugging/` | Debugger integration |
| Options | `Core/Impl/Options/` | VS options pages |

**Key Files:**
- `/src/VisualStudio/Core/Def/ProjectSystem/VisualStudioWorkspaceImpl.cs`
- `/src/VisualStudio/Core/Impl/CodeModel/` - Code model for automation

---

## Scripting and Interactive

### Scripting (`/src/Scripting/`)

C# and VB scripting APIs.

```
Scripting/
├── Core/                  # Scripting infrastructure
├── CSharp/                # C# scripting
└── VisualBasic/           # VB scripting
```

**Key APIs:**
```csharp
// Location: /src/Scripting/CSharp/CSharpScript.cs
var result = await CSharpScript.EvaluateAsync("1 + 2");
var script = CSharpScript.Create("int x = 1;");
var state = await script.RunAsync();
```

### Interactive (`/src/Interactive/`)

REPL implementation.

```
Interactive/
├── Host/                  # Interactive host
├── csi/                   # C# Interactive (csi.exe)
└── vbi/                   # VB Interactive (vbi.exe)
```

**Interactive Window:**
- Submission-based evaluation
- Statement history
- Multi-line input

---

## Expression Evaluator

**Location:** `/src/ExpressionEvaluator/`

Debugger expression evaluation.

### Structure

```
ExpressionEvaluator/
├── Core/                  # EE infrastructure
├── CSharp/                # C# expression evaluator
├── VisualBasic/           # VB expression evaluator
└── Package/               # Debugger integration
```

### Functionality

- **Watch Window:** Evaluate expressions
- **Locals/Autos:** Inspect variables
- **Immediate Window:** Execute commands
- **Data Tips:** Hover inspection

**Key Files:**
- `/src/ExpressionEvaluator/CSharp/Source/ExpressionCompiler/` - C# EE compiler
- `/src/ExpressionEvaluator/Core/Source/ExpressionCompiler/` - EE infrastructure

---

## Tools and Utilities

**Location:** `/src/Tools/`

Developer tools and utilities.

### Key Tools

| Tool | Location | Purpose |
|------|----------|---------|
| dotnet-format | `Tools/dotnet-format/` | Code formatter CLI |
| Analyzer Runner | `Tools/AnalyzerRunner/` | Run analyzers on code |
| Build Validator | `Tools/BuildValidator/` | Validate builds |
| Benchmarks | `Tools/IdeBenchmarks/` | Performance benchmarking |

**Key Files:**
- `/src/Tools/dotnet-format/` - Code formatting tool
- `/src/Tools/AnalyzerRunner/` - Analyzer execution

---

## Quick Reference

### Common Paths

| Component | Path |
|-----------|------|
| C# Compiler | `/src/Compilers/CSharp/Portable/` |
| VB Compiler | `/src/Compilers/VisualBasic/Portable/` |
| Workspace API | `/src/Workspaces/Core/Portable/` |
| IDE Features | `/src/Features/Core/Portable/` |
| Editor Features | `/src/EditorFeatures/Core/` |
| Analyzers | `/src/Analyzers/` |
| Code Style | `/src/CodeStyle/` |
| LSP Server | `/src/LanguageServer/` |
| VS Integration | `/src/VisualStudio/` |
| Scripting | `/src/Scripting/` |
| REPL | `/src/Interactive/` |
| Debugger EE | `/src/ExpressionEvaluator/` |

---

**Next:** [Feature Location Guide](03-feature-location-guide.md) - Find specific features
