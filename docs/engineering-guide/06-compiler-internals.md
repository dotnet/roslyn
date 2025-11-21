# C# Compiler Internals: High-Resolution Map

**Surgical guide for modifying the C# language**

This document provides a high-resolution map of the C# compiler implementation, showing exactly which files, types, and methods to modify when changing the language itself.

---

## Table of Contents

- [Compiler Pipeline Overview](#compiler-pipeline-overview)
- [Parser Files](#parser-files)
- [Binder Files](#binder-files)
- [Symbol System](#symbol-system)
- [Syntax Infrastructure](#syntax-infrastructure)
- [Bound Tree](#bound-tree)
- [Lowering System](#lowering-system)
- [Flow Analysis](#flow-analysis)
- [Code Generation](#code-generation)
- [Error System](#error-system)
- [Modification Workflows](#modification-workflows)
- [Language Feature Reference](#language-feature-reference)

---

## Compiler Pipeline Overview

```
Source Code (string)
    ↓
┌──────────────────────────────────────────────────────────┐
│  LEXER (/Parser/Lexer*.cs)                               │
│  - Converts characters to tokens                         │
│  - Handles keywords, operators, literals                 │
└──────────────────────────────────────────────────────────┘
    ↓ [Tokens]
┌──────────────────────────────────────────────────────────┐
│  PARSER (/Parser/LanguageParser.cs)                      │
│  - Converts tokens to syntax tree                        │
│  - Incremental parsing via Blender                       │
└──────────────────────────────────────────────────────────┘
    ↓ [Syntax Tree - Red/Green nodes]
┌──────────────────────────────────────────────────────────┐
│  BINDER (/Binder/Binder_*.cs)                            │
│  - Semantic analysis                                     │
│  - Symbol resolution                                     │
│  - Type checking                                         │
└──────────────────────────────────────────────────────────┘
    ↓ [Bound Tree + Symbols]
┌──────────────────────────────────────────────────────────┐
│  FLOW ANALYSIS (/FlowAnalysis/*.cs)                      │
│  - Definite assignment                                   │
│  - Nullable reference analysis                           │
│  - Control flow analysis                                 │
└──────────────────────────────────────────────────────────┘
    ↓ [Analyzed Bound Tree]
┌──────────────────────────────────────────────────────────┐
│  LOWERING (/Lowering/LocalRewriter*.cs)                  │
│  - Desugar language features                            │
│  - foreach → while loop                                  │
│  - using → try/finally                                   │
│  - async → state machine                                 │
└──────────────────────────────────────────────────────────┘
    ↓ [Lowered Bound Tree]
┌──────────────────────────────────────────────────────────┐
│  ASYNC/ITERATOR REWRITING (if needed)                    │
│  - /Lowering/AsyncRewriter/                              │
│  - /Lowering/IteratorRewriter/                           │
└──────────────────────────────────────────────────────────┘
    ↓
┌──────────────────────────────────────────────────────────┐
│  CLOSURE CONVERSION (/Lowering/ClosureConversion/)       │
│  - Lambda capturing                                      │
│  - Display class generation                              │
└──────────────────────────────────────────────────────────┘
    ↓
┌──────────────────────────────────────────────────────────┐
│  CODE GENERATION (/CodeGen/*.cs)                         │
│  - Emit IL opcodes                                       │
│  - Generate metadata                                     │
└──────────────────────────────────────────────────────────┘
    ↓ [IL + Metadata]
┌──────────────────────────────────────────────────────────┐
│  PE WRITER (/Emit/)                                      │
│  - Write .dll/.exe file                                  │
└──────────────────────────────────────────────────────────┘
    ↓
.NET Assembly
```

**Base Location:** `/src/Compilers/CSharp/Portable/`

---

## Parser Files

**Location:** `/src/Compilers/CSharp/Portable/Parser/`

### Core Parsing Infrastructure

| File | Size | Purpose |
|------|------|---------|
| **LanguageParser.cs** | 652KB | **MAIN PARSER** - Parses all C# constructs |
| **Lexer.cs** | 186KB | **MAIN LEXER** - Tokenizes source code |
| **SyntaxParser.cs** | 48KB | Base parser class with error recovery |
| **DirectiveParser.cs** | — | Preprocessor directives (#if, #define, etc.) |
| **DocumentationCommentParser.cs** | 75KB | XML documentation comments |

### Specialized Parsing

| File | Purpose |
|------|---------|
| **LanguageParser_Patterns.cs** | Pattern matching syntax (is, switch patterns) |
| **LanguageParser_InterpolatedString.cs** | Interpolated string expressions |
| **Lexer_StringLiteral.cs** | String literal tokenization |
| **Lexer_RawStringLiteral.cs** | Raw string literals (C# 11+) |
| **Lexer.Interpolation.cs** | String interpolation transitions |

### Incremental Parsing

| File | Purpose |
|------|---------|
| **Blender.cs** | Merges old and new syntax trees |
| **Blender.Cursor.cs** | Cursor for blending |
| **Blender.Reader.cs** | Reader for incremental parse |
| **LexerCache.cs** | Caches lexer results |

### Key Methods in LanguageParser.cs

#### Top-Level Parsing
```csharp
CompilationUnitSyntax ParseCompilationUnit()
```
Parses entire file - entry point for parsing.

#### Declaration Parsing
```csharp
MemberDeclarationSyntax ParseMemberDeclaration()
```
Parses class, interface, struct, enum, method, property, field, etc.

```csharp
TypeDeclarationSyntax ParseTypeDeclaration()
```
Parses type declarations (class, struct, interface, record).

#### Statement Parsing
```csharp
StatementSyntax ParseStatement()
```
**Main statement parser** - handles all statement types.

```csharp
StatementSyntax ParseEmbeddedStatement()
```
Parses statements in contexts that don't allow declarations.

```csharp
BlockSyntax ParseBlock()
```
Parses block statements `{ ... }`.

#### Expression Parsing
```csharp
ExpressionSyntax ParseExpression()
```
**Main expression parser** - entry point.

```csharp
ExpressionSyntax ParseSubExpression(Precedence precedence)
```
Precedence-climbing expression parser.

```csharp
ExpressionSyntax ParseTerm(Precedence precedence)
```
Parses primary expressions and operators.

#### Pattern Parsing
```csharp
PatternSyntax ParsePattern()
```
Parses pattern syntax for pattern matching.

#### Type Parsing
```csharp
TypeSyntax ParseType()
```
Parses type syntax.

```csharp
NameSyntax ParseName()
```
Parses qualified names.

### Where to Add New Syntax

| Syntax Category | Method to Modify |
|----------------|------------------|
| **New statement type** | `ParseStatement()` - add case to switch |
| **New expression** | `ParseTerm()` or `ParseSubExpression()` |
| **New operator** | `ParseSubExpression()` - add precedence |
| **New declaration** | `ParseMemberDeclaration()` |
| **New type construct** | `ParseType()` or `ParseTypeName()` |
| **New pattern** | `ParsePattern()` |
| **New keyword** | `Lexer.cs` - add to keyword table |

---

## Binder Files

**Location:** `/src/Compilers/CSharp/Portable/Binder/`

The binder performs semantic analysis, converting syntax trees to bound trees with type information.

### Core Binder Classes

| File | Size | Purpose |
|------|------|---------|
| **Binder.cs** | 39KB | **BASE BINDER** - Symbol lookup, name resolution |
| **Binder_Expressions.cs** | 594KB | **EXPRESSION BINDING** - All expression types |
| **Binder_Statements.cs** | 204KB | **STATEMENT BINDING** - All statement types |
| **Binder_Symbols.cs** | 142KB | Symbol declaration binding |
| **Binder_Lookup.cs** | 105KB | Name resolution and symbol lookup |
| **Binder_Conversions.cs** | 200KB | Type conversion analysis |
| **Binder_Operators.cs** | 317KB | Operator overload resolution |
| **Binder_Invocation.cs** | 133KB | Method/delegate invocation binding |

### Specialized Binders

| File | Purpose |
|------|---------|
| **Binder_Patterns.cs** (99KB) | Pattern matching binding |
| **Binder_Query.cs** (53KB) | LINQ query expression binding |
| **Binder_Lambda.cs** (21KB) | Lambda expression binding |
| **Binder_Attributes.cs** (54KB) | Attribute binding |
| **Binder_Await.cs** (35KB) | Await expression binding |
| **Binder_Initializers.cs** (16KB) | Object/collection initializer binding |
| **Binder_Deconstruct.cs** (49KB) | Deconstruction binding |
| **Binder_Constraints.cs** (31KB) | Generic constraint validation |
| **Binder_AnonymousTypes.cs** (10KB) | Anonymous type binding |
| **Binder_InterpolatedString.cs** (60KB) | Interpolated string binding |
| **Binder_TupleOperators.cs** (22KB) | Tuple operation binding |
| **Binder_WithExpression.cs** | Record with-expression binding |
| **Binder_Crefs.cs** (61KB) | XML doc comment cref binding |

### Scope Binder Classes

These binders create lexical scopes:

| Class | File | Purpose |
|-------|------|---------|
| **BlockBinder** | BlockBinder.cs | Block statement scope |
| **LocalScopeBinder** | LocalScopeBinder.cs | Local variable scope |
| **ForLoopBinder** | ForLoopBinder.cs | For-loop variable scope |
| **ForEachLoopBinder** | ForEachLoopBinder.cs | ForEach variable scope |
| **SwitchBinder** | SwitchBinder.cs | Switch statement scope |
| **UsingStatementBinder** | UsingStatementBinder.cs | Using statement scope |
| **LockBinder** | LockOrUsingBinder.cs | Lock statement scope |
| **CatchClauseBinder** | CatchClauseBinder.cs | Catch clause variable |
| **ExecutableCodeBinder** | ExecutableCodeBinder.cs | Method body scope |
| **WithUsingNamespacesAndTypesBinder** | WithUsingNamespacesAndTypesBinder.cs | Using directives |
| **WithExternAliasesBinder** | WithExternAliasesBinder.cs | Extern aliases |
| **WithLambdaParametersBinder** | WithLambdaParametersBinder.cs | Lambda parameters |
| **WithTypeParametersBinder** | WithTypeParametersBinder.cs | Generic type parameters |

### BinderFactory

**File:** `BinderFactory.cs`

Creates the appropriate binder hierarchy for any syntax node.

```csharp
internal static Binder GetBinder(
    SyntaxNode node,
    NamespaceOrTypeSymbol container,
    CSharpCompilation compilation)
```

### Key Binding Methods

#### Expression Binding (Binder_Expressions.cs)

```csharp
BoundExpression BindExpression(ExpressionSyntax node, BindingDiagnosticBag diagnostics)
```
**Main expression binder** - dispatches to specific expression types.

```csharp
BoundExpression BindMemberAccessExpression(MemberAccessExpressionSyntax node, ...)
```
Binds member access (`.` operator).

```csharp
BoundExpression BindInvocationExpression(InvocationExpressionSyntax node, ...)
```
Binds method invocations.

```csharp
BoundExpression BindObjectCreationExpression(ObjectCreationExpressionSyntax node, ...)
```
Binds `new` expressions.

#### Statement Binding (Binder_Statements.cs)

```csharp
BoundStatement BindStatement(StatementSyntax node, BindingDiagnosticBag diagnostics)
```
**Main statement binder** - dispatches to specific statement types.

```csharp
BoundStatement BindIfStatement(IfStatementSyntax node, ...)
```
Binds if statements.

```csharp
BoundStatement BindForStatement(ForStatementSyntax node, ...)
```
Binds for loops.

#### Operator Binding (Binder_Operators.cs)

```csharp
BoundExpression BindBinaryOperator(BinaryExpressionSyntax node, ...)
```
Binds binary operators with overload resolution.

```csharp
BoundExpression BindUnaryOperator(PrefixUnaryExpressionSyntax node, ...)
```
Binds unary operators.

#### Pattern Binding (Binder_Patterns.cs)

```csharp
BoundPattern BindPattern(PatternSyntax node, ...)
```
Binds pattern matching syntax.

### Where to Add Binding Logic

| Feature Type | File to Modify |
|-------------|----------------|
| **New expression** | `Binder_Expressions.cs` - add to `BindExpression()` |
| **New statement** | `Binder_Statements.cs` - add to `BindStatement()` |
| **New operator** | `Binder_Operators.cs` - add overload resolution |
| **New pattern** | `Binder_Patterns.cs` - add to `BindPattern()` |
| **Type checking** | `Binder_Conversions.cs` - add conversion logic |
| **Method resolution** | `Binder_Invocation.cs` - modify overload resolution |

---

## Symbol System

**Location:** `/src/Compilers/CSharp/Portable/Symbols/`

Symbols represent semantic program elements (types, methods, fields, etc.).

### Core Symbol Types

| Symbol Type | File | Represents |
|------------|------|------------|
| **Symbol** | Symbol.cs (77KB) | Base class for all symbols |
| **TypeSymbol** | TypeSymbol.cs (136KB) | All types (classes, structs, etc.) |
| **NamedTypeSymbol** | NamedTypeSymbol.cs (76KB) | Named types with members |
| **MethodSymbol** | MethodSymbol.cs (55KB) | Methods and constructors |
| **PropertySymbol** | PropertySymbol.cs (18KB) | Properties |
| **FieldSymbol** | FieldSymbol.cs (19KB) | Fields |
| **EventSymbol** | EventSymbol.cs (12KB) | Events |
| **ParameterSymbol** | ParameterSymbol.cs (18KB) | Method parameters |
| **LocalSymbol** | LocalSymbol.cs (12KB) | Local variables |
| **TypeParameterSymbol** | TypeParameterSymbol.cs (24KB) | Generic type parameters |
| **NamespaceSymbol** | NamespaceSymbol.cs (13KB) | Namespaces |

### Source Symbols

**Location:** `/src/Compilers/CSharp/Portable/Symbols/Source/`

Symbols for declarations in source code:

| File | Purpose |
|------|---------|
| **SourceNamedTypeSymbol.cs** (96KB) | Class/struct/interface from source |
| **SourceMemberContainerSymbol.cs** (288KB) | Base for types with members |
| **SourceMemberMethodSymbol.cs** (44KB) | Method from source |
| **SourcePropertySymbol.cs** (40KB) | Property from source |
| **SourceFieldSymbol.cs** (15KB) | Field from source |
| **SourceEventSymbol.cs** (42KB) | Event from source |
| **SourceLocalSymbol.cs** (39KB) | Local variable from source |
| **SourceConstructorSymbol.cs** | Constructor from source |
| **SourceDestructorSymbol.cs** | Destructor from source |
| **LocalFunctionSymbol.cs** (23KB) | Local function |
| **LambdaSymbol.cs** (16KB) | Lambda expression |
| **SourceAssemblySymbol.cs** (140KB) | Assembly from source |

### Metadata Symbols

**Location:** `/src/Compilers/CSharp/Portable/Symbols/Metadata/PE/`

Symbols for declarations in referenced assemblies:

- `PEMethodSymbol.cs` - Methods from metadata
- `PEPropertySymbol.cs` - Properties from metadata
- `PEFieldSymbol.cs` - Fields from metadata
- `PENamedTypeSymbol.cs` - Types from metadata

### Synthesized Symbols

**Location:** `/src/Compilers/CSharp/Portable/Symbols/Synthesized/`

Compiler-generated symbols:

| Category | Files |
|----------|-------|
| **Display classes** | `SynthesizedClosureEnvironment.cs` - Lambda closure classes |
| **State machines** | `StateMachineTypeSymbol.cs` - Async/iterator state machines |
| **Records** | `/Synthesized/Records/` - Generated record members |
| **Anonymous types** | `AnonymousTypeManager.cs` - Anonymous type symbols |

### Symbol Interfaces (Public API)

**Location:** `/src/Compilers/Core/Portable/Symbols/`

```csharp
ISymbol              // Base interface
INamespaceSymbol     // Namespace
ITypeSymbol          // Type
INamedTypeSymbol     // Named type (class, struct, etc.)
IMethodSymbol        // Method
IPropertySymbol      // Property
IFieldSymbol         // Field
IEventSymbol         // Event
IParameterSymbol     // Parameter
ILocalSymbol         // Local variable
```

---

## Syntax Infrastructure

**Location:** `/src/Compilers/CSharp/Portable/Syntax/`

### SyntaxKind Enum

**File:** `SyntaxKind.cs`

Defines all syntax node kinds. **Modify this when adding new syntax.**

**Categories:**
- **Tokens** (8193-8287): Keywords, operators, punctuation
- **Expressions**: All expression types
- **Statements**: All statement types
- **Declarations**: All declaration types
- **Patterns**: Pattern matching constructs

**Example values:**
```csharp
// Keywords
PublicKeyword = 8342
ClassKeyword = 8291
IfKeyword = 8326

// Expressions
AddExpression = 8668
InvocationExpression = 8821
MemberAccessExpression = 8824

// Statements
IfStatement = 8936
WhileStatement = 8938
ForStatement = 8939

// Declarations
ClassDeclaration = 9025
MethodDeclaration = 9056
PropertyDeclaration = 9061
```

### Key Syntax Files

| File | Purpose |
|------|---------|
| **CSharpSyntaxNode.cs** (22KB) | Base class for all C# syntax nodes |
| **CSharpSyntaxTree.cs** | Syntax tree representation |
| **CSharpSyntaxVisitor.cs** | Visitor pattern for traversal |
| **CSharpSyntaxRewriter.cs** | Rewriter for transformations |
| **SyntaxFactory.cs** | Factory for creating syntax nodes |

### Syntax Node Generation

**Source:** `Syntax.xml`

Syntax nodes are code-generated from XML definitions. To add a new syntax node:

1. Edit `/src/Compilers/CSharp/Portable/Syntax/Syntax.xml`
2. Define node structure
3. Rebuild to regenerate code

**Example:**
```xml
<Node Name="IfStatementSyntax" Base="StatementSyntax">
  <Kind Name="IfStatement"/>
  <Field Name="IfKeyword" Type="SyntaxToken">
    <Kind Name="IfKeyword"/>
  </Field>
  <Field Name="Condition" Type="ExpressionSyntax"/>
  <Field Name="Statement" Type="StatementSyntax"/>
  <Field Name="Else" Type="ElseClauseSyntax" Optional="true"/>
</Node>
```

---

## Bound Tree

**Location:** `/src/Compilers/CSharp/Portable/BoundTree/`

Bound nodes are type-checked syntax with semantic information.

### BoundKind Enum

All bound node types:
- `BoundExpression` - Typed expressions
- `BoundStatement` - Typed statements
- `BoundPattern` - Typed patterns

### Key Bound Node Files

| File | Purpose |
|------|---------|
| **BoundNode.cs** (26KB) | Base bound node class |
| **BoundExpression.cs** (20KB) | Expression base with type |
| **BoundStatement.cs** | Statement base |
| **BoundPattern.cs** | Pattern base |

### Common Bound Node Types

**Expressions:**
```csharp
BoundBinaryOperator       // Binary operations (+, -, *, etc.)
BoundUnaryOperator        // Unary operations (++, --, !, etc.)
BoundCall                 // Method calls
BoundLocal                // Local variable reference
BoundFieldAccess          // Field access
BoundPropertyAccess       // Property access
BoundObjectCreationExpression  // new operator
BoundConversion           // Type conversions
BoundLambda               // Lambda expressions
```

**Statements:**
```csharp
BoundBlock                // Block statements
BoundIfStatement          // If statements
BoundWhileStatement       // While loops
BoundForStatement         // For loops
BoundForEachStatement     // ForEach loops
BoundSwitchStatement      // Switch statements
BoundTryStatement         // Try-catch-finally
BoundReturnStatement      // Return statements
```

**Patterns:**
```csharp
BoundConstantPattern      // Constant patterns
BoundTypePattern          // Type patterns
BoundRecursivePattern     // Recursive patterns (property patterns)
BoundListPattern          // List patterns
```

### Bound Node Definition

**Source:** `BoundNodes.xml`

Bound nodes are code-generated. Add new bound nodes here.

### Utilities

| File | Purpose |
|------|---------|
| **BoundTreeRewriter.cs** (12KB) | Base class for rewriting bound trees |
| **BoundTreeWalker.cs** (7KB) | Visitor for walking bound trees |
| **BoundNodeExtensions.cs** (11KB) | Extension methods on bound nodes |

---

## Lowering System

**Location:** `/src/Compilers/CSharp/Portable/Lowering/`

Lowering transforms high-level C# constructs into simpler IL-ready forms.

### LocalRewriter Files

**Main file:** `LocalRewriter.cs` (56KB)

The LocalRewriter orchestrates all lowering transformations.

### Statement Lowering

| File | Lowers |
|------|--------|
| **LocalRewriter_IfStatement.cs** | If statements |
| **LocalRewriter_ForStatement.cs** (10KB) | For loops |
| **LocalRewriter_WhileStatement.cs** | While loops |
| **LocalRewriter_DoStatement.cs** | Do-while loops |
| **LocalRewriter_ForEachStatement.cs** (66KB) | **ForEach → while + enumerator** |
| **LocalRewriter_TryStatement.cs** | Try-catch-finally |
| **LocalRewriter_UsingStatement.cs** (26KB) | **Using → try/finally** |
| **LocalRewriter_LockStatement.cs** (11KB) | **Lock → Monitor.Enter/Exit** |
| **LocalRewriter_FixedStatement.cs** (28KB) | Fixed statements |
| **LocalRewriter_Yield.cs** | Yield return/break |
| **LocalRewriter_Block.cs** | Block statements |
| **LocalRewriter_LocalDeclaration.cs** | Local declarations |
| **LocalRewriter_ReturnStatement.cs** | Return statements |
| **LocalRewriter_ThrowStatement.cs** | Throw statements |
| **LocalRewriter_GotoStatement.cs** | Goto statements |

### Expression Lowering

| File | Lowers |
|------|--------|
| **LocalRewriter_BinaryOperator.cs** (126KB) | Binary operators (+, -, *, /, etc.) |
| **LocalRewriter_UnaryOperator.cs** (63KB) | Unary operators (++, --, !, ~, etc.) |
| **LocalRewriter_Call.cs** (87KB) | Method calls |
| **LocalRewriter_Conversion.cs** (97KB) | Type conversions |
| **LocalRewriter_IndexerAccess.cs** (57KB) | Indexer access |
| **LocalRewriter_PropertyAccess.cs** | Property access → get/set calls |
| **LocalRewriter_ObjectCreationExpression.cs** (19KB) | new operator |
| **LocalRewriter_AssignmentOperator.cs** (21KB) | Assignment (=) |
| **LocalRewriter_CompoundAssignmentOperator.cs** (53KB) | Compound assignments (+=, -=, etc.) |
| **LocalRewriter_StringConcat.cs** (40KB) | String concatenation |
| **LocalRewriter_StringInterpolation.cs** (15KB) | **Interpolated strings → string.Format or handler** |
| **LocalRewriter_NullCoalescingOperator.cs** (14KB) | **?? and ??= operators** |
| **LocalRewriter_ConditionalAccess.cs** (8KB) | **?. and ?[] operators** |
| **LocalRewriter_ObjectOrCollectionInitializerExpression.cs** (38KB) | Object/collection initializers |
| **LocalRewriter_Range.cs** (8KB) | Range operator (..) |
| **LocalRewriter_Index.cs** | Index operator (^) |
| **LocalRewriter_TupleBinaryOperator.cs** (35KB) | Tuple operators |
| **LocalRewriter_DeconstructionAssignmentOperator.cs** (24KB) | Deconstruction |
| **LocalRewriter_Event.cs** (16KB) | Event operations |

### Pattern Matching Lowering

| File | Purpose |
|------|---------|
| **LocalRewriter_IsPatternOperator.cs** (16KB) | is patterns |
| **LocalRewriter_PatternSwitchStatement.cs** (9KB) | Pattern switch statements |
| **LocalRewriter_SwitchExpression.cs** (11KB) | Switch expressions |
| **LocalRewriter.DecisionDagRewriter.cs** (63KB) | **Decision tree for pattern matching** |

### LINQ Query Lowering

| File | Purpose |
|------|---------|
| **LocalRewriter_Query.cs** | **LINQ query → method calls** |

### Async/Await Lowering

**Location:** `/src/Compilers/CSharp/Portable/Lowering/AsyncRewriter/`

| File | Purpose |
|------|---------|
| **AsyncRewriter.cs** (17KB) | Main async rewriter |
| **AsyncMethodToStateMachineRewriter.cs** | **Async method → state machine** |
| **AsyncIteratorRewriter.cs** | Async iterator state machine |
| **AsyncStateMachine.cs** | Synthesized state machine type |

**Transformation:**
```csharp
async Task<int> MethodAsync()
{
    await Task.Delay(100);
    return 42;
}

// Becomes:
class <MethodAsync>d__0 : IAsyncStateMachine
{
    int <>1__state;
    AsyncTaskMethodBuilder<int> <>t__builder;

    void MoveNext()
    {
        // State machine implementation
    }
}
```

### Iterator Lowering

**Location:** `/src/Compilers/CSharp/Portable/Lowering/IteratorRewriter/`

| File | Purpose |
|------|---------|
| **IteratorMethodToStateMachineRewriter.cs** | **Iterator → state machine** |
| **IteratorStateMachine.cs** | Synthesized iterator type |

**Transformation:**
```csharp
IEnumerable<int> GetNumbers()
{
    yield return 1;
    yield return 2;
}

// Becomes:
class <GetNumbers>d__0 : IEnumerable<int>, IEnumerator<int>
{
    int <>1__state;
    int <>2__current;

    bool MoveNext()
    {
        // State machine implementation
    }
}
```

### Closure Conversion

**Location:** `/src/Compilers/CSharp/Portable/Lowering/ClosureConversion/`

| File | Purpose |
|------|---------|
| **ClosureConversion.cs** (88KB) | Main closure conversion |
| **ClosureConversion.Analysis.cs** (31KB) | Analyzes captured variables |
| **ExpressionLambdaRewriter.cs** (62KB) | Expression tree lambdas |
| **SynthesizedClosureEnvironment.cs** | Generated display class |

**Transformation:**
```csharp
int x = 5;
Func<int> lambda = () => x + 1;

// Becomes:
class <>c__DisplayClass0_0
{
    public int x;
}
var displayClass = new <>c__DisplayClass0_0();
displayClass.x = 5;
Func<int> lambda = displayClass.<Method>b__0;
```

---

## Flow Analysis

**Location:** `/src/Compilers/CSharp/Portable/FlowAnalysis/`

Flow analysis validates definite assignment, reachability, and nullable references.

### Core Flow Analysis Files

| File | Size | Purpose |
|------|------|---------|
| **DefiniteAssignment.cs** | 126KB | **Definite assignment analysis** |
| **NullableWalker.cs** | 656KB | **Nullable reference type analysis** |
| **ControlFlowPass.cs** | 15KB | Control flow path analysis |
| **AbstractFlowPass.cs** | 144KB | Base flow analysis framework |

### Definite Assignment

**File:** `DefiniteAssignment.cs`

Ensures variables are assigned before use.

**Key methods:**
```csharp
void VisitExpression(BoundExpression node)
void VisitStatement(BoundStatement node)
void Assign(Symbol symbol, bool definitelyAssigned)
```

### Nullable Analysis

**File:** `NullableWalker.cs`

Analyzes nullable reference types and generates warnings.

**Related files:**
- `NullableWalker_Patterns.cs` (55KB) - Pattern nullability
- `NullableWalker.Variables.cs` (17KB) - Variable state tracking

### Data Flow Walkers

| File | Purpose |
|------|---------|
| **DataFlowsInWalker.cs** | Variables that flow into region |
| **DataFlowsOutWalker.cs** (10KB) | Variables that flow out of region |
| **ReadWriteWalker.cs** (13KB) | Variable read/write tracking |
| **AlwaysAssignedWalker.cs** | Variables always assigned |

---

## Code Generation

**Location:** `/src/Compilers/CSharp/Portable/CodeGen/`

Generates IL opcodes from lowered bound trees.

### Core Code Generation Files

| File | Size | Purpose |
|------|------|---------|
| **CodeGenerator.cs** | 23KB | Main IL generation engine |
| **EmitExpression.cs** | 178KB | **Expression IL emission** |
| **EmitStatement.cs** | 87KB | **Statement IL emission** |
| **EmitOperators.cs** | 32KB | Operator IL emission |
| **EmitConversion.cs** | 18KB | Type conversion IL |
| **EmitAddress.cs** | 23KB | Address IL (ldloca, ldfld) |
| **EmitArrayInitializer.cs** | 39KB | Array initialization |
| **Optimizer.cs** | 91KB | IL optimization |

### IL Builder

**Location:** `/src/Compilers/Core/Portable/CodeGen/ILBuilder.cs`

Low-level IL opcode emission.

**Key methods:**
```csharp
void EmitOpCode(ILOpCode opCode)
void EmitIntConstant(int value)
void EmitLoadLocal(LocalDefinition local)
void EmitStoreLocal(LocalDefinition local)
void EmitCall(MethodSymbol method)
```

### Where to Add IL Emission

| Feature | File to Modify |
|---------|----------------|
| **New expression IL** | `EmitExpression.cs` |
| **New statement IL** | `EmitStatement.cs` |
| **New operator IL** | `EmitOperators.cs` |
| **Type conversion IL** | `EmitConversion.cs` |

---

## Error System

**Location:** `/src/Compilers/CSharp/Portable/Errors/`

### Error Codes

**File:** `ErrorCode.cs` (112KB)

Defines all compiler errors and warnings.

**Naming convention:**
- `ERR_` - Errors (prevent compilation)
- `WRN_` - Warnings
- `FTL_` - Fatal errors

**Example:**
```csharp
ERR_SemicolonExpected = 1002,
ERR_CloseParenExpected = 1026,
ERR_BadExpressionOrDeclaration = 1525,
WRN_UnreachableCode = 162,
WRN_NullabilityMismatch = 8600,
```

### Error Messages

**File:** `CSharpResources.resx`

Error message templates.

### Error Facts

**File:** `ErrorFacts.cs` (155KB)

Metadata about errors (severity, category, help link).

### Message Provider

**File:** `MessageProvider.cs` (17KB)

Creates diagnostic instances from error codes.

### Adding New Errors

1. Add error code to `ErrorCode.cs`
2. Add message to `CSharpResources.resx`
3. Use in code:
```csharp
diagnostics.Add(ErrorCode.ERR_MyNewError, location, arg1, arg2);
```

---

## Modification Workflows

### Workflow 1: Add a New Operator

**Example: Adding a new binary operator `^^^`**

#### Step 1: Lexer
**File:** `/src/Compilers/CSharp/Portable/Parser/Lexer.cs`

Add token recognition:
```csharp
case '^':
    if (TextWindow.PeekChar(1) == '^' && TextWindow.PeekChar(2) == '^')
    {
        TextWindow.AdvanceChar(3);
        return SyntaxKind.CaretCaretCaretToken;
    }
    // ... existing code
```

#### Step 2: SyntaxKind
**File:** `/src/Compilers/CSharp/Portable/Syntax/SyntaxKind.cs`

Add enum values:
```csharp
CaretCaretCaretToken = 8XXX,           // ^^^ token
TripleXorExpression = 9XXX,            // expression kind
```

#### Step 3: Parser
**File:** `/src/Compilers/CSharp/Portable/Parser/LanguageParser.cs`

Add to operator precedence:
```csharp
private ExpressionSyntax ParseSubExpression(Precedence precedence)
{
    // ...
    case SyntaxKind.CaretCaretCaretToken:
        return ParseBinaryExpression(left, SyntaxKind.TripleXorExpression);
}
```

#### Step 4: Binder
**File:** `/src/Compilers/CSharp/Portable/Binder/Binder_Operators.cs`

Add operator binding:
```csharp
private BoundExpression BindBinaryOperator(BinaryExpressionSyntax node, ...)
{
    if (node.Kind() == SyntaxKind.TripleXorExpression)
    {
        // Type checking
        // Overload resolution
        return new BoundBinaryOperator(node, kind, left, right, resultType);
    }
}
```

#### Step 5: Lowering
**File:** `/src/Compilers/CSharp/Portable/Lowering/LocalRewriter_BinaryOperator.cs`

Add lowering (if needed):
```csharp
public override BoundNode VisitBinaryOperator(BoundBinaryOperator node)
{
    if (node.OperatorKind == BinaryOperatorKind.TripleXor)
    {
        // Transform to simpler operations
        // Example: a ^^^ b  →  (a ^ b) ^ (a & b)
    }
}
```

#### Step 6: Code Generation
**File:** `/src/Compilers/CSharp/Portable/CodeGen/EmitOperators.cs`

Add IL emission:
```csharp
private void EmitBinaryOperatorExpression(BoundBinaryOperator expr, bool used)
{
    if (expr.OperatorKind == BinaryOperatorKind.TripleXor)
    {
        EmitExpression(expr.Left, used: true);
        EmitExpression(expr.Right, used: true);
        _builder.EmitOpCode(ILOpCode.Xor);
        // ... additional IL
    }
}
```

#### Step 7: Tests
Add tests for parsing, binding, lowering, and IL emission.

---

### Workflow 2: Add a New Statement Type

**Example: Adding a `repeat-until` statement**

#### Step 1: SyntaxKind
Add `RepeatStatement` to `SyntaxKind.cs`.

#### Step 2: Syntax Definition
**File:** `/src/Compilers/CSharp/Portable/Syntax/Syntax.xml`

```xml
<Node Name="RepeatStatementSyntax" Base="StatementSyntax">
  <Kind Name="RepeatStatement"/>
  <Field Name="RepeatKeyword" Type="SyntaxToken">
    <Kind Name="RepeatKeyword"/>
  </Field>
  <Field Name="Statement" Type="StatementSyntax"/>
  <Field Name="UntilKeyword" Type="SyntaxToken">
    <Kind Name="UntilKeyword"/>
  </Field>
  <Field Name="Condition" Type="ExpressionSyntax"/>
</Node>
```

#### Step 3: Parser
**File:** `/src/Compilers/CSharp/Portable/Parser/LanguageParser.cs`

```csharp
private StatementSyntax ParseStatement()
{
    switch (CurrentToken.Kind)
    {
        case SyntaxKind.RepeatKeyword:
            return ParseRepeatStatement();
        // ... existing cases
    }
}

private RepeatStatementSyntax ParseRepeatStatement()
{
    var repeatKeyword = EatToken(SyntaxKind.RepeatKeyword);
    var statement = ParseEmbeddedStatement();
    var untilKeyword = EatToken(SyntaxKind.UntilKeyword);
    var condition = ParseExpression();
    return SyntaxFactory.RepeatStatement(repeatKeyword, statement, untilKeyword, condition);
}
```

#### Step 4: Bound Tree
Define `BoundRepeatStatement` in `BoundNodes.xml`.

#### Step 5: Binder
**File:** `/src/Compilers/CSharp/Portable/Binder/Binder_Statements.cs`

```csharp
private BoundStatement BindStatement(StatementSyntax node, ...)
{
    switch (node.Kind())
    {
        case SyntaxKind.RepeatStatement:
            return BindRepeatStatement((RepeatStatementSyntax)node, diagnostics);
    }
}

private BoundStatement BindRepeatStatement(RepeatStatementSyntax syntax, ...)
{
    var boundStatement = BindEmbeddedStatement(syntax.Statement, diagnostics);
    var boundCondition = BindBooleanExpression(syntax.Condition, diagnostics);

    return new BoundRepeatStatement(syntax, boundStatement, boundCondition);
}
```

#### Step 6: Lowering
**File:** `/src/Compilers/CSharp/Portable/Lowering/LocalRewriter_RepeatStatement.cs` (new file)

```csharp
public override BoundNode VisitRepeatStatement(BoundRepeatStatement node)
{
    // Lower to do-while:
    // repeat S until C;  →  do S while (!C);

    var negatedCondition = MakeUnaryOperator(
        UnaryOperatorKind.BoolLogicalNegation,
        node.Condition);

    return new BoundDoStatement(
        node.Syntax,
        node.Statement,
        negatedCondition);
}
```

#### Step 7: IL Emission
No additional work needed if lowered to existing constructs.

#### Step 8: Flow Analysis
Update `DefiniteAssignment.cs` and `ControlFlowPass.cs` to handle the new statement.

---

### Workflow 3: Add Pattern Matching Feature

**Example: Adding a new pattern type**

#### Step 1: Syntax
Define pattern syntax in `Syntax.xml`.

#### Step 2: Parser
**File:** `/src/Compilers/CSharp/Portable/Parser/LanguageParser_Patterns.cs`

Add parsing logic.

#### Step 3: Binder
**File:** `/src/Compilers/CSharp/Portable/Binder/Binder_Patterns.cs`

Bind the new pattern type.

#### Step 4: Decision DAG
**File:** `/src/Compilers/CSharp/Portable/Lowering/LocalRewriter.DecisionDagRewriter.cs`

Add pattern to decision tree generation.

#### Step 5: Nullable Analysis
**File:** `/src/Compilers/CSharp/Portable/FlowAnalysis/NullableWalker_Patterns.cs`

Handle nullability in the new pattern.

---

## Language Feature Reference

Quick reference for where existing language features are implemented.

### Async/Await

| Phase | Location |
|-------|----------|
| Parsing | `LanguageParser.cs` - `ParseAwaitExpression()` |
| Binding | `Binder_Await.cs` - `BindAwaitExpression()` |
| Lowering | `/Lowering/AsyncRewriter/AsyncMethodToStateMachineRewriter.cs` |
| IL Emission | Handled by lowering to state machine |

### Nullable Reference Types

| Phase | Location |
|-------|----------|
| Parsing | Handled in type parsing |
| Binding | `Binder.cs` - nullable context tracking |
| Flow Analysis | `NullableWalker.cs` - entire nullable analysis |
| Diagnostics | `ErrorCode.cs` - CS8600-CS8899 range |

### Pattern Matching

| Phase | Location |
|-------|----------|
| Parsing | `LanguageParser_Patterns.cs` |
| Binding | `Binder_Patterns.cs` |
| Lowering | `LocalRewriter.DecisionDagRewriter.cs` |
| Nullable | `NullableWalker_Patterns.cs` |

### Records

| Phase | Location |
|-------|----------|
| Parsing | `LanguageParser.cs` - record keyword |
| Binding | `Binder.cs` - record handling |
| Synthesis | `/Symbols/Synthesized/Records/` - generated members |
| With-expression | `Binder_WithExpression.cs` |

### LINQ Query Syntax

| Phase | Location |
|-------|----------|
| Parsing | `LanguageParser.cs` - `ParseQueryExpression()` |
| Binding | `Binder_Query.cs` |
| Lowering | `LocalRewriter_Query.cs` - converts to method calls |

### Lambda Expressions

| Phase | Location |
|-------|----------|
| Parsing | `LanguageParser.cs` - `ParseLambdaExpression()` |
| Binding | `Binder_Lambda.cs` |
| Closure Conversion | `/Lowering/ClosureConversion/ClosureConversion.cs` |
| Expression Trees | `ExpressionLambdaRewriter.cs` |

### Iterators (yield)

| Phase | Location |
|-------|----------|
| Parsing | `LanguageParser.cs` - `ParseYieldStatement()` |
| Binding | `Binder_Statements.cs` |
| Lowering | `/Lowering/IteratorRewriter/IteratorMethodToStateMachineRewriter.cs` |

### String Interpolation

| Phase | Location |
|-------|----------|
| Lexing | `Lexer.Interpolation.cs` |
| Parsing | `LanguageParser_InterpolatedString.cs` |
| Binding | `Binder_InterpolatedString.cs` |
| Lowering | `LocalRewriter_StringInterpolation.cs` |

### Tuples

| Phase | Location |
|-------|----------|
| Parsing | `LanguageParser.cs` - tuple type/literal |
| Binding | `Binder_Expressions.cs` - tuple binding |
| Operators | `Binder_TupleOperators.cs` |
| Lowering | `LocalRewriter_TupleBinaryOperator.cs` |

---

## Summary

This high-resolution map provides surgical precision for modifying C#. For any language feature:

1. **Lexer** - Tokenize new syntax
2. **Parser** - Parse into syntax tree
3. **Binder** - Semantic analysis and type checking
4. **Lowering** - Transform to simpler constructs
5. **Code Gen** - Emit IL opcodes

Every file, class, and method needed to modify C# is documented above. Use this as your reference when implementing language changes.

---

**See also:**
- [Component Guide](02-component-guide.md) - Higher-level component overview
- [Feature Location Guide](03-feature-location-guide.md) - Quick feature lookup
- [Developer Guide](05-developer-guide.md) - Practical workflows
