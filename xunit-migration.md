# xUnit 2 → xUnit 3 Migration Notes

## Overview

This document tracks the migration of `dotnet/roslyn` from **xUnit 2.9.2** to **xUnit v3 3.2.2**
(`xunit.v3`). It captures every breaking change, workaround, and per-component status to make the
migration reproducible and auditable.

---

## Ralph Loop Methodology

A "Ralph loop" is used to guarantee correctness at each step:

1. **Before**: Run the integration test suite and record baseline pass/fail/skip counts.
2. **Change**: Apply a well-scoped change.
3. **After**: Re-run the same suite and compare.
4. **No new skips**: Assert `skipped_after <= skipped_before`.
5. **No false negatives**: Assert `failed_after <= failed_before` (excluding expected churn).

Baseline command (30-second smoke test):

```bash
dotnet test src/LanguageServer/Microsoft.CommonLanguageServerProtocol.Framework.UnitTests/ \
    --framework net10.0 --no-restore
# Expected: 26 passed, 0 failed, 0 skipped
```

---

## Package Mapping

| xUnit 2 package | xUnit v3 package | Notes |
|---|---|---|
| `xunit` | `xunit.v3` | Meta-package |
| `xunit.core` | `xunit.v3.core` | Executable test projects only (`<OutputType>Exe</OutputType>`) |
| `xunit.assert` | `xunit.v3.assert` | Add to all library utility projects explicitly |
| `xunit.extensibility.core` | `xunit.v3.extensibility.core` | Library/utility projects |
| `xunit.extensibility.execution` | *(removed)* | Merged into `xunit.v3.core` |
| `xunit.abstractions` | *(removed)* | All interfaces moved into xunit.v3.* assemblies |
| `xunit.runner.utility` | `xunit.v3.runner.utility` | For tooling that hosts test execution |
| `Xunit.Combinatorial 1.6.24` | `Xunit.Combinatorial 2.0.24` | Required for xUnit v3 compatibility |

**Central version file:** `eng/Packages.props`  
**Per-project targets:** `eng/targets/XUnit.targets` — added `<OutputType>Exe</OutputType>` for all test executables.

---

## Key API Breaking Changes

### 1. `xunit.abstractions` removed

`ITestMethod`, `ITypeInfo`, `IMethodInfo`, `IAttributeInfo`, `IAssemblyInfo` are all removed.
Any code referencing these types must be updated:

- `WpfTestSharedData.ExecutingTest(ITestMethod)` → replaced with `ExecutingTest(MethodInfo)` only.
- Trait discoverer infrastructure completely redesigned (see §3).

### 2. `IAsyncLifetime` returns `ValueTask`

```csharp
// xUnit 2
Task InitializeAsync();
Task DisposeAsync();

// xUnit 3
ValueTask InitializeAsync();
ValueTask DisposeAsync();
```

~40 files updated; affected classes include `AbstractInteractiveHostTests`,
`AbstractMetadataAsSourceTests`, Razor test harness files, and all `New.IntegrationTests/`.

### 3. Trait discoverer infrastructure redesigned

| xUnit 2 | xUnit 3 |
|---|---|
| `[TraitDiscoverer("...", "...")]` on attribute | *(removed)* |
| `ITraitDiscoverer.GetTraits(IAttributeInfo)` | *(removed)* |
| `ITraitAttribute` (marker) | `ITraitAttribute` with `GetTraits()` method |

In v3 the trait attribute implements `GetTraits()` directly:

```csharp
// xUnit v3
public sealed class CompilerTraitAttribute : Attribute, ITraitAttribute
{
    public IReadOnlyCollection<KeyValuePair<string, string>> GetTraits()
        => [new("Compilers", Feature.ToString())];
}
```

Deleted files: `CompilerTraitDiscoverer.cs`, `WorkItemTraitDiscoverer.cs`.

### 4. `BeforeAfterTestAttribute` signature change

```csharp
// xUnit 2 (Xunit.Sdk)
public virtual void Before(MethodInfo methodUnderTest) { }
public virtual void After(MethodInfo methodUnderTest) { }

// xUnit v3 (Xunit.v3)
public virtual void Before(MethodInfo methodUnderTest, IXunitTest test) { }
public virtual void After(MethodInfo methodUnderTest, IXunitTest test) { }
```

Affected: `UseCultureAttribute`, `ValidatePooledObjectsAttribute`, `UseExportProviderAttribute`,
`InitializeTestFileAttribute`, `LogIntegrationTestAttribute`, `ExceptionBeforeTestAttribute`,
`ExceptionAfterTestAttribute`.

### 5. `AsyncTestSyncContext` removed

`TestExportJoinableTaskContext.GetEffectiveSynchronizationContext()` previously returned
`AsyncTestSyncContext.Current ?? SynchronizationContext.Current`. In v3 the class is gone; the
method now returns `SynchronizationContext.Current` directly.

### 6. `ITestOutputHelper` gained new members

```csharp
// New in xUnit v3
string Output { get; }
void Write(string message);
void Write(string format, params object[] args);
```

`AppDomainTestOutputHelper.cs` was updated to implement these (guarded by `#if NET472` where needed).

### 7. `XunitException` namespace

`XunitException` is in `Xunit.Sdk` (from `xunit.v3.assert`). Utility libraries that use it must
add `using Xunit.Sdk;` even though they use `Xunit.v3` namespace for `BeforeAfterTestAttribute`.

### 8. `using Xunit.Abstractions` removal

~533 files had `using Xunit.Abstractions;`:

- 476 files: `using Xunit.Abstractions;` removed outright (was only used for `ITestOutputHelper`
  which moved to `using Xunit;`).
- 57 files: replaced with `using Xunit;` (needed `ITestOutputHelper` explicitly).

