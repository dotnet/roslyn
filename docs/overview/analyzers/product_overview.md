# Analyzers: Product Overview

**Last Updated:** January 29, 2026

## The Story: Catching Bugs Before They Bite

A developer writes a method that never uses one of its parameters. In a traditional workflow, this might go unnoticed—the code compiles, runs, and the dead parameter survives for years, confusing future maintainers.

With Roslyn analyzers, a squiggle appears immediately: "Parameter 'options' is never used." A lightbulb offers to remove it. The developer clicks, and the code is cleaner.

**The Power of Analyzers**

Analyzers run during compilation and in the IDE, providing:
- **Instant feedback** — See issues as you type
- **Consistent enforcement** — Same rules for everyone
- **Automated fixes** — One-click corrections
- **Custom rules** — Team-specific standards

**Types of Analysis**

1. **Compiler Errors** — Actual code problems (type mismatches, missing references)
2. **Code Style** — Formatting and preference issues (IDE0xxx)
3. **Code Quality** — Potential bugs and maintainability issues
4. **Custom Rules** — Team or project-specific standards

---

## Core Concepts

### Diagnostic Analyzer

**What it is:** A component that examines code and reports diagnostics.

**How it works:**
```csharp
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class NoPublicFieldsAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
        id: "DEMO001",
        title: "Avoid public fields",
        messageFormat: "Field '{0}' should not be public",
        category: "Design",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);
    
    public override void Initialize(AnalysisContext context)
    {
        context.RegisterSymbolAction(AnalyzeField, SymbolKind.Field);
    }
    
    private void AnalyzeField(SymbolAnalysisContext context)
    {
        var field = (IFieldSymbol)context.Symbol;
        if (field.DeclaredAccessibility == Accessibility.Public)
        {
            var diagnostic = Diagnostic.Create(Rule, field.Locations[0], field.Name);
            context.ReportDiagnostic(diagnostic);
        }
    }
}
```

### Code Fix Provider

**What it is:** A component that offers fixes for diagnostics.

**How it works:**
```csharp
[ExportCodeFixProvider(LanguageNames.CSharp)]
public class MakeFieldPrivateCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create("DEMO001");
    
    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var diagnostic = context.Diagnostics.First();
        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Make field private",
                createChangedDocument: ct => MakePrivateAsync(context.Document, diagnostic, ct)),
            diagnostic);
    }
}
```

### Diagnostic IDs

Roslyn uses prefixed IDs to categorize diagnostics:

| Prefix | Category | Examples |
|--------|----------|----------|
| `CS` | C# compiler errors | CS0246 (Type not found) |
| `BC` | VB compiler errors | BC30456 (Member not found) |
| `IDE` | IDE suggestions | IDE0001 (Simplify name) |
| `CA` | Code Analysis | CA1000 (Static member design) |

---

## Analyzer Types

### Built-in IDE Analyzers (`src/Analyzers/`)

Analyzers shipped with Roslyn for IDE features.

**Deployment Note:** The analyzer code in `src/Analyzers/` is compiled into separate DLLs that ship with the .NET SDK (for command-line builds). However, the same code is also *linked* into the corresponding Features DLLs to ship in Visual Studio, avoiding duplication while ensuring consistent behavior across both environments.

| Category | Examples |
|----------|----------|
| **Simplification** | Remove unnecessary casts, simplify names |
| **Modernization** | Use collection expressions, pattern matching |
| **Style** | Expression-bodied members, var preferences |
| **Formatting** | Blank lines, spacing |
| **Unnecessary Code** | Remove unused imports, parameters |

### RoslynAnalyzers (`src/RoslynAnalyzers/`)

Packaged analyzers for external consumption:

| Package | Purpose |
|---------|---------|
| **Microsoft.CodeAnalysis.Analyzers** | Rules for analyzer authors |
| **Roslyn.Diagnostics.Analyzers** | Roslyn-specific rules |
| **PublicApiAnalyzers** | API surface tracking |
| **BannedApiAnalyzers** | Block forbidden APIs |

---

## Analyzer Architecture

### Analysis Phases

```
┌─────────────────────────────────────────────────────────────────────────┐
│                        Analyzer Execution                               |
│                                                                         |
│  Source Code                                                            |
│       │                                                                 |
│       ▼                                                                 |
│  ┌────────────────────────────────────────────────────────────────────┐ |
│  │                    Syntax Analysis                                 │ |
│  │  RegisterSyntaxNodeAction() — analyze specific syntax nodes        │ |
│  │  RegisterSyntaxTreeAction() — analyze entire tree                  │ |
│  └────────────────────────────────────────────────────────────────────┘ |
│       │                                                                 |
│       ▼                                                                 |
│  ┌────────────────────────────────────────────────────────────────────┐ |
│  │                    Semantic Analysis                               │ |
│  │  RegisterSymbolAction() — analyze declared symbols                 │ |
│  │  RegisterOperationAction() — analyze IOperation nodes              │ |
│  └────────────────────────────────────────────────────────────────────┘ |
│       │                                                                 |
│       ▼                                                                 |
│  ┌────────────────────────────────────────────────────────────────────┐ |
│  │                    Compilation Analysis                            │ |
│  │  RegisterCompilationAction() — analyze entire compilation          │ |
│  │  RegisterCompilationStartAction() — setup + analyze                │ |
│  └────────────────────────────────────────────────────────────────────┘ |
└─────────────────────────────────────────────────────────────────────────┘
```

