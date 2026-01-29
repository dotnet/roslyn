# Editor Integration: Product Overview

**Last Updated:** January 29, 2026

## The Story: From Features to Pixels

A developer is writing C# in Visual Studio. As they type, they see:
- Syntax coloring (keywords in blue, strings in red)
- Error squiggles appearing under typos
- A lightbulb icon appearing in the margin
- IntelliSense dropdown with method suggestions
- Inline type hints showing inferred types

Each of these visual elements requires bridging Roslyn's abstract "features" to concrete editor UI.

**The Challenge: Two Different Worlds**

Roslyn's Features layer deals in abstractions:
- `CompletionItem` — An item in a completion list
- `Diagnostic` — An error or warning
- `CodeAction` — A suggested fix

But Visual Studio's editor deals in concrete UI concepts:
- `ITextView` — The editor viewport
- `ITextBuffer` — The text storage
- `ITagger<T>` — Provider of visual decorations

**The Solution: EditorFeatures + VisualStudio Layers**

Two layers bridge the gap:

```
┌─────────────────────────────────────────────────────────────────────────┐
│                        Visual Studio                                    │
│  • VS packages (RoslynPackage, CSharpPackage)                          │
│  • VS-specific services (error list, task list)                        │
│  • VSIX deployment                                                      │
└──────────────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                       EditorFeatures                                    │
│  • Taggers (syntax coloring, error squiggles)                          │
│  • Command handlers (format, rename)                                    │
│  • Adornments (inline hints, reference highlighting)                   │
└──────────────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                          Features                                       │
│  • CompletionService, DiagnosticService, etc.                          │
│  • Language-agnostic feature logic                                     │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## Core Concepts

### Text Buffer and Text View

**What they are:** The fundamental editor abstractions.

- `ITextBuffer` — The mutable text storage
- `ITextView` — The viewport displaying the buffer
- `ITextSnapshot` — Immutable snapshot of buffer contents

**Why they matter:** All editor integration works with these abstractions. Roslyn must track changes to text buffers and provide features scoped to text views.

### Taggers

**What they are:** Components that provide "tags" for text spans.

**Examples:**
- Classification tags → syntax coloring
- Error tags → red squiggles
- Text marker tags → highlighted references

**Pattern:**
```csharp
public class DiagnosticTagger : ITagger<IErrorTag>
{
    public IEnumerable<ITagSpan<IErrorTag>> GetTags(NormalizedSnapshotSpanCollection spans)
    {
        // Return error tags for diagnostic locations
    }
}
```

### Command Handlers

**What they are:** Components that respond to editor commands.

**Examples:**
- Format Document (Ctrl+K, Ctrl+D)
- Rename (F2)
- Comment Selection (Ctrl+K, Ctrl+C)

**Pattern:**
```csharp
[ExportCommandHandler(nameof(FormatDocumentHandler))]
public class FormatDocumentHandler : ICommandHandler<FormatDocumentCommandArgs>
{
    public bool ExecuteCommand(FormatDocumentCommandArgs args, CommandExecutionContext context)
    {
        // Format the document
        return true;
    }
}
```

### Adornments

**What they are:** Visual overlays on the editor surface.

**Examples:**
- Inline type hints (`var x` → `string x`)
- Parameter name hints (`Method(name: "value")`)
- Diagnostic underlines

---

## EditorFeatures Layer

The EditorFeatures layer (`src/EditorFeatures/`) bridges Features to the WPF editor.

### Key Components

| Component | Purpose |
|-----------|---------|
| **Taggers** | Provide visual decorations |
| **Command Handlers** | Process editor commands |
| **Completion Presenters** | Display IntelliSense UI |
| **Adornment Managers** | Manage visual overlays |
| **Navigation Handlers** | Handle Go to Definition, etc. |

### Tagger Types

| Tagger | What It Provides |
|--------|------------------|
| `ClassificationTagger` | Syntax coloring |
| `DiagnosticTagger` | Error/warning squiggles |
| `ReferenceHighlightingTagger` | Symbol highlighting |
| `BraceMatchingTagger` | Brace highlighting |
| `LineSeparatorTagger` | Method separators |

---

## VisualStudio Layer

The VisualStudio layer (`src/VisualStudio/`) provides VS-specific integration.

### Key Components

| Component | Purpose |
|-----------|---------|
| **Packages** | VS package entry points |
| **Language Services** | VS language service registration |
| **VSIX Deployment** | Extension packaging |
| **VS Services** | Error List, Task List integration |

### Packages

| Package | Purpose |
|---------|---------|
| `RoslynPackage` | Base package for Roslyn |
| `CSharpPackage` | C# language service |
| `VisualBasicPackage` | VB language service |

### VS Service Integration

| Service | Purpose |
|---------|---------|
| Error List | Display diagnostics |
| Task List | TODO comments |
| Find Results | Find All References results |
| Code Definition Window | Show definitions |

---

## Architecture at a Glance

```
┌─────────────────────────────────────────────────────────────────────────┐
│                        Visual Studio                                    │
│  ┌───────────────────────────────────────────────────────────────────┐ │
│  │                    VS Shell & Editor                              │ │
│  │  • IVsTextView, IVsTextBuffer                                     │ │
│  │  • Shell services (Error List, Task List)                         │ │
│  └───────────────────────────────────────────────────────────────────┘ │
└──────────────────────────────────────────────────────────────────────────┘
                              │
                              │ Uses VS Editor APIs
                              ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                   VisualStudio Layer                                    │
