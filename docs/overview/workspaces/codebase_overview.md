# Workspaces: Codebase Overview

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
│                  Workspace Implementations                  │
│                                                             │
│  ┌────────────┐   ┌──────────────┐   ┌──────────────┐       │
│  │  MSBuild   │   │ VisualStudio │   │    Remote    │       │
│  │ Workspace  │   │  Workspace   │   │  Workspace   │       │
│  └─────┬──────┘   └──────┬───────┘   └──────┬───────┘       │
│        │                 │                  │               │
│        └─────────────────┼──────────────────┘               │
│                          │                                  │
│                          ▼                                  │
│  ┌───────────────────────────────────────────────────────┐  │
│  │               Workspace (abstract)                    │  │
│  │  • SetCurrentSolution() — Apply solution changes      │  │
│  │  • TryApplyChanges() — Apply and persist changes      │  │
│  │  • Services — HostWorkspaceServices                   │  │
│  └───────────────────────────────────────────────────────┘  │
└────────────────────────────┬────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────┐
│                         Solution                            │
│  ┌───────────────────────────────────────────────────────┐  │
│  │ SolutionState (internal, immutable)                   │  │
│  │  • ProjectStates: ImmutableDictionary<ProjectId, ...> │  │
│  │  • Analyzer/Generator state                           │  │
│  └───────────────────────────────────────────────────────┘  │
│  ┌───────────────────────────────────────────────────────┐  │
│  │ SolutionCompilationState (internal)                   │  │
│  │  • Compilation caching                                │  │
│  │  • Generator outputs                                  │  │
│  └───────────────────────────────────────────────────────┘  │
└────────────────────────────┬────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────┐
│                          Project                            │
│  ┌───────────────────────────────────────────────────────┐  │
│  │ ProjectState (internal, immutable)                    │  │
│  │  • DocumentStates: TextDocumentStates<DocumentState>  │  │
│  │  • CompilationOptions, ParseOptions                   │  │
│  │  • References (metadata, project, analyzer)           │  │
│  └───────────────────────────────────────────────────────┘  │
└────────────────────────────┬────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────┐
│                         Document                            │
│  ┌───────────────────────────────────────────────────────┐  │
│  │ DocumentState (internal, immutable)                   │  │
│  │  • SourceText (lazy)                                  │  │
│  │  • SyntaxTree (lazy)                                  │  │
│  └───────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
```

---

## Core Components

### Workspace Core (`src/Workspaces/Core/Portable/`)

**What it is:** The core workspace model and abstractions.

**Key responsibilities:**
- Define `Workspace`, `Solution`, `Project`, `Document` hierarchy
- Manage immutable state snapshots
- Provide workspace services infrastructure
- Handle solution/project change tracking

**Key files/classes:**
- `Workspace/Workspace.cs` — Abstract workspace base
- `Workspace/Solution.cs` — Solution wrapper (public API)
- `Workspace/Project.cs` — Project wrapper (public API)
- `Workspace/Document.cs` — Document wrapper (public API)
- `Workspace/SolutionState.cs` — Internal immutable state
- `Workspace/ProjectState.cs` — Internal project state
- `Workspace/DocumentState.cs` — Internal document state

### MSBuild Workspace (`src/Workspaces/MSBuild/`)

**What it is:** Workspace implementation that loads from MSBuild projects.

**Key responsibilities:**
- Load .sln and .csproj/.vbproj files
- Use MSBuild APIs to resolve project properties
- Convert MSBuild data to Roslyn ProjectInfo
- Support applying changes back to project files

**Key files/classes:**
- `MSBuildWorkspace.cs` — Main entry point
- `MSBuildProjectLoader.cs` — Project file parsing
- `BuildHost/` — Out-of-process MSBuild execution

### Remote Workspace (`src/Workspaces/Remote/`)

**What it is:** Out-of-process workspace for ServiceHub services.

**Key responsibilities:**
- Mirror workspace state in remote process
- Synchronize via checksums (content-addressable)
- Provide solution to ServiceHub services

**Key files/classes:**
- `RemoteWorkspace.cs` — Remote workspace implementation
- `AssetProvider.cs` — Asset synchronization
- `SolutionCreator.cs` — Builds solutions from assets

### Shared Utilities (`src/Workspaces/SharedUtilitiesAndExtensions/`)

**What it is:** Utilities shared between Workspaces and Features.

**Key responsibilities:**
- Syntax facts and helpers
- Code generation utilities
- Formatting infrastructure
- Extension methods

---

## Component Interactions

### Solution Change Flow

```
┌──────────────────────────────────────────────────────────────────────┐
│                        Change Flow                                   │
│                                                                      │
│  1. User/System initiates change (e.g., edit document)               │
│         │                                                            │
│         ▼                                                            │
│  2. Call solution.With*() method                                     │
│     var newSolution = solution.WithDocumentText(docId, newText);     │
│         │                                                            │
│         ▼                                                            │
│  3. New immutable Solution created                                   │
│     (shares unchanged data with old solution)                        │
│         │                                                            │
│         ▼                                                            │
│  4. Workspace.SetCurrentSolution(newSolution)                        │
│         │                                                            │
│         ▼                                                            │
│  5. WorkspaceChanged event raised                                    │
│     WorkspaceChangeEventArgs { Kind, DocumentId, OldSolution, ... }  │
│         │                                                            │
│         ▼                                                            │
│  6. Subscribers notified (analyzers, features, UI)                   │
└──────────────────────────────────────────────────────────────────────┘
```

### Lazy Evaluation Pattern

Documents don't parse/analyze until requested:

```csharp
// Document exists but nothing computed yet
var document = solution.GetDocument(documentId);

