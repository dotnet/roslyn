---
coverage: Roslyn's telemetry and logging architecture - event sinks, metric sinks, host composition, and how each host (VS, OOP, standalone LSP, build server, tests, Razor) wires them up
---

# Telemetry & Logging

## The two call-site APIs

Everything Roslyn records goes through one of three families. Pick by *what you are recording*, not by
which host you are in.

| Recording | Call | Available in |
|---|---|---|
| A discrete event or a timed scope | `RoslynTelemetry.Log(FunctionId, ...)` / `RoslynTelemetry.LogBlock(FunctionId, ...)` | Every layer, including the CodeStyle packages |
| An aggregated measurement | `RoslynTelemetry.Count(FunctionId, metricName, delta, tags...)` / `.Record(...)` / `.RecordBlockTime(...)` | Every layer |
| A reliability failure | `FatalError.ReportAndCatch(...)` and friends | Every layer |

All live in `Microsoft.CodeAnalysis.Internal.Log`
(`src/Workspaces/SharedUtilitiesAndExtensions/Compiler/Core/Log/`), which is linked source compiled into
~15 assemblies. With no sink configured every method is a cheap no-op — that is what the build server,
the CodeStyle packages, and most tests rely on.

`Logger.Log` / `Logger.LogBlock` still exist as a thin forwarding shim onto `RoslynTelemetry` so that the
150+ existing call sites did not have to change in one go. **New code should call `RoslynTelemetry`
directly.** The shim carries no `[Obsolete]` because the repository builds with warnings as errors.

## Sinks

```
call site ──► RoslynTelemetry ──┬─► IEventSink   (events + scopes)
                                └─► IMetricSink  (aggregated measurements)
```

- **`IEventSink`** — `IsEnabled(FunctionId)`, `Log`, `LogBlockStart`, `LogBlockEnd`. `IsEnabled` is
  consulted *before* any `LogMessage` is constructed, so a disabled sink costs nothing. This is where
  both consent (telemetry sinks return `session.IsOptedIn`) and opt-in enablement (diagnostic sinks
  consult a predicate) live.
- **`IMetricSink`** — `Count`, `Record`, `Flush`. Deliberately free of any telemetry-backend or BCL
  metrics type so it can live in the dependency-minimal shared layer, and keyed by a plain
  `string eventName` rather than `FunctionId` so Razor can share the implementation.
  `FunctionId` → event-name mapping happens one level up, in `TelemetryNaming`.

`TelemetryNaming` is the only place the `vs/ide/vbcs/` and `vs.ide.vbcs.` conventions appear.

## Composition is fixed; enablement is not

A host builds its sink list **once**, at startup, via `AggregateEventSink.Create(...)`, and never mutates
it. Turning a sink off means its own `IsEnabled` returns false — not removing it from the list. This is
load-bearing: a sink registered twice posts every event twice, and the previous predicate-based
add/replace/remove API made that easy to do by accident.

- `EtwLogger`, `TraceLogger`, `OutputWindowLogger` expose `UpdatePredicate(...)`; the Performance Loggers
  options page refreshes the **composed instances** through
  `VisualStudioWorkspaceTelemetryService.UpdateDiagnosticSinkEnablement` (and its OOP mirror on
  `RemoteWorkspaceTelemetryService`).
- `RoslynActivityLogger.Sink` is composed once and holds an `ImmutableArray<TraceSource>`; adding and
  removing a `TraceSource` mutates that set, not the sink list.
- Sinks that live in assemblies the composition root cannot reference (the diagnostics tool window VSIX,
  integration tests) attach themselves once with `RoslynTelemetry.AddEventSink` and are thereafter
  controlled by their predicate. They never detach.

## Aggregation: `VSMetricSink`

