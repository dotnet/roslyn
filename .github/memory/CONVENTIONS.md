---
coverage: Repo-wide code style, naming, immutability, resource & public-API rules (layer-specific conventions live in the instruction files)
---

# Conventions

Authoritative formatting lives in `.editorconfig`; path-scoped rules live in `.github/instructions/{Compiler,IDE,Razor}.instructions.md` and apply automatically to files under their globs. This file holds only **repo-wide** conventions.

**Layer-specific conventions live in the path-scoped instruction files — read the one for your area:**
- MEF service/analyzer exports, per-language services, code-fix patterns → `.github/instructions/IDE.instructions.md` (these do **not** apply to the compiler).
- Code generation, performance/pooling patterns → `.github/instructions/Compiler.instructions.md`.

## Naming Conventions

- **Namespaces:** `Microsoft.CodeAnalysis.[Language].[Area]` (e.g., `Microsoft.CodeAnalysis.CSharp.Formatting`).
- **Private fields:** `_camelCase`.
- **Types/methods/properties:** PascalCase. Interfaces prefixed `I`.

## Code Style

From `.editorconfig`:
- Indentation: 4 spaces for `*.cs`/`*.vb`; 2 spaces for project/XML/JSON/PS1/SH files. Never tabs.
- `*.cs`/`*.vb`: `insert_final_newline = true`, `charset = utf-8-bom`.
- **Blank lines must contain no whitespace** (no spaces/tabs) — this is a hard lint failure.
- **No trailing whitespace.**
- File-scoped namespaces and `var`/expression-body preferences are enforced via editorconfig analyzers — follow the file you are editing.

Running the formatter:
- `dotnet format whitespace --folder . --include <path>` (the `--folder .`/`--include` form avoids a slow design-time build).

## Patterns in Active Use

### Immutability (all layers)
```csharp
// Use With*/Add*/Replace* to produce new instances — never mutate.
// IDE/workspace: oldDocument.WithSyntaxRoot(newRoot)
// Compiler:      compilation.ReplaceSyntaxTree(oldTree, newTree)

// Always thread CancellationToken through async/semantic calls.
var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
var symbolInfo = semanticModel.GetSymbolInfo(expression, cancellationToken);
```

## Patterns Explicitly Avoided

- **No `TODO` or `TODO2` comments** — CI correctness leg flags `TODO`. Track follow-up work as a GitHub issue and link it in code (e.g. `// https://github.com/dotnet/roslyn/issues/NNNN`). Existing `TODO2` markers are a frozen baseline from when enforcement started, not a pattern to follow.
- **No `PROTOTYPE` comments in PRs targeting `main`** — CI enforces removal (they are allowed only on feature branches).
- **Do not hand-edit generated code** (`Syntax.xml`/`BoundNodes.xml`-derived `.cs`, `eng/common`, `*.xlf` content beyond the regen tool).
- **Do not break layering** — lower layers (Compilers) must not reference higher layers (Workspaces/Features/IDE). See `docs/Layering.md`.

## Resources, Localization & Public API

- Resource strings live in `.resx`, accessed via generated designer classes (`CSharpResources`, `FeaturesResources`, `AnalyzersResources`, …).
- After editing a `.resx`, run `dotnet msbuild <project.csproj> /t:UpdateXlf` to refresh the `.xlf` translation files.
- When adding/changing public APIs, update the project's `PublicAPI.Unshipped.txt` (the PublicApiAnalyzer / RS0016 enforces this).

## Language / Framework Constraints

- SDK pinned in `global.json` (currently .NET SDK `10.0.x`); VS toolset `17.14`.
- Arcade-based build (`Microsoft.DotNet.Arcade.Sdk`); package versions centralized in `Directory.Packages.props`.

## Documentation Files

- New docs use **kebab-case** filenames (e.g., `roslyn-language-server-copilot-plugin.md`, not `Roslyn Language Server Copilot Plugin.md`).
- Place docs in the appropriate `docs/` subdirectory (`docs/contributing/`, `docs/compilers/`, `docs/features/`, …); general docs that don't fit a subdirectory go directly in `docs/`.
