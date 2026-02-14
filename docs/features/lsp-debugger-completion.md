# LSP Debugger Completion

## Overview

This document describes a design for supporting IntelliSense completion in debugger expression
windows (Quick Watch, Watch, Immediate) via the debug adapter and Roslyn LSP. This improves the
debugging experience when using the [C# Dev Kit extension for VS Code](https://github.com/microsoft/vscode-dotnettools).

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
   - The user's debugger input (Quick Watch text) is inserted at a calculated position
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

### How VS Code implements debugger completion

VS Code uses the DAP (Debug Adapter Protocol) for debugger communication.  
But the debug adapter for C# in VS Code lacks support for the
[`completions` request](https://microsoft.github.io/debug-adapter-protocol/specification#Requests_Completions).  
This proposal makes it possible to support the 'completions' request by delegating the task to Roslyn.

## Proposed Design

The proposal is to add `roslyn/debuggerCompletion`, an LSP endpoint that provides completion items for debugger expressions.
Given the context of a document, position and expression being edited, the server will return completion items that are relevant to the debugging context.

### End-to-end flow

#### 1. Completion triggered

After a breakpoint is hit, the user types partial expression in Quick Watch / Watch / Immediate window.

The client (VS Code or Quick Watch) sends DAP [`completions` request](https://microsoft.github.io/debug-adapter-protocol/specification#Requests_Completions).  

For example:
```json
{
  "frameId": 1000,
  "text": "myCustomer.Add",
  "column": 15,
  "line": 1
}
```

Note: the frameId and line are optional.

#### 2. Debug adapter handles DAP request

The debug adapter processes the DAP `completions` request.  
It identifies the source file and statement range from the current stack frame.  
It converts the position from DAP (line/column) to LSP (character offset).  
It sends a `roslyn/debuggerCompletion` LSP request to the Roslyn server.  

For example:

```json
{
  "textDocument": { "uri": "file:///src/MyClass.cs" },
  "statementRange": {
    "start": { "line": 42, "character": 8 },
    "end": { "line": 42, "character": 8 }
  },
  "expression": "myCustomer.Add",
  "cursorOffset": 14,
  "context": { "triggerKind": 1 }
}
```

#### 3. LSP server handles debug completion request

1. Resolves document from workspace
2. Creates spliced source text with expression inserted
3. Forks solution with spliced document
4. Calls `CompletionHandler.GetCompletionListAsync()`
5. Returns a `VSInternalCompletionList`

For example, given the document and breakpoint statement:
```csharp
string hello = "Hello";
[|Console.Write(hello);|]
```
and given the expression and cursor offset: `hel$`, the completion would be processed against the spliced text:
```csharp
string hello = "Hello";
hel$; Console.Write(hello);
```

The server returns a `VSInternalCompletionList`:
```json
{
  "isIncomplete": false,
  "items": [
    {
      "label": "hello",
      "kind": 6,
      "filterText": "hello",
      "insertText": "hello",
      "data": { "resultId": 1 }
    }
  ],
  "_vs_suggestionMode": false
}
```

#### 4. Completions flow back

The client receives completion items and shows them in the UI.

Items include:
- Label, kind (method/property/field)
- Insert text, filter text
- Optional: documentation (if pre-resolved)

The client may make additional requests to the
[`completionItem/resolve`](https://microsoft.github.io/language-server-protocol/specifications/lsp/3.17/specification/#completionItem_resolve)
LSP method for item details (e.g., documentation) if not pre-resolved.

For example, a `completionItem/resolve` request for the item above would return:
```json
{
  "label": "hello",
  "kind": 6, // a variable
  "filterText": "hello",
  "insertText": "hello",
  "detail": "(local variable) string hello",
  "documentation": {
    "kind": "plaintext",
    "value": ""
  },
  "data": { "resultId": 1 }
}
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
   */
  statementRange: Range;

  /**
   * The debugger expression input (Quick Watch/Immediate text).
   */
  expression: string;

  /**
   * Caret offset within `expression`, in UTF-16 code units.
   */
  cursorOffset: number;

  /**
   * The completion context. This is only available if the client specifies
   * to send this using the client capability
   * `completion.contextSupport === true`
   */
  context?: CompletionContext;
}
```

More details on the [`CompletionContext`](https://microsoft.github.io/language-server-protocol/specifications/specification-current/#completionContext) type.

### Response Contract

Returns `VSInternalCompletionList` which Roslyn LSP returns for `textDocument/completion` requests.
Note that `VSInternalCompletionList` is a sub-class of `CompletionList`, which is a possible response specified for `textDocument/completion`.

This allows reuse of:
- Completion list caching (`CompletionListCache`)
- Item resolve (`completionItem/resolve`)

## Limitations

1. Visual Basic support is out-of-scope.
  Visual Basic has debugger completion support in Visual Studio (via `VisualBasicDebuggerIntelliSenseContext`),
  but the C# DevKit does not need VB support.

2. Haven't planned or prototyped Signature Help support yet.
  For reference, the LSP spec includes a 
  [`textDocument/signatureHelp`](https://microsoft.github.io/language-server-protocol/specifications/lsp/3.17/specification/#textDocument_signatureHelp)
  method.

3. VS Code lacks debug completion in Watch window. This could be independently added in the future
   (it would rely on DAP just like Debug Console does).

