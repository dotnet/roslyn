---
coverage: VS integration test harness (IdeFact/IdeTheory) on xUnit v3 — project setup gotchas, scope, and v2→v3 API shape differences
---

# VS Integration Tests — xUnit v3

`src/VisualStudio/IntegrationTest/` hosts the `IdeFact`/`IdeTheory` VS-integration-test
harness (`Microsoft.VisualStudio.Extensibility.Testing.Xunit*`) and its consumers
(`New.IntegrationTests`, `Roslyn.SDK.IntegrationTests`). This harness and its direct
consumers run on **xUnit v3**, independent of the rest of the repo's unit tests, which
remain on xUnit v2. The two test-utility forks
(`Microsoft.CodeAnalysis.Test.Utilities.IntegrationTests`,
`Microsoft.CodeAnalysis.Workspaces.Test.Utilities.IntegrationTests`) are dedicated v3
copies of the unit-test utility assemblies — they share no code with the v2 unit-test
side by design, so the two suites can be upgraded/maintained independently.

`src/VisualStudio/IntegrationTest/IntegrationTestBuildProject.csproj` (a
`Microsoft.Build.Traversal` project) is the actual CI entry point that builds only the
projects needed for VS integration tests; it is more authoritative than building the
broader `Ide.slnf` filter, which also pulls in unrelated projects.

**Razor's VS integration tests (`Microsoft.AspNetCore.Razor.Test.Common.IntegrationTests`,
`Microsoft.VisualStudio.Razor.IntegrationTests`) are intentionally NOT ported to v3.**
They still consume the (now-v3) `Microsoft.CodeAnalysis.Test.Utilities.IntegrationTests`
fork via `ProjectReference` but keep v2 xUnit packages directly, so they no longer
build. `IntegrationTestBuildProject.csproj` has their `ProjectReference` commented out.
`dotnet build Ide.slnf` still fails on them (NU1109) because `.slnf` restore evaluates
the full underlying `.sln`'s project graph, not just the filtered subset — this is
accepted as out of scope; do not try to "fix" it without porting Razor's integration
tests (which also need custom v2-specific discoverers such as
`FormattingFactDiscoverer`/`FormattingTheoryDiscoverer` rewritten for v3).

## Arcade `IsTestProject` / naming-heuristic gotcha

`eng/targets/XUnit.targets` unconditionally injects **v2** xUnit `PackageReference`s
into any project where `IsTestProject == true`. Arcade's `Tests.props` sets
`IsTestProject`/`IsIntegrationTestProject`/`IsUnitTestProject` to `true` by
**name-matching convention**, independently of each other:

- `IsIntegrationTestProject` defaults `true` when the project name ends in
  `.IntegrationTests`.
- `IsUnitTestProject` defaults `true` when the project name ends in `.UnitTests` or
  `.Tests`.
- `IsTestProject` defaults `true` if either of the above is `true`.

Any project that must build against xUnit v3 packages instead needs
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

## Package version overrides needed for xUnit v3 projects

Central Package Management centrally pins v2-era defaults that must be overridden
per-project when a project needs v3:

- **`Xunit.Combinatorial`**: centrally pinned to a v2-dependent version. Any project
  that references it directly, or transitively pulls it in through a v3
  `ProjectReference`, needs
  `<PackageReference Include="Xunit.Combinatorial" VersionOverride="2.1.41" />` added
  directly in its own csproj (CPM's transitive pinning otherwise resolves it back to
  the v2-compatible default and produces an NU1109 downgrade error), even if the
  project's own source never references the package.
- **`xunit.analyzers`**: centrally pinned below what `xunit.v3` requires. Any project
  that references the `xunit.v3` package directly (as opposed to only
  `xunit.v3.extensibility.core`) — typically Exe-based self-executing test runners —
  needs `<PackageReference Include="xunit.analyzers" VersionOverride="1.24.0" />`.

## v2 → v3 API shape differences hit during migration

- `ITraitAttribute.GetTraits()` no longer exists as an interface method to override;
  trait discovery logic must be inlined into the attribute/discoverer directly.
- `BeforeAfterTestAttribute`'s method signatures changed (additional/renamed
  parameters) — check the current v3 base class signature rather than assuming v2's.
- `ITestOutputHelper` gained new members in v3.
- `AsyncTestSyncContext` was removed in v3.
- Test-lifecycle methods like `InitializeCoreAsync()` on VS in-process test-service
  types now return `ValueTask` instead of `Task` in the v3 harness — check every
  override when porting a new in-process service.
- Custom `[CollectionBehavior]`/`TestFrameworkAttribute` v2 assembly attributes
  (e.g. a stray `XUnitAssemblyInfo.cs`) are incompatible with v3 and should be removed
  once `IsTestProject=false` stops v2 package injection for a given project.
