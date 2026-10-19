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
- Language Server orchestration tests can pass additional MEF parts to
  `LanguageServerTestComposition.GetSharedExportProvider`. A controllable
  `PartNotDiscoverable` project loader can provide deterministic design-time
  build timing and results without invoking MSBuild.

## Visual Studio package tests

- Package tests that use the VS SDK `GlobalServiceProvider` fixture run in an
  isolated AppDomain using `AppDomainUtils`; see `VisualBasicPackageTests`.
  SDK statics can retain the mocked UI context after the fixture is disposed,
  so a fixture in one xUnit collection can affect other collections.
- Create Roslyn MEF test environments on a WPF dispatcher with a
  `UseExportProviderAttribute` scope, then await a separate helper for SDK-main-thread
  work. The SDK mock synchronization context is not the WPF dispatcher context
  expected by Roslyn's MEF threading exports.
- Use the existing native `MockComWrapperFactory` for aggregation tests. Cast COM
  interfaces directly to exercise QI; reflection-based type-assignability checks
  do not model RCW interface support.
