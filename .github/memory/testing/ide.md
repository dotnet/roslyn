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

## Language Server daemon tests

- `AbstractLanguageServerHostTests.CreateDaemonServerAsync` runs the real multi-client connection manager and named-pipe listener in process. Each connected test client exposes its server's `LspServices`.
- The harness gives daemon tests a dedicated process-level `RoslynTelemetry` and creates its `TelemetrySession` only when the test supplies an explicit level. Each daemon `LanguageServerHost` gets an isolated `RoslynTelemetry` and owns a child session only when telemetry is enabled. Single-server tests use the ambient default `RoslynTelemetry` directly; tests never inherit `COPILOT_TELEMETRY_LEVEL`.
