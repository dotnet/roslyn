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

- Use `[UseExportProvider]` on every test class that constructs `AdhocWorkspace` or
  otherwise creates MEF host services, even when the test does not explicitly request a
  service. `AdhocWorkspace()` uses `MefHostServices.DefaultHost`. After another MEF test
  tears down, `UseExportProviderAttribute` installs a hook that throws
  `Cannot create host services after test tear down` unless this class entered the
  export-provider scope.
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