// First call triggers parsing
var syntaxTree = await document.GetSyntaxTreeAsync();

// First call triggers binding
var semanticModel = await document.GetSemanticModelAsync();

// Subsequent calls return cached results
var syntaxTree2 = await document.GetSyntaxTreeAsync(); // Cached
```

### State vs Wrapper Pattern

```
┌─────────────────────────────────────────────────────────────────────┐
│                     Public API (Wrappers)                           │
│                                                                     │
│  Solution ─────┐                                                    │
│  Project  ─────┼── Provide public API                               │
│  Document ─────┘   Hide internal state                              │
│                                                                     │
└──────────────────────────────────┬──────────────────────────────────┘
                                   │ wraps
                                   ▼
┌─────────────────────────────────────────────────────────────────────┐
│                   Internal State (Immutable)                        │
│                                                                     │
│  SolutionState ─────┐                                               │
│  ProjectState  ─────┼── Immutable data containers                   │
│  DocumentState ─────┘   Efficient sharing between snapshots         │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

---

## Data Model

### Key Entities

| Entity | Description | Internal State |
|--------|-------------|----------------|
| `Workspace` | Root manager | N/A (abstract) |
| `Solution` | All projects snapshot | `SolutionState` |
| `Project` | Document collection | `ProjectState` |
| `Document` | Source file | `DocumentState` |
| `TextDocument` | Non-source document | `TextDocumentState` |

### ID System

Every entity has a unique ID:

| ID Type | Purpose |
|---------|---------|
| `SolutionId` | Identifies solution instance |
| `ProjectId` | Identifies project within solution |
| `DocumentId` | Identifies document within project |

IDs are stable across solution snapshots, allowing tracking of entities through changes.

---

## Technology Stack

| Category | Technology | Purpose |
|----------|------------|---------|
| Collections | `ImmutableDictionary`, `ImmutableArray` | Thread-safe state |
| Async | `Task<T>`, `ValueTask<T>` | Lazy evaluation |
| Serialization | System.Text.Json | Remote workspace sync |
| IPC | StreamJsonRpc | ServiceHub communication |

---

## Design Patterns

### Immutable Snapshot Pattern

All state is immutable. Changes create new instances:

```csharp
// Old solution is unchanged
var oldSolution = workspace.CurrentSolution;

// New solution shares unchanged projects
var newSolution = oldSolution.WithDocumentText(docId, newText);

// Both solutions can be used concurrently
var oldDoc = oldSolution.GetDocument(docId);
var newDoc = newSolution.GetDocument(docId);
```

### Service Locator Pattern

Services accessed via workspace:

```csharp
var formattingService = workspace.Services.GetService<IFormattingService>();
var languageService = document.GetLanguageService<ICompletionService>();
```

### Event-Driven Updates

Changes broadcast via events:

```csharp
workspace.WorkspaceChanged += (s, e) =>
{
    switch (e.Kind)
    {
        case WorkspaceChangeKind.DocumentChanged:
            // Handle document change
            break;
        case WorkspaceChangeKind.ProjectAdded:
            // Handle new project
            break;
    }
};
```

---

## Configuration

### Workspace Options

| Option | Purpose |
|--------|---------|
| `SolutionCrawlerWorkspaceServiceFactory` | Background analysis |
| `HostDiagnosticAnalyzerService` | Analyzer hosting |
| `GeneratorDriver` | Source generator execution |

### MSBuild Workspace Options

| Option | Purpose |
|--------|---------|
| `LoadMetadataForReferencedProjects` | Full load vs metadata-only |
| `SkipUnrecognizedProjects` | Handle unknown project types |
| `AssociateFileExtensionWithLanguage` | Custom file associations |

---

## Internal Names

- **SolutionState** — Immutable internal state for Solution
- **ProjectState** — Immutable internal state for Project
- **DocumentState** — Immutable internal state for Document
- **SolutionCompilationState** — Manages compilation caching
- **Checksum** — Content hash for remote synchronization
- **Asset** — Serialized workspace component for remote transfer
- **InFlightSolution** — Solution being computed in remote workspace

See also: [../glossary.md](../glossary.md)

---

## Important Links

**Internal Code:**
- `src/Workspaces/Core/Portable/` — Core workspace model
- `src/Workspaces/MSBuild/` — MSBuild integration
- `src/Workspaces/Remote/` — Remote workspace

**Related Docs:**
- [Product Overview](./product_overview.md)
- [Glossary](../glossary.md)
- [Main Overview](../main_overview.md)

**Existing Codebase Docs:**
- [Roslyn Overview](../../wiki/Roslyn-Overview.md) — See "Working with a Workspace" section

---

## Documentation Scope

This document provides a high-level architectural overview of the Workspaces layer. It covers major components and state management but does not detail all workspace implementations.

**What's covered:** Architecture, state management patterns, MSBuild/Remote workspace concepts

**What's not covered:** All workspace types, serialization details, ServiceHub internals

**To go deeper:** Start a new AI session using the [Expanding Documentation Prompt](https://github.com/CyrusNajmabadi/codebase-explorer/blob/main/LOADER.md#expanding-documentation-prompt).

**Parent document:** [Main Overview](../main_overview.md)
