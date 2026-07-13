---
coverage: System overview — components, layers, data flow
---

# Architecture

Roslyn is the .NET Compiler Platform: the open-source C# and Visual Basic compilers plus the language services and IDE features built on their public APIs. Everything is built around **immutable** syntax trees, semantic models, symbols, and workspace/solution snapshots, exposed through rich analysis and code-generation APIs.

## Components

Layered bottom-up (lower layers must not depend on higher layers — see `docs/Layering.md`):

| Component | Location | Purpose |
|-----------|----------|---------|
| **Compilers** | `src/Compilers/` | C# and VB compilers: syntax trees, semantic models, symbols, emit. `Core` is shared; `CSharp` and `VisualBasic` are per-language. |
| **Workspaces** | `src/Workspaces/` | Solution/Project/Document object model, host services, MEF composition. Language-agnostic core + per-language layers. |
| **Features** | `src/Features/` | Language-agnostic IDE features: refactorings, completion, code fixes, navigation. |
| **Analyzers** | `src/Analyzers/` | IDE code-style diagnostic analyzers and fixes (IDE0xxx). |
| **CodeStyle** | `src/CodeStyle/` | Code-style analyzer packaging shared with the compiler/command-line. |
| **EditorFeatures** | `src/EditorFeatures/` | Editor/text-buffer integration of Features. |
| **LanguageServer** | `src/LanguageServer/` | LSP server consumed by the VS Code C# extension and `roslyn-language-server`. |
| **VisualStudio** | `src/VisualStudio/` | VS-specific language services and UI integration. |
| **Razor** | `src/Razor/src/` | Razor compiler + Razor IDE tooling (merged from `dotnet/razor`; keeps its own internal layout). |
| **ExpressionEvaluator** | `src/ExpressionEvaluator/` | Debugger expression evaluator built on the compilers. |
| **Scripting / Interactive** | `src/Scripting/`, `src/Interactive/` | C#/VB scripting and REPL. |
| **RoslynAnalyzers** | `src/RoslynAnalyzers/` | The `Microsoft.CodeAnalysis.*` analyzer packages (formerly dotnet/roslyn-analyzers). |

For per-layer directory detail, key files, and coding conventions, read the matching path-scoped instruction file (`.github/instructions/{Compiler,IDE,Razor}.instructions.md`). That layer's known issues and test conventions live in `.github/memory/known-issues/<area>.md` and `.github/memory/testing/<area>.md` — load on demand (see `INDEX.md`).

## Key Abstractions

- **Immutability everywhere.** `SyntaxTree`, `Compilation`, `SemanticModel`, `Solution`, `Project`, `Document` are immutable. Mutations produce new instances via `With*`/`Add*` methods. The `Workspace` holds the current `Solution` snapshot.
- **MEF composition.** Services are exported per-language with `[ExportLanguageService(typeof(IService), LanguageNames.CSharp), Shared]` and imported via `[ImportingConstructor]` (+ obsolete-guard). Tests that need MEF use `[UseExportProvider]`.
- **Red/green syntax trees.** Public "red" syntax nodes wrap an internal immutable "green" node tree for cheap incremental reuse.
- **Symbols & bound nodes.** Semantic analysis binds syntax to `ISymbol`/bound nodes; `SemanticModel` is the query entry point. Always thread `CancellationToken`.
- **Code generation from XML.** Syntax (`Syntax.xml`) and bound (`BoundNodes.xml`) node classes are generated — never hand-edit the generated `.cs`.

## Data Flow

Primary compile path:
1. Source text → lexer/parser → `SyntaxTree` (immutable).
2. `Compilation` created from syntax trees + references; binders produce symbols and bound trees.
3. `SemanticModel` answers semantic queries (`GetSymbolInfo`, `GetTypeInfo`, diagnostics).
4. Emit phase lowers bound trees and writes IL/PE + PDB.

Primary IDE path:
1. `Workspace` exposes the current `Solution` snapshot.
2. A feature gets a `Document`, asks for its `SyntaxTree`/`SemanticModel` (`await document.GetSemanticModelAsync(ct)`).
3. The feature computes changes and returns a new `Document`/`Solution` (immutable update), which the host applies.

## Active vs. Legacy

| Area | Status | Notes |
|------|--------|-------|
| `src/Compilers`, `src/Workspaces`, `src/Features`, `src/LanguageServer` | Active | Primary development. |
| `src/Razor` | Active | Merged sub-tree; follow `.github/instructions/Razor.instructions.md`. |
| `eng/common`, `eng/config/globalconfigs` | Generated/Managed | Synced by DARC/Arcade — do not hand-edit `eng/common`. |
| Generated `*.Generated.cs`, `Syntax.xml`-derived files | Generated | Regenerate via `dotnet run --file eng/generate-compiler-code.cs`. |
