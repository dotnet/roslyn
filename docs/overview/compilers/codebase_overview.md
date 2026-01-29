# Compilers: Codebase Overview

| | |
|---|---|
| **Last Updated** | January 29, 2026 |
| **Git SHA** | `771fe9b8443e955573725b4db6cc019685d8c2d4` |
| **Parent Doc** | [Main Overview](../main_overview.md) |

For product context, see [product_overview.md](./product_overview.md). See [../glossary.md](../glossary.md) for terms.

---

## Architecture Overview

```
┌───────────────────────────────────────────────────────────────┐
│                    Compiler Entry Points                      │
│      csc.exe / vbc.exe → CommonCompiler → BuildClient         │
└─────────────────────────────┬─────────────────────────────────┘
                              │
                              ▼
┌───────────────────────────────────────────────────────────────┐
│                    Compilation Pipeline                       │
│                                                               │
│  SourceText → Lexer → Tokens → Parser → SyntaxTree            │
│                                   │                           │
│                                   ▼                           │
│        SyntaxTree → Binder → BoundTree → Lowering → Emitter   │
│                         │                                     │
│                         ▼                                     │
│      Compilation.GetSemanticModel() → SemanticModel (API)     │
└───────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌───────────────────────────────────────────────────────────────┐
│                          Output                               │
│                 PE File (.dll/.exe) + PDB                     │
└───────────────────────────────────────────────────────────────┘
```

The compiler is organized into three main areas that share code:
- **Core** (`src/Compilers/Core/`) — Shared infrastructure
- **CSharp** (`src/Compilers/CSharp/`) — C# compiler
- **VisualBasic** (`src/Compilers/VisualBasic/`) — VB compiler

---

## Core Components

### Core/Portable (`src/Compilers/Core/Portable/`)

**What it is:** Shared abstractions and infrastructure used by both C# and VB compilers.

**Key responsibilities:**
- Define base `Compilation` class
- Define `SyntaxTree`, `SemanticModel`, `ISymbol` interfaces
- Implement emit infrastructure (PE/PDB writing)
- Implement `IOperation` semantic operations
- Source generator infrastructure

**Key files/classes:**
- `Compilation/Compilation.cs` — Base compilation class
- `Syntax/SyntaxTree.cs` — Syntax tree abstraction
- `Semantics/SemanticModel.cs` — Semantic model base
- `Symbols/ISymbol.cs` — Symbol interface hierarchy
- `Operations/IOperation.cs` — Operation interfaces
- `Emit/CommonPEModuleBuilder.cs` — PE generation

### CSharp Compiler (`src/Compilers/CSharp/Portable/`)

**What it is:** The C# compiler implementation.

**Key responsibilities:**
- Lexing C# source text
- Parsing C# syntax
- Binding C# semantics
- Lowering C#-specific constructs
- C# code generation

**Key files/classes:**
- `Parser/Lexer.cs` — Tokenizes C# source
- `Parser/LanguageParser.cs` — Parses tokens into syntax tree
- `Binder/Binder.cs` — Semantic binding (name resolution, type checking)
- `BoundTree/BoundNode.cs` — Semantic tree nodes
- `Lowering/LocalRewriter.cs` — Transforms high-level constructs
- `CodeGen/CodeGenerator.cs` — Emits IL

### VisualBasic Compiler (`src/Compilers/VisualBasic/Portable/`)

**What it is:** The Visual Basic compiler implementation.

**Key responsibilities:**
- Same as C#, but for VB syntax and semantics
- VB uses "Scanner" instead of "Lexer" (same concept)

**Key files/classes:**
- `Scanner/Scanner.vb` — Tokenizes VB source
- `Parser/Parser.vb` — Parses tokens into syntax tree
- `Binding/Binder.vb` — Semantic binding
- `BoundTree/BoundNode.vb` — Semantic tree nodes
- `Lowering/LocalRewriter.vb` — Transforms high-level constructs

### Compiler Server (`src/Compilers/Server/`)

