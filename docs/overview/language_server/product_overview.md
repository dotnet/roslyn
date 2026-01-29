# Language Server: Product Overview

**Last Updated:** January 29, 2026

## The Story: Roslyn Beyond Visual Studio

A developer using VS Code opens a C# project. They start typing and see IntelliSense suggestions. They rename a variable with F2 and see all references updated. They hover over a method and see its documentation. All the familiar IDE features work—but they're not in Visual Studio.

**The Challenge: Editor Diversity**

Visual Studio isn't the only code editor. Developers use:
- VS Code
- JetBrains Rider
- Vim/Neovim
- Sublime Text
- And many others

Rewriting all IDE features for each editor would be impractical.

**The Solution: Language Server Protocol**

LSP (Language Server Protocol) is a standardized protocol for language features. Any editor that speaks LSP can use any language server that speaks LSP.

```
┌─────────────────┐      LSP (JSON-RPC)      ┌─────────────────┐
│    VS Code      │ ◄──────────────────────► │  Roslyn LSP     │
│    (Editor)     │                          │  Server         │
└─────────────────┘                          └─────────────────┘
                                                      │
                                                      │ Uses
                                                      ▼
                                             ┌─────────────────┐
                                             │ Roslyn Features │
                                             │  Workspaces     │
                                             │  Compilers      │
                                             └─────────────────┘
```

**How It Works:**

1. **User types** in VS Code
2. **VS Code sends** `textDocument/completion` request via JSON-RPC
3. **Roslyn LSP server** receives request, calls Roslyn's `CompletionService`
4. **Server responds** with completion items
5. **VS Code displays** the suggestions

The same Roslyn features that power Visual Studio now power VS Code—through a protocol translation layer.

---

## Core Concepts

### Language Server Protocol (LSP)

**What it is:** A JSON-RPC based protocol defining how editors and language servers communicate.

**Key message types:**
- **Requests** — Client asks server, expects response (e.g., `textDocument/completion`)
- **Notifications** — One-way message, no response (e.g., `textDocument/didChange`)
- **Responses** — Server's answer to a request

### CLaSP Framework

**What it is:** Microsoft's Common Language Server Protocol Framework—Roslyn's base for building LSP servers.

**Why it matters:** CLaSP provides:
- Request routing and dispatching
- Lifecycle management
- Handler discovery via MEF
- Request serialization/deserialization

### Handlers

**What it is:** Components that process specific LSP messages.

**Pattern:**
```csharp
[Method("textDocument/completion")]
internal class CompletionHandler : IRequestHandler<CompletionParams, CompletionList>
{
    public async Task<CompletionList> HandleAsync(CompletionParams request, RequestContext context)
    {
        // Use Roslyn's CompletionService to get completions
        var document = context.Document;
        var completions = await CompletionService.GetCompletionsAsync(document, position);
        return ConvertToLspCompletionList(completions);
    }
}
```

---

## LSP Message Flow

```
┌──────────────────────────────────────────────────────────────────────────┐
│                         Request Flow                                     │
│                                                                          │
│  VS Code                    LSP Server                    Roslyn         │
│    │                           │                            │            │
│    │  textDocument/completion  │                            │            │
│    │ ────────────────────────► │                            │            │
│    │                           │                            │            │
│    │                           │  CompletionService         │            │
│    │                           │  .GetCompletionsAsync()    │            │
│    │                           │ ─────────────────────────► │            │
│    │                           │                            │            │
│    │                           │    CompletionList          │            │
│    │                           │ ◄───────────────────────── │            │
│    │                           │                            │            │
│    │   CompletionList (LSP)    │                            │            │
│    │ ◄──────────────────────── │                            │            │
│    │                           │                            │            │
└──────────────────────────────────────────────────────────────────────────┘
```

---

## Major Protocol Handlers

### Document Lifecycle

| LSP Method | Handler | Purpose |
|------------|---------|---------|
| `textDocument/didOpen` | `DidOpenHandler` | Document opened |
| `textDocument/didChange` | `DidChangeHandler` | Document edited |
| `textDocument/didClose` | `DidCloseHandler` | Document closed |

### Language Features

| LSP Method | Handler | Roslyn Service |
|------------|---------|----------------|
| `textDocument/completion` | `CompletionHandler` | `CompletionService` |
| `textDocument/hover` | `HoverHandler` | `QuickInfoService` |
| `textDocument/signatureHelp` | `SignatureHelpHandler` | `SignatureHelpService` |
| `textDocument/definition` | `GoToDefinitionHandler` | `IDefinitionService` |
| `textDocument/references` | `FindAllReferencesHandler` | `IFindReferencesService` |
| `textDocument/rename` | `RenameHandler` | `IRenameService` |
| `textDocument/codeAction` | `CodeActionsHandler` | `ICodeActionService` |
| `textDocument/formatting` | `FormatDocumentHandler` | `IFormattingService` |

