# Integration Test Artifact Structure

## Published Artifact Naming

Log artifacts from integration test runs follow this naming pattern:

```
{RunNumber}-Logs {Configuration} OOP64_{Bool} LspEditor_{Bool} {BuildNumber}
```

Examples:
- `1-Logs Debug OOP64_True LspEditor_False 20260326.17`
- `2-Logs Release OOP64_False LspEditor_False 20260326.17`

The configuration (Debug/Release) in the artifact name matches the integration test job configuration:
- `VS_Integration_Debug_64` → artifacts with "Debug" in the name
- `VS_Integration_Release_32` → artifacts with "Release" in the name

## Artifact Contents

### Test Result Files

| File Pattern | Description |
|-------------|-------------|
| `{HH.MM.SS}-{TestClass}.{TestMethod}-{ExceptionType}.Activity.xml` | Exception log with VS activity entries and full stack traces |
| `{HH.MM.SS}-{TestClass}.{TestMethod}-{ExceptionType}.png` | Screenshot of VS at the time of the exception |
| `Screenshots/{TestClass}.{TestMethod}/*.png` | Screenshots captured during test execution |
| `MEFErrors*.txt` | MEF composition errors (missing exports, failed imports) |
| `VSIXInstaller-{GUID}.log` | VSIX sideloading log |
| `ServiceHub*.log` | ServiceHub OOP process logs |
| `StartingBuild.png` | Screenshot taken when test harness first starts VS |

### Root-Level Screenshots (Timeout Artifacts)

When a test job times out, the test harness captures screenshots at the root directory level
(not under `Screenshots/`). These show the state of Visual Studio when the pipeline killed the job.

### Activity XML Structure

```xml
<entries>
  <entry>
    <record>1</record>
    <time>2026/03/27 01:14:49.354</time>
    <type>Information</type>
    <source>VisualStudio</source>
    <description>Microsoft Visual Studio 2026 version: 18.0.11518.184</description>
  </entry>
  <entry>
    <record>721</record>
    <time>2026/03/27 01:15:29.005</time>
    <type>Error</type>
    <source>VisualStudioErrorReportingService</source>
    <description>Feature 'Solution Events' is currently unavailable due to an internal error.
Microsoft.ServiceHub.Framework.ServiceActivationFailedException : ...</description>
  </entry>
</entries>
```

Key fields:
- `<type>Error</type>` — Error entries contain the failure details
- `<source>` — The VS component that reported the error
- `<description>` — Full error message with stack trace

### Common Error Sources

| Source | Meaning |
|--------|---------|
| `VisualStudioErrorReportingService` | Roslyn feature failure (remote service activation, etc.) |
| `GlobalBrokeredServiceContainer` | ServiceHub brokered service failure |
| `Editor or Editor Extension` | Editor/extension crash (often ObjectDisposedException) |

## Common Root Cause Patterns

### Assembly Load Failure
```
Could not load file or assembly 'System.Runtime, Version=10.0.0.0'
```
- The ServiceHub process can't find a required .NET runtime assembly
- Usually caused by TFM changes (e.g., `$(NetVS)` updated) without matching runtime on CI queue
- Cascades to ALL remote Roslyn services

### Service Activation Failure
```
ServiceActivationFailedException: Activating the "Microsoft.VisualStudio.LanguageServices.XxxCore64" service failed
```
- A specific Roslyn remote service can't be activated
- Inner exception usually reveals the root cause (assembly load, MEF error, etc.)

### MEF Composition Error
- Missing `[Export]` attributes or version mismatches
- Usually indicates VSIX packaging issues

### VSIX Install Failure
```
Install Error : System.IO.FileNotFoundException: Unable to find the specified file
```
- VS instance state is corrupted or locked by another process
- The Roslyn VSIX didn't sideload properly into the RoslynDev hive