**What it is:** VBCSCompiler daemon that keeps compilers warm for faster builds.

**Key responsibilities:**
- Listen for compilation requests via named pipes
- Reuse compiler instances across builds
- Manage process lifecycle (timeout, shutdown)

**Key files/classes:**
- `VBCSCompiler/VBCSCompiler.cs` — Server entry point
- `ServerCore/BuildServerController.cs` — Manages server lifecycle
- `ServerCore/ClientConnectionHandler.cs` — Handles client connections

---

## Component Interactions

### Compilation Flow

```
┌─────────────┐    ┌─────────────┐    ┌─────────────┐
│ Source Text │ ─▶ │    Lexer    │ ─▶ │   Tokens    │
└─────────────┘    └─────────────┘    └──────┬──────┘
                                             │
                                             ▼
                   ┌─────────────┐    ┌─────────────┐
                   │ Syntax Tree │ ◀─ │   Parser    │
                   └──────┬──────┘    └─────────────┘
                          │
     ┌────────────────────┼────────────────────┐
     │                    │                    │
     ▼                    ▼                    ▼
┌─────────────┐    ┌─────────────┐    ┌─────────────┐
│ Declarations│    │SemanticModel│    │  BoundTree  │
│    Table    │    │    API      │    │ (Internal)  │
└─────────────┘    └─────────────┘    └──────┬──────┘
                                             │
                                             ▼
                                      ┌─────────────┐
                                      │  Lowering   │
                                      └──────┬──────┘
                                             │
                                             ▼
                                      ┌─────────────┐
                                      │   Emitter   │
                                      └──────┬──────┘
                                             │
                                             ▼
                                      ┌─────────────┐
                                      │   PE/PDB    │
                                      └─────────────┘
```

### Layered API Access

**Public APIs (for tool authors):**
```
SyntaxTree.ParseText() → SyntaxTree → SyntaxNode hierarchy
Compilation.Create()   → Compilation → SemanticModel, Symbols
SemanticModel.GetSymbolInfo() → Symbol information
SemanticModel.GetOperation()  → IOperation tree
```

**Internal APIs (for compiler implementation):**
```
Binder.BindExpression() → BoundExpression
LocalRewriter.Rewrite()  → Lowered BoundTree
CodeGenerator.Generate() → IL bytes
```

---

## Data Model

### Key Entities

| Entity | Description | Immutable? |
|--------|-------------|------------|
| `Compilation` | All inputs for compilation | Yes |
| `SyntaxTree` | Parsed source file | Yes |
| `SyntaxNode` | Node in syntax tree | Yes |
| `SyntaxToken` | Terminal (keyword, identifier) | Yes |
| `SyntaxTrivia` | Whitespace, comments | Yes |
| `SemanticModel` | Semantic query API | Yes |
| `ISymbol` | Declared entity | Yes |
| `BoundNode` | Semantic tree node (internal) | Yes |

### Immutability Pattern

All compiler data structures are immutable. "Changes" produce new instances:

```csharp
// Syntax tree modification
var newNode = oldNode.WithIdentifier(newIdentifier);
var newRoot = oldRoot.ReplaceNode(oldNode, newNode);
var newTree = oldTree.WithRootAndOptions(newRoot, oldTree.Options);

// Compilation modification
var newCompilation = oldCompilation.AddSyntaxTrees(newTree);
```

---

## Technology Stack

| Category | Technology | Purpose |
|----------|------------|---------|
| Language | C# | Implementation |
| Collections | `ImmutableArray<T>` | Thread-safe, immutable collections |
| Output | PE/COFF | Executable format |
| Debugging | Portable PDB | Cross-platform debug symbols |
| IPC | Named Pipes | Compiler server communication |

---

## Design Patterns

### Visitor Pattern

Used extensively for tree traversal:

