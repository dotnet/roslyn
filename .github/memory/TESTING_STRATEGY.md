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

Frameworks: xUnit with Roslyn test utilities.

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

These entry points invoke `src/Tools/RunTests` to run already-built assemblies
locally with VSTest, one work item per assembly; build the test projects first.
They never submit to Helix, and local execution does not require `testlist.json`.
See the tool's
[`README.md`](../../src/Tools/RunTests/README.md) for assembly filters, test
framework selection, and environment-variable options. Use `dotnet test` directly
for a single project.

The build scripts also accept `-test`, `-testSet:<name>`, `-testKind:<name>`, or
`-testFramework:<name>` (also with `--` on Unix) to invoke RunTests after successful
build actions. They forward the build configuration and supplied test-option
values; test discovery, selection, and validation remain in RunTests.

### Helix submission

`src/Tools/RunHelix` is the separate submission executable used by
`eng/pipelines/test-windows-job.yml` and `test-unix-job.yml`. It consumes
PrepareTests output, partitions tests using historical timings (or test counts),
and submits work items whose workers invoke VSTest directly. Success means jobs
were submitted, not tests passed; the external **Monitor Helix Jobs** job handles
completion and retries. See its [`README.md`](../../src/Tools/RunHelix/README.md)
for queue, authentication, history, and artifact options.

RunTests accepts local execution options only.
RunHelix rejects local execution options, including `--out` and `--logs`;
submission diagnostics remain under `artifacts/log/<Configuration>`.
Both executables compile the flat `src/Tools/TestRunnerCommon/*.cs` source glob
(namespace `TestRunner`), with no shared-library project or runner-to-runner
reference. Local code uses `TestRunner.RunTests`; submission code uses
`TestRunner.Helix`. Build both executables when changing shared sources.

PrepareTests transports both built runners, their dependencies, and rehydration
scripts; an absent RunHelix output is allowed for local-only builds.
Single-machine templates, official pipeline local test steps, and Visual Studio
integration callers stay on RunTests. Despite its name,
`eng/pipelines/test-integration-helix.yml` runs integration tests locally through
`eng/test-vsi.ps1`, not RunHelix.

### Runner regression tests

Build `src/Tools/RunTests/RunTests.csproj`,
`src/Tools/RunHelix/RunHelix.csproj`, and
`src/Tools/PrepareTests/PrepareTests.csproj` in the selected configuration first,
then run:

```powershell
pwsh -NoProfile -File eng\test-test-runners.ps1 -configuration Debug
```

This offline suite validates the built tools using temporary harnesses, without
submitting Helix jobs or requiring production credentials.

### SDK installation in CI

CI test-only jobs use `eng/pipelines/install-dotnet.yml` to install the SDK
from `global.json` with `UseDotNet@2`. The template reads `sdk.version` into a
read-only job variable on every run and passes that exact version to the task.
PrepareTests includes the checked-in
`global.json` in the downloaded test payload for jobs without a source checkout.
The task adds the SDK to `PATH`, and test steps call `dotnet exec` directly;
the template also installs the .NET 10 runtime for the runner and testhosts,
without requiring job-wide roll-forward environment overrides.
build-then-test jobs reuse the SDK already
installed and added to `PATH` by the build step. Both runners default to the dotnet
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

## CI

PR test CI runs via `azure-pipelines.yml` (Azure DevOps + Helix).
`azure-pipelines-pr-validation.yml` builds and publishes insertion-validation
artifacts; it does not build or run tests. For investigating failures, use the
`ci-analysis` and `integration-test-analysis` skills.
