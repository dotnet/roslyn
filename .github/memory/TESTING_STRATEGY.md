---
coverage: Repo-wide test layout & how to run tests; per-layer test base classes live in the instruction files
---

# Testing Strategy

Repo-wide test layout and run commands. **Per-layer test base classes and conventions** live in the path-scoped instruction files:
- Compiler (`CSharpTestBase`, `VerifyEmitDiagnostics`) → `.github/instructions/Compiler.instructions.md`
- IDE (`AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest_NoEditor`, `[UseExportProvider]`, `TestInRegularAndScriptAsync`) → `.github/instructions/IDE.instructions.md`
- Razor (`TestCode` span markers) → `.github/instructions/Razor.instructions.md`

## Test Layout

| Type | Location convention |
|------|---------------------|
| Unit tests | Sibling `*Test` / `*.UnitTests` project next to the product project (e.g., `Workspaces/Core` ↔ `Workspaces/CoreTest`). |
| Compiler tests | `src/Compilers/*/Test/`. |
| IDE/analyzer tests | `*Test` projects under `src/Features`, `src/Analyzers`, `src/EditorFeatures`. |
| Integration tests | VS integration tests (`azure-pipelines-integration*.yml`); runnable locally on **Windows** hosts with a VS install, also run in CI. |

Frameworks: xUnit with Roslyn test utilities.

## Repo-wide Authoring Conventions

- Prefer raw string literals (`"""..."""`) over verbatim strings for test source code.
- Keep tests focused: use `.Single()` rather than asserting a count then indexing.
- For issue-linked changes add `[Fact, WorkItem("https://github.com/dotnet/roslyn/issues/1234")]`.

## Running Tests

### During development (preferred — targeted)
```bash
dotnet test <path/to/Specific.UnitTests.csproj>
dotnet test <proj> --filter "FullyQualifiedName~MyTestClass"
```
Targeted runs are strongly preferred — the full suite is large and slow. Tests can take a while to build/run; wait for completion unless you're confident a run is hung.

### Full suite (final validation only)
```bash
./test.sh        # or Test.cmd on Windows
```

### Test types to be aware of
- VS integration tests (`azure-pipelines-integration*.yml`) require a VS install, so they run only on **Windows** hosts (not CI-only — they can be run locally on Windows). Prefer unit tests for the inner development loop; reach for integration tests when validating end-to-end VS behavior.
- A handful of tests fail only for environmental reasons — see `KNOWN_ISSUES.md`.

## CI

PR validation runs via `azure-pipelines-pr-validation.yml` (Azure DevOps + Helix). For investigating failures, use the `ci-analysis`, `helix-investigation`, and `integration-test-analysis` skills.
