# Workspaces: Product Overview

| | |
|---|---|
| **Last Updated** | January 29, 2026 |
| **Git SHA** | `771fe9b8443e955573725b4db6cc019685d8c2d4` |
| **Parent Doc** | [Main Overview](../main_overview.md) |

## The Story: Building an IDE That Understands Your Code

A developer opens a large solution with 50 projects in Visual Studio. They type a few characters in a file and expect:
- Instant syntax highlighting
- IntelliSense completion within milliseconds
- Real-time error squiggles as they type
- "Find All References" across the entire solution

**Before: The Challenge**

The compiler APIs (Compilation, SyntaxTree, etc.) are designed for single-point-in-time compilation. An IDE needs:
- To track changes as the user types, character by character
- To manage multiple projects with complex dependencies
- To keep semantic information up-to-date without recompiling everything
- To coordinate multiple features querying the same code simultaneously

Creating a Compilation for every keystroke would be impossibly slow.

**With Workspaces: The IDE View of Code**

Workspaces provide an abstraction over compilations optimized for IDE scenarios:

1. **Open a solution** — Workspaces load project structure from MSBuild:
   ```csharp
   var workspace = MSBuildWorkspace.Create();
   var solution = await workspace.OpenSolutionAsync("MySolution.sln");
   ```

2. **Track document changes** — Changes create new immutable snapshots efficiently:
   ```csharp
   var newSolution = solution.WithDocumentText(documentId, newText);
   workspace.TryApplyChanges(newSolution);
   ```

3. **Get semantic information** — Workspaces cache and reuse compilation data:
   ```csharp
   var document = solution.GetDocument(documentId);
   var semanticModel = await document.GetSemanticModelAsync();
   ```

4. **React to changes** — Subscribe to workspace events:
   ```csharp
   workspace.WorkspaceChanged += (sender, args) =>
   {
       if (args.Kind == WorkspaceChangeKind.DocumentChanged)
       {
           // Update UI for changed document
       }
   };
   ```

**Result**

The IDE maintains a live model of the entire solution. When the developer types, only the affected document's syntax tree is re-parsed. Semantic analysis is done incrementally. Multiple features (completion, diagnostics, navigation) can query the same snapshot concurrently without interference.

---

## Core Concepts

### Workspace

**What it is:** The root abstraction for managing solutions, projects, and documents.

**Why it matters:** Different hosts (Visual Studio, VS Code, command line) have different workspace implementations, but features code against the abstract `Workspace` API.

**Workspace types:**
- `MSBuildWorkspace` — Loads from .sln/.csproj files
- `AdhocWorkspace` — In-memory workspace for testing
- `VisualStudioWorkspace` — VS-specific integration
- `RemoteWorkspace` — Out-of-process mirror for ServiceHub

### Solution

**What it is:** An immutable snapshot of all projects and documents at a point in time.

**Why it matters:** Immutability enables safe concurrent access. Multiple features can analyze the same solution snapshot without locks.

**Key insight:** "Changing" a solution creates a new snapshot:
```csharp
// This doesn't modify solution, it returns a new one
var newSolution = solution.WithDocumentText(docId, newText);
```

### Project

**What it is:** A collection of documents with shared compilation options and references.

**Why it matters:** Projects define compilation boundaries—how documents are grouped for semantic analysis.

**Key properties:**
- `Documents` — Source code files
- `AdditionalDocuments` — Non-source files (config, etc.)
- `MetadataReferences` — Assembly references
- `ProjectReferences` — References to other projects
- `CompilationOptions` — How to compile

### Document

**What it is:** A source file within a project.

**Why it matters:** Documents are the granularity of change tracking and lazy evaluation.

**Key APIs:**
- `GetSyntaxTreeAsync()` — Parsed syntax
- `GetSemanticModelAsync()` — Semantic analysis
- `GetTextAsync()` — Source text

---

## Key Features

| Feature | Description | When to Use |
|---------|-------------|-------------|
| **Solution Loading** | Load from MSBuild projects | IDE startup, CLI tools |
| **Change Tracking** | Efficient snapshots | Real-time editing |
| **Lazy Evaluation** | Compute on demand | Performance optimization |
| **Event System** | React to changes | UI updates, background analysis |
| **Remote Execution** | Out-of-process analysis | Performance isolation |

---

## Architecture at a Glance

