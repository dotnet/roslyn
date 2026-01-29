# Analyzers: Codebase Overview

**Last Updated:** January 29, 2026

For product context, see [product_overview.md](./product_overview.md). See [../glossary.md](../glossary.md) for terms.

---

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────────────────┐
│                           Analyzers                                     │
│                                                                          │
│  src/Analyzers/                                                          │
│  ├── Core/                  # Language-agnostic base classes             │
│  │   ├── Analyzers/         # Abstract analyzer implementations          │
│  │   └── CodeFixes/         # Abstract code fix implementations          │
│  │                                                                       │
│  ├── CSharp/                # C# specific                                │
│  │   ├── Analyzers/         # C# analyzer implementations                │
│  │   ├── CodeFixes/         # C# code fix implementations                │
│  │   └── Tests/             # C# analyzer tests                          │
│  │                                                                       │
│  └── VisualBasic/           # VB specific                                │
│      ├── Analyzers/         # VB analyzer implementations                │
│      ├── CodeFixes/         # VB code fix implementations                │
│      └── Tests/             # VB analyzer tests                          │
│                                                                          │
└─────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────┐
│                        RoslynAnalyzers                                  │
│                                                                          │
│  src/RoslynAnalyzers/                                                    │
│  ├── Microsoft.CodeAnalysis.Analyzers/     # Analyzer author rules       │
│  ├── Roslyn.Diagnostics.Analyzers/         # Roslyn-specific rules       │
│  ├── Microsoft.CodeAnalysis.BannedApiAnalyzers/                         │
│  ├── PublicApiAnalyzers/                   # API surface tracking        │
│  └── PerformanceSensitiveAnalyzers/        # Allocation detection        │
│                                                                          │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## Core Components

### Built-in Analyzers (`src/Analyzers/`)

**What it is:** Analyzers integrated into the IDE for code style and quality.

**Key responsibilities:**
- Code style enforcement (IDE0xxx diagnostics)
- Simplification suggestions
- Modernization recommendations
- Code quality warnings

**Key directories:**
- `Core/Analyzers/` — Abstract base classes
- `Core/CodeFixes/` — Abstract code fix base classes
- `CSharp/Analyzers/` — C# implementations
- `VisualBasic/Analyzers/` — VB implementations

**Key base classes:**
- `AbstractBuiltInCodeStyleDiagnosticAnalyzer` — Base for style analyzers
- `AbstractBuiltInUnnecessaryCodeStyleDiagnosticAnalyzer` — For "unnecessary" diagnostics
- `SyntaxEditorBasedCodeFixProvider` — Base for syntax-based fixes

### RoslynAnalyzers (`src/RoslynAnalyzers/`)

**What it is:** Packaged analyzers distributed as NuGet packages.

**Key packages:**

| Package | Purpose |
|---------|---------|
| `Microsoft.CodeAnalysis.Analyzers` | Rules for analyzer authors |
| `Roslyn.Diagnostics.Analyzers` | Internal Roslyn rules |
| `PublicApiAnalyzers` | Track public API surface |
| `BannedApiAnalyzers` | Block forbidden APIs |
| `PerformanceSensitiveAnalyzers` | Detect allocations |

---

## Component Interactions

### Analyzer Registration Flow

```
┌───────────────────────────────────────────────────────────────────────────┐
│                       Analyzer Registration                               │
│                                                                           │
│  1. DiagnosticAnalyzer.Initialize(AnalysisContext context)               │
│         │                                                                 │
│         ▼                                                                 │
│  2. Register analysis callbacks                                          │
│     • context.RegisterSyntaxNodeAction(callback, SyntaxKind.*)           │
│     • context.RegisterSymbolAction(callback, SymbolKind.*)               │
│     • context.RegisterOperationAction(callback, OperationKind.*)         │
│         │                                                                 │
│         ▼                                                                 │
│  3. Compiler/IDE invokes callbacks during analysis                       │
│         │                                                                 │
│         ▼                                                                 │
│  4. Analyzer reports diagnostics                                         │
│     context.ReportDiagnostic(Diagnostic.Create(descriptor, location))    │
└───────────────────────────────────────────────────────────────────────────┘
```

### Code Fix Flow

```
┌───────────────────────────────────────────────────────────────────────────┐
│                        Code Fix Flow                                      │
│                                                                           │
│  1. Diagnostic reported by analyzer                                      │
│         │                                                                 │
│         ▼                                                                 │
│  2. CodeFixProvider.FixableDiagnosticIds checked                         │
│         │                                                                 │
│         ▼                                                                 │
│  3. RegisterCodeFixesAsync() called with diagnostic context              │
│         │                                                                 │
│         ▼                                                                 │
│  4. Provider creates CodeAction                                          │
│         │                                                                 │
│         ▼                                                                 │
│  5. User selects fix → CodeAction.GetChangedDocumentAsync()              │
│         │                                                                 │
│         ▼                                                                 │
│  6. Changes applied to document                                          │
└───────────────────────────────────────────────────────────────────────────┘
```

