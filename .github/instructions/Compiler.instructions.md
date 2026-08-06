---
applyTo: "src/{Compilers,Dependencies,ExpressionEvaluator,Tools}/**/*.{cs,vb}"
---

# Roslyn Compiler Instructions for AI Coding Agents

## Architecture Overview

Roslyn follows a **layered compiler architecture**:
- **Lexer → Parser → Syntax Trees → Semantic Analysis → Lowering/Rewriting → Symbol Tables → Emit**
- Core abstraction: `Compilation` is immutable and reusable. Create new compilations via `AddSyntaxTrees()`, `RemoveSyntaxTrees()`, `ReplaceSyntaxTree()` for incremental changes
- **Internal vs Public APIs**: Use `InternalSyntax` namespace for performance-critical parsing; `Microsoft.CodeAnalysis` for public consumption

### Key Directories
- `src/Compilers/Core/Portable/` - Language-agnostic compiler infrastructure
- `src/Compilers/CSharp/Portable/` - C# compiler implementation  
- `src/Compilers/VisualBasic/Portable/` - VB compiler implementation
- `src/Compilers/Server/` - `VBCSCompiler` build server
- `src/Dependencies/` - High-performance collections (`PooledObjects`, `Threading`)
- `src/ExpressionEvaluator/` - Debugger expression evaluation (uses special `LexerMode.DebuggerSyntax`)
- `src/Tools/` - Compiler tooling (BuildBoss, format tools, analyzers)

### Essential Files for Context
- `src/Compilers/CSharp/Portable/Errors/ErrorCode.cs` - All C# compiler error codes
- `src/Compilers/CSharp/Portable/Errors/MessageID.cs` - Language feature version gating
- `src/Compilers/CSharp/Portable/Syntax/Syntax.xml` - Syntax tree node definitions (generated code source)
- `src/Compilers/CSharp/Portable/BoundTree/BoundNodes.xml` - Bound tree node definitions (generated code source)
- `docs/wiki/Roslyn-Overview.md` - Architecture deep-dive

## Code Generation

Several core data structures are generated from XML definitions — **never edit the generated `.cs` or `.vb` files directly**:
- **Syntax trees**: `src/Compilers/CSharp/Portable/Syntax/Syntax.xml`
- **Bound trees**: `src/Compilers/CSharp/Portable/BoundTree/BoundNodes.xml`
- After modifying these XML files, regenerate and build:
  ```bash
  dotnet run --file eng/generate-compiler-code.cs
  dotnet build src/Compilers/{CSharp,VisualBasic}/Portable # choose the project matching the C# or VB syntax you changed
  ```

## Conventions

- **MEF is not used in the compiler layer.** `ExportLanguageService` / `ImportingConstructor` and the IDE service model are IDE-layer concepts — ignore them here.
- **Null checks**: validate internal-API preconditions with `Debug.Assert(...)` (a violated internal precondition may NRE in release); validate public APIs with explicit null checking when appropriate, throwing a dedicated exception with a localized string.
- **Immutability** is via `Compilation` (`AddSyntaxTrees`/`RemoveSyntaxTrees`/`ReplaceSyntaxTree`), not the workspace `Document`/`Solution` model.

## Essential Patterns

### Memory Management
- **Avoid LINQ in hot paths** - use manual enumeration or `struct` enumerators
- **Avoid `foreach` over collections without struct enumerators** 
- **Use object pools extensively** - see patterns in `src/Dependencies/PooledObjects/`
- **Prefer `Debug.Assert()` over exceptions** for internal validation

## Build & Test Workflows

### Essential Build Commands

```powershell
# Full build (use VS Code tasks when available)
./build.sh

# Build specific components  
dotnet build Compilers.slnf                    # Compiler-only build
dotnet build src/Compilers/CSharp/csc/AnyCpu/  # C# compiler

# Generate compiler code after changes
dotnet run --file eng/generate-compiler-code.cs
```

## Debugger Integration

**Expression Evaluator** uses special parsing modes:
- `LexerMode.DebuggerSyntax` for expression evaluation
- `IsInFieldKeywordContext` flag for context-aware parsing
- `ConsumeFullText` parameter for complete expression parsing

## MSBuild Integration

Compiler tasks are in `src/Compilers/Core/MSBuildTask/`:
- `Csc.cs` - C# compiler task
- `Vbc.cs` - VB compiler task  
- `ManagedCompiler.cs` - Base compiler task functionality

## Performance Considerations

1. **Lexer/Parser optimizations**: Use `InternalSyntax` types for performance-critical code
2. **Immutable data structures**: Roslyn heavily uses immutable collections and copy-on-write semantics
3. **Caching**: `Compilation` objects cache semantic information - reuse when possible
4. **Threading**: Most compiler operations are thread-safe through immutability

## Symbol Resolution

Navigate the symbol hierarchy:
```cs
var compilation = CreateCompilation(source);
var globalNamespace = compilation.GlobalNamespace;
var typeSymbol = globalNamespace.GetTypeMembers("MyClass").Single();
var methodSymbol = typeSymbol.GetMembers("MyMethod").Single();
```

Symbol equality is complex due to generics and substitution - always test with multiple generic scenarios.
