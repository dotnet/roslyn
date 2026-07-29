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
- Language Server daemon integration tests derive from
  `AbstractLanguageServerMefHost` and use `CreateDaemonServerAsync`, which starts
  the same multi-client connection manager and named-pipe listener as product
  daemon mode. Its nullable `useSharedMetadataCache` override supports
  composition comparisons without replacing an otherwise explicit server
  configuration. Metadata lifetime tests should keep references in each real
  host workspace's current solution, remove projects from the solution being
  closed, and use `ObjectReference<T>` from a non-inlined helper to avoid
  async/JIT temporaries affecting GC assertions.
  `TestLspServer.OpenProjectsAsync` and `OpenSolutionAsync` exercise the
  corresponding LSP notifications and wait for the
  project-initialization-complete callback before returning.
