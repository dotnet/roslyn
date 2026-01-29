# Features: Codebase Overview

**Last Updated:** January 29, 2026

For product context, see [product_overview.md](./product_overview.md). See [../glossary.md](../glossary.md) for terms.

---

## Architecture Overview

Features follows Roslyn's core design pattern: **language-agnostic core with language-specific specializations**. The Core/Portable layer contains abstract base classes and shared logic (typically 80-90% of the code), while CSharp and VisualBasic layers provide concrete implementations.

```
src/Features/
├── Core/Portable/           # Language-agnostic implementations
│   ├── Completion/          # Abstract completion service
│   ├── CodeRefactorings/    # Abstract refactoring infrastructure
│   ├── CodeFixes/           # Abstract code fix infrastructure
│   ├── Diagnostics/         # Diagnostic analyzer infrastructure
│   └── [Feature]/           # Each feature has its own folder
│
├── CSharp/Portable/         # C# specific implementations
│   ├── Completion/          # C# completion providers
│   ├── CodeRefactorings/    # C# refactorings
│   └── [Feature]/           # C# implementations
│
└── VisualBasic/Portable/    # VB specific implementations
    └── ...                  # Same structure as CSharp
```

---

## Core Components

### Core/Portable (`src/Features/Core/Portable/`)

**What it is:** Language-agnostic feature implementations and infrastructure.

**Key responsibilities:**
- Define abstract base classes for features
- Provide shared utility code
- Implement language-agnostic portions of features
- Define provider interfaces and registration

**Key files/classes:**
- `Completion/CompletionService.cs` — Completion orchestration
- `CodeRefactorings/CodeRefactoringService.cs` — Refactoring orchestration
- `CodeFixes/CodeFixService.cs` — Code fix orchestration
- `QuickInfo/QuickInfoService.cs` — Hover information
- `SignatureHelp/SignatureHelpService.cs` — Parameter hints

### Language-Specific (`src/Features/CSharp/`, `src/Features/VisualBasic/`)

**What it is:** Language-specific implementations that extend abstract base classes.

**Key responsibilities:**
- Implement language-specific syntax handling
- Export services with language-specific attributes
- Provide language-specific providers

**Pattern:**
```csharp
// Core defines abstract service
abstract class AbstractIntroduceVariableService<TExpressionSyntax>
{
    protected abstract ISyntaxFacts SyntaxFacts { get; }
    // Shared logic using generic types
}

// C# provides concrete implementation
[ExportLanguageService(typeof(IIntroduceVariableService), LanguageNames.CSharp)]
sealed class CSharpIntroduceVariableService 
    : AbstractIntroduceVariableService<ExpressionSyntax>
{
    protected override ISyntaxFacts SyntaxFacts => CSharpSyntaxFacts.Instance;
}
```

---

## Component Interactions

### Feature Registration Flow

```
┌───────────────────────────────────────────────────────────────────────────┐
│                        MEF Registration                                   │
│                                                                           │
│  [ExportCodeRefactoringProvider(LanguageNames.CSharp)]                   │
│  class ExtractMethodProvider : CodeRefactoringProvider                   │
│                        │                                                  │
│                        ▼                                                  │
│  MEF discovers exports at composition time                                │
│                        │                                                  │
│                        ▼                                                  │
│  CodeRefactoringService aggregates all providers                         │
│                        │                                                  │
│                        ▼                                                  │
│  When user invokes refactoring:                                          │
│    1. Service calls GetRefactoringsAsync() on each provider              │
│    2. Providers return CodeAction instances                              │
│    3. Service collects and presents to user                              │
└───────────────────────────────────────────────────────────────────────────┘
```

### Service → Provider Pattern

Services aggregate multiple providers:

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    CompletionService                                     │
│                          │                                               │
│         ┌────────────────┼────────────────┐                             │
│         │                │                │                             │
│         ▼                ▼                ▼                             │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐                     │
│  │  Keyword    │  │   Symbol    │  │  Snippet    │                     │
│  │  Provider   │  │  Provider   │  │  Provider   │                     │
│  └─────────────┘  └─────────────┘  └─────────────┘                     │
└─────────────────────────────────────────────────────────────────────────┘
```

### Abstract + Language-Specific Pattern

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    Core/Portable                                         │
│                                                                          │
│  abstract class AbstractExtractMethodService<TSyntax>                   │
│  {                                                                       │
│      // 90% of logic here                                               │
│      protected abstract ISyntaxFacts SyntaxFacts { get; }               │
│      protected abstract Document ExtractMethod(Document, TSyntax);      │
│  }                                                                       │
└────────────────────────────┬────────────────────────────────────────────┘
                             │
              ┌──────────────┴──────────────┐
              │                             │
              ▼                             ▼
┌─────────────────────────────┐  ┌─────────────────────────────┐
│          CSharp             │  │       VisualBasic           │
│                             │  │                             │
│  [ExportLanguageService]    │  │  [ExportLanguageService]    │
│  class CSharpExtract        │  │  class VBExtract            │
│    : AbstractExtract        │  │    : AbstractExtract        │
│  {                          │  │  {                          │
│    // C# specific only      │  │    // VB specific only      │
│  }                          │  │  }                          │
└─────────────────────────────┘  └─────────────────────────────┘
```

