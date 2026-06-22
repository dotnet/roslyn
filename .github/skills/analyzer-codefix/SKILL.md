---
name: analyzer-codefix
description: "Create or modify Roslyn IDE analyzers, code fixes, and code refactorings. Use when: adding a new IDE diagnostic (IDE0xxx), implementing a CodeFixProvider, implementing a CodeRefactoringProvider, writing analyzer/fixer tests, or working with AbstractBuiltInCodeStyleDiagnosticAnalyzer. Also use for: diagnostic analyzer, code action, FixAllProvider, TestInRegularAndScriptAsync, TestMissingInRegularAndScriptAsync."
---

# Roslyn Analyzer, Code Fix & Code Refactoring Patterns

## When to Use

- Adding a new IDE analyzer (IDE0xxx diagnostic)
- Implementing or modifying a `CodeFixProvider`
- Implementing or modifying a `CodeRefactoringProvider`
- Writing tests for analyzers, code fixes, or refactorings
- Working with `AbstractBuiltInCodeStyleDiagnosticAnalyzer`

## IDE Diagnostic IDs

All IDE diagnostics use `IDE0xxx` format, defined as constants in `src/Analyzers/Core/Analyzers/IDEDiagnosticIds.cs`. Always reference these constants rather than hardcoding string IDs.

## Analyzer Patterns

### Code Style Analyzer (preferred for IDE diagnostics)

Inherit from `AbstractBuiltInCodeStyleDiagnosticAnalyzer` — not raw `DiagnosticAnalyzer`:

```csharp
[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class CSharpUseXxxDiagnosticAnalyzer : AbstractBuiltInCodeStyleDiagnosticAnalyzer
{
    public CSharpUseXxxDiagnosticAnalyzer()
        : base(IDEDiagnosticIds.UseXxxDiagnosticId,
               EnforceOnBuildValues.UseXxx,
               option: CSharpCodeStyleOptions.PreferXxx,
               title: new LocalizableResourceString(
                   nameof(AnalyzersResources.Use_xxx),
                   AnalyzersResources.ResourceManager,
                   typeof(AnalyzersResources))) { }

    protected override void InitializeWorker(AnalysisContext context)
        => context.RegisterCompilationStartAction(context =>
            context.RegisterOperationAction(AnalyzeOperation, OperationKind.Invocation));
}
```

### Non-Style Analyzer

Use raw `DiagnosticAnalyzer` with a `DiagnosticDescriptor`:

```csharp
[DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
public sealed class MyAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor s_rule = new(
        "IDE0xxx", "Title", "Message format", "Category",
        DiagnosticSeverity.Warning, isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => ImmutableArray.Create(s_rule);

    public override void Initialize(AnalysisContext context)
    {
        context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.ClassDeclaration);
    }
}
```

## CodeFixProvider Pattern

```csharp
[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.UseXxx), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class CSharpUseXxxCodeFixProvider() : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds
        => [IDEDiagnosticIds.UseXxxDiagnosticId];

    // Always provide FixAllProvider — typically BatchFixer
    public override FixAllProvider? GetFixAllProvider()
        => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var diagnostic = context.Diagnostics.First();
        context.RegisterCodeFix(
            CodeAction.Create(
                title,
                c => FixAsync(context.Document, diagnostic, c),
                equivalenceKey: nameof(AnalyzersResources.Use_xxx)),
            diagnostic);
    }
}
```

## CodeRefactoringProvider Pattern

```csharp
[ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.MyRefactoring), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class MyRefactoringProvider() : CodeRefactoringProvider
{
    public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
    {
        // Register refactoring actions via context.RegisterRefactoring(...)
    }
}
```

## EditorConfig / Options Integration

IDE analyzers read user preferences from `.editorconfig` via the options system:
- Options are defined in `CSharpCodeStyleOptions`, `CodeStyleOptions2`, etc.
- Access in analyzers: `context.GetCSharpAnalyzerOptions().PreferXxx`
- Analyzers should respect user-configured severity and enablement

## Resource & Localization

- Error messages and UI strings live in `.resx` files (e.g., `AnalyzersResources.resx`, `FeaturesResources.resx`)
- Reference via generated designer class: `AnalyzersResources.Use_xxx`
- For localizable strings in descriptors: `new LocalizableResourceString(nameof(AnalyzersResources.Use_xxx), AnalyzersResources.ResourceManager, typeof(AnalyzersResources))`
- After modifying `.resx` files, run `dotnet msbuild <path to csproj> /t:UpdateXlf` to update `.xlf` localization files

## Testing

### Test Base Class

For analyzer + fixer pairs, inherit from `AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest_NoEditor`:

```csharp
public sealed class UseXxxTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest_NoEditor
{
    internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
        => (new CSharpUseXxxDiagnosticAnalyzer(), new CSharpUseXxxCodeFixProvider());

    [Fact]
    public async Task TestBasicCase()
    {
        await TestInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    [|var x = 1;|]
                }
            }
            """,
            """
            class C
            {
                void M()
                {
                    int x = 1;
                }
            }
            """);
    }

    [Fact]
    public async Task TestNoFixWhenAlreadyCorrect()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    int x = 1;
                }
            }
            """);
    }
}
```

### Test Markup Syntax
- `[|...|]` — diagnostic span (the code the diagnostic highlights)
- `{|DiagnosticId:...|}` — named span with specific diagnostic ID
- `$$` — cursor position marker (single instance only)

### Verifier-Based Tests (for standalone analyzers)

```csharp
await new VerifyCS.Test
{
    TestCode = source,
    FixedCode = fixedSource,
    ExpectedDiagnostics = { VerifyCS.Diagnostic("IDE0xxx").WithSpan(5, 9, 5, 20) },
}.RunAsync();
```

### Test Conventions
- Use `TestInRegularAndScriptAsync` to cover both regular and script contexts
- Use `TestMissingInRegularAndScriptAsync` to verify no fix is offered
- Prefer raw string literals (`"""..."""`) over verbatim strings (`@"..."`) for test source code
- Add `[WorkItem("https://github.com/dotnet/roslyn/issues/NNN")]` for tests fixing specific issues
- Keep tests focused — avoid unnecessary intermediary assertions

## Checklist for New Analyzer + Code Fix

1. Add diagnostic ID constant to `IDEDiagnosticIds.cs`
2. Add resource strings to the appropriate `.resx` file
3. Run `dotnet msbuild <path to csproj> /t:UpdateXlf` for localization
4. Implement analyzer (inherit `AbstractBuiltInCodeStyleDiagnosticAnalyzer` for code style)
5. Implement code fix with `FixAllProvider`
6. Write tests inheriting `AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest_NoEditor`
7. Test both "fix applies" and "fix does not apply" cases
