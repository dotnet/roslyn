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

## VS integration test gates

- VS integration tests can be scoped with the `TestGate` trait
  (`Traits.TestGates` in `src/Compilers/Test/Core/Traits/Traits.cs`).
- `eng/pipelines/test-integration-helix.yml` exposes a `testFilter` parameter that
  defaults to `TestGate!=NuGetPackageUpgrade`, so ordinary integration jobs skip the
  NuGet package upgrade validation tests; a dedicated job opts in with
  `TestGate=NuGetPackageUpgrade`. DartLab runs are already scoped to
  `TestGate=RoslynVSIntegration` and need no change.
- Package Manager Console commands are driven from integration tests through
  `PackageManagerConsoleInProcess`, which waits on a sentinel result file written by
  the executed script rather than assuming `DTE.ExecuteCommand` is synchronous.
