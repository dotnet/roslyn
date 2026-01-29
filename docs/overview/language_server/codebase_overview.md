# Language Server: Codebase Overview

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
│                   LSP Client (VS Code)                      │
└────────────────────────────┬────────────────────────────────┘
                             │ JSON-RPC (stdio/named pipes)
                             ▼
┌─────────────────────────────────────────────────────────────┐
│                  RoslynLanguageServer                       │
│                                                             │
│  ┌───────────────────────────────────────────────────────┐  │
│  │ RequestExecutionQueue                                 │  │
│  │ • Serializes mutating requests (didChange, etc.)      │  │
│  │ • Allows concurrent non-mutating requests             │  │
│  │ • Ensures solution state consistency                  │  │
│  └───────────────────────────────────────────────────────┘  │
│                             │                               │
│                             ▼                               │
│  ┌───────────────────────────────────────────────────────┐  │
│  │ HandlerProvider                                       │  │
│  │ • Discovers handlers via MEF [Method("...")]          │  │
│  │ • Routes requests to appropriate handler              │  │
│  └───────────────────────────────────────────────────────┘  │
│                             │                               │
│                             ▼                               │
│  ┌───────────────────────────────────────────────────────┐  │
│  │ IMethodHandler implementations                        │  │
│  │ CompletionHandler, HoverHandler, RenameHandler, etc.  │  │
│  └───────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────┐
│                   LspWorkspaceManager                       │
│  • Tracks open LSP documents                                │
│  • Manages LspMiscellaneousFilesWorkspace                   │
│  • Creates Solution snapshots for LSP state                 │
└─────────────────────────────────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────┐
│                    Roslyn Workspace                         │
│  • Solution / Project / Document model                      │
│  • Language services (CompletionService, etc.)              │
│  • Compiler APIs                                            │
└─────────────────────────────────────────────────────────────┘
```

---

## Core Components

### Protocol (`src/LanguageServer/Protocol/`)

**What it is:** The main LSP server implementation for Roslyn.

**Key responsibilities:**
- Define LSP message handlers
- Map LSP requests to Roslyn services
- Manage document state synchronization
- Handle request execution ordering

**Key files/classes:**
- `RoslynLanguageServer.cs` — Main server class
- `Handler/` — LSP method handlers
- `LspWorkspaceManager.cs` — Document tracking

### CLaSP Framework (`src/LanguageServer/Microsoft.CommonLanguageServerProtocol.Framework/`)

**What it is:** The base framework for building LSP servers.

**Key responsibilities:**
- Abstract LSP server infrastructure
- Request routing and dispatching
- Handler lifecycle management
- Service container abstraction

**Key files/classes:**
- `AbstractLanguageServer<TRequestContext>.cs` — Base server
- `IRequestHandler<TRequest, TResponse>.cs` — Handler interface
- `RequestExecutionQueue.cs` — Request ordering

---

## Component Interactions

### Request Processing Flow

```
┌───────────────────────────────────────────────────────────────────────────┐
│                        Request Processing                                 │
│                                                                           │
│  1. JSON-RPC message arrives (e.g., textDocument/completion)             │
│                        │                                                  │
│                        ▼                                                  │
│  2. RoslynLanguageServer receives and deserializes                       │
│                        │                                                  │
│                        ▼                                                  │
│  3. RequestExecutionQueue determines ordering                            │
│     • Mutating request? → Wait for prior mutations                       │
│     • Non-mutating? → Run concurrently                                   │
│                        │                                                  │
│                        ▼                                                  │
│  4. HandlerProvider finds handler for method                             │
│     [Method("textDocument/completion")] → CompletionHandler              │
│                        │                                                  │
│                        ▼                                                  │
│  5. RequestContextFactory creates context                                │
│     • Gets current Solution snapshot                                     │
│     • Resolves Document from URI                                         │
│                        │                                                  │
│                        ▼                                                  │
│  6. Handler.HandleAsync() invoked                                        │
│     • Calls Roslyn services (CompletionService, etc.)                    │
│     • Converts Roslyn results to LSP types                               │
│                        │                                                  │
│                        ▼                                                  │
│  7. Response serialized and sent back                                    │
└───────────────────────────────────────────────────────────────────────────┘
```

### Document Synchronization

```
┌───────────────────────────────────────────────────────────────────────────┐
│                     Document Sync Flow                                    │
│                                                                           │
│  textDocument/didOpen                                                     │
│      │                                                                    │
│      ▼                                                                    │
│  LspWorkspaceManager.HandleDidOpenAsync()                                │
│      │                                                                    │
│      ├── Track document in TrackedDocumentInfo                           │
│      │                                                                    │
│      └── Update Workspace with new document text                         │
│                                                                           │
│  textDocument/didChange                                                   │
│      │                                                                    │
│      ▼                                                                    │
│  LspWorkspaceManager.HandleDidChangeAsync()                              │
│      │                                                                    │
│      ├── Apply text changes to tracked document                          │
│      │                                                                    │
│      └── Update Workspace.CurrentSolution                                │
│                                                                           │
│  textDocument/didClose                                                    │
│      │                                                                    │
│      ▼                                                                    │
│  LspWorkspaceManager.HandleDidCloseAsync()                               │
│      │                                                                    │
│      └── Remove from tracking, revert to disk version                    │
└───────────────────────────────────────────────────────────────────────────┘
```

---

## Data Model

### Key Entities

| Entity | Description | Purpose |
|--------|-------------|---------|
| `RequestContext` | Per-request context | Solution, Document, Capabilities |
| `TrackedDocumentInfo` | LSP document state | Text, version, language |
| `LspWorkspaceManager` | Document tracking | Sync LSP ↔ Roslyn |

### Handler Attributes

```csharp
[Method("textDocument/completion")]      // LSP method name
[LanguageServerEndpoint(typeof(...))]    // Endpoint type
```

---

## Technology Stack

| Category | Technology | Purpose |
|----------|------------|---------|
| Protocol | LSP 3.17 | Editor communication |
| Transport | JSON-RPC | Message format |
| Serialization | System.Text.Json | Fast JSON handling |
| DI | MEF | Handler discovery |

---

## Design Patterns

### Handler Pattern

Each LSP method has a handler:

```csharp
[Method("textDocument/hover")]
internal class HoverHandler : IRequestHandler<HoverParams, Hover?>
{
    private readonly IQuickInfoService _quickInfoService;
    