---

## Data Model

### Key Entities

| Entity | Description | Purpose |
|--------|-------------|---------|
| `DiagnosticAnalyzer` | Base analyzer class | Define analysis logic |
| `DiagnosticDescriptor` | Diagnostic metadata | ID, title, message, severity |
| `Diagnostic` | Instance of diagnostic | Reported to user |
| `CodeFixProvider` | Base fix provider | Offer corrections |
| `CodeAction` | A suggested change | Fix implementation |

### Diagnostic Descriptor

```csharp
public static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
    id: "IDE0001",                    // Unique ID
    title: "Simplify name",           // Short title
    messageFormat: "Name '{0}' can be simplified", // Parameterized message
    category: "Style",                // Category
    defaultSeverity: DiagnosticSeverity.Info,
    isEnabledByDefault: true,
    helpLinkUri: "https://...");
```

---

## Technology Stack

| Category | Technology | Purpose |
|----------|------------|---------|
| API | `Microsoft.CodeAnalysis.Diagnostics` | Analyzer framework |
| Registration | Attributes | `[DiagnosticAnalyzer]` |
| Collections | `ImmutableArray<T>` | Thread-safe results |
| Configuration | EditorConfig | User preferences |

---

## Design Patterns

### Abstract + Language-Specific Pattern

```csharp
// Core: Abstract base
public abstract class AbstractSimplifyNameDiagnosticAnalyzer : DiagnosticAnalyzer
{
    protected abstract ISyntaxFacts SyntaxFacts { get; }
    // Shared logic
}

// C#: Concrete implementation
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class CSharpSimplifyNameDiagnosticAnalyzer : AbstractSimplifyNameDiagnosticAnalyzer
{
    protected override ISyntaxFacts SyntaxFacts => CSharpSyntaxFacts.Instance;
}
```

### Options-Aware Analyzer

```csharp
public class UseVarAnalyzer : AbstractBuiltInCodeStyleDiagnosticAnalyzer
{
    protected override void InitializeWorker(AnalysisContext context)
    {
        context.RegisterSyntaxNodeAction(ctx =>
        {
            var options = ctx.Options.GetAnalyzerOptions(ctx.Tree);
            var useVarPreference = options.GetOption(UseVarOption);
            
            if (useVarPreference.Value)
            {
                // Check and report
            }
        }, SyntaxKind.VariableDeclaration);
    }
}
```

### IOperation-Based Analyzer

```csharp
[DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
public class NullCheckAnalyzer : DiagnosticAnalyzer
{
    public override void Initialize(AnalysisContext context)
    {
        // Works for both C# and VB using IOperation
        context.RegisterOperationAction(
            AnalyzeOperation,
            OperationKind.BinaryOperator);
    }
}
```

---

## Analyzer Categories

### Code Style (IDE0xxx)

| ID Range | Category |
|----------|----------|
| IDE0001-IDE0099 | Code style (simplification, preferences) |
| IDE0100-IDE0199 | Expression preferences |
| IDE0200-IDE0299 | Code quality suggestions |
| IDE0300+ | Newer features |

### Unnecessary Code

| ID | Description |
|----|-------------|
| IDE0005 | Remove unnecessary imports |
| IDE0047 | Remove unnecessary parentheses |
| IDE0051 | Remove unused private members |
| IDE0059 | Remove unnecessary value assignment |

### Quality

| ID | Description |
|----|-------------|
| IDE0032 | Use auto property |
| IDE0044 | Make field readonly |
| IDE0060 | Remove unused parameter |

---

## Configuration

### EditorConfig Integration

```ini
# Enable/disable
dotnet_diagnostic.IDE0001.severity = warning

# Severity levels: none, silent, suggestion, warning, error

# Style options
csharp_style_var_for_built_in_types = true:suggestion
csharp_style_expression_bodied_methods = true:warning
```

### Analyzer Configuration

Analyzers read options via `AnalyzerOptions`:

```csharp
var options = context.Options.GetAnalyzerOptions(context.Tree);
var value = options.GetOption(MyAnalyzerOption);
```

---

## Internal Names

- **Built-in Analyzer** — Analyzer included with Roslyn IDE
- **Packaged Analyzer** — Analyzer distributed as NuGet package
- **Fixer** — Code fix provider
- **Fix All Provider** — Applies fix across scope (document/project/solution)
- **Meta-Analyzer** — Analyzer that analyzes analyzer code

See also: [../glossary.md](../glossary.md)

---

## Important Links

**External:**
- [Analyzer Tutorial](https://docs.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/tutorials/how-to-write-csharp-analyzer-code-fix)
- [Code Analysis Docs](https://docs.microsoft.com/en-us/dotnet/fundamentals/code-analysis/)

**Internal Code:**
- `src/Analyzers/` — Built-in IDE analyzers
- `src/RoslynAnalyzers/` — Packaged analyzers

**Related Docs:**
- [Product Overview](./product_overview.md)
- [Glossary](../glossary.md)
- [Main Overview](../main_overview.md)
