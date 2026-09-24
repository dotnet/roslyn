---
coverage: VS integration test harness (IdeFact/IdeTheory) on xUnit v3 — project setup gotchas, scope, and v2→v3 API shape differences
---

# VS Integration Tests — xUnit v3

`src/VisualStudio/IntegrationTest/` hosts the `IdeFact`/`IdeTheory` VS-integration-test
harness (`Microsoft.VisualStudio.Extensibility.Testing.Xunit*`) and its consumers
(`New.IntegrationTests`, `Roslyn.SDK.IntegrationTests`,
`Microsoft.VisualStudio.Razor.IntegrationTests`). This harness and its direct
consumers run on **xUnit v3 3.0.1**, independent of the rest of the repo's unit tests,
which use xUnit v3 4.0.0. The Roslyn test-utility forks
(`Microsoft.CodeAnalysis.Test.Utilities.IntegrationTests`,
`Microsoft.CodeAnalysis.Workspaces.Test.Utilities.IntegrationTests`) are dedicated v3
copies of the unit-test utility assemblies — they share no code with the unit-test
side by design, so the two suites can be upgraded/maintained independently.

Razor has its own dedicated v3 test-utility forks for VS integration tests:
`Microsoft.AspNetCore.Razor.Test.Common.IntegrationTests` and
`Microsoft.AspNetCore.Razor.Test.Common.Tooling.IntegrationTests`. These mirror the
Razor unit-test utility projects but are compiled independently against xUnit v3, so
do not point VS integration-test code back at the Razor unit-test utility projects.

`src/VisualStudio/IntegrationTest/IntegrationTestBuildProject.csproj` (a
`Microsoft.Build.Traversal` project) is the actual CI entry point that builds only the
projects needed for VS integration tests; it is more authoritative than building the
broader `Ide.slnf` filter, which also pulls in unrelated projects.

Razor's VS integration tests are included in `IntegrationTestBuildProject.csproj`.
`dotnet build Ide.slnf` should also stay green for these projects; if restore reports
NU1109 involving Razor integration tests, check for a direct project-level
`Xunit.Combinatorial` `VersionOverride="2.1.41"` or a v2 xUnit package reference
leaking in through the Razor integration-test utility forks.

## Arcade `IsTestProject` / naming-heuristic gotcha

`eng/targets/XUnit.targets` selects packages using `UseXunitV3`, which defaults to
true for non-integration tests. VS integration projects manage their own package
references instead. The build's naming conventions set
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

`eng/Packages.props` selects xUnit v3 3.0.1 for project names ending in
`.IntegrationTests` and names beginning with
`Microsoft.VisualStudio.Extensibility.Testing.`. Unit-test projects use 4.0.0.
Keep integration forks and their consumers on the same release: runner extension
interfaces differ between these releases. `Xunit.Combinatorial` is centrally
pinned to 2.1.41 and `xunit.analyzers` to 2.0.0; individual integration projects may
retain explicit overrides.

## v2 → v3 API shape differences hit during migration

- `ITraitAttribute.GetTraits()` no longer exists as an interface method to override;
  trait discovery logic must be inlined into the attribute/discoverer directly.
- `BeforeAfterTestAttribute`'s method signatures changed (additional/renamed
  parameters) — check the current v3 base class signature rather than assuming v2's.
- `ITestOutputHelper` gained new members in v3.
- `AsyncTestSyncContext` was removed in v3.
- `IAsyncLifetime.InitializeAsync()` now returns `ValueTask`, and disposal is through
  `IAsyncDisposable.DisposeAsync()` (`ValueTask`) rather than a `Task`-returning
  xUnit v2 dispose method.
- Test-lifecycle methods like `InitializeCoreAsync()` on VS in-process test-service
  types now return `ValueTask` instead of `Task` in the v3 harness — check every
  override when porting a new in-process service.
- Custom `[CollectionBehavior]`/`TestFrameworkAttribute` v2 assembly attributes
  (e.g. a stray `XUnitAssemblyInfo.cs`) are incompatible with v3 and should be removed
  once `IsTestProject=false` stops v2 package injection for a given project.
