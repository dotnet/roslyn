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

## ExternalAccess Assemblies

Partner assemblies (AspNetCore, Copilot, Extensions) follow a **consolidated** pattern introduced in PRs #81593 and #84349:

- **Implementation** lives in `src/Features/ExternalAccess/Core/` (`Microsoft.CodeAnalysis.Features.ExternalAccess`). Partner areas each get a subfolder, e.g., `Core/Extensions/External/`, `Core/Extensions/Internal/`, with their resource files under `Core/Extensions/`.
- **Partner facades** (e.g., `src/Features/ExternalAccess/Extensions/`) are thin assembly-identity wrappers containing only `TypeForwards.cs` (`[assembly: TypeForwardedTo(...)]`) that redirect every public and internal type to the Core assembly. They carry no implementation.
- **Resource files** moved to Core get their source generated with `<EmbeddedResource Update="Partner/Resource.resx" GenerateSource="true" />`. The generated class namespace includes the subfolder path: `RootNamespace.Partner.ResourceClass`.
- **MEF exports** live exclusively in `Microsoft.CodeAnalysis.Features.ExternalAccess`; the facade assemblies export nothing. `RemoteExportProviderBuilder.RemoteHostAssemblyNames` includes Core EA so the remote host discovers MEF exports.
- **API baselines**: public types go in Core's `PublicAPI.Unshipped.txt`; internal implementation goes in Core's `InternalAPI.Unshipped.txt`. Partner facade baselines retain shipped entries for binary-compat tracking but add no new entries after consolidation.

## Common Gotchas

- **ImportingConstructor must be marked `[Obsolete]`** with `MefConstruction.ImportingConstructorMessage`
- **Language services must be exported with a specific language name** — don't use generic exports for both C#/VB
- **Workspace changes must use immutable updates** — `Workspace.SetCurrentSolution()`
