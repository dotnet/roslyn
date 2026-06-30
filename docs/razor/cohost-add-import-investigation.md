# Cohost AddImport investigation

This document captures the current state of the intermittent Razor cohost `AddImport` / `AddUsing` failure investigation so a future session can continue from a failing CI run instead of repeating the same exploration.

## Problem statement

The tracked symptom is that Razor cohost code action tests intermittently fail because they expect `AddImport`, but only `GenerateType` is offered.

The main public tracker is:

- `dotnet/roslyn#83775`

The important detail is that this is not a generic "code actions missing" failure. When it happens, `GenerateType` still appears, which means the delegated Roslyn query is still returning some actions, but the missing-import path is disappearing.

## Current understanding

The strongest lead is that the missing-import brokered service call is intermittently failing and getting silently collapsed into "no fixes".

The most useful chain is:

1. Razor cohost requests delegated C# code actions.
2. Roslyn AddImport runs through `AbstractAddImportFeatureService.GetFixesAsync(...)`.
3. If a remote client is available, that path calls `IRemoteMissingImportDiscoveryService.GetFixesAsync(...)`.
4. `BrokeredServiceConnection.TryInvokeAsync(...)` catches remote exceptions and returns an empty `Optional<T>`.
5. `AbstractAddImportFeatureService.GetFixesAsync(...)` converts that to `[]`.
6. Razor filtering still sees `GenerateType`, but no `AddImport` / `FullyQualify`.

That exactly matches the failure shape from the tests.

## Evidence gathered so far

### 1. Local repro was not reliable

Filtered `AddUsingTests` passed locally in both importing test projects, including repeated runs. That does **not** disprove the CI failure, but it means local repro is not the primary way to drive this investigation.

### 2. Feedback logs showed MissingImportDiscovery serialization failures

User-provided LogHub / ServiceHub logs from a real feedback report showed repeated warnings from the MissingImport discovery service during active code action requests:

- `MessagePackSerializationException: Failed to deserialize System.Object value`
- `Extension type code 0x64 is not supported by the PrimitiveObjectFormatter`

Those warnings overlapped Roslyn `textDocument/codeAction` activity in the LSP logs.

### 3. `0x64` strongly suggests typeless MessagePack payloads

`0x64` is the MessagePack typeless extension code. In this AddImport payload, the most interesting typeless values are nested inside `AddImportOptions.CleanupOptions`:

- `SimplifierOptions`
- `SyntaxFormattingOptions`

Those are serialized through the typeless formatter path in Roslyn remoting.

### 4. Other log failures existed, but looked secondary

The feedback logs also contained:

- repeated `DidSaveHandler` document-vs-textdocument exceptions
- a separate Razor source generator crash

Those may matter for the customer scenario overall, but they were not as directly aligned with the `AddImport` test symptom as the MissingImportDiscovery serialization failure.

### 5. CI build 1460878 reproduced the test failure but not the old trace

Build `https://dev.azure.com/dnceng-public/public/_build/results?buildId=1460878` finally reproduced the target failure in `Test_Linux_Debug_Spanish_Single_Machine`. The downloaded xUnit failure log contained 12 Razor cohost `AddUsing` / `FullyQualify` failures with the expected shape: `AddImport` / `FullyQualify` missing while `GenerateType` was still offered.

That run did **not** contain any of the earlier `AddImport LocalRequest`, `AddImport RemoteRequest`, or `Typeless MessagePack` markers. The most likely explanations were that the failing path ran in-process, AddImport was never reached, or the old trace output did not flow into the xUnit artifact. The current branch adds the assertion-message flight recorder and local AddImport logging specifically to close that gap.

## Why `GenerateType` can still appear

Razor does not surface all delegated C# actions uniformly.

- `GenerateType` survives via the general C# provider allowlist.
- `AddImport` / `FullyQualify` only appear if Roslyn actually returned missing-import candidates and Razor recognizes them.

So "only `GenerateType` is offered" is a strong signal that the AddImport-specific Roslyn results are already missing before Razor finishes filtering.

## Relevant code paths

### Razor side

- `src/Razor/src/Razor/test/Microsoft.CodeAnalysis.Razor.CohostingShared.UnitTests/CodeActions/AddUsingTests.cs`
- `src/Razor/src/Razor/test/Microsoft.CodeAnalysis.Razor.CohostingShared.UnitTests/CodeActions/CohostCodeActionsEndpointTestBase.cs`
- `src/Razor/src/Razor/src/Microsoft.CodeAnalysis.Razor.CohostingShared/CodeActions/CohostCodeActionsEndpoint.cs`
- `src/Razor/src/Razor/src/Microsoft.CodeAnalysis.Remote.Razor/CodeActions/CodeActionsService.cs`
- `src/Razor/src/Razor/src/Microsoft.CodeAnalysis.Remote.Razor/CodeActions/CSharp/TypeAccessibilityCodeActionProvider.cs`
- `src/Razor/src/Razor/src/Microsoft.CodeAnalysis.Remote.Razor/CodeActions/CSharp/CSharpCodeActionProvider.cs`

