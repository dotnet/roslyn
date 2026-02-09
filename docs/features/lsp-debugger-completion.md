# LSP Debugger Completion for QuickWatch

## Overview

This document describes a design for supporting IntelliSense completion in debugger expression
windows (QuickWatch, Watch, Immediate) via LSP. This enables the
[C# extension for VS Code](https://github.com/dotnet/vscode-csharp) to provide the same
completion experience as Visual Studio.

## Background

### How Visual Studio Implements Debugger Completion

VS uses a specialized integration path that does not go through LSP:

1. The debugger calls `IVsImmediateStatementCompletion2.SetCompletionContext(...)` to tell Roslyn:
   - `filePath`: The source file path
   - `buffer`: A buffer (`IVsTextLines`) representing the source context
   - `currentStatementSpan`: The statement span where the debugger is stopped
   - `punkContext`: An optional context object (currently unused by Roslyn)
   - `textView`: The debugger text view

2. Roslyn creates a "spliced" view of the document using VS Editor projection buffers:
   - The real source document text is wrapped in a read-only projection
   - The user's debugger input (QuickWatch text) is inserted at a calculated position
   - Separators (`;`) are added to make the result parse as valid C#

3. Roslyn forks the workspace solution:
   ```csharp
   var forkedSolution = solution.WithDocumentText(
       document.Id,
       _projectionBuffer.CurrentSnapshot.AsText(),
       PreservationMode.PreserveIdentity);
   ```
   This creates an isolated snapshot where the document contains the spliced text.

4. A `DebuggerIntelliSenseWorkspace` hosts the forked solution and the document is opened against the projection buffer.

5. Completion runs against this forked document using standard Roslyn completion services.

### How VS Code Implements Debugger Completion

VS Code uses the DAP (Debug Adapter Protocol) for debugger communication.  
But the debug adapter for C# in VS Code lacks support for the
['completions' request](https://microsoft.github.io/debug-adapter-protocol/specification#Requests_Completions).  
This proposal makes it possible to support the 'completions' request by delegating the task to Roslyn.

## Proposed Design

The proposal is to add `roslyn/debuggerCompletion`, an LSP endpoint that provides completion items for debugger expressions.
Given the context of a document, position and expression being edited, the server will return completion items that are relevant to the debugging context.

### End-to-End Flow (Client Perspective)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  1. BREAKPOINT HIT                                                          │
│     ─────────────────                                                       │
│     • Debugger stops execution at a breakpoint                              │
│     • Debug Adapter Protocol (DAP) notifies VSCode of stopped state         │
│     • VSCode receives stack frame info:                                     │
│       - Source file path (e.g., "src/MyClass.cs")                           │
│       - Line/column of instruction pointer (IP)                             │
│       - Frame ID for evaluation context                                     │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│  2. USER OPENS QUICKWATCH / WATCH / IMMEDIATE WINDOW                        │
│     ────────────────────────────────────────────────────────────────        │
│     • User invokes QuickWatch or types in Watch window                      │
│     • C# extension captures:                                                │
│       - Document URI from the stopped frame                                 │
│       - IP location (line/column) as the "statement range"                  │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│  3. USER TYPES EXPRESSION                                                   │
│     ─────────────────────                                                   │
│     • User types partial expression, e.g., "myCustomer.Add"                 │
│     • Completion triggered by:                                              │
│       - Typing a trigger character (`.`)                                    │
│       - Explicit invocation (Ctrl+Space)                                    │
│       - Typing identifier characters (implicit trigger)                     │
│     • Extension captures cursor position within the expression              │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│  4. CLIENT SENDS LSP REQUEST                                                │
│     ─────────────────────────                                               │
│     • C# extension intercepts DAP 'completions' requests via a proxy        │
│     • Proxy fulfills them by calling `roslyn/debuggerCompletion`:           │
│                                                                             │
│       {                                                                     │
│         "textDocument": { "uri": "file:///src/MyClass.cs" },                │
│         "statementRange": {                                                 │
│           "start": { "line": 42, "character": 8 },                          │
│           "end": { "line": 42, "character": 8 }                             │
│         },                                                                  │
│         "expression": "myCustomer.Add",                                     │
│         "cursorOffset": 14,                                                 │
│         "context": { "triggerKind": 1 }                                     │
│       }                                                                     │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│  5. SERVER PROCESSES REQUEST                                                │
│     ─────────────────────────                                               │
│                                                                             │
│     a) Resolve document from workspace                                      │
│     b) Create spliced source text with expression inserted                  │
│     c) Fork solution with spliced document                                  │
│     d) Call CompletionHandler.GetCompletionListAsync()                      │
│     e) Return VSInternalCompletionList                                      │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│  6. CLIENT RECEIVES RESPONSE                                                │
│     ─────────────────────────                                               │
│     • Extension receives completion list with items like:                   │
│       - "Address", "AddOrder", "AddPayment", etc.                           │
│     • Items include:                                                        │
│       - Label, kind (method/property/field)                                 │
│       - Insert text, filter text                                            │
│       - Optional: documentation (if pre-resolved)                           │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│  7. USER SELECTS COMPLETION                                                 │
│     ─────────────────────────                                               │
│     • User navigates list and selects "AddOrder"                            │
│     • Extension applies the insert text to the expression input             │
│     • Expression becomes: "myCustomer.AddOrder"                             │
│     • User can continue typing or evaluate the expression                   │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│  8. EXPRESSION EVALUATION (separate flow)                                   │
│     ───────────────────────────────────────                                 │
│     • User presses Enter or clicks Evaluate                                 │
│     • Expression sent to debugger's expression evaluator via DAP            │
│     • Result displayed in QuickWatch/Watch window                           │
│     • (This step uses DAP, not LSP - out of scope for this design)          │
└─────────────────────────────────────────────────────────────────────────────┘
```

## LSP Method

**Method Name**: `roslyn/debuggerCompletion`

**Direction**: Client → Server

### Request Contract

```typescript
interface DebuggerCompletionParams {
  /**
   * The source document corresponding to the stopped stack frame.
   */
  textDocument: TextDocumentIdentifier;

  /**
   * The "current statement" range in the source document.
   * Server uses `statementRange.end` as the context/anchor point.
   * Can be a zero-width range (point) if only IP position is known.
   */
  statementRange: Range;

  /**
   * The debugger expression input (QuickWatch/Immediate text).
   */
  expression: string;

  /**
   * Caret offset within `expression`, in UTF-16 code units.
   */
  cursorOffset: number;

  /**
   * Optional: standard LSP completion context (trigger kind/character).
   */
  context?: CompletionContext;
}
```

### Response Contract

Returns the same type as `textDocument/completion`:

```typescript
type DebuggerCompletionResponse = VSInternalCompletionList | null;
```

This allows reuse of:
- Completion list caching (`CompletionListCache`)
- Item resolve (`completionItem/resolve`)

If `context.Document` is null (file not in workspace), return `null` (empty completion list).  
If syntax root cannot be obtained or splicing logic throws, catch and return `null`. Log the error for diagnostics.  
If `cursorOffset < 0` or `cursorOffset > expression.Length`, return `null`.  

## Limitations

1. Visual Basic not supported: The LSP implementation only supports C#. Visual Basic has debugger
   completion support in Visual Studio (via `VisualBasicDebuggerIntelliSenseContext`), but the
   C# extension for VS Code does not need VB support, so no `IDebuggerSplicer` implementation is
   provided for VB. If a VB document is passed, the handler returns `null`.

2. Haven't prototyped Signature Help yet.

3. VS Code lacks debug completion in Watch window. This could be independently added in the future
   (it would rely on DAP just like Debug Console does).
