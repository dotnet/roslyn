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

## Common Gotchas

- **ImportingConstructor must be marked `[Obsolete]`** with `MefConstruction.ImportingConstructorMessage`
- **Language services must be exported with a specific language name** — don't use generic exports for both C#/VB
- **Workspace changes must use immutable updates** — `Workspace.SetCurrentSolution()`
- **LanguageServer file watching should use `IFileChangeWatcher`** (which delegates to `LspFileChangeWatcher` when supported) instead of directly creating `System.IO.FileSystemWatcher` in host services.
- **LanguageServer on-demand loading** is coordinated by `HostWorkspace/OnDemandProjectLoader.cs` through `Protocol/Workspaces/{IOnDemandProjectLoader,OnDemandProjectLoadOperation}.cs`. Documents already represented in the host workspace skip on-demand loading. Each other eligible document demand performs a fresh nearest-ancestor filesystem search on a background task; only active root-plus-dependency operations are shared, keyed by normalized project path. Workspace folders bound discovery but do not affect closure loading once a project is found. `LanguageServerProjectLoader._loadedProjects` is the sole authority for tracked projects. Each `LoadedProject` owns a one-shot signal that its initial load has settled; after awaiting it, callers determine success from the current primordial project or loaded targets, so a later successful file-change reload can recover from an initial failure without replacing the signal. Workspace-scoped requests snapshot tracked projects under the loader gate and await those signals. Queued evaluations run serially per normalized project path and in parallel across paths. On-demand dependency closure queries current project references only when the project currently has loaded state instead of retaining target `ProjectId` snapshots. A failed reload preserves existing targets. Request cancellation must not cancel shared discovery or project loading.
- **LanguageServer workspace-folder state** is owned by `Protocol/Handler/WorkspaceFolderTracker.cs`, which atomically applies added and removed folders, normalizes local-file paths without trailing separators, and raises `WorkspaceFoldersChanged` with the updated immutable paths. Initialization and `workspace/didChangeWorkspaceFolders` update the tracker. `RequestContextFactory` snapshots that state when creating each queued request and passes it directly to on-demand project discovery. Long-lived services operating outside a request read the tracker when they need the current state rather than caching an initialization snapshot.
- **LanguageServer solution context loading** is lazy and owned by `RequestContext`: non-mutating handlers wait when they first request async workspace, document, or solution state, then re-resolve the latest workspace using the request-time tracked-text snapshot. Mutating handlers use their initial snapshot without waiting so they cannot stall the serialized mutation queue.
- **LanguageServer document context lookup** is owned by `LspWorkspaceManager`: registered workspaces are searched before miscellaneous fallback. The manager retains fork-cache ownership, telemetry, stale miscellaneous-document cleanup, and exception handling.
