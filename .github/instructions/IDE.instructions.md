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
- **VisualStudio integration-test harness** (`src/VisualStudio/IntegrationTest/Harness/`): shared integration-test infrastructure
- **EditorConfig templates** (`src/VisualStudio/EditorConfig/`): item templates, generation wizard, context-menu command, VSIX projects, and Visual Studio insertion setup
  - The setup insertion component is `Templates.Editorconfig.Setup`, but its SWR package identity must remain `Templates.Editorconfig.SolutionFile.Setup` because existing Visual Studio template packages depend on that ID.

### External Access assemblies

Partner APIs that depend on IDE layers are grouped into one ExternalAccess assembly per layer:

- `src/Features/ExternalAccess/Core/`
- `src/EditorFeatures/ExternalAccess/Core/`
- `src/LanguageServer/ExternalAccess/Core/`
- `src/VisualStudio/ExternalAccess/Core/`

Partner-specific compatibility assembly for ASP.NET remains under `src/Features/ExternalAccess/AspNetCore/`; ExternalAccess projects for APIs that are not part of the unified layer assemblies remain separate.

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
- `src/Workspaces/Core/Portable/Utilities/StandardHandleInheritance.cs` prevents redirected Windows child processes from inheriting unrelated standard handles. The LanguageServer and MSBuild BuildHost disable inheritance for their lifetimes before launching descendants; dependency-light hosts may source-link this utility.

### Validating local language-server experiments

- Record the selected source/build/package closure: source revision and local changes, SDK/runtime, build configuration and target framework/RID where applicable, resolved package versions, server and extension paths, and client launch settings. Keep the server, extensions, and their dependencies coherent; swapping one assembly does not establish which implementation the host actually uses. Preserve comparison outputs as described in [Preserving comparison builds](../../docs/contributing/Building,%20Debugging,%20and%20Testing%20on%20Unix.md#preserving-comparison-builds).
- Verify cold MEF composition and actual activation of the selected component, not just assembly loading or a warm-cache launch. [LanguageServerExportProviderBuilder](../../src/LanguageServer/Microsoft.CodeAnalysis.LanguageServer/LanguageServerExportProviderBuilder.cs) selects the composition inputs; [ExportProviderBuilderTests](../../src/LanguageServer/Microsoft.CodeAnalysis.LanguageServer.UnitTests/ExportProviderBuilderTests.cs) exercises composition/cache behavior using the real-composition [AbstractLanguageServerMefHost](../../src/LanguageServer/Microsoft.CodeAnalysis.LanguageServer.UnitTests/Utilities/AbstractLanguageServerMefHost.cs) with a fresh test cache.
- For brokered services, record the exact moniker/version and RPC descriptor requested by the consumer. Verify that the real provider can be acquired and a representative call succeeds; when forwarding is involved, exercise the bridge as well. Assembly loading, advertised registration, or a passing mocked registration test alone is not proof of this. Check both [container registration](../../src/LanguageServer/Microsoft.CodeAnalysis.LanguageServer/BrokeredServices/BrokeredServiceContainer.cs) and [bridge forwarding](../../src/LanguageServer/Microsoft.CodeAnalysis.LanguageServer/BrokeredServices/BrokeredServiceBridgeProvider.cs), and start from [ServiceBrokerFactoryTests](../../src/LanguageServer/Microsoft.CodeAnalysis.LanguageServer.UnitTests/ServiceBrokerFactoryTests.cs) for acquisition and RPC patterns. Preserve existing supported versions while testing the selected contract; do not replace compatibility registrations merely to make the experiment pass.
- For cross-process session changes, add focused coverage where relevant for partial or canceled startup, abrupt disconnect, cleanup followed by a new session, and isolation of another active client. Reuse [ServerDisconnectTests](../../src/LanguageServer/Microsoft.CodeAnalysis.LanguageServer.UnitTests/ServerDisconnectTests.cs) and [LanguageServerDaemonTests](../../src/LanguageServer/Microsoft.CodeAnalysis.LanguageServer.UnitTests/Daemon/LanguageServerDaemonTests.cs) as starting points. For expected shared-process lifetime, follow the existing [daemon-mode guidance](../../src/LanguageServer/roslyn-language-server/README.md#daemon-mode).

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
- **MSBuild project extensions are stored with a leading `.`.** `ProjectFileExtensionRegistry` accepts registration and lookup values with or without the dot, but its enumeration API returns the canonical dot-prefixed form.
