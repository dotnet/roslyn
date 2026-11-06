# Editor Integration: Codebase Overview

| | |
|---|---|
| **Last Updated** | January 29, 2026 |
| **Git SHA** | `771fe9b8443e955573725b4db6cc019685d8c2d4` |
| **Parent Doc** | [Main Overview](../main_overview.md) |

For product context, see [product_overview.md](./product_overview.md). See [../glossary.md](../glossary.md) for terms.

---

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────┐
│               Visual Studio Shell & Editor                  │
│  IVsTextView, IVsTextBuffer, IVsHierarchy, Error List       │
└────────────────────────────┬────────────────────────────────┘
                             │ VS SDK / Editor APIs
                             ▼
┌─────────────────────────────────────────────────────────────┐
│           VisualStudio Layer (src/VisualStudio/)            │
│                                                             │
│  ┌───────────────────┐  ┌────────────────────┐              │
│  │    Packages       │  │ VisualStudio       │              │
│  │ • RoslynPackage   │  │ Workspace          │              │
│  │ • CSharpPackage   │  │ • VS-specific      │              │
│  │ • VBPackage       │  │ • Project system   │              │
│  └───────────────────┘  └────────────────────┘              │
│                                                             │
│  ┌───────────────────────────────────────────────────────┐  │
│  │              VS Service Integration                   │  │
│  │  • Error List service                                 │  │
│  │  • Task List service                                  │  │
│  │  • Find Results service                               │  │
│  └───────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
                             │ MEF composition
                             ▼
┌─────────────────────────────────────────────────────────────┐
│         EditorFeatures Layer (src/EditorFeatures/)          │
│                                                             │
│  ┌───────────────────────────────────────────────────────┐  │
│  │                      Taggers                          │  │
│  │  ┌────────────┐ ┌────────────┐ ┌────────────┐         │  │
│  │  │Classific.  │ │ Diagnostic │ │ Reference  │         │  │
│  │  │  Tagger    │ │   Tagger   │ │ Highlight  │         │  │
│  │  └────────────┘ └────────────┘ └────────────┘         │  │
│  └───────────────────────────────────────────────────────┘  │
│                                                             │
│  ┌───────────────────────────────────────────────────────┐  │
│  │               Command Handlers                        │  │
│  │  Format, Rename, Comment, GoTo, etc.                  │  │
│  └───────────────────────────────────────────────────────┘  │
│                                                             │
│  ┌───────────────────────────────────────────────────────┐  │
│  │                   Adornments                          │  │
│  │  Inline hints, parameter name hints, diagnostics      │  │
│  └───────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
                             │ Uses
                             ▼
