---
applyTo: "src/{Compilers,Dependencies,ExpressionEvaluator,Tools}/**/*.{cs,vb}"
---

# Roslyn Compiler Instructions for AI Coding Agents

## Architecture Overview

Roslyn follows a **layered compiler pipeline**: Lexer → Parser → Syntax Trees → Binding (BoundNodes) → Lowering/Rewriting → Emit

- `Compilation` is immutable — create new instances via `AddSyntaxTrees()`, `ReplaceSyntaxTree()`, etc.
- **Internal vs Public APIs**: `InternalSyntax` namespace for performance-critical parsing; `Microsoft.CodeAnalysis` for public consumption

### Key Directories
- `src/Compilers/Core/Portable/` — Language-agnostic compiler infrastructure
- `src/Compilers/CSharp/Portable/` — C# compiler implementation
- `src/Compilers/VisualBasic/Portable/` — VB compiler implementation
- `src/Dependencies/` — High-performance collections (`PooledObjects`, `Threading`)
- `src/ExpressionEvaluator/` — Debugger expression evaluation
- `src/Tools/` — Compiler tooling (BuildBoss, format tools, analyzers)

## Code Generation from XML Definitions

Several core data structures are **generated from XML** — never edit generated `.cs` files directly.

- **Syntax trees**: Defined in `src/Compilers/CSharp/Portable/Syntax/Syntax.xml` (C#) and the VB equivalent. Defines all `SyntaxNode` subclasses, their fields, and kinds.
- **Bound trees**: Defined in `src/Compilers/CSharp/Portable/BoundTree/BoundNodes.xml`. Defines the intermediate representation used during semantic analysis and lowering.
- **After modifying XML files**, run: `dotnet run --file eng/generate-compiler-code.cs`

## Error and Diagnostic Patterns

### Adding New Errors
1. Add entry to `ErrorCode` enum in `src/Compilers/CSharp/Portable/Errors/ErrorCode.cs` — use `ERR_` prefix for errors, `WRN_` for warnings, `FTL_` for fatal
2. Add the error message string to `CSharpResources.resx`
3. Update `ErrorFacts.cs` if the error needs custom severity/category logic
4. Run `dotnet msbuild <path to csproj> /t:UpdateXlf` to update localization `.xlf` files after `.resx` changes

### Language Version Feature Gating
New language features are gated by `MessageID` → `LanguageVersion` mapping:
- Add a `IDS_Feature*` entry to `MessageID` enum in `src/Compilers/CSharp/Portable/Errors/MessageID.cs`
- Use `MessageID.IDS_FeatureXxx.RequiredVersion()` to get the minimum `LanguageVersion`
- In parsing/binding, check `languageVersion >= feature.RequiredVersion()` before allowing the feature

### Public API Tracking
Each assembly has `PublicAPI.Shipped.txt` and `PublicAPI.Unshipped.txt` files. When adding/changing public APIs, update `PublicAPI.Unshipped.txt` with the new API signature. This is enforced by analyzers at build time.

## Testing Patterns

### Test Base Classes
Inherit from `CSharpTestBase` (C#) or `VisualBasicTestBase` (VB):
```cs
public class MyTests : CSharpTestBase
{
    [Fact]
    public void TestMethod()
    {
        var comp = CreateCompilation(sourceCode);
        comp.VerifyDiagnostics(
            // ERR_NameNotInContext: The name 'x' does not exist in the current context
            Diagnostic(ErrorCode.ERR_NameNotInContext, "x").WithArguments("x"));
    }
}
```

### Key Test APIs
- `CreateCompilation(source, references, parseOptions, options)` — create a test compilation
- `CompileAndVerify(source)` — compile and verify IL output
- `comp.VerifyDiagnostics(...)` — assert specific diagnostics with `Diagnostic(ErrorCode).WithLocation(line, col).WithArguments(...)`
- `comp.VerifyEmitDiagnostics()` — verify diagnostics during emit so reviewers can see if code is legal
- Use `TestOptions.Regular.WithLanguageVersion(...)` or `TestOptions.RegularPreview` for parse options
- Add `[WorkItem("https://github.com/dotnet/roslyn/issues/NNN")]` to tests fixing specific issues
- Prefer raw string literals (`"""..."""`) over verbatim strings (`@"..."`) for test source code
- Keep tests focused — use `.Single()` instead of asserting count then extracting

### Testing New Language Features
```cs
// Test that feature works with correct language version
var comp = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp12));
comp.VerifyDiagnostics();

// Test that feature is rejected with older language version
comp = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp11));
comp.VerifyDiagnostics(
    Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion11, ...).WithArguments(...));
```

### Symbol Resolution in Tests
```cs
var compilation = CreateCompilation(source);
var globalNamespace = compilation.GlobalNamespace;
var typeSymbol = globalNamespace.GetTypeMembers("MyClass").Single();
var methodSymbol = typeSymbol.GetMembers("MyMethod").Single();
```

## Performance Rules

- **Avoid LINQ in hot paths** — use manual enumeration or `struct` enumerators
- **Avoid `foreach` over collections without struct enumerators**
- **Use object pools** — see patterns in `src/Dependencies/PooledObjects/`
- **Use `InternalSyntax` types** for performance-critical lexer/parser code
- **Prefer `Debug.Assert()` over exceptions** for internal invariant validation
- **Threading**: Most compiler operations are thread-safe through immutability — avoid unnecessary locking

## Source Generators

- **Use `IIncrementalGenerator`** (preferred) — `ISourceGenerator` is deprecated
- Attribute: `[Generator(LanguageNames.CSharp)]`
- Define pipeline via `IncrementalGeneratorInitializationContext` in `Initialize()`

## MSBuild Integration

Compiler MSBuild tasks are in `src/Compilers/Core/MSBuildTask/`:
- `Csc.cs` — C# compiler task
- `Vbc.cs` — VB compiler task
- `ManagedCompiler.cs` — Base compiler task functionality

## Build Commands

```powershell
dotnet build Compilers.slnf                    # Compiler-only build
dotnet build src/Compilers/CSharp/csc/AnyCpu/  # C# compiler only
dotnet run --file eng/generate-compiler-code.cs # Regenerate from XML definitions
```