```
┌─────────────────────────────────────────────────────────────┐
│                    Workspace (abstract)                     │
│  • CurrentSolution: Solution                                │
│  • Services: HostWorkspaceServices                          │
│  • Events: WorkspaceChanged                                 │
└──────────────────────────┬──────────────────────────────────┘
                           │
          ┌────────────────┼────────────────┐
          │                │                │
     ┌────▼────┐     ┌─────▼─────┐    ┌─────▼─────┐
     │ MSBuild │     │   Host    │    │  Remote   │
     │Workspace│     │ Workspace │    │ Workspace │
     └─────────┘     └───────────┘    └───────────┘
          │
          ▼
┌─────────────────────────────────────────────────────────────┐
│                   Solution (immutable)                      │
│  • Projects: Project[]                                      │
│  • GetDocument(DocumentId): Document                        │
│  • WithDocumentText(): Solution (new snapshot)              │
└──────────────────────────┬──────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────┐
│                   Project (immutable)                       │
│  • Documents: Document[]                                    │
│  • GetCompilationAsync(): Compilation                       │
└──────────────────────────┬──────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────┐
│                   Document (immutable)                      │
│  • GetSyntaxTreeAsync(): SyntaxTree                         │
│  • GetSemanticModelAsync(): SemanticModel                   │
└─────────────────────────────────────────────────────────────┘
```

For detailed architecture, see [Codebase Overview](./codebase_overview.md).

---

## Common Use Cases

### Loading a Solution

**Scenario:** Analyze a solution from the file system

**Solution:**
```csharp
using var workspace = MSBuildWorkspace.Create();
var solution = await workspace.OpenSolutionAsync("MySolution.sln");

foreach (var project in solution.Projects)
{
    var compilation = await project.GetCompilationAsync();
    var diagnostics = compilation.GetDiagnostics();
    // Process diagnostics...
}
```

### Tracking Document Changes

**Scenario:** Update a document and get new semantic information

**Solution:**
```csharp
// Get document
var document = solution.GetDocument(documentId);

// Create new text
var text = await document.GetTextAsync();
var newText = text.Replace(span, newContent);

// Apply change (creates new snapshot)
var newSolution = solution.WithDocumentText(documentId, newText);
var newDocument = newSolution.GetDocument(documentId);
var newSemanticModel = await newDocument.GetSemanticModelAsync();
```

### Reacting to Changes

**Scenario:** Update UI when documents change

**Solution:**
```csharp
workspace.WorkspaceChanged += async (sender, args) =>
{
    switch (args.Kind)
    {
        case WorkspaceChangeKind.DocumentChanged:
            var document = args.NewSolution.GetDocument(args.DocumentId);
            await RefreshDiagnosticsAsync(document);
            break;
            
        case WorkspaceChangeKind.ProjectAdded:
            await LoadProjectAsync(args.ProjectId);
            break;
    }
};
```

---

## What's NOT Covered Here

- **Compiler APIs** — Compilation, SyntaxTree, etc.; see [Compilers Overview](../compilers/product_overview.md)
- **IDE Features** — Code completion, refactoring; see [Features Overview](../features/product_overview.md)
- **Remote Services** — ServiceHub and out-of-process execution; see Codebase Overview

---

## Related Documentation

**In This Overview:**
- [Codebase Overview](./codebase_overview.md) — Technical architecture and components
- [Main Overview](../main_overview.md) — Full codebase map
- [Glossary](../glossary.md) — Terminology

**Existing Codebase Docs:**
- [Roslyn Overview](../../wiki/Roslyn-Overview.md) — Official architecture (see "Working with a Workspace")
- [Workspace and Source Generated Documents](../../ide/api-designs/Workspace%20and%20Source%20Generated%20Documents.md)

---

## Documentation Scope

This document explains why the Workspaces layer exists and what problems it solves. It provides context for understanding the workspace model but does not cover implementation details.

**What's covered:** Workspace concept, solution/project/document model, change tracking

**What's not covered:** Implementation details, all workspace types, remote workspace internals

**To go deeper:** See [Codebase Overview](./codebase_overview.md) for architecture. For more detail, start a new AI session using the [Expanding Documentation Prompt](https://github.com/CyrusNajmabadi/codebase-explorer/blob/main/LOADER.md#expanding-documentation-prompt).

**Parent document:** [Main Overview](../main_overview.md)
