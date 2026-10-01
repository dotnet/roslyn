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
| RunTests tool tests | `src/Tools/RunTests.UnitTests/`; run with `dotnet test src/Tools/RunTests.UnitTests/RunTests.UnitTests.csproj`. |
| Project-data tests | `src/ProjectData/Microsoft.NET.ProjectData{,.Generators,.Tasks}.Tests/`; assemblies use the `UnitTests` suffix. |
| Integration tests | VS integration tests (`azure-pipelines-integration*.yml`); runnable locally on **Windows** hosts with a VS install, also run in CI. |

Frameworks: Unit and VS integration tests use xUnit v3 4.0.0 with the shared
Roslyn/Razor test utilities. `eng/Packages.props` centralizes the xUnit v3
package versions for both suites.

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
- Custom xUnit v3 fact/theory attributes need public constructors with optional
  `[CallerFilePath]` and `[CallerLineNumber]` parameters that forward to the
  `FactAttribute`/`TheoryAttribute` base constructor. If an attribute retains
  a variadic `params` conditions constructor, use a caller-aware overload for
  commonly used positional condition forms; source parameters cannot follow
  a `params` parameter.

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

These entry points invoke `src/Tools/RunTests` to run already-built assemblies
efficiently; build the test projects first. See the tool's
[`README.md`](../../src/Tools/RunTests/README.md) for assembly filters, test
framework selection, and environment-variable options. Use `dotnet test` directly
for a single project.

The build scripts also accept `-test`, `-testSet:<name>`, `-testKind:<name>`, or
`-testFramework:<name>` (also with `--` on Unix) to invoke RunTests after successful
build actions. They forward the build configuration and supplied test-option
values; test discovery, selection, and validation remain in RunTests.

CI test-only jobs use `eng/pipelines/install-dotnet.yml` to install the SDK
from `global.json` with `UseDotNet@2`. The template reads `sdk.version` into a
read-only job variable on every run and passes that exact version to the task.
PrepareTests includes the checked-in
`global.json` in the downloaded test payload for jobs without a source checkout.
The task adds the SDK to `PATH`, and test steps call `dotnet exec` directly;
the template also installs the .NET 10 runtime for the runner and testhosts,
without requiring job-wide roll-forward environment overrides.
build-then-test jobs reuse the SDK already
installed and added to `PATH` by the build step. RunTests defaults to the dotnet
executable above its hosting runtime directory, so `--dotnet` is unnecessary.

### Test types to be aware of
- VS integration tests (`azure-pipelines-integration*.yml`) require a VS install, so they run only on **Windows** hosts (not CI-only — they can be run locally on Windows). Prefer unit tests for the inner development loop; reach for integration tests when validating end-to-end VS behavior.
- `eng/test-vsi.ps1` selects the testhost architecture with `-testPlatform`,
  independently of the `-oop64bit` setting for Visual Studio's out-of-process
  services. It configures architecture-specific `DOTNET_ROOT` variables so
  native testhosts use the repository's SDK runtime.
- A handful of tests fail only for environmental reasons:
  - `RuntimeHostInfoTests.DotNetInPath_Symlinked` requires symlink-creation privilege (run elevated).
  - `Workspaces.MSBuild` `NewlyCreatedProjectsFromDotNetNew.Validate*TemplateProjects` fail without mobile (ios/tvos/macos/maccatalyst) dotnet workloads installed.

### xUnit v3 unit-test infrastructure

Repo unit-test projects use xUnit v3 through `eng/targets/XUnit.targets`. Test
projects build as executables for xUnit v3 but keep a `.dll` target extension so
Roslyn's test naming/discovery checks and VSTest-based infrastructure continue
to work. The common target adds `xunit.v3.mtp-off`; Roslyn still uses VSTest
rather than Microsoft.Testing.Platform for these tests. All projects importing
this target use the centrally pinned xUnit v3 4.0.0 packages.

The runner-only package references in `XUnit.targets` use `PrivateAssets="all"`.
Keep them private so xUnit's `buildTransitive` entry-point targets do not flow
through project references into non-test consumers such as benchmark projects.

`TestDiscoveryWorker` references `xunit.v3.runner.utility` 4.0.0 for
version-independent discovery. That official v3 runner package has a transitive
`xunit.abstractions` 2.0.3 compatibility dependency so it can inspect v1/v2
assemblies; it is not a test-framework-v2 consumer. The worker uses the default
out-of-process front controller, so the discovered test assembly and its
dependencies load in the test process rather than in the worker. `PrepareTests`
sets `DOTNET_ROOT` and the current architecture's `DOTNET_ROOT_*` variable for
the worker from the selected dotnet executable so the xUnit test apphost can
locate the same runtime.

VS integration projects keep `IsTestProject=true` so `XUnit.targets` supplies
their xUnit v3 package references and VSTest discovery works; see
`testing/vs-integration-tests-xunit-v3.md`.

### Shared test-infrastructure projects

Unit tests and VS integration tests reference the same shared test-utility
projects:

| Shared utility project | Used by |
|---|---|
| `Compilers/Test/Core` (`Microsoft.CodeAnalysis.Test.Utilities`) | Compiler, IDE, SDK, Razor, and VS integration tests |
| `Workspaces/CoreTestUtilities` (`Microsoft.CodeAnalysis.Workspaces.Test.Utilities`) | Workspace/IDE tests and `Microsoft.VisualStudio.LanguageServices.New.IntegrationTests` |
| `Razor/src/Shared/Microsoft.AspNetCore.Razor.Test.Common` | Razor unit and integration test utilities |
| `Razor/src/Razor/test/Microsoft.AspNetCore.Razor.Test.Common.Tooling` | Razor tooling/unit tests and `Microsoft.VisualStudio.Razor.IntegrationTests` |

If an integration project needs access to internal members in a shared
test-utility assembly, add the integration test assembly to that utility
project's `InternalsVisibleTo` list. Do not create a new source-copied
integration-only fork.

## CI

PR test CI runs via `azure-pipelines.yml` (Azure DevOps + Helix).
`azure-pipelines-pr-validation.yml` builds and publishes insertion-validation
artifacts; it does not build or run tests. For investigating failures, use the
`ci-analysis` and `integration-test-analysis` skills.
