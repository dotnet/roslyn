---
coverage: Repo-wide test layout, run commands, and shared authoring conventions; per-layer test base classes live in testing/{compiler,ide,razor}.md
---

# Testing Strategy

Repo-wide test layout, run commands, and shared authoring conventions. **Per-layer
test base classes and conventions** live in dedicated per-layer files (load only
the one for your area):
- Compiler (`CSharpTestBase`, `VerifyEmitDiagnostics`) → `.github/memory/testing/compiler.md`
- IDE (`AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest_NoEditor`, `[UseExportProvider]`, `TestInRegularAndScriptAsync`) → `.github/memory/testing/ide.md`
- Razor (`TestCode` span markers) → `.github/memory/testing/razor.md`

## Test Layout

| Type | Location convention |
|------|---------------------|
| Unit tests | Sibling `*Test` / `*.UnitTests` project next to the product project (e.g., `Workspaces/Core` ↔ `Workspaces/CoreTest`). |
| Compiler tests | `src/Compilers/*/Test/`. |
| IDE/analyzer tests | `*Test` projects under `src/Features`, `src/Analyzers`, `src/EditorFeatures`. |
| Project-data tests | `src/ProjectData/Microsoft.NET.ProjectData{,.Generators,.Tasks}.Tests/`; assemblies use the `UnitTests` suffix. |
| Integration tests | VS integration tests (`azure-pipelines-integration*.yml`); runnable locally on **Windows** hosts with a VS install, also run in CI. |

Frameworks: Unit tests use xUnit v3 with Roslyn test utilities. VS integration
tests and their dedicated `.IntegrationTests` test-utility forks intentionally
remain on xUnit v2 until the integration-test migration happens.

## Repo-wide Authoring Conventions

- Prefer raw string literals (`"""..."""`) over verbatim strings for test source code.
- Keep tests focused: use `.Single()` rather than asserting a count then indexing.
- Analyzer testing-library tests should use `ReferenceAssemblies.Default` for
  the stable .NET Core 3.1 reference surface. Use an explicit
  `ReferenceAssemblies` value, such as `ReferenceAssemblies.Net.Net100`, when
  the scenario intentionally depends on a newer or specific framework API
  surface.
- For issue-linked changes, add a `WorkItem` attribute next to the test
  attribute, e.g. `[Fact, WorkItem("https://github.com/dotnet/roslyn/issues/1234")]`
  or `[Theory, WorkItem("https://github.com/dotnet/roslyn/issues/1234")]`.
  Use the originating GitHub issue/PR or Azure DevOps work item URL.

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
- A handful of tests fail only for environmental reasons:
  - `RuntimeHostInfoTests.DotNetInPath_Symlinked` requires symlink-creation privilege (run elevated).
  - `Workspaces.MSBuild` `NewlyCreatedProjectsFromDotNetNew.Validate*TemplateProjects` fail without mobile (ios/tvos/macos/maccatalyst) dotnet workloads installed.

### xUnit v3 unit-test infrastructure

Repo unit-test projects use xUnit v3 through `eng/targets/XUnit.targets`. Test
projects build as executables for xUnit v3 but keep a `.dll` target extension so
Roslyn's test naming/discovery checks and VSTest-based infrastructure continue
to work. The common target sets `UseXunitV3=true` by default for non-integration
test projects, adds `xunit.v3.mtp-off`, and sets
`IsTestingPlatformApplication=false` because Roslyn still uses VSTest rather
than Microsoft.Testing.Platform for these tests.

When a test project must stay on xUnit v2, set `<UseXunitV3>false</UseXunitV3>`
in the project. This is reserved for integration-test infrastructure that is
intentionally still on xUnit v2.

### Integration-test-only forks of shared test-infrastructure projects

The 4 VS integration test projects (`Roslyn.SDK.IntegrationTests`,
`Microsoft.VisualStudio.Extensibility.Testing.Xunit.IntegrationTests`,
`Microsoft.VisualStudio.LanguageServices.New.IntegrationTests`,
`Microsoft.VisualStudio.Razor.IntegrationTests`) do **not** reference the shared,
xUnit-v3-coupled test-utility assemblies that hundreds of unit test projects use.
Instead they reference dedicated `.IntegrationTests`-suffixed sibling forks so the
two test suites can use different xUnit major versions independently (xUnit major
versions cannot coexist in one compiled assembly's dependency graph).

| Original (unit-test-only) | Fork (integration-test-only) |
|---|---|
| `Compilers/Test/Core` (`Microsoft.CodeAnalysis.Test.Utilities`) | `Compilers/Test/Core.IntegrationTests` (`Microsoft.CodeAnalysis.Test.Utilities.IntegrationTests`) |
| `Workspaces/CoreTestUtilities` (`Microsoft.CodeAnalysis.Workspaces.Test.Utilities`) | `Workspaces/CoreTestUtilities.IntegrationTests` (`Microsoft.CodeAnalysis.Workspaces.Test.Utilities.IntegrationTests`) |
| `Razor/src/Shared/Microsoft.AspNetCore.Razor.Test.Common` | `Razor/src/Shared/Microsoft.AspNetCore.Razor.Test.Common.IntegrationTests` |
| `Razor/src/Razor/test/Microsoft.AspNetCore.Razor.Test.Common.Tooling` | `Razor/src/Razor/test/Microsoft.AspNetCore.Razor.Test.Common.Tooling.IntegrationTests` |

Only assemblies that themselves carry an xUnit package reference (or exist solely
to satisfy compilation of forked code) were forked. Pure product assemblies and
xUnit-free test-resource assemblies (e.g. `Microsoft.CodeAnalysis.Compiler.Test.Resources`,
`Microsoft.CodeAnalysis.TestAnalyzerReference`) remain shared since they carry no
version-conflict risk.

Each fork is source-copied (not linked), single-targets `net472`, keeps the same
namespaces as the original, and trims its `InternalsVisibleTo` list down to only
the integration test project(s) that need it. Product assemblies that grant
`InternalsVisibleTo` to an original test-utility assembly name also grant it to
the matching `.IntegrationTests` fork name (a sibling `<InternalsVisibleTo>` entry
immediately after the original) — this is required for `protected internal`
overrides, IVT-gated polyfill types (`IsExternalInit`, etc.), and other
internal-only members the forked test code accesses. When adding new members or
usages to a forked project, keep its source in sync with the original by hand;
there is no automated sync.

## CI

PR validation runs via `azure-pipelines-pr-validation.yml` (Azure DevOps + Helix). For investigating failures, use the `ci-analysis` and `integration-test-analysis` skills.
