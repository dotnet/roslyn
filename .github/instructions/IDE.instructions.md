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

- Daemon mode owns one process telemetry session plus one `RoslynTelemetry`/`TelemetrySession` pair per connected language server. Per-server services resolve both through `LspServices`.
- The server's `RoslynTelemetry` ambient is established before host construction and reapplied at request dispatch. Per-server callbacks and background entry points that may run outside the request queue must capture the instance from `LspServices` and use a nested `RoslynTelemetry.SetCurrent(...)` scope.
- Daemon lifecycle events bypass the ambient and log through the daemon's explicitly captured `RoslynTelemetry`.
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