┌─────────────────────────────────────────────────────────────┐
│              Features Layer (src/Features/)                 │
│  CompletionService, DiagnosticService, FormattingService    │
└─────────────────────────────────────────────────────────────┘
```

---

## Core Components

### EditorFeatures (`src/EditorFeatures/`)

**What it is:** Bridge between abstract Features and the WPF editor.

**Key responsibilities:**
- Implement taggers for visual decorations
- Handle editor commands
- Manage adornments and overlays
- Coordinate with text buffer changes

**Key directories:**
- `Core/` — Shared editor feature infrastructure
- `CSharp/` — C#-specific editor features
- `VisualBasic/` — VB-specific editor features
- `Test/` — Test infrastructure

**Key files/classes:**
- `Tagging/AbstractAsynchronousTaggerProvider.cs` — Base tagger infrastructure
- `Commands/` — Command handler implementations
- `InlineHints/` — Inline hint adornments

### VisualStudio (`src/VisualStudio/`)

**What it is:** Visual Studio-specific integration layer.

**Key responsibilities:**
- Package initialization and lifecycle
- VS workspace implementation
- VS service integration (Error List, etc.)
- VSIX deployment configuration

**Key directories:**
- `Core/Def/` — Core VS definitions and services
- `CSharp/Impl/` — C# language service
- `VisualBasic/Impl/` — VB language service
- `Setup/` — VSIX packaging

**Key files/classes:**
- `Core/Def/RoslynPackage.cs` — Base Roslyn package
- `CSharp/Impl/CSharpPackage.cs` — C# package
- `Core/Def/VisualStudioWorkspace.cs` — VS workspace

---

## Component Interactions

### TextView Connection Flow

```
┌───────────────────────────────────────────────────────────────────────────┐
│                    Text View Connection                                   │
│                                                                           │
│  1. User opens C# file                                                   │
│         │                                                                 │
│         ▼                                                                 │
│  2. VS creates ITextView, ITextBuffer                                    │
│         │                                                                 │
│         ▼                                                                 │
│  3. ITextViewConnectionListener.SubjectBuffersConnected()                │
│         │                                                                 │
│         ▼                                                                 │
│  4. Roslyn creates per-view services                                     │
│     • Taggers for the buffer                                             │
│     • Command handlers for the view                                      │
│         │                                                                 │
│         ▼                                                                 │
│  5. Services start providing features                                    │
│     • Taggers return tags for visible spans                              │
│     • Commands become available                                          │
└───────────────────────────────────────────────────────────────────────────┘
```

### Tagger Data Flow

```
┌───────────────────────────────────────────────────────────────────────────┐
│                        Tagger Data Flow                                   │
│                                                                           │
│  Text Change in Buffer                                                   │
│         │                                                                 │
│         ▼                                                                 │
│  Tagger.GetTags() called for visible spans                              │
│         │                                                                 │
│         ▼                                                                 │
│  Tagger queries Roslyn service (async)                                   │
│  • DiagnosticTagger → DiagnosticService.GetDiagnosticsAsync()           │
│  • ClassificationTagger → ClassificationService                         │
│         │                                                                 │
│         ▼                                                                 │
│  Tagger converts results to ITagSpan<T>                                 │
│         │                                                                 │
│         ▼                                                                 │
│  Editor renders tags (squiggles, colors, etc.)                          │
└───────────────────────────────────────────────────────────────────────────┘
```

---

## Data Model

### Key Entities

| Entity | Description | VS API |
|--------|-------------|--------|
| Text Buffer | Mutable text storage | `ITextBuffer` |
| Text View | Editor viewport | `ITextView` |
| Text Snapshot | Immutable buffer state | `ITextSnapshot` |
| Tag | Visual decoration | `ITag` |
| Tag Span | Tag with text span | `ITagSpan<T>` |

### Tag Types

| Tag Interface | Purpose | Visual |
|---------------|---------|--------|
| `IClassificationTag` | Syntax coloring | Text color |
| `IErrorTag` | Error markers | Red squiggle |
| `ITextMarkerTag` | Highlighted references | Background color |
| `IOutliningRegionTag` | Code folding | Collapse regions |

---

## Technology Stack

| Category | Technology | Purpose |
|----------|------------|---------|
| Framework | .NET Framework 4.7.2 | VS runtime requirement |
| UI | WPF | Editor rendering |
| DI | MEF v2 | Component composition |
| Editor | VS Editor APIs | Text buffer, view |
| Packaging | VSIX | Extension deployment |

---

## Design Patterns

### Tagger Provider Pattern

Taggers are created by providers (tagger implementations are internal):

```csharp
[Export(typeof(ITaggerProvider))]
[TagType(typeof(IErrorTag))]
[ContentType(ContentTypeNames.RoslynContentType)]  // Use constants
public class DiagnosticTaggerProvider : ITaggerProvider
{
    [ImportingConstructor]
    public DiagnosticTaggerProvider(IDiagnosticService diagnosticService)
    {
        _diagnosticService = diagnosticService;
    }
    
    public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
    {
        return buffer.Properties.GetOrCreateSingletonProperty(
            () => new DiagnosticTagger(_diagnosticService, buffer)) as ITagger<T>;
    }
}

