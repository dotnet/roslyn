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
- WPF editor tests use Roslyn's `WpfFactAttribute`/`WpfTheoryAttribute` in
  `EditorFeatures/TestUtilities/Threading`. Their discoverers create
  `WpfTestCase`s that `WpfTestCaseRunner` runs serially on one persistent STA
  Dispatcher thread, so async continuations and disposal stay on the UI thread
  that `ThreadHelper` recognizes. Unconditionally skipped `[WpfTheory]` methods
  report one skipped case (stock xUnit v3 behavior), not one per data row.
- Language Server orchestration tests can pass additional MEF parts to
  `LanguageServerTestComposition.GetSharedExportProvider`. A controllable
  `PartNotDiscoverable` project loader can provide deterministic design-time
  build timing and results without invoking MSBuild.

## Completing asynchronous operations from test callbacks

xUnit v3 installs no `SynchronizationContext`, so an `await` in a test body
resumes inline on whichever thread completed the awaited task. When that is a
Roslyn worker draining an operation tracked by `IAsynchronousOperationListener`
(for example a workspace-changed handler run from an `AsyncBatchingWorkQueue`),
the rest of the test, and then `UseExportProviderAttribute.After`, runs nested
inside that operation. Cleanup then waits for an operation it is nested inside.

Create a `TaskCompletionSource` that a test completes from such a callback with
`TaskCreationOptions.RunContinuationsAsynchronously`, so the test continues on
another thread.

`UseExportProviderAttribute` waits up to one minute (`CleanupTimeout`) for
outstanding asynchronous operations. An operation that never completes fails the
test with a `TimeoutException` listing the pending listener tokens rather than
hanging the test host. That exception text is the starting point for diagnosing
a leaked operation.

## VS integration tests (`IdeFact`/`IdeTheory`)

`src/VisualStudio/IntegrationTest/` is a separate suite from the unit tests above.
It runs on xUnit v3 and references the shared Roslyn and Razor test utilities.
See `testing/vs-integration-tests-xunit-v3.md` for its project-setup conventions
and xUnit v3 API requirements.