### Diagnostics

| LSP Method | Handler | Purpose |
|------------|---------|---------|
| `textDocument/diagnostic` | `DocumentPullDiagnosticHandler` | File diagnostics |
| `workspace/diagnostic` | `WorkspacePullDiagnosticHandler` | Solution diagnostics |

---

## Architecture at a Glance

```
┌─────────────────────────────────────────────────────────────────────────┐
│                         RoslynLanguageServer                            │
│                                                                          │
│  ┌────────────────────────────────────────────────────────────────────┐ │
│  │                    RequestExecutionQueue                           │ │
│  │  • Serializes mutating requests (didChange, etc.)                  │ │
│  │  • Allows concurrent non-mutating requests                         │ │
│  └────────────────────────────────────────────────────────────────────┘ │
│                              │                                          │
│                              ▼                                          │
│  ┌────────────────────────────────────────────────────────────────────┐ │
│  │                       HandlerProvider                               │ │
│  │  • Discovers handlers via MEF                                      │ │
│  │  • Maps LSP methods to handlers                                    │ │
│  └────────────────────────────────────────────────────────────────────┘ │
│                              │                                          │
│                              ▼                                          │
│  ┌────────────────────────────────────────────────────────────────────┐ │
│  │                    RequestContextFactory                            │ │
│  │  • Creates RequestContext with Solution/Document                   │ │
│  │  • Synchronizes LSP document state                                 │ │
│  └────────────────────────────────────────────────────────────────────┘ │
│                              │                                          │
│                              ▼                                          │
│  ┌────────────────────────────────────────────────────────────────────┐ │
│  │                      LspWorkspaceManager                           │ │
│  │  • Tracks open LSP documents                                       │ │
│  │  • Manages workspace state                                         │ │
│  └────────────────────────────────────────────────────────────────────┘ │
└──────────────────────────────────┬──────────────────────────────────────┘
                                   │
                                   ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                         Roslyn Workspace                                │
│  • Solution / Project / Document model                                  │
│  • Language services (Completion, Hover, etc.)                         │
│  • Compiler APIs                                                       │
└─────────────────────────────────────────────────────────────────────────┘
```

For detailed architecture, see [Codebase Overview](./codebase_overview.md).

---

## Key Features

| Feature | Description |
|---------|-------------|
| **Completion** | IntelliSense with filtering and sorting |
| **Hover** | Symbol information on mouse hover |
| **Signature Help** | Parameter hints during method calls |
| **Go to Definition** | Navigate to symbol definitions |
| **Find References** | Find all usages across solution |
| **Rename** | Rename symbol across solution |
| **Code Actions** | Refactorings and fixes |
| **Formatting** | Code formatting |
| **Diagnostics** | Errors, warnings, suggestions |
| **Semantic Tokens** | Semantic syntax highlighting |
| **Inlay Hints** | Inline type annotations |

---

## What's NOT Covered Here

- **Features Implementation** — How features work internally; see [Features Overview](../features/product_overview.md)
- **VS Integration** — Visual Studio-specific; see [Editor Integration](../editor_integration/product_overview.md)
- **Protocol Specification** — See [LSP Spec](https://microsoft.github.io/language-server-protocol/)

---

## Related Documentation

**In This Overview:**
- [Codebase Overview](./codebase_overview.md) — Technical architecture and components
- [Main Overview](../main_overview.md) — Full codebase map
- [Glossary](../glossary.md) — Terminology

**Existing Codebase Docs:**
- [CLaSP Framework README](../../src/LanguageServer/Microsoft.CommonLanguageServerProtocol.Framework/README.md)
- [LSP Protocol README](../../src/LanguageServer/Protocol/Protocol/README.md)

**External:**
- [LSP Specification](https://microsoft.github.io/language-server-protocol/)

---

## Documentation Scope

This document explains why the Language Server layer exists and how LSP enables Roslyn in non-VS editors. It covers the architecture but does not detail all protocol handlers.

**What's covered:** LSP architecture, handler pattern, feature mapping

**What's not covered:** All handlers, protocol edge cases, CLaSP internals

**To go deeper:** See [Codebase Overview](./codebase_overview.md) for architecture. For more detail, start a new AI session using the [Expanding Documentation Prompt](https://github.com/CyrusNajmabadi/codebase-explorer/blob/main/LOADER.md#expanding-documentation-prompt).

**Parent document:** [Main Overview](../main_overview.md)
