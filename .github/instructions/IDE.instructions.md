---
applyTo: "src/{Analyzers,CodeStyle,Features,Workspaces,EditorFeatures,VisualStudio,LanguageServer}/**/*.{cs,vb}"
---

# Roslyn IDE Development Guide

## Architecture Overview

Roslyn uses a **layered service architecture** built on MEF (Managed Extensibility Framework):

- **Workspaces** (`src/Workspaces/`): Core abstractions — `Workspace`, `Solution`, `Project`, `Document`
- **Features** (`src/Features/`): Language-agnostic IDE features (refactoring, navigation, completion)
- **Analyzers** (`src/Analyzers/`): IDE diagnostic analyzers and code fixes (IDE0xxx diagnostics)
- **CodeStyle** (`src/CodeStyle/`): Code-style analyzer packaging shared with the command-line
- **LanguageServer** (`src/LanguageServer/`): Shared LSP protocol implementation and Roslyn LSP executable (`roslyn-language-server`)
- **EditorFeatures** (`src/EditorFeatures/`): VS Editor integration and text manipulation
- **VisualStudio** (`src/VisualStudio/`): Visual Studio-specific implementations

### Service Resolution
```csharp
// Workspace services
var service = workspace.Services.GetRequiredService<IMyWorkspaceService>();

// Language-specific services
var csharpService = workspace.Services.GetLanguageServices(LanguageNames.CSharp)
    .GetRequiredService<IMyCSharpService>();
```

### Language Server Telemetry

- Daemon mode owns one process telemetry session plus one `RoslynTelemetry`/`TelemetrySession` pair per connected language server. `LanguageServerHost` creates each child from the ambient process owner and owns its lifetime; the connection manager has no telemetry ownership.
- `LanguageServerHost` establishes the server's ambient in two places and both are load-bearing. The **constructor** scope covers everything constructed under it that captures `RoslynTelemetry.Current` — the `RoslynTelemetry` LSP base service, and `RequestExecutionQueue`'s processing loop, which captures its execution context once when it is started and then runs for the life of the server. **`Start`** covers `JsonRpc.StartListening`: StreamJsonRpc captures the execution context at `StartListening`, *not* at construction, and dispatches every inbound message on it. `LspServices` reapplies the ambient when lazily constructing services so their factories can capture it.
- `ExecutionContext` therefore already carries the owning server's instance through `Task.Run`, awaits, LSP request dispatch, and inbound brokered service calls (`ConstructRpc` constructs and starts listening in one scope), so **do not** reapply it per request or per brokered call. `Daemon_EachServerHasAnIsolatedTelemetrySession` and `InboundBrokeredServiceCallsUseTheOwningServersTelemetryAsync` guard those two paths respectively.
- Reapply the ambient only where the execution context genuinely does not reach the owning server: OS file-watcher callbacks (`DefaultFileChangeWatcher`, whose watchers are shared across servers, so only the per-context instance is correct), work queues whose batch may be started by an arbitrary `AddWork` caller (`LanguageServerProjectLoader.ReloadProjectsAsync`), continuations scheduled from disposal paths (`LspFileWatchRegistration.Dispose`), and code reached through `ExecutionContext.SuppressFlow` (the `serviceBroker/connect` path, which re-establishes the ambient on a clean context so it flows to the whole bridge).
- An `AsyncLocal` write inside a **synchronous** method leaks to its caller, so such scopes need the `SetCurrent` disposable; a write inside an **async** method does not, because the state machine restores the execution context after its synchronous prefix.
- `NamedPipeDaemonConnectionSource` logs daemon lifecycle events through the ambient; `Program` and the daemon test harness establish the daemon instance as ambient before creating it, and per-server scopes do not leak into the supervision loop that reports client disconnect.
- `FeaturesSessionTelemetry.Report()` currently reports process-wide aggregators once during process shutdown; do not invoke it from per-server telemetry disposal.

### MEF Export Patterns
```csharp
// Workspace service (language-agnostic)
[ExportWorkspaceService(typeof(IMyService)), Shared]
internal class MyService : IMyService { }

// Language service (per-language — never share across C#/VB)
[ExportLanguageService(typeof(IMyService), LanguageNames.CSharp), Shared]
internal class CSharpMyService : IMyService { }

// Constructor — always include both attributes
[ImportingConstructor]
[Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
public MyService(IDependency dependency) { }
```

## Resource & Localization

- UI strings live in `.resx` files (e.g., `AnalyzersResources.resx`, `FeaturesResources.resx`, `WorkspacesResources.resx`)
- Reference via generated designer class: `FeaturesResources.Some_string`
- For localizable strings: `new LocalizableResourceString(nameof(FeaturesResources.Some_string), FeaturesResources.ResourceManager, typeof(FeaturesResources))`
- After modifying `.resx` files, run `dotnet msbuild <path to csproj> /t:UpdateXlf` to update `.xlf` localization files

## Analyzers & Code Fixes (IDE0xxx)

- IDE code-style analyzers inherit from `AbstractBuiltInCodeStyleDiagnosticAnalyzer` — not raw `DiagnosticAnalyzer`
- Always provide a `FixAllProvider` for code fixes (typically `WellKnownFixAllProviders.BatchFixer`)
- Diagnostic ID constants live in `src/Analyzers/Core/Analyzers/IDEDiagnosticIds.cs`

## Out-of-Process (OOP) Services

- ServiceHub components live under `src/Workspaces/Remote/` and have special deployment considerations for .NET Core vs .NET Framework — keep both targets in mind when changing remote services

## Key Development Patterns

### TestAccessor Pattern
Expose internal state to tests without making it public:
```csharp
internal class ProductionClass
{
    private int _privateField;

    internal TestAccessor GetTestAccessor() => new(this);

    internal readonly struct TestAccessor
    {
        private readonly ProductionClass _instance;
        internal TestAccessor(ProductionClass instance) => _instance = instance;
        internal ref int PrivateField => ref _instance._privateField;
    }
}
```
**TestAccessor calls are forbidden in production code** — enforced by analyzer RS0043.

### SyntaxGenerator (Language-Agnostic Code Generation)
Use `SyntaxGenerator` to generate code without language-specific knowledge:
```csharp
var generator = SyntaxGenerator.GetGenerator(document);
var methodDecl = generator.MethodDeclaration("MyMethod", ...);
```

## Coding Conventions

- **Private fields**: `_camelCase`
- **Naming**: MEF exports match interface names without "I" prefix
- **Null checks**: Use `Contract.ThrowIfNull()` instead of manual null checks
- **Immutability**: All `Document`, `Solution`, `Project` instances are immutable — use `With*` methods
- **Cancellation**: Always thread `CancellationToken` through async operations
- **Performance**: Avoid LINQ in hot paths, prefer `for` loops or `.AsSpan()`, use `ObjectPool<T>`
- **LanguageServer request context**: Handlers should use the asynchronous `RequestContext.Get*Async` methods for workspace, solution, and document access. Obsolete synchronous members remain only for compatibility with existing external-access consumers and forward to the asynchronous accessors.
- **LanguageServer daemon tests**: Use `AbstractLanguageServerHostTests.CreateDaemonServerAsync` for in-process multi-client tests. The harness creates isolated daemon/per-server telemetry owners; opt-in levels must be explicit so tests never inherit telemetry consent from the machine environment.

## Common Gotchas

- **ImportingConstructor must be marked `[Obsolete]`** with `MefConstruction.ImportingConstructorMessage`
- **Language services must be exported with a specific language name** — don't use generic exports for both C#/VB
- **Workspace changes must use immutable updates** — `Workspace.SetCurrentSolution()`
