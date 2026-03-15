# Compilers: Product Overview

| | |
|---|---|
| **Last Updated** | January 29, 2026 |
| **Git SHA** | `771fe9b8443e955573725b4db6cc019685d8c2d4` |
| **Parent Doc** | [Main Overview](../main_overview.md) |

## The Story: Building a Code Analysis Tool

A developer at a software company needs to enforce coding standards across 500+ C# projects. The team has specific rules: no `public` fields, consistent naming conventions, and required XML documentation on public APIs.

**Before Roslyn**

Without Roslyn, options were limited:
- Regular expressions that break on edge cases
- Third-party parsers that don't match compiler behavior
- StyleCop with limited extensibility
- Manual code reviews that don't scale

The fundamental problem: understanding code at the level the compiler does—knowing that `var x = 5;` makes `x` an `int`, that `Foo()` resolves to a specific method overload, that `await` requires an async context—required reimplementing significant portions of the compiler.

**With Roslyn: Compiler as a Service**

Roslyn exposes the outputs of each compilation phase through APIs:

1. **Parse the code** — Get a syntax tree that represents every character of the source, including whitespace and comments:
   ```csharp
   var tree = CSharpSyntaxTree.ParseText(sourceCode);
   var root = tree.GetRoot();
   ```

2. **Understand the meaning** — Get semantic information about any syntax node:
   ```csharp
   var compilation = CSharpCompilation.Create("Analysis", new[] { tree });
   var model = compilation.GetSemanticModel(tree);
   var symbol = model.GetSymbolInfo(methodInvocation).Symbol;
   ```

3. **Write an analyzer** — Create diagnostics that run in the IDE and at build time:
   ```csharp
   public class NoPublicFieldsAnalyzer : DiagnosticAnalyzer
   {
       public override void Initialize(AnalysisContext context)
       {
           context.RegisterSymbolAction(AnalyzeField, SymbolKind.Field);
       }
   }
   ```

**Result**

The developer creates a NuGet package with custom analyzers. Every project references the package. Violations appear immediately in the IDE with squiggles and light bulb fixes. Build pipelines enforce the rules. New hires learn the coding standards through immediate feedback.

---

## Core Concepts

### Compilation

**What it is:** An immutable object representing everything needed to compile code—source files, references, and options.

**Why it matters:** Compilation is the entry point to semantic analysis. You ask a Compilation questions and it lazily computes answers.

**Key insight:** Compilations are immutable. To "change" a compilation, you create a new one:
```csharp
var newCompilation = compilation.AddSyntaxTrees(anotherTree);
```

### Syntax Trees

**What it is:** A full-fidelity representation of source code as a tree of nodes, tokens, and trivia.

**Why it matters:** Syntax trees preserve everything—whitespace, comments, formatting. This enables code transformations that don't destroy unrelated code.

**Key insight:** Syntax trees are immutable and can be incrementally updated:
```csharp
var newRoot = root.ReplaceNode(oldNode, newNode);
var newTree = tree.WithRootAndOptions(newRoot, tree.Options);
```

### Semantic Model

**What it is:** The API for asking semantic questions about syntax nodes.

**Why it matters:** This is how you go from "there's a method call here" to "this calls `List<int>.Add(int)`."

**Key APIs:**
- `GetSymbolInfo(node)` — What symbol does this refer to?
- `GetTypeInfo(node)` — What type is this expression?
- `GetOperation(node)` — Get the language-agnostic semantic representation

### Symbols

**What it is:** Representations of declared entities—types, methods, properties, fields, etc.

**Why it matters:** Symbols give you the compiler's understanding of your code's structure.

**Symbol hierarchy (`ISymbol` is the root):**
```
ISymbol (root interface)
├── INamespaceOrTypeSymbol
│   ├── INamespaceSymbol
│   └── ITypeSymbol
│       ├── INamedTypeSymbol (class, struct, interface, enum, delegate)
│       ├── IArrayTypeSymbol
│       ├── IPointerTypeSymbol
│       └── ITypeParameterSymbol
├── IMethodSymbol
├── IPropertySymbol
├── IFieldSymbol
├── IEventSymbol
├── IParameterSymbol
├── ILocalSymbol
└── IAliasSymbol
```

### IOperation (Semantic AST)

**What it is:** A language-agnostic semantic representation of code operations. This is commonly referred to as an "Abstract Syntax Tree" (AST) in compiler literature, though in Roslyn it represents semantic operations rather than raw syntax.

**Why it matters:** Write analyzers once that work for both C# and VB. IOperation abstracts away language-specific syntax while preserving semantic meaning.

**Example:** Both C#'s `foreach` and VB's `For Each` become `IForEachLoopOperation`.

---

## Compilation Pipeline

The compilation pipeline transforms source text into executable code:

```
Source Text (.cs / .vb files)
         │
         ▼
┌─────────────────┐
│  Lexer/Scanner  │  → Tokens (keywords, identifiers, literals)
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│     Parser      │  → Syntax Tree (grammatical structure)
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│     Binder      │  → Bound Tree (types, symbols resolved)
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│    Lowering     │  → Simplified Bound Tree (async, iterators)
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│     Emitter     │  → IL Code → PE/PDB files
└─────────────────┘
```

