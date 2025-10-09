# Roslyn (.NET Compiler Platform) AI Coding Instructions

## Architecture Overview

**Core Components** (layered from bottom-up):
- **Compilers** (`src/Compilers/`): C# and VB.NET compilers with syntax trees, semantic models, symbols, and emit APIs
- **Workspaces** (`src/Workspaces/`): Solution/project model, document management, and host services
- **Features** (`src/Features/`): Language-agnostic IDE features (refactoring, completion, diagnostics)
- **EditorFeatures** (`src/EditorFeatures/`): Editor-specific implementations and text buffer integration
- **VisualStudio** (`src/VisualStudio/`): VS-specific language services and UI integration

**Key Service Boundaries**:
- Language services via MEF exports: `[ExportLanguageService(typeof(IService), LanguageNames.CSharp)]`
- Host services for cross-language functionality
- Remote services for out-of-process execution via ServiceHub

## Development Workflow

**Building**:
- `.\Build.cmd` - Full solution build
- `dotnet build Compilers.slnf` - Compiler-only build  
- Use VS tasks: `build current project`, `build Compilers.slnf`

**Testing**:
- `.\Test.cmd` - Run all tests
- `dotnet test` for specific test projects
- Tests inherit from base classes like `AbstractLanguageServerProtocolTests`, `WorkspaceTestBase`
- Use `[UseExportProvider]` for MEF-dependent tests

**Debugging in VS**:
- Set `RoslynDeployment` as startup project and F5 to launch experimental hive
- Deploy changes: `.\Build.cmd -deployExtensions -launch`
- Launch hive manually: `devenv /rootSuffix RoslynDev`
- VSIX projects automatically deploy to experimental instance

**Formatting**:
- Whitespace formatting preferences are stored in the `.editorconfig` file
- Run `dotnet format whitespace -f . --include ` followed by the relative path to changed files to apply formatting preferences
## Code Patterns

**Service Architecture** (use MEF consistently):
```csharp
[ExportLanguageService(typeof(IMyService), LanguageNames.CSharp), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class CSharpMyService : IMyService
```

**Roslyn API Usage**:
```csharp
// Always use immutable patterns
var newTree = oldTree.WithChangedText(newText);
var newDocument = oldDocument.WithSyntaxTree(newTree);

// Semantic analysis
var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
var symbolInfo = semanticModel.GetSymbolInfo(expression);
```

**Testing Conventions**:
- Inherit from `TestBase` or language-specific base classes
- Use `UseExportProvider` for MEF services
- Test utilities in `Microsoft.CodeAnalysis.Test.Utilities`
- Language-specific test bases: `CSharpTestBase`, `VisualBasicTestBase`

## Critical Integration Points

**Language Server Protocol**: `src/LanguageServer/` contains LSP implementation used by VS Code extension
**ServiceHub**: Remote services (`src/Workspaces/Remote/`) run out-of-process for performance
**Analyzers**: `src/Analyzers/` for static analysis, separate from `src/RoslynAnalyzers/` (internal tooling)
**VSIX Packaging**: Multiple deployment targets - `src/VisualStudio/Setup/` for main VS integration

## Key Conventions

**Namespace Strategy**: `Microsoft.CodeAnalysis.[Language].[Area]` (e.g., `Microsoft.CodeAnalysis.CSharp.Formatting`)
**File Organization**: Group by feature area, separate language-specific implementations
**Immutability**: All syntax trees, documents, and solutions are immutable - create new instances for changes
**Cancellation**: Always thread `CancellationToken` through async operations
**MEF Lifecycle**: Use `[ImportingConstructor]` with obsolete attribute for MEF v2 compatibility

## Common Gotchas

- Don't modify `SyntaxTree`/`Document` directly - use `With*` methods
- Language services must be exported per-language, not shared across C#/VB
- Test failures often indicate MEF composition issues - check export attributes
- VSIX deployment targets multiple architectures - ensure platform-specific assets are handled
- ServiceHub components require special deployment considerations for .NET Core vs Framework

## Essential Files for Context

- `docs/wiki/Roslyn-Overview.md` - Architecture deep-dive
- `docs/contributing/Building, Debugging, and Testing on Windows.md` - Development setup
- `src/Compilers/Core/Portable/` - Core compiler APIs
- `src/Workspaces/Core/Portable/` - Workspace object model
- Solution filters: `Roslyn.sln`, `Compilers.slnf`, `Ide.slnf` for focused builds