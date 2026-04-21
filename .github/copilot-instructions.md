# Roslyn (.NET Compiler Platform) AI Coding Instructions

## Architecture Overview

**Core Components** (layered from bottom-up):
- **Compilers** (`src/Compilers/`): C# and VB.NET compilers — syntax trees, semantic models, symbols, and emit APIs
- **Workspaces** (`src/Workspaces/`): Solution/project model, document management, and host services
- **Features** (`src/Features/`): Language-agnostic IDE features (refactoring, completion, diagnostics)
- **Analyzers** (`src/Analyzers/`): IDE diagnostic analyzers and code fixes (IDE0xxx)
- **EditorFeatures** (`src/EditorFeatures/`): Editor-specific implementations and text buffer integration
- **LanguageServer** (`src/LanguageServer/`): LSP implementation used by VS Code extension
- **VisualStudio** (`src/VisualStudio/`): VS-specific language services and UI integration

## Development Workflow

**Building**:
- Windows: `build.cmd` / Unix: `build.sh` — Full solution build
- `dotnet build Compilers.slnf` — Compiler-only build
- `dotnet build Ide.slnf` — IDE-only build
- Solution filters: `Roslyn.slnx` (full), `Compilers.slnf` (compilers), `Ide.slnf` (IDE)

**Testing**:
- Windows: `test.cmd` / Unix: `test.sh` — Run all tests
- `dotnet test <path to test .csproj>` — Run specific test project
- Tests inherit from base classes: `CSharpTestBase`, `VisualBasicTestBase` (compiler), `AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest_NoEditor` (IDE analyzers)
- Use `[UseExportProvider]` for MEF-dependent tests
- Copilot coding agent setup preinstalls `roslyn-language-server` as a global tool and syncs the `dotnet/skills` catalog into `~/.copilot/skills`

**Formatting**:
- Whitespace formatting preferences are stored in the `.editorconfig` file
- When running `dotnet format whitespace` use the `--folder .` option followed by `--include <path to file>` to avoid a design-time build
- **Critical**: Blank lines must not contain any whitespace characters (spaces or tabs). This causes linting errors.

**Localization**:
- `dotnet msbuild <path to csproj> /t:UpdateXlf` — Update `.xlf` files after modifying `.resx` files
- Resource strings accessed via generated designer classes (e.g., `CSharpResources.xxx`, `FeaturesResources.xxx`, `AnalyzersResources.xxx`)

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
// All syntax trees, documents, and solutions are immutable — use With* methods
var newDocument = oldDocument.WithSyntaxTree(newTree);

// Semantic analysis — always pass CancellationToken
var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
var symbolInfo = semanticModel.GetSymbolInfo(expression);
```

**Testing Conventions**:
- Add `[WorkItem("https://github.com/dotnet/roslyn/issues/NNN")]` attribute to tests that fix specific GitHub issues
- Prefer raw string literals (`"""..."""`) over verbatim strings (`@"..."`) when creating test source code
- Keep tests focused — do minimal work to validate the core issue
  - Use `.Single()` instead of asserting count and extracting elements
  - For compiler tests, use `comp.VerifyEmitDiagnostics()` so reviewers can see if code is legal
  - For IDE tests, use `TestInRegularAndScriptAsync` / `TestMissingInRegularAndScriptAsync`

## Key Conventions

- **Namespace Strategy**: `Microsoft.CodeAnalysis.[Language].[Area]` (e.g., `Microsoft.CodeAnalysis.CSharp.Formatting`)
- **Immutability**: All syntax trees, documents, and solutions are immutable — create new instances for changes
- **Cancellation**: Always thread `CancellationToken` through async operations
- **MEF Lifecycle**: Use `[ImportingConstructor]` with `[Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]`
- **Null checks**: Use `Contract.ThrowIfNull()` instead of manual null checks
- **Private fields**: `_camelCase` naming
- **PROTOTYPE Comments**: Only used to track follow-up work in feature branches — disallowed in main branch
- **Code Formatting**: Avoid trailing spaces. Blank lines must be completely empty (no whitespace characters).
- **Public API Tracking**: Update `PublicAPI.Unshipped.txt` when adding/changing public APIs

## Code Generation

Several core data structures are generated from XML definitions — never edit generated `.cs` files directly:
- **Syntax trees**: `src/Compilers/CSharp/Portable/Syntax/Syntax.xml`
- **Bound trees**: `src/Compilers/CSharp/Portable/BoundTree/BoundNodes.xml`
- **After modifying XML files**, run: `dotnet run --file eng/generate-compiler-code.cs`

## Common Gotchas

- Follow existing conventions in the file you're editing
- Language services must be exported per-language, not shared across C#/VB
- Test failures often indicate MEF composition issues — check export attributes
- ServiceHub components (`src/Workspaces/Remote/`) require special deployment considerations for .NET Core vs Framework
- IDE analyzers should inherit from `AbstractBuiltInCodeStyleDiagnosticAnalyzer` for code style diagnostics, not raw `DiagnosticAnalyzer`
- Always provide `FixAllProvider` (typically `WellKnownFixAllProviders.BatchFixer`) for code fixes

## Documentation

**Creating new docs**:
- Use **kebab-case** for file names (e.g., `roslyn-language-server-copilot-plugin.md`, not `Roslyn Language Server Copilot Plugin.md`)
- Place docs in the appropriate subdirectory under `docs/` (e.g., `docs/contributing/`, `docs/compilers/`, `docs/features/`)
- General docs that don't fit a subdirectory go directly in `docs/`

## Essential Files for Context

- `src/Compilers/CSharp/Portable/Errors/ErrorCode.cs` — All C# compiler error codes
- `src/Compilers/CSharp/Portable/Errors/MessageID.cs` — Language feature version gating
- `src/Analyzers/Core/Analyzers/IDEDiagnosticIds.cs` — All IDE diagnostic ID constants
- `src/Compilers/CSharp/Portable/Syntax/Syntax.xml` — Syntax tree node definitions
- `src/Compilers/CSharp/Portable/BoundTree/BoundNodes.xml` — Bound tree node definitions
- `docs/wiki/Roslyn-Overview.md` — Architecture deep-dive

## Code Review

When performing a code review, follow the review process, priorities, conventions, and output format defined in the [code-review skill](/.github/skills/code-review/SKILL.md).
