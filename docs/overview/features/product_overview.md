# Features: Product Overview

**Last Updated:** January 29, 2026

## The Story: Making Code Smarter

A developer is working on a C# class and types `list.` — immediately, a dropdown appears with all available members. They select `Where` and start typing a lambda; the signature help shows parameter info. They notice a squiggle under a variable name — hovering reveals it's a nullable warning. They press Ctrl+. and see a fix: "Add null check." One click, and the fix is applied.

**Behind the Scenes**

Each of these interactions is a "feature" — a discrete piece of IDE functionality:

- **Completion** — Suggest code as you type
- **Signature Help** — Show parameter info for method calls
- **Quick Info** — Hover tooltips with symbol information
- **Diagnostics** — Error/warning squiggles
- **Code Fixes** — Lightbulb suggestions to fix issues
- **Refactorings** — Code transformations (extract method, rename, etc.)

**The Challenge: Language-Agnostic Features**

Roslyn supports both C# and Visual Basic. Writing every feature twice would be:
- Expensive (double the code)
- Error-prone (features could diverge)
- Hard to maintain (fixes needed in two places)

**The Solution: Language-Agnostic Core with Language-Specific Specializations**

This is a core design pattern used throughout Roslyn. Features are structured in three tiers:

1. **Core/Portable** — Language-agnostic logic (typically 80-90% of the code)
   ```csharp
   abstract class AbstractIntroduceVariableService<TExpressionSyntax>
   {
       // Most logic lives here, parameterized by syntax types
       protected abstract ISyntaxFacts SyntaxFacts { get; }
   }
   ```

2. **CSharp** — C#-specific implementation
   ```csharp
   [ExportLanguageService(typeof(IIntroduceVariableService), LanguageNames.CSharp)]
   class CSharpIntroduceVariableService : AbstractIntroduceVariableService<ExpressionSyntax>
   {
       protected override ISyntaxFacts SyntaxFacts => CSharpSyntaxFacts.Instance;
   }
   ```

3. **VisualBasic** — VB-specific implementation (same pattern)

This pattern ensures features work consistently across both languages while allowing language-specific customization where needed.

---

## Core Concepts

### Code Actions

**What it is:** A suggested change shown in the lightbulb menu.

**Types:**
- **Code Fix** — Fixes a diagnostic (error/warning)
- **Code Refactoring** — Restructures code without fixing a diagnostic

**Example:**
```csharp
public class AddBracesCodeFixProvider : CodeFixProvider
{
    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var diagnostic = context.Diagnostics.First();
        context.RegisterCodeFix(
            CodeAction.Create("Add braces", ct => AddBracesAsync(context.Document, ct)),
            diagnostic);
    }
}
```

### Completion

**What it is:** IntelliSense suggestions as you type.

**How it works:**
1. User types a trigger character (`.`, space, etc.)
2. `CompletionService` collects items from all `CompletionProvider`s
3. Items are filtered and ranked
4. UI displays the list

### Diagnostics

**What it is:** Errors, warnings, and suggestions shown as squiggles.

**Sources:**
- Compiler diagnostics (syntax errors, type errors)
- Analyzer diagnostics (code style, best practices)
- IDE diagnostics (simplification suggestions)

### Navigation

**What it is:** Going to definitions, implementations, and references.

**Features:**
- Go to Definition (F12)
- Go to Implementation
- Find All References
- Navigate To (Ctrl+,)

---

## Major IDE Features

### Code Intelligence

| Feature | Description | Trigger |
|---------|-------------|---------|
| **Completion** | Suggest code while typing | `.`, space, Ctrl+Space |
| **Signature Help** | Parameter info for calls | `(`, `,` |
| **Quick Info** | Hover tooltips | Mouse hover |
| **Highlighting** | Highlight symbol references | Cursor on symbol |

### Code Actions

| Feature | Description | Trigger |
|---------|-------------|---------|
| **Code Fixes** | Fix diagnostics | Ctrl+. on error |
| **Refactorings** | Transform code | Ctrl+. anywhere |
| **Code Cleanup** | Apply multiple fixes | Ctrl+K, Ctrl+E |

### Refactoring Operations

| Refactoring | Description |
|-------------|-------------|
| Extract Method | Move code to new method |
| Extract Interface | Create interface from class |
| Introduce Variable | Replace expression with variable |
| Inline | Inline variable/method |
| Rename | Rename symbol across solution |
| Change Signature | Modify method parameters |
| Move Type | Move to matching file |
| Pull Members Up | Move to base class |

