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

## Arcade `IsTestProject` / naming-heuristic gotcha

`eng/targets/XUnit.targets` configures conventionally discovered test projects
for xUnit v3. Some VS integration projects manage their own package references
and custom runners instead. The build's naming conventions set
`IsTestProject`/`IsIntegrationTestProject`/`IsUnitTestProject` to `true` by
**name-matching convention**, independently of each other:

- `IsIntegrationTestProject` defaults `true` when the project name ends in
  `.IntegrationTests`.
- `IsUnitTestProject` defaults `true` when the project name ends in `.UnitTests` or
  `.Tests`.
- `IsTestProject` defaults `true` if either of the above is `true`.

Integration projects that manage their own xUnit v3 packages need
**`<IsTestProject>false</IsTestProject>`** explicitly set. Setting `IsTestProject=false`
does **not** clear `IsIntegrationTestProject`/`IsUnitTestProject` — if the project's
`OutputType` doesn't match Arcade's expected `TargetFileName` suffix for those flags
(e.g. an `Exe`-output project named `*.IntegrationTests`), the
`_CheckTestProjectTargetFileName` target in `eng/targets/Imports.targets` will fail;
set the specific flag(s) (`IsIntegrationTestProject`/`IsUnitTestProject`) to `false`
too.

Setting `IsTestProject=false` also drops the implicit `DotNetBuildTests=false`
source-build exclusion normally implied by that flag. Restore it explicitly with
**`<ExcludeFromDotNetBuild>true</ExcludeFromDotNetBuild>`** (the same pattern used by
e.g. `src/Tools/Replay/Replay.csproj`).

## Package version selection

`eng/Packages.props` selects xUnit v3 4.0.0 for unit and integration projects.
Keep the VS integration harness and its consumers on the centralized xUnit v3
release: runner extension interfaces can differ between releases.
`Xunit.Combinatorial` is centrally pinned to 2.1.41 and `xunit.analyzers` to
2.0.0; individual integration projects should not need explicit overrides.

## xUnit v3 API considerations

- `ITraitAttribute.GetTraits()` no longer exists as an interface method to override;
  trait discovery logic must be inlined into the attribute/discoverer directly.
- `BeforeAfterTestAttribute`'s method signatures changed (additional/renamed
  parameters) — check the current v3 base class signature rather than assuming v2's.
- `ITestOutputHelper` gained new members in v3.
- `AsyncTestSyncContext` was removed in v3.
- `IAsyncLifetime.InitializeAsync()` now returns `ValueTask`, and disposal is through
  `IAsyncDisposable.DisposeAsync()` (`ValueTask`).
- Test-lifecycle methods like `InitializeCoreAsync()` on VS in-process test-service
  types now return `ValueTask` instead of `Task` in the v3 harness — check every
  override when porting a new in-process service.
- xUnit v3 4.0 marks `CollectionBehaviorAttribute.DisableTestParallelization`
  obsolete as an error; use `[assembly: Parallelization(Mode = ParallelMode.None)]`
  with `[assembly: CollectionBehavior(CollectionBehavior.CollectionPerAssembly)]`.
