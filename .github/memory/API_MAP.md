---
coverage: Repo-wide build/test entry points and PublicAPI tracking; layer-specific surfaces live in the instruction files
---

# API Map

Repo-wide entry points and the formal public-API tracking rules. Layer-specific surfaces (compiler error codes, IDE diagnostic IDs, extensibility patterns) live in the path-scoped instruction files:
- Compiler error codes, `MessageID`, Syntax/BoundNodes, codegen, `csc`/`vbc` → `.github/instructions/Compiler.instructions.md`
- IDE diagnostic IDs, MEF service/analyzer/code-fix exports, LSP → `.github/instructions/IDE.instructions.md`

## Build & Test Entry Points

| Command | Purpose |
|---------|---------|
| `build.sh` / `Build.cmd` | Full solution build (Arcade). |
| `dotnet build Compilers.slnf` | Compiler-only build. |
| `dotnet build Ide.slnf` | IDE-only build. |
| `dotnet build Razor.slnf` | Razor compiler & tooling-only build. |
| `test.sh` / `Test.cmd` | Full test run. |
| `dotnet test <test.csproj>` | Run a specific test project. |
| `dotnet run --file eng/generate-compiler-code.cs` | Regenerate Syntax/BoundNodes code. |
| `dotnet msbuild <proj> /t:UpdateXlf` | Refresh `.xlf` after `.resx` changes. |

Solution filters: `Roslyn.slnx` (full), `Compilers.slnf`, `Ide.slnf`, `Razor.slnf`.

## Public API Tracking

- Every public-API addition/change must update the owning project's `PublicAPI.Unshipped.txt`.
- Enforced by the PublicApiAnalyzer (e.g., `RS0016` for undeclared public API). Promote entries to `PublicAPI.Shipped.txt` at release/snap time.

## Resource Strings

- Strings live in `.resx`, accessed via generated designer classes (`CSharpResources`, `FeaturesResources`, `AnalyzersResources`, …).
- After editing a `.resx`, run `dotnet msbuild <project.csproj> /t:UpdateXlf` to refresh `.xlf` files.