// Note: Some taggers use IViewTaggerProvider instead of ITaggerProvider
// for taggers that need access to the ITextView (e.g., ReferenceHighlightingViewTaggerProvider)
```

### Async Tagger Pattern

Taggers compute tags asynchronously:

```csharp
public class AbstractAsynchronousTaggerProvider<TTag> : ITaggerProvider where TTag : ITag
{
    // Background computation with cancellation
    // Results cached and updated incrementally
    // TagsChanged event fires when new tags available
}
```

### Command Handler Pattern

Commands handled via MEF exports:

```csharp
[Export]
[Export(typeof(ICommandHandler))]
[ContentType(ContentTypeNames.RoslynContentType)]
[Name(PredefinedCommandHandlerNames.FormatDocument)]
[Order(After = PredefinedCommandHandlerNames.Rename)]
internal sealed partial class FormatCommandHandler : ICommandHandler<FormatDocumentCommandArgs>
{
    public bool ExecuteCommand(FormatDocumentCommandArgs args, CommandExecutionContext context)
    {
        var document = args.SubjectBuffer.GetDocument();
        var formatted = Formatter.FormatAsync(document);
        // Apply changes
        return true;
    }
}
```

---

## Configuration

### Package Registration

Packages are registered via `[Guid]` attributes with separate `.pkgdef` files for VS registration:

```csharp
// Packages use [Guid] attribute (not [PackageRegistration])
[Guid(Guids.CSharpPackageIdString)]
public sealed class CSharpPackage : AbstractPackage<CSharpPackage, CSharpLanguageService>
{
}

// Registration is in PackageRegistration.pkgdef files:
// - src/VisualStudio/CSharp/Impl/PackageRegistration.pkgdef
// - src/VisualStudio/VisualBasic/Impl/PackageRegistration.pkgdef
```

### VSIX Manifest

`source.extension.vsixmanifest` defines deployment:

```xml
<Content>
  <MefComponent>Microsoft.CodeAnalysis.EditorFeatures.dll</MefComponent>
  <MefComponent>Microsoft.CodeAnalysis.Features.dll</MefComponent>
  <MefComponent>Microsoft.VisualStudio.LanguageServices.dll</MefComponent>
</Content>
```

---

## Internal Names

- **Subject Buffer** — The `ITextBuffer` being operated on
- **Tagger** — Provides tags for text spans
- **Tag Source** — Internal tagger state management
- **Viewport Tagger** — Tagger optimized for visible area only
- **Adornment** — Visual overlay on editor surface

See also: [../glossary.md](../glossary.md)

---

## Important Links

**External:**
- [VS Editor API](https://docs.microsoft.com/en-us/visualstudio/extensibility/editor-imports)
- [MEF Documentation](https://docs.microsoft.com/en-us/dotnet/framework/mef/)

**Internal Code:**
- `src/EditorFeatures/Core/` — Shared editor features
- `src/EditorFeatures/CSharp/` — C# editor features
- `src/VisualStudio/Core/Def/` — VS integration

**Related Docs:**
- [Product Overview](./product_overview.md)
- [Glossary](../glossary.md)
- [Main Overview](../main_overview.md)

**Existing Codebase Docs:**
- [IDE README](../../docs/ide/README.md)
- [IDE Glossary](../../docs/ide/glossary.md)

---

## Documentation Scope

This document provides a high-level architectural overview of the Editor Integration layers. It covers the layering and MEF composition but does not detail each tagger or handler.

**What's covered:** Architecture, tagger/command patterns, package structure, MEF composition

**What's not covered:** All taggers, all handlers, VS SDK details

**To go deeper:** Start a new AI session using the [Expanding Documentation Prompt](https://github.com/CyrusNajmabadi/codebase-explorer/blob/main/LOADER.md#expanding-documentation-prompt).

**Parent document:** [Main Overview](../main_overview.md)
