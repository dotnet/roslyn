# Architecture and Design Patterns

Understanding Roslyn's architecture, design patterns, and key concepts.

---

## Table of Contents

- [Overall Architecture](#overall-architecture)
- [Compiler Architecture](#compiler-architecture)
- [Workspace Architecture](#workspace-architecture)
- [Service Architecture](#service-architecture)
- [Common Design Patterns](#common-design-patterns)
- [Key Data Structures](#key-data-structures)
- [Performance Considerations](#performance-considerations)
- [Threading Model](#threading-model)

---

## Overall Architecture

Roslyn uses a layered architecture with clear separation of concerns:

```
┌─────────────────────────────────────────────────────────┐
│              Visual Studio Integration                  │
│         (/src/VisualStudio/, /src/EditorFeatures/)     │
├─────────────────────────────────────────────────────────┤
│              Language Server Protocol                   │
│                (/src/LanguageServer/)                   │
├─────────────────────────────────────────────────────────┤
│           IDE Features & Refactorings                   │
│      (/src/Features/, /src/EditorFeatures/)            │
├─────────────────────────────────────────────────────────┤
│             Workspace API (MEF Services)                │
│              (/src/Workspaces/)                         │
├─────────────────────────────────────────────────────────┤
│          Compiler API (Core Services)                   │
│              (/src/Compilers/)                          │
└─────────────────────────────────────────────────────────┘
```

### Layer Responsibilities

**Compiler Layer** - Pure compilation functionality:
- Syntax parsing
- Semantic analysis
- IL emission
- No dependencies on IDE or workspace

**Workspace Layer** - Solution/project management:
- Solution/project/document model
- Persistence
- Change tracking
- Services abstraction

**Features Layer** - IDE algorithms:
- Refactorings
- Code fixes
- Formatting
- Navigation
- Language-agnostic implementations

**Editor Features Layer** - Editor integration:
- UI components
- Text buffer integration
- Command handlers
- Visual Studio or LSP integration

---

## Compiler Architecture

### Compiler Pipeline

```
Source Code (string)
    ↓
┌──────────────────────┐
│   Lexical Analysis   │  → Tokens
│    (Lexer/Scanner)   │
└──────────────────────┘
    ↓
┌──────────────────────┐
│   Syntax Analysis    │  → Syntax Tree (Green/Red nodes)
│      (Parser)        │
└──────────────────────┘
    ↓
┌──────────────────────┐
│  Semantic Analysis   │  → Symbols + Bound Tree
│      (Binder)        │
└──────────────────────┘
    ↓
┌──────────────────────┐
│      Lowering        │  → Lowered Bound Tree
│   (LocalRewriter)    │
└──────────────────────┘
    ↓
┌──────────────────────┐
│    IL Emission       │  → Assembly (.dll/.exe)
│     (Emitter)        │
└──────────────────────┘
```

### Syntax Trees: Red-Green Tree Architecture

Roslyn uses a unique "red-green" tree structure for optimal memory and performance.

#### Green Nodes (Immutable, Cached)
**Location:** `/src/Compilers/Core/Portable/Syntax/GreenNode.cs`

- **Immutable** - Never change after creation
- **Shared** - Same node used in multiple trees
- **No parent pointers** - Reduces memory
- **Cacheable** - Identical nodes share memory
- **Position-independent** - Can be reused anywhere

```csharp
// Green nodes are implementation details
// Created by parser, cached and reused
```

#### Red Nodes (Mutable facade)
**Location:** `/src/Compilers/Core/Portable/Syntax/SyntaxNode.cs`

- **Mutable facade** over green nodes
- **Has parent pointers** - Navigation
- **Has position** - Absolute position in file
- **Provides API** - Public API developers use
- **Created on-demand** - Lazy materialization

```csharp
// Public API - Red nodes
SyntaxNode root = tree.GetRoot();
SyntaxNode parent = node.Parent;
int position = node.SpanStart;
```

#### Benefits

1. **Memory efficiency** - Green nodes shared across versions
2. **Incremental parsing** - Reuse unchanged green nodes
3. **Fast equality** - Reference equality for green nodes
4. **Thread safety** - Immutable green nodes

**Key Files:**
- `/src/Compilers/Core/Portable/Syntax/GreenNode.cs`
- `/src/Compilers/Core/Portable/Syntax/SyntaxNode.cs`
- `/src/Compilers/Core/Portable/Syntax/SyntaxTree.cs`

### Symbols

**Location:** `/src/Compilers/Core/Portable/Symbols/`

Symbols represent semantic elements (types, methods, properties, etc.).

#### Symbol Hierarchy

```
ISymbol (interface)
├── INamespaceSymbol
├── ITypeSymbol
│   ├── INamedTypeSymbol (classes, structs, interfaces)
│   ├── IArrayTypeSymbol
│   ├── IPointerTypeSymbol
│   └── ITypeParameterSymbol
├── IMethodSymbol
├── IPropertySymbol
├── IFieldSymbol
├── IEventSymbol
├── IParameterSymbol
└── ILocalSymbol
```

#### Symbol Sources

1. **Source Symbols** - Defined in source code
   - `/src/Compilers/CSharp/Portable/Symbols/Source/`
   - `SourceNamedTypeSymbol.cs`, `SourceMethodSymbol.cs`

2. **Metadata Symbols** - From referenced assemblies
   - `/src/Compilers/Core/Portable/Symbols/Metadata/`
   - `PENamedTypeSymbol.cs`, `PEMethodSymbol.cs`

3. **Synthesized Symbols** - Generated by compiler
   - `/src/Compilers/CSharp/Portable/Symbols/Synthesized/`
   - Display classes, iterator classes, async state machines

### Binding

**Location:** `/src/Compilers/CSharp/Portable/Binder/`

Binding connects syntax to symbols and performs semantic analysis.

#### Binder Responsibilities

1. **Name lookup** - Resolve names to symbols
2. **Type checking** - Verify type compatibility
3. **Overload resolution** - Select correct method overload
4. **Conversion checking** - Implicit/explicit conversions
5. **Diagnostic generation** - Semantic errors

#### Binder Chain

Binders form a chain for lexical scoping:

```
FileScope (usings, namespace)
    ↓
NamespaceScope
    ↓
TypeScope (class members)
    ↓
MethodScope (parameters, locals)
    ↓
BlockScope (block-scoped variables)
```

**Key Files:**
- `/src/Compilers/CSharp/Portable/Binder/Binder.cs` - Base binder
- `/src/Compilers/CSharp/Portable/Binder/BinderFactory.cs` - Creates binders
- `/src/Compilers/CSharp/Portable/Binder/Binder_Expressions.cs` - Expression binding
- `/src/Compilers/CSharp/Portable/Binder/Binder_Statements.cs` - Statement binding

### Bound Tree

**Location:** `/src/Compilers/CSharp/Portable/BoundTree/`

Intermediate representation between syntax and IL.

#### Bound Node Types

- `BoundExpression` - Expressions with type information
- `BoundStatement` - Statements
- `BoundPattern` - Pattern matching
- `BoundLocal` - Local variable reference
- `BoundCall` - Method call with resolved method

**Definition:** `/src/Compilers/CSharp/Portable/BoundTree/BoundNodes.xml`
- Code-generated from XML

### Lowering (Desugaring)

**Location:** `/src/Compilers/CSharp/Portable/Lowering/`

Transforms high-level constructs to simpler IL-ready forms.

#### LocalRewriter

**Main file:** `/src/Compilers/CSharp/Portable/Lowering/LocalRewriter.cs`

Transforms:
- `async`/`await` → State machine
- `foreach` → `while` loop with enumerator
- `using` → `try`/`finally`
- Properties → Get/set methods
- Events → Add/remove methods
- Query expressions → Method calls
- Pattern matching → If/switch logic

#### Async Lowering

**Location:** `/src/Compilers/CSharp/Portable/Lowering/AsyncRewriter/`

Converts async methods to state machines:
- Creates state machine struct
- Hoists locals to fields
- Generates `MoveNext()` method
- Creates resumption points

### IL Emission

**Location:** `/src/Compilers/Core/Portable/Emit/`

Generates IL opcodes and metadata.

**Key Files:**
- `/src/Compilers/Core/Portable/Emit/PEBuilder.cs` - Builds PE file
- `/src/Compilers/CSharp/Portable/CodeGen/ILBuilder.cs` - IL generation
- `/src/Compilers/CSharp/Portable/Emitter/Model/PEModuleBuilder.cs` - Module building

---

## Workspace Architecture

**Location:** `/src/Workspaces/Core/Portable/`

### Core Workspace Model

```
Workspace
├── Solution (immutable)
│   ├── SolutionId
│   ├── Projects (ImmutableArray<Project>)
│   └── Metadata
├── Project (immutable)
│   ├── ProjectId
│   ├── Documents (ImmutableArray<Document>)
│   ├── Compilation
│   └── References
└── Document (immutable)
    ├── DocumentId
    ├── Text (SourceText)
    ├── SyntaxTree
    └── SemanticModel
```

**Key Classes:**
- `/src/Workspaces/Core/Portable/Workspace/Workspace.cs`
- `/src/Workspaces/Core/Portable/Workspace/Solution.cs`
- `/src/Workspaces/Core/Portable/Workspace/Project.cs`
- `/src/Workspaces/Core/Portable/Workspace/Document.cs`

### Immutability

All workspace objects are **immutable**:
- Changes create new instances
- Uses structural sharing for efficiency
- Thread-safe by default

```csharp
// Immutable update pattern
Solution oldSolution = workspace.CurrentSolution;
Document document = oldSolution.GetDocument(docId);
Document newDocument = document.WithText(newText);
Solution newSolution = oldSolution.WithDocumentText(docId, newText);
```

### Change Tracking

Workspace tracks changes through versions:
- `VersionStamp` - Tracks document/project changes
- Incremental updates
- Minimal recompilation

**Location:** `/src/Workspaces/Core/Portable/Workspace/VersionStamp.cs`

---

## Service Architecture

Roslyn uses **MEF (Managed Extensibility Framework)** for service composition.

### Language Services

**Interface:** `/src/Workspaces/Core/Portable/LanguageServices/ILanguageService.cs`

Every language feature is a service:
- `ISyntaxFactsService` - Syntax queries
- `ISemanticFactsService` - Semantic queries
- `IFormattingService` - Formatting
- `IRenameRewriterLanguageService` - Rename support

**Registration:**
```csharp
[ExportLanguageService(typeof(IMyService), LanguageNames.CSharp)]
internal class CSharpMyService : IMyService
{
    // Implementation
}
```

### Workspace Services

**Interface:** `/src/Workspaces/Core/Portable/Workspace/Host/IWorkspaceService.cs`

Workspace-level services:
- `IMetadataService` - Metadata loading
- `IPersistentStorageService` - Caching
- `IDocumentationProviderService` - XML docs

### Host Services

**Interface:** `/src/Workspaces/Core/Portable/Host/HostServices.cs`

Host-specific services:
- Visual Studio services
- LSP services
- MSBuild services

### Service Discovery

```csharp
// Get language service
var service = document.GetLanguageService<ISyntaxFactsService>();

// Get workspace service
var service = workspace.Services.GetService<IMetadataService>();
```

---

## Common Design Patterns

### 1. Visitor Pattern

**Used for:** Traversing syntax trees and bound trees

```csharp
// Location: /src/Compilers/Core/Portable/Syntax/SyntaxWalker.cs
public class MySyntaxWalker : CSharpSyntaxWalker
{
    public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        // Process method
        base.VisitMethodDeclaration(node);
    }
}
```

**Visitors:**
- `CSharpSyntaxWalker` - Walks syntax tree
- `CSharpSyntaxRewriter` - Rewrites syntax tree
- `BoundTreeWalker` - Walks bound tree
- `OperationWalker` - Walks operation tree

### 2. Factory Pattern

**Used for:** Creating syntax nodes, symbols, and services

```csharp
// Syntax factory
// Location: /src/Compilers/CSharp/Portable/Syntax/SyntaxFactory.cs
var method = SyntaxFactory.MethodDeclaration(
    returnType, identifier);

// Symbol factory
// Location: /src/Compilers/CSharp/Portable/Symbols/
var symbol = SourceMethodSymbol.Create(...);
```

### 3. Builder Pattern

**Used for:** Compilation options, formatters

```csharp
// Location: /src/Compilers/CSharp/Portable/Compilation/CSharpCompilationOptions.cs
var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
    .WithOptimizationLevel(OptimizationLevel.Release)
    .WithNullableContextOptions(NullableContextOptions.Enable);
```

### 4. Strategy Pattern

**Used for:** Formatting rules, completion providers

```csharp
// Formatting rules
// Location: /src/Workspaces/CSharp/Portable/Formatting/
interface IFormattingRule
{
    void AddIndentBlockOperations(operations);
    void AddSuppressOperations(operations);
}
```

### 5. Object Pool Pattern

**Used for:** Memory efficiency, reducing allocations

```csharp
// Location: /src/Dependencies/PooledObjects/
using var pooled = ArrayBuilder<T>.GetInstance();
var array = pooled.ToImmutableAndFree();
```

**Common pooled objects:**
- `ArrayBuilder<T>` - Pooled list
- `PooledStringBuilder` - Pooled StringBuilder
- `ObjectPool<T>` - Generic pool

---

## Key Data Structures

### ImmutableArray<T>

**Used everywhere** for collections.

```csharp
// Location: System.Collections.Immutable
ImmutableArray<Symbol> members = type.GetMembers();
```

**Benefits:**
- Thread-safe
- Efficient structural sharing
- Value equality

### PooledHashSet<T> / PooledDictionary<K,V>

**Location:** `/src/Dependencies/PooledObjects/`

Reduces allocations in hot paths.

### SyntaxList<T>, SeparatedSyntaxList<T>

**Location:** `/src/Compilers/Core/Portable/Syntax/SyntaxList.cs`

Specialized collections for syntax nodes:
- `SyntaxList<T>` - List of nodes
- `SeparatedSyntaxList<T>` - Nodes with separators (e.g., parameters)

### TextSpan, TextLine

**Location:** `/src/Compilers/Core/Portable/Text/TextSpan.cs`

Represents positions and ranges in source files.

```csharp
TextSpan span = node.Span;  // Start and length
int start = span.Start;
int end = span.End;
```

---

## Performance Considerations

### 1. Incremental Parsing

**Location:** `/src/Compilers/Core/Portable/Syntax/InternalSyntax/SyntaxParser.cs`

- Reuses green nodes from previous parse
- Only reparses changed portions
- Significant perf win for large files

### 2. Lazy Compilation

Compilations are lazy:
- Symbols created on-demand
- Binding happens lazily
- Metadata loaded when needed

### 3. Caching

**Location:** `/src/Workspaces/Core/Portable/Storage/`

- Persistent storage for symbol data
- SQLite database backend
- Reduces recomputation

### 4. Object Pooling

**Location:** `/src/Dependencies/PooledObjects/`

Reduces GC pressure:
- Pool builders and collections
- Reuse instead of allocate

### 5. Structural Sharing

Immutable data structures share unchanged parts:
- Solutions share unchanged projects
- Projects share unchanged documents
- Syntax trees share unchanged nodes

---

## Threading Model

### Compilation Threading

**Compilers are mostly single-threaded:**
- Parsing: Single-threaded per file
- Binding: Single-threaded
- Emit: Some parallelism

### Workspace Threading

**Workspace updates:**
- Immutable data structures → thread-safe reads
- Writes require serialization
- Events marshaled to UI thread (in VS)

### Analyzer Execution

**Location:** `/src/Compilers/Core/Portable/DiagnosticAnalyzer/AnalyzerDriver.cs`

Analyzers run in parallel:
- Thread pool execution
- Isolated per-analyzer state
- Exception isolation

### Background Analysis

**Location:** `/src/Workspaces/Core/Portable/Diagnostics/`

IDE runs analysis in background:
- Solution crawler
- Prioritizes visible documents
- Throttles to avoid UI lag

---

## Architectural Principles

### 1. Immutability by Default

All core data structures are immutable:
- Syntax trees
- Symbols
- Compilations
- Workspace objects

### 2. Separation of Concerns

Clear layering:
- Compiler has no IDE dependencies
- Workspaces have no editor dependencies
- Features are language-agnostic when possible

### 3. Testability

Everything is testable:
- Minimal static state
- Dependency injection via MEF
- Comprehensive test utilities

### 4. API Surface Design

Public APIs are carefully designed:
- Immutable interfaces
- Extension methods for convenience
- Clear naming conventions

### 5. Performance

Performance is a feature:
- Incremental parsing
- Lazy evaluation
- Object pooling
- Caching

---

## Reference Documentation

**Design Documents:** `/docs/compilers/Design/`
- Parser design
- Bound tree design
- Closure conversion
- Sequence points

**Feature Specs:** `/docs/features/`
- Nullable reference types
- Source generators
- Records
- Pattern matching

**API Guidelines:** `/docs/contributing/API Review Process.md`

---

**Next:** [Developer Guide](05-developer-guide.md) - Practical development tasks