`src/VisualStudio/Core/Def/Telemetry/Shared/VSMetricSink.cs` is the single aggregating implementation,
backed by VS Telemetry's `IMeter`/`ICounter<long>`/`IHistogram<long>`. The `Shared` folder is linked into
`Microsoft.VisualStudio.LanguageServices`, `Microsoft.CodeAnalysis.Remote.ServiceHub`, and
`Microsoft.CodeAnalysis.LanguageServer`, so all three hosts compile their own copy.

Three properties worth knowing before changing it:

1. **Buckets are keyed by `(TelemetrySessionKey, eventName, metricName, dimensionKey)`.** The session key
   is a constant today; it exists so aggregation state is never keyed on the assumption of a single
   session. A process running several language servers (daemon mode) needs each server's measurements
   bucketed and posted separately, and retrofitting that later would be a rewrite of the aggregation
   rather than a configuration change.
2. **`dimensionKey` is the tag values concatenated in declaration order.** This reproduces the compound
   string call sites used to build by hand (`server.method.language`), so migrating a call site to tags
   does not change which measurements aggregate together.
3. **`Flush()` is global and clears everything.** It posts each bucket to the session that produced it.
   Clearing on flush is also what keeps a long-lived process from accruing buckets for ended sessions.
   The two-level lock (`_flushLock` plus a per-aggregation lock) is required — see
   https://github.com/dotnet/roslyn/pull/71606, where concurrent `PostMetricEvent` calls for one
   instrument were crashing.

`VSMetricSink.IMetricPoster` is the per-session seam that lets tests assert exactly how many events a
flush posts without standing up a real, opted-in `TelemetrySession`.

## Per-host wiring

| Host | Entry point | Sinks |
|---|---|---|
| **Visual Studio** | `VisualStudioWorkspaceTelemetryService.CreateLogger` via `AbstractWorkspaceTelemetryService.InitializeTelemetrySession` | `CodeMarkerLogger`, `EtwLogger`, `TraceLogger`, `RoslynActivityLogger.Sink`, `TelemetryLogger`, `FileLogger` + `VSMetricSink` |
| **ServiceHub / OOP** | `RemoteWorkspaceTelemetryService.CreateLogger`; VS serializes its session and RPCs `InitializeTelemetrySessionAsync` | `EtwLogger`, `TraceLogger`, `TelemetryLogger` + `VSMetricSink` |
| **Standalone LSP** | `LanguageServerTelemetryReporter.InitializeSession`, called from `Program.cs` | `TelemetryLogger` + `VSMetricSink` |
| **VBCSCompiler** | `BuildServerController.RunServer` | none — uses `ICompilerServerLogger` only, by design |
| **Tests** | `UseExportProviderAttribute` resets sinks after every test | none by default |

`AbstractWorkspaceTelemetryService` also starts the 30-minute periodic `RoslynTelemetry.Flush()`. Shutdown
paths flush explicitly as well, because a host can exit too abruptly for the timer to run
(https://github.com/dotnet/roslyn/pull/73287).

## Consent

Consent is a **sink-level, non-bypassable** gate, never a call-site decision. `TelemetryLogger.IsEnabled`
returns `session.IsOptedIn`, and `VSMetricSink` checks `IMetricPoster.IsOptedIn` before building any
aggregation. Because `RoslynTelemetry` consults `IEventSink.IsEnabled` before constructing a
`LogMessage`, an opted-out session allocates nothing at all
(https://github.com/dotnet/roslyn/pull/52484).

## Razor

Razor keeps its own call-site facade (`Microsoft.CodeAnalysis.Razor.Telemetry.ITelemetryReporter`), which
is already tag-shaped (`Property` is a name/value pair and the overloads are `ReadOnlySpan<Property>`).
It still has its own aggregation implementation (`AggregatingTelemetryLog`,
`AggregatingTelemetryLogManager`, and the request `Counter` inside `TelemetryReporter`), which duplicates
`VSMetricSink`. Consolidating it is tracked separately — see `.github/memory/known-issues/razor.md`.
