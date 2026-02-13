---
applyTo: "src/{Analyzers,CodeStyle,Features,Workspaces,EditorFeatures,VisualStudio}/**/*.{cs,vb}"
---

# Roslyn IDE Development Guide

This guide provides essential knowledge for working effectively with Roslyn's IDE-focused codebase.

## Architecture Overview

Roslyn uses a **layered service architecture** built on MEF (Managed Extensibility Framework):

- **Workspaces** (`src/Workspaces/`): Core abstractions - `Workspace`, `Solution`, `Project`, `Document`
- **Features** (`src/Features/`): Language-agnostic IDE features (refactoring, navigation, completion)
- **LanguageServer** (`src/LanguageServer/`): Shared LSP protocol implementation and Roslyn LSP executable
- **EditorFeatures** (`src/EditorFeatures/`): VS Editor integration and text manipulation
- **VisualStudio** (`src/VisualStudio/`): Visual Studio-specific implementations

### Service Resolution Pattern

```csharp
// Get workspace services
var service = workspace.Services.GetRequiredService<IMyWorkspaceService>();

// Get language-specific services  
var csharpService = workspace.Services.GetLanguageServices(LanguageNames.CSharp)
    .GetRequiredService<IMyCSharpService>();

// In tests, use ExportProvider directly
var service = ExportProvider.GetExportedValue<IMyService>();
```

### MEF Export Patterns

```csharp
// Workspace service
[ExportWorkspaceService(typeof(IMyService)), Shared]
internal class MyService : IMyService { }

// Language service
[ExportLanguageService(typeof(IMyService), LanguageNames.CSharp), Shared]
internal class CSharpMyService : IMyService { }

// Always use ImportingConstructor with obsolete warning
[ImportingConstructor]
[Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
public MyService(IDependency dependency) { }
```

## Key Development Patterns

### TestAccessor Pattern
For exposing internal state to tests without making it public:

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

### Diagnostic Analyzer Structure
```csharp
[DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
public sealed class MyAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor s_rule = new(
        "MyAnalyzer001", "Title", "Message format", "Category",
        DiagnosticSeverity.Warning, isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics 
        => ImmutableArray.Create(s_rule);

    public override void Initialize(AnalysisContext context)
    {
        context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.ClassDeclaration);
    }
}
```

## Essential Build & Test Commands

Always set `DOTNET_CLI_WORKLOAD_UPDATE_NOTIFY_DISABLE=1` before running any `dotnet` command to prevent workload update checks that require network access and can cause build failures in restricted environments.

```bash
# Full build
.build.sh

# Run specific test project
dotnet test src/EditorFeatures/Test/

# Build with analyzers
.build.sh -testUsedAssemblies

# Generate compiler code (if changing syntax)
dotnet run --file eng/generate-compiler-code.cs
```

## Working with Tests

### Test Workspace Creation
```csharp
[UseExportProvider]
public class MyTests
{
    [Fact]
    public async Task TestSomething()
    {
        var workspace = EditorTestWorkspace.CreateCSharp("class C { }");
        var document = workspace.Documents.Single();
        // Test logic here
    }
}
```

### Common Test Utilities
- `DescriptorFactory.CreateSimpleDescriptor()` - Create test diagnostic descriptors
- `VerifyCS.VerifyAnalyzerAsync()` - Verify C# analyzer behavior
- `TestWorkspace.CreateCSharp()` - Create test workspaces
- `UseExportProviderAttribute` - Required for MEF-dependent tests

## Coding Conventions

### Performance Rules
- **Avoid LINQ in hot paths** - Use manual loops in compiler/analyzer code
- **Avoid `foreach` over non-struct enumerators** - Use `for` loops or `.AsSpan()`
- **Use object pooling** - See `ObjectPool<T>` usage patterns
- **Prefer `ReadOnlySpan<T>`** over `IEnumerable<T>` for performance-critical APIs

### Naming Conventions
- Private fields: `_camelCase` 
- Internal test accessors: `GetTestAccessor()` returning `TestAccessor` struct
- Diagnostic IDs: Consistent prefixes (RS, CA, IDE followed by numbers)
- MEF exports: Match interface names without "I" prefix

### Resource Management
- MEF services are automatically disposed by the container
- Use `TestAccessor` pattern instead of `internal` accessibility for test-only APIs
- Always implement `IDisposable` for stateful services

## Common Gotchas

- **ImportingConstructor must be marked `[Obsolete]`** with `MefConstruction.ImportingConstructorMessage`
- **Use `Contract.ThrowIfNull()`** instead of manual null checks in public APIs
- **TestAccessor calls are forbidden in production code** - enforced by analyzer RS0043
- **Language services must be exported with specific language name** - don't use generic exports
- **Workspace changes must use immutable updates** - call `Workspace.SetCurrentSolution()` appropriately