---

## Data Model

### Key Entities

| Entity | Description | Purpose |
|--------|-------------|---------|
| `CodeAction` | A suggested code change | Lightbulb items |
| `CodeRefactoring` | Container for code actions | Groups related actions |
| `CodeFix` | Fix for a diagnostic | Error/warning fixes |
| `CompletionItem` | Single completion suggestion | IntelliSense item |
| `SignatureHelpItem` | Method signature info | Parameter hints |
| `QuickInfoItem` | Hover tooltip content | Symbol info |

### Code Action Structure

```csharp
public class CodeAction
{
    public string Title { get; }                    // Display text
    public virtual Task<Document> GetChangedDocumentAsync();
    public virtual Task<Solution> GetChangedSolutionAsync();
    public virtual ImmutableArray<CodeAction> NestedActions { get; }
}
```

---

## Technology Stack

| Category | Technology | Purpose |
|----------|------------|---------|
| DI | MEF v2 | Service/provider discovery |
| Async | Task-based | Non-blocking UI |
| Collections | ImmutableArray | Thread-safe results |

---

## Design Patterns

### Provider Pattern

Features use providers for extensibility:

```csharp
// Provider interface
public abstract class CompletionProvider
{
    public abstract Task ProvideCompletionsAsync(CompletionContext context);
}

// Registration via MEF
[ExportCompletionProvider(LanguageNames.CSharp, Name = "KeywordCompletionProvider")]
public class KeywordCompletionProvider : CompletionProvider
{
    public override async Task ProvideCompletionsAsync(CompletionContext context)
    {
        // Add keyword completion items
    }
}
```

### Service Aggregation

Services collect and invoke providers:

```csharp
public class CompletionService
{
    private readonly IEnumerable<CompletionProvider> _providers;
    
    public async Task<CompletionList> GetCompletionsAsync(Document document, int position)
    {
        var context = new CompletionContext(document, position);
        foreach (var provider in _providers)
        {
            await provider.ProvideCompletionsAsync(context);
        }
        return context.ToCompletionList();
    }
}
```

### Language Service Pattern

Per-language services via MEF:

```csharp
[ExportLanguageService(typeof(ICompletionService), LanguageNames.CSharp), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class CSharpCompletionService(/* dependencies */) : CompletionService
{
}
```

---

## Configuration

### Feature Options

Features respect EditorConfig and VS options:

```csharp
var options = document.Project.AnalyzerOptions;
var completionOptions = CompletionOptions.From(options);
```

### Provider Ordering

Providers can specify order:

```csharp
[ExportCompletionProvider(LanguageNames.CSharp, Name = "...", Order = 100)]
```

---

## Internal Names

- **Code Action** — A single suggested change
- **Provider** — Extensible component that contributes items
- **Service** — Aggregates providers and orchestrates
- **Semantic Document** — Document wrapper with semantic model
- **Fix All Provider** — Applies fix across multiple locations
- **Solution Crawler** — Background analysis system

See also: [../glossary.md](../glossary.md)

---

## Major Feature Areas

| Directory | Features |
|-----------|----------|
| `Completion/` | IntelliSense completion |
| `CodeRefactorings/` | Extract Method, Rename, etc. |
| `CodeFixes/` | Diagnostic fixes |
| `Diagnostics/` | IDE diagnostics |
| `Navigation/` | Go to Definition, Find References |
| `QuickInfo/` | Hover tooltips |
| `SignatureHelp/` | Parameter hints |
| `InlineHints/` | Type hints in code |
| `Formatting/` | Code formatting |

---

## Important Links

**Internal Code:**
- `src/Features/Core/Portable/` — Shared feature code
- `src/Features/CSharp/Portable/` — C# features
- `src/Features/VisualBasic/Portable/` — VB features

**Related Docs:**
- [Product Overview](./product_overview.md)
- [Glossary](../glossary.md)
- [Main Overview](../main_overview.md)
