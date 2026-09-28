---
coverage: IDE-layer (src/{Analyzers,CodeStyle,Features,Workspaces,EditorFeatures,VisualStudio,LanguageServer}) test base classes & authoring conventions
---

# IDE — Testing

Layer-specific test guidance for the IDE/Workspaces stack under
`src/{Features,Analyzers,EditorFeatures,...}`.

## Test workspace (MEF-dependent tests)

```csharp
[UseExportProvider]
public class MyTests
{
    [Fact]
    public async Task TestSomething()
    {
        var workspace = EditorTestWorkspace.CreateCSharp("class C { }");
        var document = workspace.Documents.Single();
    }
}
```

## Conventions

- Use `[UseExportProvider]` for any test that depends on MEF services (a missing
  attribute typically surfaces as an unrelated-looking failure).
- Analyzer tests inherit from
  `AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest_NoEditor` (and the VB
  equivalents).
- For analyzer/code-fix tests, use `TestInRegularAndScriptAsync` /
  `TestMissingInRegularAndScriptAsync`.
- Prefer raw string literals (`"""..."""`) over verbatim strings (`@"..."`) for
  test source code.
- Keep tests focused — avoid unnecessary intermediary assertions; use `.Single()`
  rather than asserting a count then indexing.
- When a test needs a feature waiter, retrieve the concrete
  `AsynchronousOperationListenerProvider` from the export provider and call
  `GetWaiter`; do not retrieve the interface and cast the listener.
- WPF editor tests use Roslyn's `WpfFactAttribute`/`WpfTheoryAttribute` wrappers
  in `EditorFeatures/TestUtilities/Threading`; those wrappers delegate to the
  aliased `Xunit.StaFact` xUnit v3 discoverers.
- Language Server orchestration tests can pass additional MEF parts to
  `LanguageServerTestComposition.GetSharedExportProvider`. A controllable
  `PartNotDiscoverable` project loader can provide deterministic design-time
  build timing and results without invoking MSBuild.

## Test synchronization context

`UseExportProviderAttribute.Before` installs a `TestSynchronizationContext`
(`src/Workspaces/CoreTestUtilities/MEF/`) as the ambient
`SynchronizationContext` for the duration of each test, and `After` restores the
previous context.

xUnit v3 installs no `SynchronizationContext` of its own, so without this an
`await` in a test body resumes inline on whichever thread completed the awaited
task. When that is a Roslyn worker draining an operation tracked by
`IAsynchronousOperationListener`, the rest of the test — and
`UseExportProviderAttribute.After` — runs nested inside that operation's stack,
and cleanup then blocks forever waiting for the operation it is nested inside.
`TestSynchronizationContext` posts continuations to the outer context when one
exists (preserving WPF dispatcher affinity) and to the thread pool otherwise, so
test bodies and cleanup never resume on a Roslyn worker thread.

Consequences to keep in mind when writing or debugging tests:

- `TestExportJoinableTaskContext.GetEffectiveSynchronizationContext` unwraps this
  context, so `DenyExecutionSynchronizationContext` and WPF dispatcher detection
  behave as if it were not installed. Code that inspects
  `SynchronizationContext.Current` directly sees the wrapper instead.
- `UseExportProviderAttribute` waits up to one minute
  (`CleanupTimeout`) for outstanding asynchronous operations. An operation that
  never completes fails the test with a `TimeoutException` listing the pending
  listener tokens rather than hanging the test host — that exception text is the
  starting point for diagnosing a leaked operation.

## VS integration tests (`IdeFact`/`IdeTheory`)

`src/VisualStudio/IntegrationTest/` is a separate suite from the unit tests above.
It runs on xUnit v3 and references the shared Roslyn and Razor test utilities.
See `testing/vs-integration-tests-xunit-v3.md` for its project-setup conventions
and xUnit v3 API requirements.
