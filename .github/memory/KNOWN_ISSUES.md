---
coverage: Repo-wide / cross-cutting quirks and workarounds; layer-specific issues live in known-issues/{compiler,ide,razor}.md
---

# Known Issues

Repo-wide and cross-cutting issues only. Layer-specific gotchas live in dedicated
per-layer files (load only the one for your area):
- Compiler → `.github/memory/known-issues/compiler.md`
- IDE → `.github/memory/known-issues/ide.md`
- Razor → `.github/memory/known-issues/razor.md`

## Blank lines with whitespace fail linting

**Affected area:** all `*.cs` / `*.vb`
**Description:** Blank lines containing any space or tab fail lint/format; trailing whitespace also fails.
**Workaround:** Keep blank lines completely empty; run `dotnet format whitespace --folder . --include <path>`.

## Generated code must not be hand-edited

**Affected area:** `Syntax.xml`/`BoundNodes.xml`-derived `.cs`, `eng/common`, `*.xlf`
**Description:** These are produced by generators or external sync (DARC/Arcade); manual edits are overwritten or break builds.
**Workaround:** Edit the source (XML/`.resx`), then regenerate (`dotnet run --file eng/generate-compiler-code.cs`, or `/t:UpdateXlf` for `.xlf`). Never edit `eng/common` by hand.

## TODO / PROTOTYPE comments are CI-gated

**Affected area:** whole repo, PRs targeting `main`
**Description:** CI flags `TODO` comments; PROTOTYPE comments are disallowed in PRs to `main`.
**Guidance:** Do **not** add new `TODO` or `TODO2` comments. Track follow-up work as a GitHub issue and link it in code (e.g. `// See https://github.com/dotnet/roslyn/issues/NNNN`). Existing `TODO2` markers are only a frozen baseline from when enforcement was introduced — they are not a pattern to copy. Remove all `PROTOTYPE` markers before merging to `main` (allowed only on feature branches).

## Environmental test failures (not code bugs)

**Affected area:** full `test.sh`/`Test.cmd` runs
**Description:** A few tests fail for environment reasons unrelated to changes:
- `RuntimeHostInfoTests.DotNetInPath_Symlinked` requires symlink-creation privilege (run elevated).
- `Workspaces.MSBuild` `NewlyCreatedProjectsFromDotNetNew.Validate*TemplateProjects` fail without mobile (ios/tvos/macos/maccatalyst) dotnet workloads installed.
**Workaround:** Prefer targeted test projects; treat these specific failures as environmental.