    public async Task<Hover?> HandleAsync(
        HoverParams request, 
        RequestContext context, 
        CancellationToken ct)
    {
        var document = context.Document;
        var position = ProtocolConversions.PositionToLinePosition(request.Position);
        
        var quickInfo = await _quickInfoService.GetQuickInfoAsync(document, position, ct);
        return ProtocolConversions.QuickInfoToHover(quickInfo);
    }
}
```

### Request Execution Queue

Controls request ordering:

```csharp
// Mutating requests (MutatesSolutionState = true) are serialized
[Method("textDocument/didChange")]
class DidChangeHandler : INotificationHandler<DidChangeTextDocumentParams>
{
    public bool MutatesSolutionState => true;
}

// Non-mutating requests run concurrently
[Method("textDocument/completion")]
class CompletionHandler : IRequestHandler<CompletionParams, CompletionList>
{
    public bool MutatesSolutionState => false;
}
```

---

## Configuration

### Server Capabilities

Server advertises capabilities on initialize:

```json
{
  "capabilities": {
    "completionProvider": { "triggerCharacters": [".", " "] },
    "hoverProvider": true,
    "definitionProvider": true,
    "referencesProvider": true,
    "renameProvider": { "prepareProvider": true }
  }
}
```

### Client Capabilities

Handler behavior adapts to client capabilities:

```csharp
if (context.ClientCapabilities.TextDocument?.Completion?.CompletionItem?.SnippetSupport == true)
{
    // Include snippet completions
}
```

---

## Internal Names

- **CLaSP** — Common Language Server Protocol Framework
- **RequestContext** — Per-request context (Solution, Document, Capabilities)
- **LspWorkspaceManager** — Manages LSP document tracking
- **TrackedDocumentInfo** — LSP document state (text, version, language)
- **MutatesSolutionState** — Flag indicating if handler modifies solution
- **RequiresLSPSolution** — Flag indicating if handler needs Solution/Document

See also: [../glossary.md](../glossary.md)

---

## Important Links

**External:**
- [LSP Specification](https://microsoft.github.io/language-server-protocol/)
- [JSON-RPC Spec](https://www.jsonrpc.org/specification)

**Internal Code:**
- `src/LanguageServer/Protocol/` — LSP implementation
- `src/LanguageServer/Microsoft.CommonLanguageServerProtocol.Framework/` — CLaSP

**Related Docs:**
- [Product Overview](./product_overview.md)
- [Glossary](../glossary.md)
- [Main Overview](../main_overview.md)

**Existing Codebase Docs:**
- [CLaSP Framework README](../../src/LanguageServer/Microsoft.CommonLanguageServerProtocol.Framework/README.md)

---

## Documentation Scope

This document provides a high-level architectural overview of the Language Server implementation. It covers request flow and handler patterns but does not detail each handler's implementation.

**What's covered:** Architecture, request execution, handler discovery, workspace integration

**What's not covered:** All handlers, protocol details, performance tuning

**To go deeper:** Start a new AI session using the [Expanding Documentation Prompt](https://github.com/CyrusNajmabadi/codebase-explorer/blob/main/LOADER.md#expanding-documentation-prompt).

**Parent document:** [Main Overview](../main_overview.md)
