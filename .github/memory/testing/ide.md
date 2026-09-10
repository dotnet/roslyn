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
## Visual Studio integration harness

- `src/VisualStudio/IntegrationTest/Harness/XUnitShared` supplies custom xUnit v3
  fact and theory discoverers. Its Visual Studio-specific test cases must carry
  xUnit's source information, traits, skip metadata, and unique IDs when adding
  the `VisualStudioInstanceKey` suffix.
- Visual Studio integration-harness retries (`[IdeFact(MaxAttempts = ...)]` /
  `[IdeTheory(MaxAttempts = ...)]`) only count the terminal attempt in the final
  run summary; intermediate failed attempts are remapped to retry notifications.
- Discovery also injects synthetic `IdeInstanceTestCase` launches per Visual
  Studio instance group; only instance-only launches should leave the IDE
  running after the collection completes.
- .NET Framework xUnit v3 test projects in the VisualStudio integration-test
  harness still keep `TargetExt=.dll` to satisfy the repo's unit-test naming
  checks, and add a post-build step to copy the built DLL/config to a matching
  `.exe` app host for discovery and execution.
- If a .NET Framework test project in this layer uses `ThrowingTraceListener`
  under xUnit v3, include an explicit `app.config` when the listener type lives
  outside the default `Microsoft.CodeAnalysis.Test.Utilities` assembly, so the
  out-of-proc test host can start with the correct listener binding.
