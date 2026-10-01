---
coverage: VS integration test harness (IdeFact/IdeTheory) on xUnit v3 — project setup gotchas, scope, and API considerations
---

# VS Integration Tests — xUnit v3

`src/VisualStudio/IntegrationTest/` hosts the `IdeFact`/`IdeTheory` VS-integration-test
harness (`Microsoft.VisualStudio.Extensibility.Testing.Xunit*`) and its consumers
(`New.IntegrationTests`, `Roslyn.SDK.IntegrationTests`,
`Microsoft.VisualStudio.Razor.IntegrationTests`). This harness and its direct
consumers run on **xUnit v3 4.0.0**, the same version as the unit-test suite. VS
integration tests reference the shared Roslyn and Razor test-utility projects
directly; do not add source-copied `.IntegrationTests` utility forks.

`src/VisualStudio/IntegrationTest/IntegrationTestBuildProject.csproj` (a
`Microsoft.Build.Traversal` project) is the actual CI entry point that builds only the
projects needed for VS integration tests; it is more authoritative than building the
broader `Ide.slnf` filter, which also pulls in unrelated projects.

Razor's VS integration tests are included in `IntegrationTestBuildProject.csproj`.
`dotnet build Ide.slnf` should also stay green for these projects; if restore reports
NU1109 involving Razor integration tests, check for a direct project-level
`Xunit.Combinatorial` `VersionOverride="2.1.41"` or a v2 xUnit package reference
leaking into the shared Razor test-utility projects.

## `IsTestProject` must stay enabled

The VS integration projects are conventional test projects: their `.IntegrationTests`
names make Arcade set `IsTestProject=true`, `eng/targets/Settings.props` then defaults
`OutputType=Exe` (so the net472 output is `<Name>.IntegrationTests.exe`), and
`eng/targets/XUnit.targets` supplies `xunit.v3.mtp-off`
and `xunit.runner.visualstudio`. Do **not** set `IsTestProject=false` on them: without
the VSTest adapter, `vstest.console` finds no tests and RunTests reports every
integration work item as passed in about a second while nothing runs. Each project sets
`<ExcludeFromDotNetBuild>true</ExcludeFromDotNetBuild>` because VS integration tests
never participate in the source-only build.

## `[IdeSettings]` lookup

Suites declare `[IdeSettings(MinVersion = VS18, RootSuffix = ..., MaxAttempts = ...,
EnvironmentVariables = ...)]` on their abstract base test class.
`IdeFactDiscoverer.GetSettingsAttributes` must read inherited attributes from both the
test method and the test class (method first). If class-level settings are ignored,
`MinVersion` falls back to VS2012, producing `(VS2022)` test cases that launch VS 2022
with the default `Exp` hive and fail to install the VS18-only
IntegrationTestService VSIX (`NoApplicableSKUsException`), crashing the test process.
Check with `<Assembly>.exe -list full`, which does not launch VS: every ID should end
in `_VS18` and use the suite's root suffix.

## Package version selection

`eng/Packages.props` selects xUnit v3 4.0.0 for unit and integration projects.
Keep the VS integration harness and its consumers on the centralized xUnit v3
release: runner extension interfaces can differ between releases.
`Xunit.Combinatorial` is centrally pinned to 2.1.41 and `xunit.analyzers` to
2.0.0; individual integration projects should not need explicit overrides.

## xUnit v3 API considerations

- Implement trait discovery logic in the attribute/discoverer directly;
  `ITraitAttribute` has no `GetTraits()` method.
- Match the current `BeforeAfterTestAttribute` method signatures, including
  their parameters.
- Implement the members of `ITestOutputHelper` required by xUnit v3.
- xUnit v3 test methods use the current `SynchronizationContext`.
- `IAsyncLifetime.InitializeAsync()` returns `ValueTask`, and disposal is through
  `IAsyncDisposable.DisposeAsync()` (`ValueTask`).
- Test-lifecycle methods like `InitializeCoreAsync()` on VS in-process test-service
  types return `ValueTask` in the v3 harness — check the signature of every override.
- xUnit v3 4.0 marks `CollectionBehaviorAttribute.DisableTestParallelization`
  obsolete as an error; use `[assembly: Parallelization(Mode = ParallelMode.None)]`
  with `[assembly: CollectionBehavior(CollectionBehavior.CollectionPerAssembly)]`.
