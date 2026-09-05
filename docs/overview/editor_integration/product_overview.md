# Editor Integration: Product Overview

| | |
|---|---|
| **Last Updated** | January 29, 2026 |
| **Git SHA** | `771fe9b8443e955573725b4db6cc019685d8c2d4` |
| **Parent Doc** | [Main Overview](../main_overview.md) |

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
[Export(typeof(ICommandHandler))]
[ContentType(ContentTypeNames.RoslynContentType)]
[Name(PredefinedCommandHandlerNames.FormatDocument)]
public class FormatCommandHandler : ICommandHandler<FormatDocumentCommandArgs>
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

### Taggers

Taggers translate Roslyn's semantic understanding into editor visuals—syntax coloring, error squiggles, reference highlighting, and more. They use an async pattern to avoid blocking the UI thread.

**For detailed tagger architecture and patterns, see [Taggers Deep Dive](./taggers.md).**

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
// Command handler export
[Export(typeof(ICommandHandler))]
[ContentType(ContentTypeNames.RoslynContentType)]
[Name(PredefinedCommandHandlerNames.FormatDocument)]
[Order(After = PredefinedCommandHandlerNames.Rename)]
public class FormatCommandHandler : ICommandHandler<FormatDocumentCommandArgs>
{
}
```

For tagger-specific MEF patterns, see [Taggers Deep Dive](./taggers.md).

---

## What's NOT Covered Here

- **Features Implementation** — Core feature logic; see [Features Overview](../features/product_overview.md)
- **Language Server** — VS Code integration; see [Language Server](../language_server/product_overview.md)
- **Analyzers** — Diagnostic analysis; see [Analyzers Overview](../analyzers/product_overview.md)

---

## Related Documentation

**In This Overview:**
- [Codebase Overview](./codebase_overview.md) — Technical architecture and components
- [Taggers Deep Dive](./taggers.md) — Detailed tagger architecture and patterns
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