│  ┌───────────────────────────────────────────────────────────────────┐ │
│  │  Packages                                                          │ │
│  │  • RoslynPackage                                                   │ │
│  │  • CSharpPackage                                                   │ │
│  │  • VisualBasicPackage                                              │ │
│  └───────────────────────────────────────────────────────────────────┘ │
│  ┌───────────────────────────────────────────────────────────────────┐ │
│  │  VisualStudioWorkspace                                             │ │
│  │  • VS-specific workspace implementation                            │ │
│  │  • Project system integration                                      │ │
│  └───────────────────────────────────────────────────────────────────┘ │
└──────────────────────────────────────────────────────────────────────────┘
                              │
                              │ Uses MEF
                              ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                   EditorFeatures Layer                                  │
│  ┌───────────────────────────────────────────────────────────────────┐ │
│  │  Taggers                                                           │ │
│  │  • ClassificationTagger → Syntax coloring                         │ │
│  │  • DiagnosticTagger → Error squiggles                             │ │
│  │  • ReferenceHighlightingTagger → Highlight references             │ │
│  └───────────────────────────────────────────────────────────────────┘ │
│  ┌───────────────────────────────────────────────────────────────────┐ │
│  │  Command Handlers                                                  │ │
│  │  • FormatDocument, Rename, CommentSelection                       │ │
│  └───────────────────────────────────────────────────────────────────┘ │
│  ┌───────────────────────────────────────────────────────────────────┐ │
│  │  Adornments                                                        │ │
│  │  • Inline hints, parameter name hints                             │ │
│  └───────────────────────────────────────────────────────────────────┘ │
└──────────────────────────────────────────────────────────────────────────┘
                              │
                              │ Uses
                              ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                       Features Layer                                    │
│  • CompletionService, DiagnosticService, FormattingService, etc.       │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## MEF Composition

All components are composed via MEF (Managed Extensibility Framework):

```csharp
// Tagger provider export
[Export(typeof(ITaggerProvider))]
[TagType(typeof(IErrorTag))]
[ContentType("CSharp")]
public class DiagnosticTaggerProvider : ITaggerProvider
{
    public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
    {
        return new DiagnosticTagger(buffer) as ITagger<T>;
    }
}

// Command handler export
[Export(typeof(ICommandHandler))]
[ContentType("CSharp")]
[Name(nameof(FormatDocumentHandler))]
public class FormatDocumentHandler : ICommandHandler<FormatDocumentCommandArgs>
{
}
```

---

## What's NOT Covered Here

- **Features Implementation** — Core feature logic; see [Features Overview](../features/product_overview.md)
- **Language Server** — VS Code integration; see [Language Server](../language_server/product_overview.md)
- **Analyzers** — Diagnostic analysis; see [Analyzers Overview](../analyzers/product_overview.md)

---

## Related Documentation

**In This Overview:**
- [Codebase Overview](./codebase_overview.md) — Technical architecture and components
- [Main Overview](../main_overview.md) — Full codebase map
- [Glossary](../glossary.md) — Terminology

**Existing Codebase Docs:**
- [IDE README](../../docs/ide/README.md)
- [IDE Glossary](../../docs/ide/glossary.md)
- [Building, Testing, and Debugging](../../wiki/Building-Testing-and-Debugging.md)

---

## Documentation Scope

This document explains why the Editor Integration layers exist and how they bridge Roslyn features to Visual Studio. It covers the layering but does not detail all taggers or command handlers.

**What's covered:** Layer architecture, tagger/command patterns, VS package structure

**What's not covered:** All taggers, all command handlers, VSIX packaging details

**To go deeper:** See [Codebase Overview](./codebase_overview.md) for architecture. For more detail, start a new AI session using the [Expanding Documentation Prompt](https://github.com/CyrusNajmabadi/codebase-explorer/blob/main/LOADER.md#expanding-documentation-prompt).

**Parent document:** [Main Overview](../main_overview.md)
