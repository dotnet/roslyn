---
coverage: Top-level src/ overview; per-layer directory detail lives in the instruction files
---

# File Map

Source lives under `src/`. Build orchestration is in `eng/` and root scripts; docs in `docs/`. A pervasive convention: a product project `X` has its tests in a sibling `XTest` / `*.UnitTests` project (e.g., `Workspaces/Core` ↔ `Workspaces/CoreTest`).

This file is a **top-level map only**. For per-area directory detail, read the matching path-scoped instruction file:
- Compiler areas → `.github/instructions/Compiler.instructions.md`
- IDE areas → `.github/instructions/IDE.instructions.md`
- Razor → `.github/instructions/Razor.instructions.md`

## `src/` Areas

| Area | Layer | Purpose |
|------|-------|---------|
| `Compilers/` | compiler | C#/VB compilers (`Core`, `CSharp`, `VisualBasic`, `Server`). |
| `Dependencies/` | compiler | High-performance pooled collections & threading. |
| `ExpressionEvaluator/` | compiler | Debugger expression evaluator. |
| `Tools/` | compiler | Compiler tooling (BuildBoss, format tools). |
| `Workspaces/` | ide | Solution/Project/Document model, MSBuild loading, Remote (OOP). |
| `Features/`, `EditorFeatures/` | ide | IDE feature logic and editor integration. |
| `Analyzers/`, `CodeStyle/` | ide | IDE0xxx code-style analyzers & fixes. |
| `LanguageServer/` | ide | LSP server. |
| `VisualStudio/` | ide | VS language services & UI. |
| `Razor/src/` | razor | Razor compiler + tooling (own sub-tree layout). |
| `Scripting/`, `Interactive/` | — | C#/VB scripting engine and REPL. |
| `RoslynAnalyzers/` | — | Shipping `Microsoft.CodeAnalysis.*` analyzer packages. |
| `Deployment/`, `NuGet/`, `Setup/`, `Test/` | — | Deployment/VSIX, packaging, shared test infrastructure. |

## Non-source Roots

| Path | Status | Purpose |
|------|--------|---------|
| `eng/` | Config / Generated | Arcade build engineering. `eng/common/` is DARC-synced — do not hand-edit. `eng/generate-compiler-code.cs` regenerates compiler code. |
| `docs/` | Active | Contributor & design docs. New docs use kebab-case filenames in the right subdirectory. |
| Root | Config | Entry points & solution filters: `build.sh`/`Build.cmd`, `test.sh`/`Test.cmd`, `Roslyn.slnx`, `Compilers.slnf`, `Ide.slnf`, `Razor.slnf`, `global.json`, `Directory.*.props/targets`, `Directory.Packages.props`. |