### 9. WPF threading infrastructure (xUnit v3 extensibility model)

xUnit v3 replaced the `XunitTestInvoker`/`XunitTestMethodRunner` extension model with:

- `XunitTestCaseRunner.RunTest(XunitTestCaseRunnerContext, IXunitTest)` — **virtual**, the key extension point.
- `XunitTestCaseRunner.Instance` — singleton; its backing field `<Instance>k__BackingField` can be
  swapped at startup via reflection to inject a custom runner.

**New WPF dispatch model:**

```
WpfFactDiscoverer (static ctor)
  └─ WpfTestCaseRunner.InjectIfNeeded()
       └─ sets XunitTestCaseRunner.<Instance>k__BackingField = WpfTestCaseRunner.WpfInstance

Test run:
WpfTestCaseRunner.RunTest(ctxt, test)
  └─ if ctxt.TestCase is WpfTestCase/WpfTheoryTestCase:
       Task.Factory.StartNew(async () => new XunitTestRunner().Run(...),
           scheduler: SynchronizationContextTaskScheduler(sta.DispatcherSynchronizationContext))
```

**Files rewritten:**

| File | Action |
|---|---|
| `WpfTestCase.cs` | Rewritten — subclasses `XunitTestCase` with new 13-arg constructor |
| `WpfTheoryTestCase.cs` | Rewritten — same as `WpfTestCase` (no separate runner needed) |
| `WpfTestCaseRunner.cs` | Rewritten — subclasses `XunitTestCaseRunner`, overrides `RunTest` |
| `WpfFactDiscoverer.cs` | Rewritten — no-arg ctor; uses `TestIntrospectionHelper` APIs |
| `WpfTestRunner.cs` | Converted to static class (only `RequireWpfFact` + reason field) |
| `WpfTheoryTestCaseRunner.cs` | **Deleted** — functionality merged into `WpfTestCaseRunner` |
| `WpfTestSharedData.cs` | Removed `ExecutingTest(ITestMethod)` overload (v2 interface gone) |

**Key v3 reflection API facts:**

- `TestIntrospectionHelper.GetTestCaseDetails(discoveryOptions, testMethod, factAttribute, testMethodArguments, timeout, baseDisplayName)` returns a ValueTuple whose members map to `XunitTestCase` constructor parameters.
- `TestIntrospectionHelper.GetTraits(IXunitTestMethod, ITheoryDataRow)` — pass `null` for dataRow on Fact tests.
- `XunitTestRunner` has a no-arg constructor and an instance `Run(...)` method (no `Instance` singleton, not static).

---

## Per-Component Migration Status

| Component | Status | Notes |
|---|---|---|
| `eng/Packages.props` | ✅ Done | All versions updated |
| `eng/targets/XUnit.targets` | ✅ Done | `<OutputType>Exe</OutputType>` added |
| `Microsoft.CodeAnalysis.Test.Utilities` | ✅ Builds (net10.0) | extensibility.core + assert |
| `Microsoft.CodeAnalysis.Workspaces.Test.Utilities` | ✅ Builds (net10.0) | extensibility.core + assert |
| `EditorFeatures.Test.Utilities` Threading/ | ✅ Rewritten | Validates on Windows (WPF/net472) |
| `using Xunit.Abstractions` removal | ✅ Done | ~533 files |
| `IAsyncLifetime` ValueTask | ✅ Done | ~40 files |
| Trait discoverer redesign | ✅ Done | CompilerTrait, WorkItem, etc. |
| `BeforeAfterTestAttribute` signatures | ✅ Done | All 7 attributes updated |
| `XUnitShared` VS integration harness | ⚠️ Pending | ~28 files; extends XunitTestFramework |
| `TestDiscoveryWorker/Program.cs` | ⚠️ Pending | `XunitFrontController` API changed |
| Razor FormattingTest discoverers | ⚠️ Pending | Same trait pattern as WpfFact |

---

## Runtime Notes

- **`<OutputType>Exe</OutputType>`** is required for every test project using `xunit.v3.core`.
  Projects omitting this will fail with a linker/entrypoint error at runtime.
- **`xunit.v3.extensibility.core`** targets `netstandard2.0` and is for library (utility) projects.
  Do **not** add `xunit.v3.core` to non-executable projects.
- **NuGet feed**: The internal Azure DevOps dnceng/public feed may be unavailable in some
  environments. Workaround: `dotnet restore --source https://api.nuget.org/v3/index.json`.
- **Windows required** for WPF/net472 test validation. The `NETSDK1136` error on Linux when
  building `EditorFeatures.Test.Utilities` with `net10.0` is expected and harmless — that TFM is
  never used for WPF tests in practice.
- **`Xunit.Combinatorial`** must be `2.0.24` (not `1.x`); v1 depends on the removed
  `xunit.extensibility.core` (v2) package.

---

## Outstanding Work

1. **`XUnitShared` VS integration test harness** (`src/VisualStudio/IntegrationTest/Harness/XUnitShared/`):
   ~28 files extending `XunitTestFramework`, `XunitTestFrameworkExecutor`,
   `XunitTestAssemblyRunner`, `XunitTestCase`. All runner/executor APIs changed in v3.

2. **`TestDiscoveryWorker/Program.cs`**: Replace `XunitFrontController(AppDomainSupport.IfAvailable, ...)`
   with the v3 `XunitFrontController` API (app-domain support removed).

3. **Razor FormattingTest discoverers** in
   `src/Razor/src/Compiler/Microsoft.AspNetCore.Razor.Language/test/`: Same
   `ITraitDiscoverer`→`ITraitAttribute.GetTraits()` pattern as `CompilerTraitAttribute`.

4. **Final Ralph loop**: After all components compile, re-run baseline and full integration suite to
   confirm zero regressions and zero new skips.