### Registration Actions

| Action | Granularity | Use Case |
|--------|-------------|----------|
| `RegisterSyntaxNodeAction` | Specific syntax | Check `if` statements |
| `RegisterSyntaxTreeAction` | Entire file | File-level checks |
| `RegisterSymbolAction` | Symbol | Check type/member declarations |
| `RegisterOperationAction` | IOperation | Language-agnostic semantic |
| `RegisterCodeBlockAction` | Method body | Method-level flow analysis |
| `RegisterCompilationAction` | Assembly | Cross-file analysis |

---

## Common Analyzer Patterns

### Code Style Analyzer

```csharp
public class UseExpressionBodyAnalyzer : AbstractBuiltInCodeStyleDiagnosticAnalyzer
{
    public override void Initialize(AnalysisContext context)
    {
        context.RegisterSyntaxNodeAction(
            AnalyzeMethodDeclaration,
            SyntaxKind.MethodDeclaration);
    }
    
    private void AnalyzeMethodDeclaration(SyntaxNodeAnalysisContext context)
    {
        var method = (MethodDeclarationSyntax)context.Node;
        if (CanUseExpressionBody(method))
        {
            context.ReportDiagnostic(...);
        }
    }
}
```

### IOperation-Based Analyzer (Language-Agnostic)

```csharp
public class UnusedParameterAnalyzer : DiagnosticAnalyzer
{
    public override void Initialize(AnalysisContext context)
    {
        context.RegisterOperationBlockStartAction(operationBlockContext =>
        {
            // Track parameter usage across operation block
            var usedParameters = new HashSet<IParameterSymbol>();
            
            operationBlockContext.RegisterOperationAction(
                ctx => TrackParameterReference(ctx, usedParameters),
                OperationKind.ParameterReference);
            
            operationBlockContext.RegisterOperationBlockEndAction(
                ctx => ReportUnusedParameters(ctx, usedParameters));
        });
    }
}
```

---

## Configuration

### EditorConfig

Analyzers respect `.editorconfig` settings:

```ini
# .editorconfig
[*.cs]
# Prefer var for built-in types
csharp_style_var_for_built_in_types = true:suggestion

# Expression-bodied members
csharp_style_expression_bodied_methods = true:suggestion

# Severity overrides
dotnet_diagnostic.IDE0003.severity = warning
```

### Severity Levels

| Severity | Behavior |
|----------|----------|
| `error` | Build fails |
| `warning` | Warning in build output |
| `suggestion` | Light bulb only |
| `silent` | Hidden but fixable |
| `none` | Completely disabled |

---

## What's NOT Covered Here

- **Features Layer** — How code fixes integrate; see [Features Overview](../features/product_overview.md)
- **Compiler Diagnostics** — Compiler errors; see [Compilers Overview](../compilers/product_overview.md)

---

---

## Related Documentation

**In This Overview:**
- [Codebase Overview](./codebase_overview.md) — Technical architecture and components
- [Main Overview](../main_overview.md) — Full codebase map
- [Glossary](../glossary.md) — Terminology

**Existing Codebase Docs:**
- [How To Write a C# Analyzer and Code Fix](../../wiki/How-To-Write-a-C%23-Analyzer-and-Code-Fix.md)
- [How To Write a Visual Basic Analyzer and Code Fix](../../wiki/How-To-Write-a-Visual-Basic-Analyzer-and-Code-Fix.md)
- [Analyzer Runner](../../wiki/Analyzer-Runner.md)
- [RoslynAnalyzers README](../../src/RoslynAnalyzers/Microsoft.CodeAnalysis.Analyzers/readme.md)

**External:**
- [Analyzer Docs](https://docs.microsoft.com/en-us/dotnet/fundamentals/code-analysis/)

---

## Documentation Scope

This document explains why the Analyzers areas exist and what types of analyzers Roslyn includes. It covers the analyzer catalog and patterns but does not detail each analyzer's implementation.

**What's covered:** Analyzer types, registration patterns, configuration via EditorConfig

**What's not covered:** Implementation of each analyzer, all diagnostic IDs, code fix details

**To go deeper:** See [Codebase Overview](./codebase_overview.md) for architecture. For more detail, start a new AI session using the [Expanding Documentation Prompt](https://github.com/CyrusNajmabadi/codebase-explorer/blob/main/LOADER.md#expanding-documentation-prompt).

**Parent document:** [Main Overview](../main_overview.md)
