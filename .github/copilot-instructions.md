# Roslyn (.NET Compiler Platform) — Copilot Instructions

> This is the **canonical** repo-wide agent entry point. `AGENTS.md` at the repo root points here. Path-scoped rules in `.github/instructions/{Compiler,IDE,Razor}.instructions.md` apply automatically by area and supplement this file. This file establishes the memory-first orientation protocol and doc-maintenance obligation.

## Project Overview

Roslyn is the open-source C# and Visual Basic compilers plus the language services and IDE features built on their APIs. Built around **immutable** syntax trees, semantic models, symbols, and workspace snapshots. Major components:
- **Compilers** (`src/Compilers/`) — C#/VB compilers (syntax, semantics, emit).
- **Workspaces** (`src/Workspaces/`) — Solution/Project/Document model + MEF host.
- **Features / EditorFeatures** (`src/Features/`, `src/EditorFeatures/`) — IDE features.
- **Analyzers / CodeStyle** (`src/Analyzers/`, `src/CodeStyle/`) — IDE0xxx diagnostics & fixes.
- **LanguageServer** (`src/LanguageServer/`) — LSP server.
- **VisualStudio** (`src/VisualStudio/`) — VS integration.
- **Razor** (`src/Razor/src/`) — Razor compiler & tooling (merged sub-tree).

## Project Structure

```
src/
  Compilers/      # C#/VB compilers (Core, CSharp, VisualBasic, Server)
  Workspaces/     # Solution model, MSBuild loading, Remote (OOP)
  Features/       # Language-agnostic IDE feature logic
  EditorFeatures/ # Editor/text-buffer integration
  Analyzers/      # IDE0xxx code-style analyzers & fixes
  LanguageServer/ # LSP server
  VisualStudio/   # VS language services & UI
  Razor/src/      # Razor compiler + tooling (own layout)
  ExpressionEvaluator/  Scripting/  Interactive/  RoslynAnalyzers/
eng/              # Arcade build engineering (eng/common is DARC-synced)
docs/             # Contributor & design docs
```

## Build & Test

### Build specific projects during development (preferred)
```bash
dotnet build Compilers.slnf      # compilers only
dotnet build Ide.slnf            # IDE only
dotnet build Razor.slnf          # Razor compiler & tooling only
dotnet build <path/to/Project.csproj>
```

### Run tests for modified code
```bash
dotnet test <path/to/Specific.UnitTests.csproj>
dotnet test <proj> --filter "FullyQualifiedName~MyTestClass"
```

Tests can take a while to build and run — monitor output and wait for completion unless you're confident a run is hung.

### Full build/test (final validation only)
```bash
./build.sh   # Build.cmd on Windows
./test.sh    # Test.cmd on Windows
```

Other entry points: `dotnet run --file eng/generate-compiler-code.cs` (regenerate Syntax/BoundNodes code), `dotnet msbuild <proj> /t:UpdateXlf` (refresh `.xlf` after `.resx` edits).

## Code Style

- 4-space indent for code; 2-space for project/XML/JSON. Never tabs. UTF-8-BOM, final newline for `*.cs`/`*.vb`.
- **Blank lines must be completely empty** (no spaces/tabs); no trailing whitespace — both are hard lint failures.
- Private fields `_camelCase`; namespaces `Microsoft.CodeAnalysis.[Language].[Area]`.
- Always thread `CancellationToken` through async operations. (Null-checking style is layer-specific — see the area's instruction file: `Contract.ThrowIfNull` in IDE, `Debug.Assert` in the compiler.)
- Language services are exported **per-language** (`[ExportLanguageService(..., LanguageNames.CSharp), Shared]`), never shared across C#/VB.
- No `TODO`/`TODO2` comments — track follow-ups as linked GitHub issues in code; existing `TODO2`s are only a frozen enforcement baseline. No `PROTOTYPE` comments in PRs to `main`.
- Update `PublicAPI.Unshipped.txt` for public API changes. Never hand-edit generated code or `eng/common`.

Full conventions: `.github/memory/CONVENTIONS.md` and `.github/instructions/{Compiler,IDE,Razor}.instructions.md`.

## Agent Orientation

When starting any task or answering any question about this repo:
1. **Read `.github/memory/INDEX.md` first** — it's the loading map for the knowledge base. Use it to find authoritative answers before searching the file system.
2. **For any non-trivial task, also read `.github/memory/ARCHITECTURE.md` and `.github/memory/CONVENTIONS.md`** as your baseline.
3. **Read the path-scoped instruction file for the area you're editing** — `.github/instructions/Compiler.instructions.md`, `IDE.instructions.md`, or `Razor.instructions.md` (these auto-apply to `.cs`/`.vb` under their glob and carry the layer's directory detail, conventions, and key files/APIs). For that layer's **known issues** and **test conventions**, load `.github/memory/known-issues/<area>.md` and `.github/memory/testing/<area>.md` on demand (see the INDEX loading map).
4. After completing work, run the `update-agent-docs` skill.

### Memory

`.github/memory/` is your persistent knowledge base. You may freely create new focused files, update existing ones when you find corrections, and reorganize when structure no longer fits. Use descriptive filenames.

**Memory freshness is your responsibility.** Files can drift from the code:
- **Always cross-check memory claims against actual code** before relying on them.
- **If a memory file is stale, fix it immediately.** If you learn something worth keeping, write it to `.github/memory/` immediately.

### Doc Update Obligation

Every task that changes code must end with a doc pass:
- Added or moved files? → Update `.github/memory/FILE_MAP.md` (top-level) and the matching `.github/instructions/<area>.instructions.md` (directory detail).
- Changed a public interface, diagnostic ID, or API? → Update the relevant `.github/instructions/<area>.instructions.md` and `PublicAPI.Unshipped.txt`.
- Hit something surprising or undocumented? → Repo-wide → `.github/memory/KNOWN_ISSUES.md`; layer-specific → `.github/memory/known-issues/<area>.md`.
- Established a new pattern? → Repo-wide → `.github/memory/CONVENTIONS.md`; layer-specific → the matching `.github/instructions/<area>.instructions.md`.
- Changed test base classes or conventions? → Repo-wide layout → `.github/memory/TESTING_STRATEGY.md`; layer-specific → `.github/memory/testing/<area>.md`.
- Added/removed/renamed a memory file? → Update `.github/memory/INDEX.md`.

### Skills

Skills live in `.github/skills/<skill-name>/SKILL.md` and are auto-discovered by their YAML `description`. Useful ones here include `code-review`, `ci-analysis`, `analyzer-codefix`, `merge-into-branch`, `snap`, and `update-agent-docs`.

## Validation Checklist

When making changes:
1. **Read `.github/memory/INDEX.md` first.**
2. For non-trivial tasks, read `ARCHITECTURE.md` and `CONVENTIONS.md`, and the `.github/instructions/<area>.instructions.md` for the area you're editing.
3. **Build the specific project(s) modified** (`Compilers.slnf` / `Ide.slnf` / `Razor.slnf` / the project).
4. **Run targeted tests** for affected test project(s).
5. If you edited a `.resx`, run `/t:UpdateXlf`; if you edited Syntax/BoundNodes XML, regenerate code. Update `PublicAPI.Unshipped.txt` for public API changes.
6. Follow existing patterns in similar files.
7. **Doc pass** (mandatory) — run the `update-agent-docs` skill and apply the Doc Update Obligation above.
