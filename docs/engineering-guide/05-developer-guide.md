# Developer Guide

Practical guide for common development tasks in the Roslyn repository.

---

## Table of Contents

- [Getting Started](#getting-started)
- [Building and Testing](#building-and-testing)
- [Adding a Language Feature](#adding-a-language-feature)
- [Creating a Diagnostic Analyzer](#creating-a-diagnostic-analyzer)
- [Adding a Code Refactoring](#adding-a-code-refactoring)
- [Adding a Code Fix](#adding-a-code-fix)
- [Implementing IDE Features](#implementing-ide-features)
- [Working with Workspace API](#working-with-workspace-api)
- [Debugging Tips](#debugging-tips)
- [Testing Strategies](#testing-strategies)
- [Common Patterns and Utilities](#common-patterns-and-utilities)

---

## Getting Started

### Prerequisites

- **.NET SDK:** 10.0.100-rc.2 (specified in `global.json`)
- **Visual Studio 2022** or later (for Windows development)
- **VS Code** (for cross-platform development)
- **Git**

### Clone and Build

```bash
# Clone repository
git clone https://github.com/dotnet/roslyn.git
cd roslyn

# Restore packages
./restore.sh   # Unix
Restore.cmd    # Windows

# Build
./build.sh     # Unix
Build.cmd      # Windows

# Run tests
./Test.cmd     # Windows
```

### Opening the Solution

**Full Solution:**
```bash
# Open Roslyn.slnx in Visual Studio
```

**Focused Development:**
```bash
# Compiler work: Open Compilers.slnf
# IDE work: Open Ide.slnf
```

---

## Building and Testing

### Build Scripts

**Windows:**
```cmd
Build.cmd                    # Full build
Build.cmd -configuration Release  # Release build
Build.cmd -project Compilers      # Build specific project
```

**Unix:**
```bash
./build.sh                   # Full build
./build.sh -c Release        # Release build
```

### Running Tests

**All tests:**
```cmd
Test.cmd
```

**Specific test project:**
```cmd
dotnet test src/Compilers/CSharp/Test/Semantic/Microsoft.CodeAnalysis.CSharp.Semantic.UnitTests.csproj
```

**Specific test:**
```cmd
dotnet test --filter "FullyQualifiedName~TestMethodName"
```

### Solution Filters

Use solution filters for faster load times:
- **Compilers.slnf** - C# and VB compilers only
- **Ide.slnf** - IDE features and editor

### Build Output

Default output: `/artifacts/bin/`

Compiler executables:
- `csc.exe` - C# compiler
- `vbc.exe` - VB compiler

---

## Adding a Language Feature

Complete workflow for adding a new C# language feature.

### Step 1: Define Syntax

**File:** `/src/Compilers/CSharp/Portable/Syntax/Syntax.xml`

Add new syntax node definition:

```xml
<Node Name="MyFeatureSyntax" Base="ExpressionSyntax">
  <Kind Name="MyFeatureExpression"/>
  <Field Name="Keyword" Type="SyntaxToken">
    <Kind Name="MyKeyword"/>
  </Field>
  <Field Name="Expression" Type="ExpressionSyntax"/>
</Node>
```

**Regenerate code:**
```cmd
# Syntax nodes auto-generated from Syntax.xml
# Build to regenerate
```

### Step 2: Lexer (if adding keyword)

**File:** `/src/Compilers/CSharp/Portable/Parser/Lexer.cs`

Add keyword to keyword table:

```csharp
private void AddKeywords()
{
    // Existing keywords...
    this.keywordKind[(int)TokenKind.MyKeyword] = SyntaxKind.MyKeyword;
}
```

### Step 3: Parser

**File:** `/src/Compilers/CSharp/Portable/Parser/LanguageParser.cs`

Implement parsing logic:

```csharp
private ExpressionSyntax ParseMyFeatureExpression()
{
    var keyword = this.EatToken(SyntaxKind.MyKeyword);
    var expression = this.ParseExpression();

    return SyntaxFactory.MyFeatureSyntax(keyword, expression);
}
```

Integrate into expression parsing:

```csharp
private ExpressionSyntax ParseTerm(...)
{
    switch (this.CurrentToken.Kind)
    {
        case SyntaxKind.MyKeyword:
            return this.ParseMyFeatureExpression();
        // ... other cases
    }
}
```

### Step 4: Bound Tree

**File:** `/src/Compilers/CSharp/Portable/BoundTree/BoundNodes.xml`

Define bound node:

```xml
<Node Name="BoundMyFeature" Base="BoundExpression">
  <Field Name="Expression" Type="BoundExpression"/>
  <Field Name="Type" Type="TypeSymbol" Override="true"/>
</Node>
```

### Step 5: Binding

**File:** `/src/Compilers/CSharp/Portable/Binder/Binder_Expressions.cs`

Implement binding:

```csharp
private BoundExpression BindMyFeatureExpression(MyFeatureSyntax syntax, BindingDiagnosticBag diagnostics)
{
    // 1. Bind sub-expression
    var boundExpr = BindExpression(syntax.Expression, diagnostics);

    // 2. Type checking
    if (!boundExpr.Type.IsReferenceType)
    {
        Error(diagnostics, ErrorCode.ERR_MyFeatureRequiresRefType, syntax);
    }

    // 3. Create bound node
    return new BoundMyFeature(syntax, boundExpr, boundExpr.Type);
}
```

Add to BindExpression switch:

```csharp
private BoundExpression BindExpression(ExpressionSyntax node, ...)
{
    switch (node.Kind())
    {
        case SyntaxKind.MyFeatureExpression:
            return BindMyFeatureExpression((MyFeatureSyntax)node, diagnostics);
        // ... other cases
    }
}
```

### Step 6: Lowering

**File:** `/src/Compilers/CSharp/Portable/Lowering/LocalRewriter.cs`

Implement lowering (if needed):

```csharp
public override BoundNode VisitMyFeature(BoundMyFeature node)
{
    // Transform to simpler construct
    // Example: Convert to method call
    return MakeCall(node.Expression, methodSymbol);
}
```

### Step 7: IL Emission

**File:** `/src/Compilers/CSharp/Portable/CodeGen/EmitExpression.cs`

Emit IL (if custom emission needed):

```csharp
private void EmitMyFeatureExpression(BoundMyFeature expr, bool used)
{
    // Emit IL opcodes
    EmitExpression(expr.Expression, used);
    _builder.EmitOpCode(ILOpCode.MyOpCode);
}
```

### Step 8: Error Messages

**File:** `/src/Compilers/CSharp/Portable/Errors/ErrorCode.cs`

Add error codes:

```csharp
ERR_MyFeatureRequiresRefType = 9000,
```

**File:** `/src/Compilers/CSharp/Portable/CSharpResources.resx`

Add error messages:

```xml
<data name="ERR_MyFeatureRequiresRefType">
  <value>My feature requires a reference type</value>
</data>
```

### Step 9: Tests

**Parser tests:** `/src/Compilers/CSharp/Test/Syntax/Parsing/`

```csharp
[Fact]
public void TestMyFeatureParsing()
{
    var code = "my keyword expression";
    var tree = SyntaxFactory.ParseExpression(code);

    Assert.Equal(SyntaxKind.MyFeatureExpression, tree.Kind());
}
```

**Semantic tests:** `/src/Compilers/CSharp/Test/Semantic/`

```csharp
[Fact]
public void TestMyFeatureSemantics()
{
    var code = @"
        class C
        {
            void M()
            {
                var x = my keyword expression;
            }
        }
    ";
    var comp = CreateCompilation(code);
    comp.VerifyDiagnostics(/* expected diagnostics */);
}
```

**Emit tests:** `/src/Compilers/CSharp/Test/Emit/CodeGen/`

```csharp
[Fact]
public void TestMyFeatureEmit()
{
    var code = "...";
    CompileAndVerify(code, expectedOutput: "...");
}
```

### Step 10: Documentation

**Feature spec:** `/docs/features/my-feature.md`

Document the feature design and implementation.

---

## Creating a Diagnostic Analyzer

Create a custom diagnostic analyzer with code fix.

### Step 1: Create Analyzer

**Location:** `/src/Analyzers/CSharp/Analyzers/MyAnalyzer/`

```csharp
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class MyDiagnosticAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "MY001";

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
        DiagnosticId,
        title: "My analyzer title",
        messageFormat: "My analyzer message: '{0}'",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "My analyzer description");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        // Register analysis actions
        context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.MethodDeclaration);
    }

    private void AnalyzeNode(SyntaxNodeAnalysisContext context)
    {
        var methodDecl = (MethodDeclarationSyntax)context.Node;

        // Analyze method
        if (/* condition */)
        {
            var diagnostic = Diagnostic.Create(
                Rule,
                methodDecl.Identifier.GetLocation(),
                methodDecl.Identifier.Text);
            context.ReportDiagnostic(diagnostic);
        }
    }
}
```

### Step 2: Create Code Fix

**Location:** `/src/Analyzers/CSharp/CodeFixes/MyAnalyzer/`

```csharp
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;

[ExportCodeFixProvider(LanguageNames.CSharp)]
public class MyCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds
        => ImmutableArray.Create(MyDiagnosticAnalyzer.DiagnosticId);

    public override FixAllProvider GetFixAllProvider()
        => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken);
        var diagnostic = context.Diagnostics.First();
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        var node = root.FindNode(diagnosticSpan);

        context.RegisterCodeFix(
            CodeAction.Create(
                title: "My code fix",
                createChangedDocument: c => FixAsync(context.Document, node, c),
                equivalenceKey: "MyCodeFix"),
            diagnostic);
    }

    private async Task<Document> FixAsync(
        Document document,
        SyntaxNode node,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken);

        // Create replacement
        var newNode = /* create fixed node */;
        var newRoot = root.ReplaceNode(node, newNode);

        return document.WithSyntaxRoot(newRoot);
    }
}
```

### Step 3: Add Tests

**Analyzer test:** `/src/Analyzers/CSharp/Tests/MyAnalyzer/`

```csharp
using Microsoft.CodeAnalysis.Testing;
using Xunit;

public class MyAnalyzerTests
{
    [Fact]
    public async Task TestAnalyzer()
    {
        var code = @"
            class C
            {
                void [|M|]() { }  // [| |] marks expected diagnostic location
            }
        ";

        await new CSharpAnalyzerTest<MyDiagnosticAnalyzer>
        {
            TestCode = code,
        }.RunAsync();
    }

    [Fact]
    public async Task TestCodeFix()
    {
        var before = @"
            class C
            {
                void M() { }
            }
        ";

        var after = @"
            class C
            {
                void M() { /* fixed */ }
            }
        ";

        await new CSharpCodeFixTest<MyDiagnosticAnalyzer, MyCodeFixProvider>
        {
            TestCode = before,
            FixedCode = after,
        }.RunAsync();
    }
}
```

---

## Adding a Code Refactoring

Create a code refactoring that doesn't require a diagnostic.

### Step 1: Create Refactoring Provider

**Location:** `/src/Features/CSharp/Portable/CodeRefactorings/MyRefactoring/`

```csharp
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Syntax;

[ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(MyRefactoringProvider))]
internal class MyRefactoringProvider : CodeRefactoringProvider
{
    public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
    {
        var document = context.Document;
        var textSpan = context.Span;
        var cancellationToken = context.CancellationToken;

        // 1. Get syntax root
        var root = await document.GetSyntaxRootAsync(cancellationToken);

        // 2. Find node at cursor
        var node = root.FindNode(textSpan);

        // 3. Check if refactoring applies
        if (node is not MethodDeclarationSyntax methodDecl)
            return;

        // 4. Register refactoring
        context.RegisterRefactoring(
            CodeAction.Create(
                title: "My refactoring",
                createChangedDocument: c => RefactorAsync(document, methodDecl, c),
                equivalenceKey: "MyRefactoring"));
    }

    private async Task<Document> RefactorAsync(
        Document document,
        MethodDeclarationSyntax method,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken);

        // Create refactored code
        var newMethod = /* transform method */;
        var newRoot = root.ReplaceNode(method, newMethod);

        return document.WithSyntaxRoot(newRoot);
    }
}
```

### Step 2: Add Tests

**Location:** `/src/Features/CSharpTest/CodeRefactorings/MyRefactoring/`

```csharp
using Xunit;
using Microsoft.CodeAnalysis.Testing;

public class MyRefactoringTests
{
    [Fact]
    public async Task TestRefactoring()
    {
        var before = @"
            class C
            {
                void [||]M() { }  // [||] marks cursor position
            }
        ";

        var after = @"
            class C
            {
                void M() { /* refactored */ }
            }
        ";

        await new CSharpCodeRefactoringTest<MyRefactoringProvider>
        {
            TestCode = before,
            FixedCode = after,
        }.RunAsync();
    }
}
```

---

## Adding a Code Fix

Code fixes are tied to specific diagnostics.

### Create Code Fix Provider

**Location:** `/src/Features/CSharp/Portable/CodeFixes/MyCodeFix/`

```csharp
[ExportCodeFixProvider(LanguageNames.CSharp)]
internal class MyCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds
        => ImmutableArray.Create("CS0103"); // Fix specific error code

    public override FixAllProvider GetFixAllProvider()
        => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var document = context.Document;
        var diagnostic = context.Diagnostics.First();
        var span = diagnostic.Location.SourceSpan;

        var root = await document.GetSyntaxRootAsync(context.CancellationToken);
        var node = root.FindNode(span);

        // Check if fix applies
        if (!CanFix(node))
            return;

        context.RegisterCodeFix(
            CodeAction.Create(
                title: "My fix",
                createChangedSolution: c => FixAsync(context.Document, node, c),
                equivalenceKey: "MyFix"),
            diagnostic);
    }

    private bool CanFix(SyntaxNode node) { /* ... */ }

    private async Task<Solution> FixAsync(Document document, SyntaxNode node, CancellationToken ct)
    {
        // Apply fix
        var solution = document.Project.Solution;
        // ... make changes
        return solution;
    }
}
```

---

## Implementing IDE Features

### Completion Provider

**Location:** `/src/Features/CSharp/Portable/Completion/CompletionProviders/`

```csharp
[ExportCompletionProvider(nameof(MyCompletionProvider), LanguageNames.CSharp)]
internal class MyCompletionProvider : CompletionProvider
{
    public override async Task ProvideCompletionsAsync(CompletionContext context)
    {
        var document = context.Document;
        var position = context.Position;

        // 1. Get syntax tree
        var tree = await document.GetSyntaxTreeAsync(context.CancellationToken);

        // 2. Check if completion applies
        if (!ShouldProvideCompletion(tree, position))
            return;

        // 3. Add completion items
        context.AddItem(CompletionItem.Create(
            displayText: "MyCompletion",
            inlineDescription: "My description",
            sortText: "MyCompletion"));
    }

    private bool ShouldProvideCompletion(SyntaxTree tree, int position)
    {
        // Check context
        return true;
    }
}
```

### Quick Info Provider

**Location:** `/src/Features/CSharp/Portable/QuickInfo/`

```csharp
[ExportQuickInfoProvider(nameof(MyQuickInfoProvider), LanguageNames.CSharp)]
internal class MyQuickInfoProvider : QuickInfoProvider
{
    public override async Task<QuickInfoItem> GetQuickInfoAsync(QuickInfoContext context)
    {
        var document = context.Document;
        var position = context.Position;

        var tree = await document.GetSyntaxTreeAsync(context.CancellationToken);
        var token = tree.GetRoot().FindToken(position);

        // Check if quick info applies
        if (!ShouldShowQuickInfo(token))
            return null;

        return QuickInfoItem.Create(
            span: token.Span,
            sections: ImmutableArray.Create(
                QuickInfoSection.Create(
                    QuickInfoSectionKinds.Description,
                    ImmutableArray.Create(new TaggedText(TextTags.Text, "My quick info")))));
    }
}
```

---

## Working with Workspace API

### Loading a Solution

```csharp
using Microsoft.CodeAnalysis.MSBuild;

var workspace = MSBuildWorkspace.Create();
var solution = await workspace.OpenSolutionAsync(@"C:\path\to\solution.sln");

foreach (var project in solution.Projects)
{
    Console.WriteLine($"Project: {project.Name}");

    foreach (var document in project.Documents)
    {
        Console.WriteLine($"  Document: {document.Name}");
    }
}
```

### Analyzing Code

```csharp
var document = solution.Projects.First().Documents.First();

// Get syntax tree
var syntaxRoot = await document.GetSyntaxRootAsync();

// Get semantic model
var semanticModel = await document.GetSemanticModelAsync();

// Find all method declarations
var methods = syntaxRoot.DescendantNodes()
    .OfType<MethodDeclarationSyntax>();

foreach (var method in methods)
{
    // Get symbol for method
    var symbol = semanticModel.GetDeclaredSymbol(method);
    Console.WriteLine($"Method: {symbol.Name}");
}
```

### Modifying Code

```csharp
// Immutable update pattern
var oldRoot = await document.GetSyntaxRootAsync();

// Find node to replace
var oldMethod = oldRoot.DescendantNodes()
    .OfType<MethodDeclarationSyntax>()
    .First();

// Create new node
var newMethod = oldMethod.WithIdentifier(
    SyntaxFactory.Identifier("NewName"));

// Replace node
var newRoot = oldRoot.ReplaceNode(oldMethod, newMethod);

// Update document
var newDocument = document.WithSyntaxRoot(newRoot);

// Apply to solution
var newSolution = newDocument.Project.Solution;
```

### Finding References

```csharp
using Microsoft.CodeAnalysis.FindSymbols;

var symbol = /* get symbol */;
var references = await SymbolFinder.FindReferencesAsync(symbol, solution);

foreach (var reference in references)
{
    foreach (var location in reference.Locations)
    {
        Console.WriteLine($"Reference in {location.Document.Name} at {location.Location.GetLineSpan()}");
    }
}
```

---

## Debugging Tips

### Debugging the Compiler

**Attach to csc.exe:**

1. Build Roslyn in Debug mode
2. Set environment variable: `set RoslynCommandLineLogFile=C:\logs\csc.log`
3. Compile test code
4. Attach debugger to csc.exe process

**Debugging in Visual Studio:**

1. Set startup project to `csc` or `vbc`
2. Set command line arguments in project properties
3. F5 to debug

### Debugging IDE Features

**Attach to Visual Studio:**

1. Build Roslyn in Debug mode
2. Deploy VSIX (or use Roslyn.sln)
3. Start experimental instance: `devenv.exe /rootsuffix Roslyn`
4. Attach debugger to experimental instance

**Debugging LSP Server:**

1. Set breakpoints in `/src/LanguageServer/`
2. Configure VS Code to use local build
3. Attach debugger to LanguageServer process

### Syntax Visualizer

**Tool:** Roslyn Syntax Visualizer (VSIX extension)

- Shows syntax tree structure
- Displays red/green nodes
- Shows trivia (whitespace, comments)

### Diagnostic Tools

**Dump compilation:**
```csharp
var compilation = /* get compilation */;
var diagnostics = compilation.GetDiagnostics();

foreach (var diag in diagnostics)
{
    Console.WriteLine(diag);
}
```

**Dump syntax tree:**
```csharp
var tree = /* get tree */;
Console.WriteLine(tree.GetRoot().ToFullString());
```

---

## Testing Strategies

### Unit Tests

**Compiler tests:**
- Syntax tests - verify parsing
- Semantic tests - verify binding and errors
- Emit tests - verify IL generation

**IDE feature tests:**
- Test completion, refactorings, etc.
- Use test helpers in `*TestUtilities/`

### Test Utilities

**Location:** `/src/Compilers/Test/Core/`

```csharp
// Create compilation
var compilation = CreateCompilation(@"
    class C
    {
        void M() { }
    }
");

// Verify diagnostics
compilation.VerifyDiagnostics(
    // (3,10): error CS0101: ...
    Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "M").WithArguments("M").WithLocation(3, 10)
);

// Compile and verify IL
CompileAndVerify(@"
    class C
    {
        static void Main()
        {
            System.Console.WriteLine(""Hello"");
        }
    }
", expectedOutput: "Hello");
```

### Integration Tests

**Location:** `/src/VisualStudio/IntegrationTest/`

Test Visual Studio integration:
- Project system
- Editor features
- Debugger integration

---

## Common Patterns and Utilities

### Using SyntaxFactory

```csharp
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

// Create method declaration
var method = SyntaxFactory.MethodDeclaration(
    returnType: SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)),
    identifier: SyntaxFactory.Identifier("MyMethod"))
    .WithBody(SyntaxFactory.Block());

// Create using directive
var usingDirective = SyntaxFactory.UsingDirective(
    SyntaxFactory.ParseName("System.Linq"));
```

### Syntax Rewriter

```csharp
public class MyRewriter : CSharpSyntaxRewriter
{
    public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        // Transform method
        var newNode = node.WithIdentifier(
            SyntaxFactory.Identifier(node.Identifier.Text + "Async"));

        return base.VisitMethodDeclaration(newNode);
    }
}

// Usage
var rewriter = new MyRewriter();
var newRoot = rewriter.Visit(oldRoot);
```

### Pooled Collections

```csharp
using Microsoft.CodeAnalysis.PooledObjects;

// Use pooled array builder
using var builder = ArrayBuilder<SyntaxNode>.GetInstance();
builder.Add(node1);
builder.Add(node2);
var array = builder.ToImmutableAndFree();

// Pooled hash set
using var set = PooledHashSet<ISymbol>.GetInstance();
set.Add(symbol);
```

### Symbol Visitor

```csharp
public class MySymbolVisitor : SymbolVisitor
{
    public override void VisitNamedType(INamedTypeSymbol symbol)
    {
        Console.WriteLine($"Type: {symbol.Name}");

        foreach (var member in symbol.GetMembers())
        {
            member.Accept(this);
        }
    }

    public override void VisitMethod(IMethodSymbol symbol)
    {
        Console.WriteLine($"  Method: {symbol.Name}");
    }
}
```

---

## Additional Resources

**Documentation:**
- Building: `/docs/contributing/Building, Debugging, and Testing on Windows.md`
- Language features: `/docs/contributing/Developing a Language Feature.md`
- Compiler test plan: `/docs/contributing/Compiler Test Plan.md`

**Design Docs:**
- `/docs/compilers/Design/` - Compiler design documents
- `/docs/features/` - Feature specifications
- `/docs/ide/` - IDE feature specs

**API Docs:**
- [Roslyn API Documentation](https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/)
- [.NET Compiler Platform SDK](https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/get-started/syntax-analysis)

---

## Getting Help

**Internal Resources:**
- Team documentation
- Code reviews
- Team meetings

**External Resources:**
- [Roslyn GitHub](https://github.com/dotnet/roslyn)
- [Roslyn Discussions](https://github.com/dotnet/roslyn/discussions)
- [Roslyn Issues](https://github.com/dotnet/roslyn/issues)

---

**End of Developer Guide**