### Navigation

| Feature | Description | Shortcut |
|---------|-------------|----------|
| Go to Definition | Jump to declaration | F12 |
| Go to Implementation | Jump to impl | Ctrl+F12 |
| Find All References | Find usages | Shift+F12 |
| Navigate To | Search symbols | Ctrl+, |
| Go to Base | Jump to base member | Alt+Home |

---

## Architecture at a Glance

```
┌─────────────────────────────────────────────────────────────────────────┐
│                         Feature Layer                                    │
│                                                                          │
│  ┌─────────────────────────────────────────────────────────────────────┐│
│  │                    Core/Portable                                    ││
│  │  • Abstract feature services                                        ││
│  │  • Shared logic (ISyntaxFacts, ISemanticFacts)                      ││
│  │  • Provider infrastructure                                          ││
│  └─────────────────────────────────────────────────────────────────────┘│
│         │                                          │                    │
│         ▼                                          ▼                    │
│  ┌───────────────┐                      ┌───────────────────┐          │
│  │    CSharp     │                      │   VisualBasic     │          │
│  │  • C# syntax  │                      │  • VB syntax      │          │
│  │  • C# impls   │                      │  • VB impls       │          │
│  └───────────────┘                      └───────────────────┘          │
└─────────────────────────────────────────────────────────────────────────┘
                              │
                              │ Uses
                              ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                        Workspaces Layer                                 │
│         (Document, SemanticModel, Solution)                             │
└─────────────────────────────────────────────────────────────────────────┘
```

For detailed architecture, see [Codebase Overview](./codebase_overview.md).

---

## Common Use Cases

### Adding a Code Fix

**Scenario:** Create a fix that adds missing `using` directives

**Solution:**
```csharp
[ExportCodeFixProvider(LanguageNames.CSharp)]
public class AddUsingCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds 
        => ImmutableArray.Create("CS0246"); // Type not found
    
    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync();
        var diagnostic = context.Diagnostics.First();
        var node = root.FindNode(diagnostic.Location.SourceSpan);
        
        context.RegisterCodeFix(
            CodeAction.Create(
                "Add using directive",
                ct => AddUsingAsync(context.Document, node, ct)),
            diagnostic);
    }
}
```

### Adding a Completion Provider

**Scenario:** Suggest custom snippets in completion

**Solution:**
```csharp
[ExportCompletionProvider(LanguageNames.CSharp)]
public class CustomCompletionProvider : CompletionProvider
{
    public override async Task ProvideCompletionsAsync(CompletionContext context)
    {
        if (IsApplicableContext(context))
        {
            context.AddItem(CompletionItem.Create(
                displayText: "mySnippet",
                properties: ImmutableDictionary<string, string>.Empty));
        }
    }
}
```

---

## What's NOT Covered Here

- **Editor Integration** — How features render in VS; see [Editor Integration](../editor_integration/product_overview.md)
- **Analyzers** — Diagnostic analysis; see [Analyzers Overview](../analyzers/product_overview.md)
- **Language Server** — How features work in VS Code; see [Language Server](../language_server/product_overview.md)

---

---

## Related Documentation

**In This Overview:**
- [Codebase Overview](./codebase_overview.md) — Technical architecture and components
- [Main Overview](../main_overview.md) — Full codebase map
- [Glossary](../glossary.md) — Terminology

**Existing Codebase Docs:**
- [Roslyn Overview](../../wiki/Roslyn-Overview.md) — Official architecture deep-dive
- [Getting Started Writing a Custom Analyzer & Code Fix](../../wiki/Getting-Started-Writing-a-Custom-Analyzer-&-Code-Fix.md)

---

## Documentation Scope

This document explains why the Features layer exists and what IDE features it provides. It covers the feature catalog and patterns but does not detail implementation of individual features.

**What's covered:** Feature catalog, provider pattern, language-agnostic design

**What's not covered:** Implementation of specific features, all providers, UI integration

**To go deeper:** See [Codebase Overview](./codebase_overview.md) for architecture. For more detail, start a new AI session using the [Expanding Documentation Prompt](https://github.com/CyrusNajmabadi/codebase-explorer/blob/main/LOADER.md#expanding-documentation-prompt).

**Parent document:** [Main Overview](../main_overview.md)