### Roslyn AddImport / remoting side

- `src/Features/Core/Portable/AddImport/AbstractAddImportFeatureService.cs`
- `src/Features/Core/Portable/AddImport/Remote/IRemoteMissingImportDiscoveryService.cs`
- `src/Features/Core/Portable/AddImport/AddImportOptions.cs`
- `src/Workspaces/Remote/ServiceHub/Services/MissingImportDiscovery/RemoteMissingImportDiscoveryService.cs`
- `src/Workspaces/Remote/Core/BrokeredServiceConnection.cs`
- `src/Workspaces/Remote/Core/Serialization/MessagePackFormatters.cs`

## Instrumentation added

The current branch adds a verbose, temporary "flight recorder" for the full Razor cohost -> delegated C# -> Razor filtering -> Roslyn AddImport chain.

### Assertion-message trace capture

`CohostCodeActionsEndpointTestBase` now records `Trace.WriteLine(...)` output during each code-action request. When an expected code action is missing, the assertion message includes a `Code action trace:` block with the captured trace. This is the most reliable first artifact for the next CI failure because it is embedded directly in the failing xUnit output, not just in side-channel logs.

The test trace includes `RazorCodeActionTrace` messages from:

- `CohostCodeActionsEndpoint`
  - `Cohost.Request`
  - `Cohost.RequestInfo`
  - `Cohost.CSharpRequest`
  - `Cohost.CSharpRawActions`
  - `Cohost.CSharpConvertedActions`
  - `Cohost.DelegatedActions`
  - `Cohost.FinalResult`
- `CodeActionsService`
  - `RemoteCodeActions.Entry`
  - `RemoteCodeActions.Context`
  - `RemoteCodeActions.DelegatedAfterLanguageProcessing`
  - `RemoteCodeActions.ExtractName.*`
  - `RemoteCodeActions.RazorProviderResults`
  - `RemoteCodeActions.Filter.ProviderResult`
  - `RemoteCodeActions.FilteredDelegatedResults`
  - `RemoteCodeActions.Final`
- Razor C# filtering providers
  - `CSharpProvider.*`
  - `TypeAccessibility.*`

### AddImport request/search logging

`AddImportTrace.CreateRemoteCallMessage(...)` produces a compact summary of AddImport calls, including document/project/language, diagnostic ID, span, max results, package source count, search option values, cleanup option runtime types, remote-client availability, and fix summaries.

AddImport now logs from both the provider and the feature service:

- `AddImport ProviderRequest`
- `AddImport ProviderOptions`
- `AddImport ProviderFixesForDiagnostics`
- `AddImport ProviderRegisterFixes`
- `AddImport LocalRequest`
- `AddImport LocalResponse`
- `AddImport LocalResponseFailure`
- `AddImport LocalInProcRequest`
- `AddImport LocalInProcResponse`
- `AddImport CurrentProcessNode`
- `AddImport CurrentProcessCanAddImport`
- `AddImport CurrentProcessReferences`
- `AddImport CurrentProcessFixData`
- `AddImport CurrentProcessResult`
- `AddImport SearchStart`
- `AddImport SearchStageComplete`
- `AddImport SearchStageSkipped`
- `AddImport SearchExactComplete`
- `AddImport SearchFuzzyComplete`
- `AddImport PackageAssemblySearch*`
- `AddImport ReferenceAssemblySearch*`
- `AddImport NuGetSearch*`
- `AddImport Diagnostics*`
- `AddImport Unique*`
- `AddImport RemoteRequest`
- `AddImport RemoteResponse`

The important improvement over the first CI attempt is that the local in-proc path now logs too. If the flaky cohost tests are not using `RemoteHostClient`, the next failure should still show why AddImport was skipped or returned no fixes.

### Typeless MessagePack failure logging

`MessagePackFormatters.AssemblyLoadContextAwareForceTypelessFormatter<T>` now logs and rethrows with more detail on failure:

- whether the failure happened during serialize or deserialize
- the exact generic type `T`
- formatter assembly
- formatter assembly load context
- target assembly
- target assembly load context
- original exception type and message

The key prefix is:

- `Typeless MessagePack deserialize failed for '...'`

## How to diagnose the next failure

Start with the failing test output and search for:

- `Code action trace:`
- `RazorCodeActionTrace`
- `AddImport `
- `Typeless MessagePack `

Use the following matrix.

| What you see | Interpretation | Likely next step |
| --- | --- | --- |
| No `Code action trace:` block in the assertion failure | The failure is from older bits or a different assertion path | Confirm CI ran the updated branch/commit |
| `Cohost.CSharpRawActions` already lacks `AddImport` / `FullyQualify` but has `GenerateType` | Roslyn delegated C# did not return missing-import actions | Read the nearby `AddImport` provider/search markers |
| `Cohost.CSharpRawActions` has `AddImport` / `FullyQualify`, but `RemoteCodeActions.DelegatedAfterLanguageProcessing` does not | Razor name extraction dropped delegated actions | Inspect `RemoteCodeActions.ExtractName.*` messages |
| `RemoteCodeActions.Filter.ProviderResult` from `TypeAccessibilityCodeActionProvider` drops `AddImport` / `FullyQualify` | Razor type-accessibility wrapping/filtering rejected the actions | Inspect the `TypeAccessibility.*` keep/drop reason |
| `CSharpProvider.Keep` includes `GenerateType` while `TypeAccessibility.*` has no AddImport inputs | Confirms general C# delegation works but missing-import candidates are absent upstream | Focus on Roslyn AddImport provider/search |
| `AddImport LocalInProcRequest` but no `AddImport LocalRequest` | AddImport ran entirely in-process | Use `CurrentProcess*` and `Search*` markers; remoting is not the immediate failure |
| `AddImport LocalRequest` but no `AddImport RemoteRequest` | Failure before the remote method body ran | Look at brokered-service serialization / transport |
| `Typeless MessagePack deserialize failed for 'X'` | A typeless payload failed to deserialize | Fix formatter / load-context / version-skew issue for `X` |
| `AddImport RemoteRequest` but no `AddImport RemoteResponse` | Failure occurred inside or after remote method entry | Inspect surrounding ServiceHub logs and exceptions |
| `AddImport RemoteResponse ..., ResultCount=0` | Remote call succeeded but search returned no candidates | Investigate search inputs, diagnostics, and candidate generation |
| `AddImport CurrentProcessCanAddImport ..., CanAddImport=False` | Syntax/span gating rejected the AddImport location | Compare failing span/node against a passing run |
| `AddImport CurrentProcessReferences ..., ReferenceCount=0` | Search produced no candidate references | Inspect `SearchStage*`, `PackageAssemblySearch*`, and diagnostic markers |
| `AddImport CurrentProcessReferences` has candidates but `CurrentProcessFixData` logs `Fix=<null>` | Candidate reference failed to produce a fix | Inspect the reference kind and surrounding cleanup/import options |
| `AddImport LocalResponseFailure` | `TryInvokeAsync` swallowed the remote failure and returned no value | Correlate with nearby trace output or ServiceHub logs |
| `Document '...' was not found` from `RemoteMissingImportDiscoveryService` | Remote solution/document synchronization problem | Investigate document mapping and remote workspace state |

## Working assumptions

The current investigation proceeded under the assumption that the earlier Add Missing Using serialization fix is already present in both CI and the customer environment. That means the new instrumentation is primarily trying to answer:

1. Is the failure still typeless MessagePack?
2. If so, **which exact type** is failing now?
3. If not, where in the local -> remote -> local MissingImport flow are we losing the result?

## If the next failure is typeless MessagePack again

Pay attention to the reported type name.

The highest-priority suspects remain:

- `Microsoft.CodeAnalysis.Simplification.SimplifierOptions`
- `Microsoft.CodeAnalysis.Formatting.SyntaxFormattingOptions`

If one of those appears, inspect:

- formatter registration in `MessagePackFormatters`
- assembly load context names in the trace
- whether the failing environment is mixing assemblies from different loads or versions

If a different type appears, that type becomes the new primary suspect immediately.

## If the next failure is not serialization

If the remote call succeeds and reports `ResultCount=0`, switch focus away from remoting and toward missing-import candidate generation:

- diagnostic used to trigger AddImport
- source / metadata / NuGet search options
- document context and span mapping
- whether the delegated code action request is passing the same diagnostics and context on failing vs passing runs

## Useful log artifacts

If raw CI artifacts are available, the most useful files are:

- the failed xUnit test output for the Razor AddUsing tests
- MissingImport ServiceHub logs, typically named like `*MissingImportDiscoveryCore64.ServiceHub*.svclog`
- Roslyn LSP logs, typically named like `*AlwaysActivateInProcLanguageClient*.svclog`

The test output should now be the fastest first stop because the trace messages above are intended to surface directly there.