### Phase Details

| Phase | Input | Output | Key Classes |
|-------|-------|--------|-------------|
| Lexing | Text | Tokens | `Lexer` (C#), `Scanner` (VB) |
| Parsing | Tokens | SyntaxTree | `LanguageParser`, `Parser` |
| Binding | SyntaxTree | BoundTree | `Binder` hierarchy |
| Lowering | BoundTree | BoundTree | `LocalRewriter`, `AsyncRewriter` |
| Emit | BoundTree | PE/PDB | `ILBuilder`, `PEWriter` |

---

## Key Features

| Feature | Description | When to Use |
|---------|-------------|-------------|
| **Syntax APIs** | Parse and manipulate syntax trees | Code generation, formatting tools |
| **Semantic APIs** | Query types, symbols, operations | Analyzers, refactoring tools |
| **Emit APIs** | Generate assemblies programmatically | Compilers, code generators |
| **Scripting APIs** | Execute C# code at runtime | REPL, scripting hosts |
| **Source Generators** | Generate source at compile time | Boilerplate reduction |

---

## Architecture at a Glance

```
┌─────────────────────────────────────────────────────────────┐
│                     Public API Surface                      │
│ Compilation, SyntaxTree, SemanticModel, ISymbol, IOperation │
└────────────────────────────┬────────────────────────────────┘
                             │
           ┌─────────────────┼─────────────────┐
           │                 │                 │
      ┌────▼────┐      ┌─────▼─────┐     ┌─────▼─────┐
      │   C#    │      │   Core    │     │    VB     │
      │Compiler │      │  Shared   │     │ Compiler  │
      └─────────┘      └───────────┘     └───────────┘
```

For detailed architecture, see [Codebase Overview](./codebase_overview.md).

---

## Common Use Cases

### Analyzing Code

**Scenario:** Find all calls to a deprecated method

**Solution:**
```csharp
var compilation = CreateCompilation(code);
var deprecatedMethod = compilation.GetTypeByMetadataName("MyClass")
    .GetMembers("OldMethod").First();

foreach (var tree in compilation.SyntaxTrees)
{
    var model = compilation.GetSemanticModel(tree);
    var invocations = tree.GetRoot().DescendantNodes()
        .OfType<InvocationExpressionSyntax>();
    
    foreach (var invocation in invocations)
    {
        var symbol = model.GetSymbolInfo(invocation).Symbol;
        if (SymbolEqualityComparer.Default.Equals(symbol, deprecatedMethod))
        {
            // Found a call to the deprecated method
        }
    }
}
```

### Transforming Code

**Scenario:** Add `readonly` modifier to all applicable fields

**Solution:**
```csharp
var rewriter = new AddReadonlyRewriter(semanticModel);
var newRoot = rewriter.Visit(root);
```

### Generating Code

**Scenario:** Create a new class programmatically

**Solution:**
```csharp
var classDeclaration = SyntaxFactory.ClassDeclaration("GeneratedClass")
    .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
    .AddMembers(
        SyntaxFactory.PropertyDeclaration(
            SyntaxFactory.ParseTypeName("string"),
            "Name")
        .AddAccessorListAccessors(
            SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
            SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))));
```

---

## What's NOT Covered Here

- **Workspaces** — Solution/project management; see [Workspaces Overview](../workspaces/product_overview.md)
- **IDE Features** — Code completion, refactoring; see [Features Overview](../features/product_overview.md)
- **Analyzers** — Writing diagnostic analyzers; see [Analyzers Overview](../analyzers/product_overview.md)

---

## Related Documentation

**In This Overview:**
- [Codebase Overview](./codebase_overview.md) — Technical architecture and components
- [Main Overview](../main_overview.md) — Full codebase map
- [Glossary](../glossary.md) — Terminology

**Existing Codebase Docs:**
- [Roslyn Overview](../../wiki/Roslyn-Overview.md) — Official architecture deep-dive
- [Getting Started C# Syntax Analysis](../../wiki/Getting-Started-C%23-Syntax-Analysis.md)
- [Getting Started C# Semantic Analysis](../../wiki/Getting-Started-C%23-Semantic-Analysis.md)
- [Official Roslyn APIs](https://docs.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/)

---

## Documentation Scope

This document explains why the Compilers area exists and what problems it solves. It provides context for understanding the compiler's purpose but does not cover implementation details.

**What's covered:** Compiler-as-a-service concept, core abstractions, pipeline overview

**What's not covered:** Implementation details, all APIs, optimization strategies

**To go deeper:** See [Codebase Overview](./codebase_overview.md) for architecture. For more detail, start a new AI session using the [Expanding Documentation Prompt](https://github.com/CyrusNajmabadi/codebase-explorer/blob/main/LOADER.md#expanding-documentation-prompt).

**Parent document:** [Main Overview](../main_overview.md)
