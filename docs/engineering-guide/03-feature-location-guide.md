# Feature Location Guide

Quick reference for finding where to modify specific features and functionality.

---

## Table of Contents

- [Compiler Features](#compiler-features)
- [Language Features](#language-features)
- [IntelliSense Features](#intellisense-features)
- [Refactorings](#refactorings)
- [Code Fixes](#code-fixes)
- [Formatting](#formatting)
- [Navigation Features](#navigation-features)
- [Diagnostics and Analyzers](#diagnostics-and-analyzers)
- [Editor UI Features](#editor-ui-features)
- [Debugger Features](#debugger-features)

---

## Compiler Features

### Adding a New C# Language Feature

**Step-by-step locations:**

1. **Syntax Definition** → `/src/Compilers/CSharp/Portable/Syntax/Syntax.xml`
   - Define new syntax nodes in XML
   - Run code generator to create syntax classes

2. **Lexer Changes** → `/src/Compilers/CSharp/Portable/Parser/Lexer.cs`
   - Add new keywords or tokens

3. **Parser** → `/src/Compilers/CSharp/Portable/Parser/LanguageParser.cs`
   - Implement parsing logic for new syntax
   - Methods like `ParseStatement()`, `ParseExpression()`

4. **Binding** → `/src/Compilers/CSharp/Portable/Binder/Binder_*.cs`
   - Add semantic analysis logic
   - Symbol resolution
   - Type checking

5. **Bound Tree** → `/src/Compilers/CSharp/Portable/BoundTree/BoundNodes.xml`
   - Define intermediate representation nodes

6. **Lowering** → `/src/Compilers/CSharp/Portable/Lowering/LocalRewriter.cs`
   - Desugar feature to simpler constructs
   - Transform to IL-ready form

7. **IL Emission** → `/src/Compilers/CSharp/Portable/CodeGen/EmitExpression.cs`
   - Generate IL opcodes

8. **Tests** → `/src/Compilers/CSharp/Test/`
   - Syntax tests → `Syntax/`
   - Semantic tests → `Semantic/`
   - Emit tests → `Emit/`

**Example files for reference:**
- Pattern matching: `/src/Compilers/CSharp/Portable/Binder/Binder_Patterns.cs`
- Async/await lowering: `/src/Compilers/CSharp/Portable/Lowering/AsyncRewriter/`
- LINQ lowering: `/src/Compilers/CSharp/Portable/Lowering/LocalRewriter/LocalRewriter_Query.cs`

### Modifying C# Parser

**Location:** `/src/Compilers/CSharp/Portable/Parser/LanguageParser.cs`

| What to modify | Method/Area |
|----------------|-------------|
| Statements | `ParseStatement()`, `ParseEmbeddedStatement()` |
| Expressions | `ParseExpression()`, `ParseSubExpression()` |
| Declarations | `ParseMemberDeclaration()`, `ParseTypeDeclaration()` |
| Attributes | `ParseAttributeDeclaration()` |
| Modifiers | `ParseModifiers()` |

### Modifying C# Binding

**Location:** `/src/Compilers/CSharp/Portable/Binder/`

| What to bind | File |
|--------------|------|
| Expressions | `Binder_Expressions.cs` |
| Statements | `Binder_Statements.cs` |
| Patterns | `Binder_Patterns.cs` |
| Type inference | `Binder_Conversions.cs` |
| Overload resolution | `Binder_Invocation.cs` |
| Lambda expressions | `Binder_Lambda.cs` |

### Compiler Diagnostics

**C# Error Definitions:** `/src/Compilers/CSharp/Portable/Errors/ErrorCode.cs`

**Error Messages:** `/src/Compilers/CSharp/Portable/CSharpResources.resx`

**Diagnostic Creation:**
```csharp
// Location: /src/Compilers/CSharp/Portable/Errors/MessageProvider.cs
diagnostics.Add(ErrorCode.ERR_BadExpressionOrDeclaration, location);
```

---

## Language Features

### Async/Await

**Lowering:** `/src/Compilers/CSharp/Portable/Lowering/AsyncRewriter/`
- `AsyncRewriter.cs` - Main rewriter
- `AsyncMethodToStateMachineRewriter.cs` - State machine generation

**Binding:** `/src/Compilers/CSharp/Portable/Binder/Binder_Await.cs`

### Nullable Reference Types

**Flow Analysis:** `/src/Compilers/CSharp/Portable/FlowAnalysis/NullableWalker.cs`
- Tracks nullability through code flow
- Generates warnings

**Binding:** `/src/Compilers/CSharp/Portable/Binder/Binder_Nullable.cs`

**Diagnostics:** Search for "CS8600-CS8899" in `/src/Compilers/CSharp/Portable/Errors/ErrorCode.cs`

### Pattern Matching

**Parser:** `/src/Compilers/CSharp/Portable/Parser/LanguageParser_Patterns.cs`

**Binding:** `/src/Compilers/CSharp/Portable/Binder/Binder_Patterns.cs`

**Lowering:** `/src/Compilers/CSharp/Portable/Lowering/LocalRewriter/LocalRewriter_PatternSwitch.cs`

### LINQ Query Syntax

**Binding:** `/src/Compilers/CSharp/Portable/Binder/Binder_Query.cs`

**Lowering:** `/src/Compilers/CSharp/Portable/Lowering/LocalRewriter/LocalRewriter_Query.cs`
- Converts query syntax to method calls

### Records

**Binding:** `/src/Compilers/CSharp/Portable/Binder/Binder_Records.cs`

**Synthesis:** `/src/Compilers/CSharp/Portable/Symbols/Synthesized/Records/`
- Generates methods: `Equals`, `GetHashCode`, `ToString`, `Deconstruct`

### Source Generators

**Infrastructure:** `/src/Compilers/Core/Portable/SourceGeneration/`
- `ISourceGenerator.cs` - Generator interface
- `GeneratorDriver.cs` - Execution driver

**Integration:** `/src/Compilers/Core/Portable/DiagnosticAnalyzer/AnalyzerDriver.cs`

---

## IntelliSense Features

### Code Completion

**Infrastructure:** `/src/Features/Core/Portable/Completion/`

**C# Completion Providers:** `/src/Features/CSharp/Portable/Completion/CompletionProviders/`

| Provider | Purpose |
|----------|---------|
| `KeywordCompletionProvider.cs` | Keywords (`if`, `class`, etc.) |
| `SymbolCompletionProvider.cs` | Types, members, variables |
| `SnippetCompletionProvider.cs` | Code snippets |
| `ObjectCreationCompletionProvider.cs` | `new` completions |
| `OverrideCompletionProvider.cs` | Override member completions |
| `PartialMethodCompletionProvider.cs` | Partial method completions |
| `XmlDocCommentCompletionProvider.cs` | XML doc comments |

**UI:** `/src/EditorFeatures/Core/IntelliSense/AsyncCompletion/`

### Quick Info (Hover Tooltips)

**Service:** `/src/Features/Core/Portable/QuickInfo/`

**C# Implementation:** `/src/Features/CSharp/Portable/QuickInfo/`

**UI:** `/src/EditorFeatures/Core/QuickInfo/`

### Signature Help (Parameter Hints)

**Service:** `/src/Features/Core/Portable/SignatureHelp/`

**C# Providers:** `/src/Features/CSharp/Portable/SignatureHelp/`
- `InvocationExpressionSignatureHelpProvider.cs` - Method calls
- `ObjectCreationExpressionSignatureHelpProvider.cs` - Constructors
- `GenericNameSignatureHelpProvider.cs` - Generic types

**UI:** `/src/EditorFeatures/Core/SignatureHelp/`

### Inline Hints (Parameter Names, Type Hints)

**Service:** `/src/Features/Core/Portable/InlineHints/`

**C# Implementation:** `/src/Features/CSharp/Portable/InlineHints/`

**UI:** `/src/EditorFeatures/Core/InlineHints/`

---

## Refactorings

### Refactoring Infrastructure

**Base:** `/src/Workspaces/Core/Portable/CodeActions/CodeAction.cs`

**Provider Base:** `/src/Features/Core/Portable/CodeRefactorings/CodeRefactoringProvider.cs`

### C# Refactorings

**Location:** `/src/Features/CSharp/Portable/CodeRefactorings/`

| Refactoring | File |
|-------------|------|
| Extract Method | `ExtractMethod/ExtractMethodCodeRefactoringProvider.cs` |
| Extract Interface | `ExtractInterface/ExtractInterfaceCodeRefactoringProvider.cs` |
| Extract Class | `ExtractClass/ExtractClassCodeRefactoringProvider.cs` |
| Inline Method | `InlineMethod/CSharpInlineMethodRefactoringProvider.cs` |
| Introduce Variable | `IntroduceVariable/IntroduceVariableCodeRefactoringProvider.cs` |
| Introduce Parameter | `IntroduceParameter/IntroduceParameterCodeRefactoringProvider.cs` |
| Move to Namespace | `MoveToNamespace/MoveToNamespaceCodeRefactoringProvider.cs` |
| Convert Anonymous to Class | `ConvertAnonymousTypeToClass/` |
| Convert Tuple to Struct | `ConvertTupleToStruct/` |
| Use Expression Body | `UseExpressionBody/` |
| Invert If | `InvertIf/InvertIfCodeRefactoringProvider.cs` |
| Invert Conditional | `InvertConditional/` |
| Convert Foreach to For | `ConvertForEachToFor/` |
| Convert For to Foreach | `ConvertForToForEach/` |
| Split/Merge If | `SplitOrMergeIfStatements/` |
| Add Missing Usings | `AddMissingImports/` |

**All refactorings:** Browse `/src/Features/CSharp/Portable/CodeRefactorings/`

### Refactoring Implementation Pattern

```csharp
// Location: /src/Features/*/Portable/CodeRefactorings/
[ExportCodeRefactoringProvider]
public class MyRefactoringProvider : CodeRefactoringProvider
{
    public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
    {
        // 1. Check if refactoring applies
        // 2. Create CodeAction
        // 3. Register with context
    }
}
```

---

## Code Fixes

### Code Fix Infrastructure

**Base:** `/src/Workspaces/Core/Portable/CodeFixes/CodeFixProvider.cs`

### C# Code Fixes

**Location:** `/src/Features/CSharp/Portable/CodeFixes/`

| Code Fix | File |
|----------|------|
| Add Using | `AddImport/AddImportCodeFixProvider.cs` |
| Remove Unused Usings | `RemoveUnnecessaryImports/` |
| Implement Interface | `ImplementInterface/ImplementInterfaceCodeFixProvider.cs` |
| Implement Abstract Class | `ImplementAbstractClass/` |
| Generate Constructor | `GenerateConstructor/` |
| Generate Method | `GenerateMethod/` |
| Generate Variable | `GenerateVariable/` |
| Generate Type | `GenerateType/` |
| Fix Return Type | `FixReturnType/` |
| Make Method Async | `MakeMethodAsynchronous/` |
| Add Null Check | `AddNullCheck/` |
| Remove Unnecessary Cast | `RemoveUnnecessaryCast/` |
| Simplify Names | `SimplifyNames/` |
| Use Var | `UseImplicitType/` |
| Use Explicit Type | `UseExplicitType/` |

**All code fixes:** Browse `/src/Features/CSharp/Portable/CodeFixes/`

### Code Fix Implementation Pattern

```csharp
// Location: /src/Features/*/Portable/CodeFixes/
[ExportCodeFixProvider]
public class MyCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ...;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        // 1. Analyze diagnostic
        // 2. Create fix
        // 3. Register with context
    }
}
```

---

## Formatting

### Formatting Engine

**Infrastructure:** `/src/Workspaces/Core/Portable/Formatting/`
- `Formatter.cs` - Main formatting API
- `AbstractFormatEngine.cs` - Formatting engine
- `FormattingOptions.cs` - Formatting options

**C# Formatting:** `/src/Features/CSharp/Portable/Formatting/`
- `CSharpFormattingOptions.cs` - C# formatting options
- `CSharpFormatEngine.cs` - C# formatting engine

### Formatting Rules

**C# Formatting Rules:** `/src/Workspaces/CSharp/Portable/Formatting/`

| Rule | Purpose |
|------|---------|
| `AlignTokensFormattingRule.cs` | Token alignment |
| `IndentBlockFormattingRule.cs` | Block indentation |
| `NewLineFormattingRule.cs` | New line rules |
| `SpacingFormattingRule.cs` | Whitespace rules |
| `WrappingFormattingRule.cs` | Line wrapping |

### Indentation

**Smart Indentation:** `/src/Features/Core/Portable/Indentation/`

**C# Indentation:** `/src/Features/CSharp/Portable/Indentation/`

---

## Navigation Features

### Go to Definition

**Service:** `/src/Features/Core/Portable/GoToDefinition/AbstractGoToDefinitionService.cs`

**C# Implementation:** `/src/Features/CSharp/Portable/GoToDefinition/`

**UI:** `/src/EditorFeatures/Core/GoToDefinition/`

### Find All References

**Service:** `/src/Features/Core/Portable/FindUsages/AbstractFindUsagesService.cs`

**Symbol Finder:** `/src/Workspaces/Core/Portable/FindSymbols/SymbolFinder.cs`

**UI:** `/src/EditorFeatures/Core/FindUsages/`

### Navigate To

**Service:** `/src/Features/Core/Portable/NavigateTo/`

**C# Implementation:** `/src/Features/CSharp/Portable/NavigateTo/`

**UI:** `/src/EditorFeatures/Core/NavigateTo/`

### Go to Implementation

**Service:** `/src/Features/Core/Portable/GoToImplementation/`

**C# Implementation:** `/src/Features/CSharp/Portable/GoToImplementation/`

### Go to Base

**Service:** `/src/Features/Core/Portable/GoToBase/`

**C# Implementation:** `/src/Features/CSharp/Portable/GoToBase/`

### Navigation Bar

**Service:** `/src/Features/Core/Portable/NavigationBar/`

**UI:** `/src/EditorFeatures/Core/NavigationBar/`

---

## Diagnostics and Analyzers

### Creating a Diagnostic Analyzer

**Base Class:** `/src/Compilers/Core/Portable/DiagnosticAnalyzer/DiagnosticAnalyzer.cs`

**Implementation Pattern:**
```csharp
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class MyAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ...;

    public override void Initialize(AnalysisContext context)
    {
        // Register actions
        context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.ClassDeclaration);
    }
}
```

### Built-in Analyzers

**Location:** `/src/Analyzers/`

**C# Analyzers:** `/src/Analyzers/CSharp/Analyzers/`
- `UsePatternMatching/` - Pattern matching suggestions
- `UseExpressionBody/` - Expression body suggestions
- `UseConditionalExpression/` - Conditional expression suggestions
- `SimplifyLinqExpression/` - LINQ simplification

### Code Style Analyzers

**Location:** `/src/CodeStyle/`

**C# Code Style:** `/src/CodeStyle/CSharp/Analyzers/`
- IDE0001-IDE0099 rules
- Naming conventions
- Expression preferences
- Code block preferences

### Analyzer Driver

**Execution:** `/src/Compilers/Core/Portable/DiagnosticAnalyzer/AnalyzerDriver.cs`

**Compilation Integration:** `/src/Compilers/Core/Portable/Compilation/Compilation.cs`
- `GetAnalyzerDiagnostics()` method

---

## Editor UI Features

### Syntax Highlighting

**Classification:** `/src/EditorFeatures/Core/Classification/`

**C# Classifier:** `/src/EditorFeatures/CSharp/Classification/`

**Semantic Classification:** `/src/Features/Core/Portable/Classification/`

### Brace Matching

**Service:** `/src/Features/Core/Portable/BraceMatching/`

**C# Implementation:** `/src/Features/CSharp/Portable/BraceMatching/`

**UI:** `/src/EditorFeatures/Core/BraceMatching/`

### Reference Highlighting

**Service:** `/src/Features/Core/Portable/DocumentHighlighting/`

**UI:** `/src/EditorFeatures/Core/ReferenceHighlighting/`

### Keyword Highlighting

**Service:** `/src/Features/Core/Portable/KeywordHighlighting/`

**C# Highlighters:** `/src/Features/CSharp/Portable/KeywordHighlighting/`

**UI:** `/src/EditorFeatures/Core/KeywordHighlighting/`

### CodeLens

**Service:** `/src/Features/Core/Portable/CodeLens/`

**VS Integration:** `/src/VisualStudio/CodeLens/`

### Inline Rename

**Service:** `/src/Workspaces/Core/Portable/Rename/`

**UI:** `/src/EditorFeatures/Core/InlineRename/`

### Light Bulb (Code Actions Menu)

**UI:** `/src/EditorFeatures/Core/CodeActions/`

**Suggested Actions:** `/src/EditorFeatures/Core/Lightup/`

---

## Debugger Features

### Edit and Continue

**Service:** `/src/Features/Core/Portable/EditAndContinue/`

**C# Implementation:** `/src/Features/CSharp/Portable/EditAndContinue/`

**UI:** `/src/EditorFeatures/Core/EditAndContinue/`

### Expression Evaluator

**Infrastructure:** `/src/ExpressionEvaluator/Core/`

**C# Evaluator:** `/src/ExpressionEvaluator/CSharp/`
- `Source/ExpressionCompiler/` - Expression compilation
- `Source/ResultProvider/` - Result formatting

**Watch Window Support:**
- Evaluates expressions in debugger context
- Formats results for display

### Immediate Window

**Handled by Expression Evaluator**

**C# Command Processing:** `/src/ExpressionEvaluator/CSharp/Source/ExpressionCompiler/`

---

## Quick Reference Table

| Feature | Primary Location |
|---------|------------------|
| C# Parser | `/src/Compilers/CSharp/Portable/Parser/` |
| C# Binding | `/src/Compilers/CSharp/Portable/Binder/` |
| IL Emission | `/src/Compilers/Core/Portable/Emit/` |
| Syntax Definitions | `/src/Compilers/CSharp/Portable/Syntax/Syntax.xml` |
| IntelliSense | `/src/Features/*/Portable/Completion/` |
| Refactorings | `/src/Features/*/Portable/CodeRefactorings/` |
| Code Fixes | `/src/Features/*/Portable/CodeFixes/` |
| Formatting | `/src/Workspaces/*/Portable/Formatting/` |
| Analyzers | `/src/Analyzers/` |
| Code Style | `/src/CodeStyle/` |
| Go to Definition | `/src/Features/Core/Portable/GoToDefinition/` |
| Find References | `/src/Features/Core/Portable/FindUsages/` |
| Rename | `/src/Workspaces/Core/Portable/Rename/` |
| Classification | `/src/EditorFeatures/Core/Classification/` |
| Edit and Continue | `/src/Features/Core/Portable/EditAndContinue/` |
| Expression Evaluator | `/src/ExpressionEvaluator/` |

---

**Next:** [Architecture and Design Patterns](04-architecture.md)