```csharp
// Syntax visitor
public class MySyntaxWalker : CSharpSyntaxWalker
{
    public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        // Process method
        base.VisitMethodDeclaration(node);
    }
}

// Symbol visitor
public class MySymbolVisitor : SymbolVisitor
{
    public override void VisitNamedType(INamedTypeSymbol symbol)
    {
        // Process type
    }
}

// Operation visitor
public class MyOperationWalker : OperationWalker
{
    public override void VisitInvocation(IInvocationOperation operation)
    {
        // Process invocation
    }
}
```

### Lazy Evaluation

Compilations compute information on demand:

```csharp
// These don't do work until you ask questions
var compilation = CSharpCompilation.Create("test", syntaxTrees);
var semanticModel = compilation.GetSemanticModel(tree);

// This triggers binding for just this node
var symbolInfo = semanticModel.GetSymbolInfo(node);
```

### Factory Pattern

Syntax nodes are created through factories:

```csharp
// SyntaxFactory for C#
var classDecl = SyntaxFactory.ClassDeclaration("MyClass")
    .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword));

// Compilation factories
var compilation = CSharpCompilation.Create(
    "assemblyName",
    syntaxTrees,
    references,
    options);
```

### Builder Pattern

Internal use of builders for performance:

```csharp
// ArrayBuilder<T> for building immutable arrays
var builder = ArrayBuilder<Symbol>.GetInstance();
builder.Add(symbol1);
builder.Add(symbol2);
var immutableArray = builder.ToImmutableAndFree();
```

---

## Configuration

### Compilation Options

| Option | Purpose |
|--------|---------|
| `OutputKind` | DLL, EXE, etc. |
| `OptimizationLevel` | Debug/Release |
| `NullableContextOptions` | Nullable reference type settings |
| `AllowUnsafe` | Enable unsafe code |
| `WarningLevel` | Warning verbosity |

### Parse Options

| Option | Purpose |
|--------|---------|
| `LanguageVersion` | C#/VB version |
| `PreprocessorSymbols` | #define symbols |
| `DocumentationMode` | XML doc handling |

---

## Internal Names

- **Green Nodes** — Internal immutable syntax nodes (memory-efficient)
- **Red Nodes** — Public syntax nodes with parent references
- **BoundTree** — Semantic tree after binding
- **Lowered BoundTree** — After high-level construct transformation
- **Spill Sequence** — Temporary variables from lowering
- **State Machine** — Generated for async/iterators

See also: [../glossary.md](../glossary.md)

---

## Important Links

**External:**
- [Roslyn Architecture](https://docs.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/compiler-api-model)
- [C# Language Design](https://github.com/dotnet/csharplang)
- [ECMA C# Spec](https://www.ecma-international.org/publications-and-standards/standards/ecma-334/)

**Internal Code:**
- `src/Compilers/Core/Portable/` — Shared infrastructure
- `src/Compilers/CSharp/Portable/` — C# compiler
- `src/Compilers/VisualBasic/Portable/` — VB compiler
- `src/Compilers/Server/` — Compiler server

**Related Docs:**
- [Product Overview](./product_overview.md)
- [Glossary](../glossary.md)
- [Main Overview](../main_overview.md)

**Existing Codebase Docs:**
- [Roslyn Overview](../../wiki/Roslyn-Overview.md) — Official architecture deep-dive
- [Getting Started C# Syntax Analysis](../../wiki/Getting-Started-C%23-Syntax-Analysis.md)
- [Getting Started C# Semantic Analysis](../../wiki/Getting-Started-C%23-Semantic-Analysis.md)

---

## Documentation Scope

This document provides a high-level architectural overview of the Compilers area. It covers major components and their interactions but does not detail internal implementation of each component.

**What's covered:** Architecture, component responsibilities, key patterns, technology choices

**What's not covered:** Detailed implementation, all APIs, performance tuning

**To go deeper:** Start a new AI session using the [Expanding Documentation Prompt](https://github.com/CyrusNajmabadi/codebase-explorer/blob/main/LOADER.md#expanding-documentation-prompt), asking to drill into specific components (e.g., "the Binder", "lowering passes", "emit pipeline").

**Parent document:** [Main Overview](../main_overview.md)
