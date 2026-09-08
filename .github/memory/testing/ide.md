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
- Language Server orchestration tests can pass additional MEF parts to
  `LanguageServerTestComposition.GetSharedExportProvider`. A
  `PartNotDiscoverable` scripted service factory is useful for deterministic
  tests of canonical project-load timing, failure, and request-context behavior.
- On-demand project-loading integration tests derive from
  `AbstractLanguageServerMefHost`, materialize valid projects with
  `MaterializedLspWorkspace`, and use the production LSP services and MSBuild
  evaluation. Keep this suite focused because composition and restore are
  comparatively expensive.
- For some LanguageServer services created via `ILspServiceFactory` (for example,
  `WorkspaceProjectDiscoveryService`), `GetRequiredLspService<T>()` in protocol
  tests may not resolve the concrete service type directly. In these cases,
  prefer direct unit tests that instantiate the service and use temporary
  filesystem state rather than constructor-injected delegates.
