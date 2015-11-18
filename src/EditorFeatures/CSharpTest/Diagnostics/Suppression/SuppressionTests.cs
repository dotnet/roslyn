// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeFixes.Suppression;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CodeFixes.Suppression;
using Microsoft.CodeAnalysis.CSharp.Diagnostics.SimplifyTypeNames;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Diagnostics.CSharp;
using Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.ErrorLogger;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.Suppression
{
    public abstract partial class CSharpSuppressionTests : AbstractSuppressionDiagnosticTest
    {
        protected override ParseOptions GetScriptOptions()
        {
            return Options.Script;
        }

        protected override Task<TestWorkspace> CreateWorkspaceFromFileAsync(string definition, ParseOptions parseOptions, CompilationOptions compilationOptions)
        {
            return CSharpWorkspaceFactory.CreateWorkspaceFromFileAsync(definition, (CSharpParseOptions)parseOptions, (CSharpCompilationOptions)compilationOptions);
        }

        protected override string GetLanguage()
        {
            return LanguageNames.CSharp;
        }

        #region "Pragma disable tests"

        public abstract partial class CSharpPragmaWarningDisableSuppressionTests : CSharpSuppressionTests
        {
            protected sealed override int CodeActionIndex
            {
                get { return 0; }
            }

            public class CompilerDiagnosticSuppressionTests : CSharpPragmaWarningDisableSuppressionTests
            {
                internal override Tuple<DiagnosticAnalyzer, ISuppressionFixProvider> CreateDiagnosticProviderAndFixer(Workspace workspace)
                {
                    return Tuple.Create<DiagnosticAnalyzer, ISuppressionFixProvider>(null, new CSharpSuppressionCodeFixProvider());
                }

                [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
                public async Task TestPragmaWarningDirective()
                {
                    await TestAsync(
        @"
class Class
{
    void Method()
    {
        [|int x = 0;|]
    }
}",
        $@"
class Class
{{
    void Method()
    {{
#pragma warning disable CS0219 // {CSharpResources.WRN_UnreferencedVarAssg_Title}
        int x = 0;
#pragma warning restore CS0219 // {CSharpResources.WRN_UnreferencedVarAssg_Title}
    }}
}}");
                }

                [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
                public async Task TestMultilineStatementPragmaWarningDirective()
                {
                    await TestAsync(
        @"
class Class
{
    void Method()
    {
        [|int x = 0
              + 1;|]
    }
}",
        $@"
class Class
{{
    void Method()
    {{
#pragma warning disable CS0219 // {CSharpResources.WRN_UnreferencedVarAssg_Title}
        int x = 0
#pragma warning restore CS0219 // {CSharpResources.WRN_UnreferencedVarAssg_Title}
              + 1;
    }}
}}");
                }

                [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
                public async Task TestPragmaWarningDirectiveWithExistingTrivia()
                {
                    await TestAsync(
        @"
class Class
{
    void Method()
    {
        // Start comment previous line
        /* Start comment same line */ [|int x = 0;|] // End comment same line
        /* End comment next line */
    }
}",
        $@"
class Class
{{
    void Method()
    {{
        // Start comment previous line
#pragma warning disable CS0219 // {CSharpResources.WRN_UnreferencedVarAssg_Title}
                              /* Start comment same line */
        int x = 0; // End comment same line
#pragma warning restore CS0219 // {CSharpResources.WRN_UnreferencedVarAssg_Title}
                              /* End comment next line */
    }}
}}");
                }

                [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
                public async Task TestMultipleInstancesOfPragmaWarningDirective()
                {
                    await TestAsync(
        @"
class Class
{
    void Method()
    {
        [|int x = 0, y = 0;|]
    }
}",
        $@"
class Class
{{
    void Method()
    {{
#pragma warning disable CS0219 // {CSharpResources.WRN_UnreferencedVarAssg_Title}
        int x = 0, y = 0;
#pragma warning restore CS0219 // {CSharpResources.WRN_UnreferencedVarAssg_Title}
    }}
}}");
                }

                [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
                [WorkItem(3311, "https://github.com/dotnet/roslyn/issues/3311")]
                public async Task TestNoDuplicateSuppressionCodeFixes()
                {
                    var source = @"
class Class
{
    void Method()
    {
        [|int x = 0, y = 0; string s;|]
    }
}";
                    using (var workspace = await CreateWorkspaceFromFileAsync(source, parseOptions: null, compilationOptions: null))
                    {
                        var diagnosticService = new TestDiagnosticAnalyzerService(LanguageNames.CSharp, new CSharpCompilerDiagnosticAnalyzer());
                        var incrementalAnalyzer = diagnosticService.CreateIncrementalAnalyzer(workspace);
                        var suppressionProvider = CreateDiagnosticProviderAndFixer(workspace).Item2;
                        var suppressionProviderFactory = new Lazy<ISuppressionFixProvider, CodeChangeProviderMetadata>(() => suppressionProvider,
                            new CodeChangeProviderMetadata("SuppressionProvider", languages: new[] { LanguageNames.CSharp }));
                        var fixService = new CodeFixService(diagnosticService,
                            SpecializedCollections.EmptyEnumerable<Lazy<IErrorLoggerService>>(),
                            SpecializedCollections.EmptyEnumerable<Lazy<CodeFixProvider, CodeChangeProviderMetadata>>(),
                            SpecializedCollections.SingletonEnumerable(suppressionProviderFactory));

                        TextSpan span;
                        var document = GetDocumentAndSelectSpan(workspace, out span);
                        var diagnostics = diagnosticService.GetDiagnosticsForSpanAsync(document, span)
                            .WaitAndGetResult(CancellationToken.None);
                        Assert.Equal(2, diagnostics.Where(d => d.Id == "CS0219").Count());

                        var allFixes = fixService.GetFixesAsync(document, span, includeSuppressionFixes: true, cancellationToken: CancellationToken.None)
                            .WaitAndGetResult(CancellationToken.None)
                            .SelectMany(fixCollection => fixCollection.Fixes);

                        var cs0219Fixes = allFixes.Where(fix => fix.PrimaryDiagnostic.Id == "CS0219");

                        // Ensure that both the fixes have identical equivalence key, and hence get de-duplicated in LB menu.
                        Assert.Equal(2, cs0219Fixes.Count());
                        var cs0219EquivalenceKey = cs0219Fixes.First().Action.EquivalenceKey;
                        Assert.NotNull(cs0219EquivalenceKey);
                        Assert.Equal(cs0219EquivalenceKey, cs0219Fixes.Last().Action.EquivalenceKey);

                        // Ensure that there *is* a fix for the other warning and that it has a *different*
                        // equivalence key so that it *doesn't* get de-duplicated
                        Assert.Equal(1, diagnostics.Where(d => d.Id == "CS0168").Count());
                        var cs0168Fixes = allFixes.Where(fix => fix.PrimaryDiagnostic.Id == "CS0168");
                        var cs0168EquivalenceKey = cs0168Fixes.Single().Action.EquivalenceKey;
                        Assert.NotNull(cs0168EquivalenceKey);
                        Assert.NotEqual(cs0219EquivalenceKey, cs0168EquivalenceKey);
                    }
                }

                [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
                public async Task TestErrorAndWarningScenario()
                {
                    await TestAsync(
        @"
class Class
{
    void Method()
    {
        return 0;
        [|int x = ""0"";|]
    }
}",
        $@"
class Class
{{
    void Method()
    {{
        return 0;
#pragma warning disable CS0162 // {CSharpResources.WRN_UnreachableCode_Title}
        int x = ""0"";
#pragma warning restore CS0162 // {CSharpResources.WRN_UnreachableCode_Title}
    }}
}}");
                }

                [WorkItem(956453)]
                [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
                public async Task TestWholeFilePragmaWarningDirective()
                {
                    await TestAsync(
        @"class Class { void Method() { [|int x = 0;|] } }",
        $@"#pragma warning disable CS0219 // {CSharpResources.WRN_UnreferencedVarAssg_Title}
class Class {{ void Method() {{ int x = 0; }} }}
#pragma warning restore CS0219 // {CSharpResources.WRN_UnreferencedVarAssg_Title}");
                }

                [WorkItem(970129)]
                [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
                public async Task TestSuppressionAroundSingleToken()
                {
                    await TestAsync(
        @"
using System;
[Obsolete]
class Session { }
class Program
{
    static void Main()
    {
      [|Session|]
    }
}",
        $@"
using System;
[Obsolete]
class Session {{ }}
class Program
{{
    static void Main()
    {{
#pragma warning disable CS0612 // {CSharpResources.WRN_DeprecatedSymbol_Title}
        Session
#pragma warning restore CS0612 // {CSharpResources.WRN_DeprecatedSymbol_Title}
    }}
}}");
                }

                [WorkItem(1066576)]
                [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
                public async Task TestPragmaWarningDirectiveAroundTrivia1()
                {
                    await TestAsync(
        @"
class Class
{
    void Method()
    {

// Comment
// Comment
[|#pragma abcde|]

    }    // Comment   



}",
        $@"
class Class
{{
    void Method()
    {{

        // Comment
        // Comment
#pragma warning disable CS1633 // {CSharpResources.WRN_IllegalPragma_Title}
#pragma abcde

    }}    // Comment   
#pragma warning restore CS1633 // {CSharpResources.WRN_IllegalPragma_Title}



}}");
                }

                [WorkItem(1066576)]
                [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
                public async Task TestPragmaWarningDirectiveAroundTrivia2()
                {
                    await TestAsync(
        @"[|#pragma abcde|]",
        $@"#pragma warning disable CS1633 // {CSharpResources.WRN_IllegalPragma_Title}
#pragma abcde
#pragma warning restore CS1633 // {CSharpResources.WRN_IllegalPragma_Title}");
                }

                [WorkItem(1066576)]
                [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
                public async Task TestPragmaWarningDirectiveAroundTrivia3()
                {
                    await TestAsync(
        @"[|#pragma abcde|]  ",
        $@"#pragma warning disable CS1633 // {CSharpResources.WRN_IllegalPragma_Title}
#pragma abcde  
#pragma warning restore CS1633 // {CSharpResources.WRN_IllegalPragma_Title}");
                }

                [WorkItem(1066576)]
                [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
                public async Task TestPragmaWarningDirectiveAroundTrivia4()
                {
                    await TestAsync(
        @"

[|#pragma abc|]
class C { }

",
        $@"

#pragma warning disable CS1633 // {CSharpResources.WRN_IllegalPragma_Title}
#pragma abc
class C {{ }}
#pragma warning restore CS1633 // {CSharpResources.WRN_IllegalPragma_Title}

");
                }

                [WorkItem(1066576)]
                [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
                public async Task TestPragmaWarningDirectiveAroundTrivia5()
                {
                    await TestAsync(
        @"class C1 { }
[|#pragma abc|]
class C2 { }
class C3 { }",
        $@"class C1 {{ }}
#pragma warning disable CS1633 // {CSharpResources.WRN_IllegalPragma_Title}
#pragma abc
class C2 {{ }}
#pragma warning restore CS1633 // {CSharpResources.WRN_IllegalPragma_Title}
class C3 {{ }}");
                }

                [WorkItem(1066576)]
                [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
                public async Task TestPragmaWarningDirectiveAroundTrivia6()
                {
                    await TestAsync(
        @"class C1 { }
class C2 { } /// <summary><see [|cref=""abc""|]/></summary>
class C3 { } // comment
  // comment
// comment",
        @"class C1 { }
class C2 { }
#pragma warning disable CS1574
/// <summary><see cref=""abc""/></summary>
class C3 { } // comment
#pragma warning enable CS1574
// comment
// comment", CSharpParseOptions.Default.WithDocumentationMode(DocumentationMode.Diagnose));
                }
            }

            public class UserHiddenDiagnosticSuppressionTests : CSharpPragmaWarningDisableSuppressionTests
            {
                internal override Tuple<DiagnosticAnalyzer, ISuppressionFixProvider> CreateDiagnosticProviderAndFixer(Workspace workspace)
                {
                    return new Tuple<DiagnosticAnalyzer, ISuppressionFixProvider>(
                        new CSharpSimplifyTypeNamesDiagnosticAnalyzer(), new CSharpSuppressionCodeFixProvider());
                }

                [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
                public async Task TestHiddenDiagnosticCannotBeSuppressed()
                {
                    await TestMissingAsync(
        @"
using System;

class Class
{
int Method()
{
    [|System.Int32 x = 0;|]
    return x;
}
}");
                }
            }

            public partial class UserInfoDiagnosticSuppressionTests : CSharpPragmaWarningDisableSuppressionTests
            {
                private class UserDiagnosticAnalyzer : DiagnosticAnalyzer
                {
                    public static readonly DiagnosticDescriptor Decsciptor =
                        new DiagnosticDescriptor("InfoDiagnostic", "InfoDiagnostic Title", "InfoDiagnostic", "InfoDiagnostic", DiagnosticSeverity.Info, isEnabledByDefault: true);

                    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
                    {
                        get
                        {
                            return ImmutableArray.Create(Decsciptor);
                        }
                    }

                    public override void Initialize(AnalysisContext context)
                    {
                        context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.ClassDeclaration);
                    }

                    public void AnalyzeNode(SyntaxNodeAnalysisContext context)
                    {
                        var classDecl = (ClassDeclarationSyntax)context.Node;
                        context.ReportDiagnostic(Diagnostic.Create(Decsciptor, classDecl.Identifier.GetLocation()));
                    }
                }

                internal override Tuple<DiagnosticAnalyzer, ISuppressionFixProvider> CreateDiagnosticProviderAndFixer(Workspace workspace)
                {
                    return new Tuple<DiagnosticAnalyzer, ISuppressionFixProvider>(
                        new UserDiagnosticAnalyzer(), new CSharpSuppressionCodeFixProvider());
                }

                [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
                public async Task TestInfoDiagnosticSuppressed()
                {
                    await TestAsync(
            @"
using System;

[|class Class|]
{
    int Method()
    {
        int x = 0;
    }
}",
            @"
using System;

#pragma warning disable InfoDiagnostic // InfoDiagnostic Title
class Class
#pragma warning restore InfoDiagnostic // InfoDiagnostic Title
{
    int Method()
    {
        int x = 0;
    }
}");
                }
            }

            public class UserErrorDiagnosticSuppressionTests : CSharpPragmaWarningDisableSuppressionTests
            {
                private class UserDiagnosticAnalyzer : DiagnosticAnalyzer
                {
                    private DiagnosticDescriptor _descriptor =
                        new DiagnosticDescriptor("ErrorDiagnostic", "ErrorDiagnostic", "ErrorDiagnostic", "ErrorDiagnostic", DiagnosticSeverity.Error, isEnabledByDefault: true);

                    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
                    {
                        get
                        {
                            return ImmutableArray.Create(_descriptor);
                        }
                    }

                    public override void Initialize(AnalysisContext context)
                    {
                        context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.ClassDeclaration);
                    }

                    public void AnalyzeNode(SyntaxNodeAnalysisContext context)
                    {
                        var classDecl = (ClassDeclarationSyntax)context.Node;
                        context.ReportDiagnostic(Diagnostic.Create(_descriptor, classDecl.Identifier.GetLocation()));
                    }
                }

                internal override Tuple<DiagnosticAnalyzer, ISuppressionFixProvider> CreateDiagnosticProviderAndFixer(Workspace workspace)
                {
                    return new Tuple<DiagnosticAnalyzer, ISuppressionFixProvider>(
                        new UserDiagnosticAnalyzer(), new CSharpSuppressionCodeFixProvider());
                }

                [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
                public async Task TestErrorDiagnosticCanBeSuppressed()
                {
                    await TestAsync(
            @"
using System;

[|class Class|]
{
    int Method()
    {
        int x = 0;
    }
}",
            @"
using System;

#pragma warning disable ErrorDiagnostic // ErrorDiagnostic
class Class
#pragma warning restore ErrorDiagnostic // ErrorDiagnostic
{
    int Method()
    {
        int x = 0;
    }
}");
                }
            }

            public class DiagnosticWithBadIdSuppressionTests : CSharpPragmaWarningDisableSuppressionTests
            {
                // Analyzer driver generates a no-location analyzer exception diagnostic, which we don't intend to test here.
                protected override bool IncludeNoLocationDiagnostics => false;

                private class UserDiagnosticAnalyzer : DiagnosticAnalyzer
                {
                    private DiagnosticDescriptor _descriptor =
                        new DiagnosticDescriptor("@~DiagnosticWithBadId", "DiagnosticWithBadId", "DiagnosticWithBadId", "DiagnosticWithBadId", DiagnosticSeverity.Info, isEnabledByDefault: true);

                    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
                    {
                        get
                        {
                            return ImmutableArray.Create(_descriptor);
                        }
                    }

                    public override void Initialize(AnalysisContext context)
                    {
                        context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.ClassDeclaration);
                    }

                    public void AnalyzeNode(SyntaxNodeAnalysisContext context)
                    {
                        var classDecl = (ClassDeclarationSyntax)context.Node;
                        context.ReportDiagnostic(Diagnostic.Create(_descriptor, classDecl.Identifier.GetLocation()));
                    }
                }

                internal override Tuple<DiagnosticAnalyzer, ISuppressionFixProvider> CreateDiagnosticProviderAndFixer(Workspace workspace)
                {
                    return new Tuple<DiagnosticAnalyzer, ISuppressionFixProvider>(
                        new UserDiagnosticAnalyzer(), new CSharpSuppressionCodeFixProvider());
                }

                [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
                public async Task TestDiagnosticWithBadIdSuppressed()
                {
                    // Diagnostics with bad/invalid ID are not reported.
                    await TestMissingAsync(
            @"
using System;

[|class Class|]
{
    int Method()
    {
        int x = 0;
    }
}");
                }
            }
        }

        public partial class MultilineDiagnosticSuppressionTests : CSharpPragmaWarningDisableSuppressionTests
        {
            private class UserDiagnosticAnalyzer : DiagnosticAnalyzer
            {
                public static readonly DiagnosticDescriptor Decsciptor =
                    new DiagnosticDescriptor("InfoDiagnostic", "InfoDiagnostic Title", "InfoDiagnostic", "InfoDiagnostic", DiagnosticSeverity.Info, isEnabledByDefault: true);

                public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
                {
                    get
                    {
                        return ImmutableArray.Create(Decsciptor);
                    }
                }

                public override void Initialize(AnalysisContext context)
                {
                    context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.ClassDeclaration);
                }

                public void AnalyzeNode(SyntaxNodeAnalysisContext context)
                {
                    var classDecl = (ClassDeclarationSyntax)context.Node;
                    context.ReportDiagnostic(Diagnostic.Create(Decsciptor, classDecl.GetLocation()));
                }
            }

            internal override Tuple<DiagnosticAnalyzer, ISuppressionFixProvider> CreateDiagnosticProviderAndFixer(Workspace workspace)
            {
                return new Tuple<DiagnosticAnalyzer, ISuppressionFixProvider>(
                    new UserDiagnosticAnalyzer(), new CSharpSuppressionCodeFixProvider());
            }

            [WorkItem(2764, "https://github.com/dotnet/roslyn/issues/2764")]
            [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
            public async Task TestPragmaWarningDirectiveAroundMultilineDiagnostic()
            {
                await TestAsync(
    @"
[|class Class
{
}|]
",
    $@"
#pragma warning disable {UserDiagnosticAnalyzer.Decsciptor.Id} // {UserDiagnosticAnalyzer.Decsciptor.Title}
class Class
{{
}}
#pragma warning restore {UserDiagnosticAnalyzer.Decsciptor.Id} // {UserDiagnosticAnalyzer.Decsciptor.Title}
");
            }
        }
        #endregion

        #region "SuppressMessageAttribute tests"

        public abstract partial class CSharpGlobalSuppressMessageSuppressionTests : CSharpSuppressionTests
        {
            protected sealed override int CodeActionIndex
            {
                get { return 1; }
            }

            public class CompilerDiagnosticSuppressionTests : CSharpGlobalSuppressMessageSuppressionTests
            {
                internal override Tuple<DiagnosticAnalyzer, ISuppressionFixProvider> CreateDiagnosticProviderAndFixer(Workspace workspace)
                {
                    return Tuple.Create<DiagnosticAnalyzer, ISuppressionFixProvider>(null, new CSharpSuppressionCodeFixProvider());
                }

                [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
                public async Task TestCompilerDiagnosticsCannotBeSuppressed()
                {
                    // Another test verifies we have a pragma warning action for this source, this verifies there are no other suppression actions.
                    await TestActionCountAsync(
        @"
class Class
{
    void Method()
    {
        [|int x = 0;|]
    }
}", 1);
                }
            }

            public class UserHiddenDiagnosticSuppressionTests : CSharpGlobalSuppressMessageSuppressionTests
            {
                internal override Tuple<DiagnosticAnalyzer, ISuppressionFixProvider> CreateDiagnosticProviderAndFixer(Workspace workspace)
                {
                    return new Tuple<DiagnosticAnalyzer, ISuppressionFixProvider>(
                        new CSharpSimplifyTypeNamesDiagnosticAnalyzer(), new CSharpSuppressionCodeFixProvider());
                }

                [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
                public async Task TestHiddenDiagnosticsCannotBeSuppressed()
                {
                    await TestMissingAsync(
        @"
using System;
class Class
{
    void Method()
    {
        [|System.Int32 x = 0;|]
    }
}");
                }
            }

            public partial class UserInfoDiagnosticSuppressionTests : CSharpGlobalSuppressMessageSuppressionTests
            {
                private class UserDiagnosticAnalyzer : DiagnosticAnalyzer
                {
                    public static readonly DiagnosticDescriptor Descriptor =
                        new DiagnosticDescriptor("InfoDiagnostic", "InfoDiagnostic", "InfoDiagnostic", "InfoDiagnostic", DiagnosticSeverity.Info, isEnabledByDefault: true);

                    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
                    {
                        get
                        {
                            return ImmutableArray.Create(Descriptor);
                        }
                    }

                    public override void Initialize(AnalysisContext context)
                    {
                        context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.ClassDeclaration, SyntaxKind.EnumDeclaration, SyntaxKind.NamespaceDeclaration, SyntaxKind.MethodDeclaration, SyntaxKind.PropertyDeclaration, SyntaxKind.FieldDeclaration, SyntaxKind.EventDeclaration);
                    }

                    public void AnalyzeNode(SyntaxNodeAnalysisContext context)
                    {
                        switch (context.Node.Kind())
                        {
                            case SyntaxKind.ClassDeclaration:
                                var classDecl = (ClassDeclarationSyntax)context.Node;
                                context.ReportDiagnostic(Diagnostic.Create(Descriptor, classDecl.Identifier.GetLocation()));
                                break;

                            case SyntaxKind.NamespaceDeclaration:
                                var ns = (NamespaceDeclarationSyntax)context.Node;
                                context.ReportDiagnostic(Diagnostic.Create(Descriptor, ns.Name.GetLocation()));
                                break;

                            case SyntaxKind.MethodDeclaration:
                                var method = (MethodDeclarationSyntax)context.Node;
                                context.ReportDiagnostic(Diagnostic.Create(Descriptor, method.Identifier.GetLocation()));
                                break;

                            case SyntaxKind.PropertyDeclaration:
                                var property = (PropertyDeclarationSyntax)context.Node;
                                context.ReportDiagnostic(Diagnostic.Create(Descriptor, property.Identifier.GetLocation()));
                                break;

                            case SyntaxKind.FieldDeclaration:
                                var field = (FieldDeclarationSyntax)context.Node;
                                context.ReportDiagnostic(Diagnostic.Create(Descriptor, field.Declaration.Variables.First().Identifier.GetLocation()));
                                break;

                            case SyntaxKind.EventDeclaration:
                                var e = (EventDeclarationSyntax)context.Node;
                                context.ReportDiagnostic(Diagnostic.Create(Descriptor, e.Identifier.GetLocation()));
                                break;

                            case SyntaxKind.EnumDeclaration:
                                // Report diagnostic on each descendant comment trivia
                                foreach (var trivia in context.Node.DescendantTrivia().Where(t => t.Kind() == SyntaxKind.SingleLineCommentTrivia || t.Kind() == SyntaxKind.MultiLineCommentTrivia))
                                {
                                    context.ReportDiagnostic(Diagnostic.Create(Descriptor, trivia.GetLocation()));
                                }
                                break;
                        }

                                                   
                    }
                }

                internal override Tuple<DiagnosticAnalyzer, ISuppressionFixProvider> CreateDiagnosticProviderAndFixer(Workspace workspace)
                {
                    return new Tuple<DiagnosticAnalyzer, ISuppressionFixProvider>(
                        new UserDiagnosticAnalyzer(), new CSharpSuppressionCodeFixProvider());
                }

                [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
                public async Task TestSuppressionOnSimpleType()
                {
                    await TestAsync(
            @"
using System;

[|class Class|]
{
    int Method()
    {
        int x = 0;
    }
}",
            $@"
// This file is used by Code Analysis to maintain SuppressMessage 
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given 
// a specific target and scoped to a namespace, type, member, etc.

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""{FeaturesResources.SuppressionPendingJustification}"", Scope = ""type"", Target = ""~T:Class"")]

");

                    // Also verify that the added attribute does indeed suppress the diagnostic.
                    await TestMissingAsync(
            @"
using System;

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""<Pending>"", Scope = ""type"", Target = ""~T:Class"")]

[|class Class|]
{
    int Method()
    {
        int x = 0;
    }
}");
                }

                [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
                public async Task TestSuppressionOnNamespace()
                {
                    await TestAsync(
            @"
using System;

[|namespace N|]
{
    class Class
    {
        int Method()
        {
            int x = 0;
        }
    }
}",
            $@"
// This file is used by Code Analysis to maintain SuppressMessage 
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given 
// a specific target and scoped to a namespace, type, member, etc.

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""{FeaturesResources.SuppressionPendingJustification}"", Scope = ""namespace"", Target = ""~N:N"")]

", index: 1);

                    // Also verify that the added attribute does indeed suppress the diagnostic.
                    await TestMissingAsync(
            @"
using System;

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""<Pending>"", Scope = ""namespace"", Target = ""~N:N"")]

[|namespace N|]
{
    class Class
    {
        int Method()
        {
            int x = 0;
        }
    }
}");
                }

                [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
                public async Task TestSuppressionOnTypeInsideNamespace()
                {
                    await TestAsync(
            @"
using System;

namespace N1
{
    namespace N2
    {
        [|class Class|]
        {
            int Method()
            {
                int x = 0;
            }
        }
    }
}",
            $@"
// This file is used by Code Analysis to maintain SuppressMessage 
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given 
// a specific target and scoped to a namespace, type, member, etc.

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""{FeaturesResources.SuppressionPendingJustification}"", Scope = ""type"", Target = ""~T:N1.N2.Class"")]

");

                    // Also verify that the added attribute does indeed suppress the diagnostic.
                    await TestMissingAsync(
            @"
using System;

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""<Pending>"", Scope = ""type"", Target = ""~T:N1.N2.Class"")]

namespace N1
{
    namespace N2
    {
        [|class Class|]
        {
            int Method()
            {
                int x = 0;
            }
        }
    }
}");
                }

                [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
                public async Task TestSuppressionOnNestedType()
                {
                    await TestAsync(
            @"
using System;

namespace N
{
    class Generic<T>
    {
        [|class Class|]
        {
            int Method()
            {
                int x = 0;
            }
        }
    }
}",
            $@"
// This file is used by Code Analysis to maintain SuppressMessage 
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given 
// a specific target and scoped to a namespace, type, member, etc.

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""{FeaturesResources.SuppressionPendingJustification}"", Scope = ""type"", Target = ""~T:N.Generic`1.Class"")]

");

                    // Also verify that the added attribute does indeed suppress the diagnostic.
                    await TestMissingAsync(
            @"
using System;

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""<Pending>"", Scope = ""type"", Target = ""~T:N.Generic`1.Class"")]

namespace N
{
    class Generic<T>
    {
        [|class Class|]
        {
            int Method()
            {
                int x = 0;
            }
        }
    }
}");
                }

                [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
                public async Task TestSuppressionOnMethod()
                {
                    await TestAsync(
            @"
using System;

namespace N
{
    class Generic<T>
    {
        class Class
        {
            [|int Method()
            {
                int x = 0;
            }|]
        }
    }
}",
            $@"
// This file is used by Code Analysis to maintain SuppressMessage 
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given 
// a specific target and scoped to a namespace, type, member, etc.

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""{FeaturesResources.SuppressionPendingJustification}"", Scope = ""member"", Target = ""~M:N.Generic`1.Class.Method~System.Int32"")]

");

                    // Also verify that the added attribute does indeed suppress the diagnostic.
                    await TestMissingAsync(
            @"
using System;

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""<Pending>"", Scope = ""member"", Target = ""~M:N.Generic`1.Class.Method~System.Int32"")]

namespace N
{
    class Generic<T>
    {
        class Class
        {
            [|int Method()|]
            {
                int x = 0;
            }
        }
    }
}");
                }

                [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
                public async Task TestSuppressionOnOverloadedMethod()
                {
                    await TestAsync(
            @"
using System;

namespace N
{
    class Generic<T>
    {
        class Class
        {
            [|int Method(int y, ref char z)
            {
                int x = 0;
            }|]

            int Method()
            {
                int x = 0;
            }
        }
    }
}",
            $@"
// This file is used by Code Analysis to maintain SuppressMessage 
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given 
// a specific target and scoped to a namespace, type, member, etc.

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""{FeaturesResources.SuppressionPendingJustification}"", Scope = ""member"", Target = ""~M:N.Generic`1.Class.Method(System.Int32,System.Char@)~System.Int32"")]

");

                    // Also verify that the added attribute does indeed suppress the diagnostic.
                    await TestMissingAsync(
            @"
using System;

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""<Pending>"", Scope = ""member"", Target = ""~M:N.Generic`1.Class.Method(System.Int32,System.Char@)~System.Int32"")]

namespace N
{
    class Generic<T>
    {
        class Class
        {
            [|int Method(int y, ref char z)|]
            {
                int x = 0;
            }

            int Method()
            {
                int x = 0;
            }
        }
    }
}");

                    await TestAsync(
        @"
using System;

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""<Pending>"", Scope = ""member"", Target = ""~M:N.Generic`1.Class.Method(System.Int32,System.Char@)~System.Int32"")]

namespace N
{
    class Generic<T>
    {
        class Class
        {
            [|int Method(int y, ref char z)
            {
                int x = 0;
            }

            int Method()
            {
                int x = 0;
            }|]
        }
    }
}",
            $@"
// This file is used by Code Analysis to maintain SuppressMessage 
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given 
// a specific target and scoped to a namespace, type, member, etc.

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""{FeaturesResources.SuppressionPendingJustification}"", Scope = ""member"", Target = ""~M:N.Generic`1.Class.Method~System.Int32"")]

");
                }

                [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
                public async Task TestSuppressionOnGenericMethod()
                {
                    await TestAsync(
            @"
using System;

namespace N
{
    class Generic<T>
    {
        class Class
        {
            [|int Method<U>(U u)
            {
                int x = 0;
            }|]
        }
    }
}",
            $@"
// This file is used by Code Analysis to maintain SuppressMessage 
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given 
// a specific target and scoped to a namespace, type, member, etc.

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""{FeaturesResources.SuppressionPendingJustification}"", Scope = ""member"", Target = ""~M:N.Generic`1.Class.Method``1(``0)~System.Int32"")]

");

                    // Also verify that the added attribute does indeed suppress the diagnostic.
                    await TestMissingAsync(
            @"
using System;

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""<Pending>"", Scope = ""member"", Target = ""~M:N.Generic`1.Class.Method``1(``0)~System.Int32"")]

namespace N
{
    class Generic<T>
    {
        class Class
        {
            [|int Method<U>(U u)|]
            {
                int x = 0;
            }
        }
    }
}");
                }

                [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
                public async Task TestSuppressionOnProperty()
                {
                    await TestAsync(
            @"
using System;

namespace N
{
    class Generic
    {
        class Class
        {
            [|int Property|]
            {
                get { int x = 0; }
            }
        }
    }
}",
            $@"
// This file is used by Code Analysis to maintain SuppressMessage 
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given 
// a specific target and scoped to a namespace, type, member, etc.

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""{FeaturesResources.SuppressionPendingJustification}"", Scope = ""member"", Target = ""~P:N.Generic.Class.Property"")]

");

                    // Also verify that the added attribute does indeed suppress the diagnostic.
                    await TestMissingAsync(
            @"
using System;

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""<Pending>"", Scope = ""member"", Target = ""~P:N.Generic.Class.Property"")]

namespace N
{
    class Generic
    {
        class Class
        {
            [|int Property|]
            {
                get { int x = 0; }
            }
        }
    }
}");
                }

                [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
                public async Task TestSuppressionOnField()
                {
                    await TestAsync(
            @"
using System;

class Class
{
    [|int field = 0;|]
}",
            $@"
// This file is used by Code Analysis to maintain SuppressMessage 
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given 
// a specific target and scoped to a namespace, type, member, etc.

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""{FeaturesResources.SuppressionPendingJustification}"", Scope = ""member"", Target = ""~F:Class.field"")]

");

                    // Also verify that the added attribute does indeed suppress the diagnostic.
                    await TestMissingAsync(
            @"
using System;

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""<Pending>"", Scope = ""member"", Target = ""~F:Class.field"")]

class Class
{
    [|int field = 0;|]
}");
                }

                [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
                [WorkItem(6379, "https://github.com/dotnet/roslyn/issues/6379")]
                public async Task TestSuppressionOnTriviaBetweenFields()
                {
                    await TestAsync(
            @"
using System;

// suppressions on field are not relevant.
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""{FeaturesResources.SuppressionPendingJustification}"", Scope = ""member"", Target = ""~F:E.Field1"")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""{FeaturesResources.SuppressionPendingJustification}"", Scope = ""member"", Target = ""~F:E.Field2"")]

enum E
{
    [|
    Field1, // trailing trivia for comma token which doesn't belong to span of any of the fields
    Field2
    |]
}",
            $@"
// This file is used by Code Analysis to maintain SuppressMessage 
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given 
// a specific target and scoped to a namespace, type, member, etc.

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""{FeaturesResources.SuppressionPendingJustification}"", Scope = ""type"", Target = ""~T:E"")]

");

                    // Also verify that the added attribute does indeed suppress the diagnostic.
                    await TestMissingAsync(
            @"
using System;

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""{FeaturesResources.SuppressionPendingJustification}"", Scope = ""type"", Target = ""~T:E"")]

enum E
{
    [|
    Field1, // trailing trivia for comma token which doesn't belong to span of any of the fields
    Field2
    |]
}");
                }

                [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
                public async Task TestSuppressionOnField2()
                {
                    await TestAsync(
            @"
using System;

class Class
{
    int [|field = 0|], field2 = 1;
}",
            $@"
// This file is used by Code Analysis to maintain SuppressMessage 
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given 
// a specific target and scoped to a namespace, type, member, etc.

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""{FeaturesResources.SuppressionPendingJustification}"", Scope = ""member"", Target = ""~F:Class.field"")]

");

                    // Also verify that the added attribute does indeed suppress the diagnostic.
                    await TestMissingAsync(
            @"
using System;

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""<Pending>"", Scope = ""member"", Target = ""~F:Class.field"")]

class Class
{
    int [|field|] = 0, field2 = 1;
}");
                }

                [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
                public async Task TestSuppressionOnEvent()
                {
                    await TestAsync(
            @"
using System;

public class SampleEventArgs
{
    public SampleEventArgs(string s) { Text = s; }
    public String Text {get; private set;} // readonly
        }

class Class
{
    // Declare the delegate (if using non-generic pattern). 
    public delegate void SampleEventHandler(object sender, SampleEventArgs e);

    // Declare the event. 
    [|public event SampleEventHandler SampleEvent
    {
        add { }
        remove { }
    }|]
}",
            $@"
// This file is used by Code Analysis to maintain SuppressMessage 
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given 
// a specific target and scoped to a namespace, type, member, etc.

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""{FeaturesResources.SuppressionPendingJustification}"", Scope = ""member"", Target = ""~E:Class.SampleEvent"")]

");

                    // Also verify that the added attribute does indeed suppress the diagnostic.
                    await TestMissingAsync(
            @"
using System;

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""<Pending>"", Scope = ""member"", Target = ""~E:Class.SampleEvent"")]

public class SampleEventArgs
{
    public SampleEventArgs(string s) { Text = s; }
    public String Text {get; private set;} // readonly
}

class Class
{
    // Declare the delegate (if using non-generic pattern). 
    public delegate void SampleEventHandler(object sender, SampleEventArgs e);

    // Declare the event. 
    [|public event SampleEventHandler SampleEvent|]
    {
        add { }
        remove { }
    }
}");
                }

                [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
                public async Task TestSuppressionWithExistingGlobalSuppressionsDocument()
                {
                    var initialMarkup = @"<Workspace>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj1"">
        <Document FilePath=""CurrentDocument.cs""><![CDATA[
using System;

class Class { }

[|class Class2|] { }
]]>
        </Document>
        <Document FilePath=""GlobalSuppressions.cs""><![CDATA[
// This file is used by Code Analysis to maintain SuppressMessage 
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given 
// a specific target and scoped to a namespace, type, member, etc.

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""<Pending>"", Scope = ""type"", Target = ""Class"")]
]]>
        </Document>
    </Project>
</Workspace>";
                    var expectedText =
                        $@"
// This file is used by Code Analysis to maintain SuppressMessage 
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given 
// a specific target and scoped to a namespace, type, member, etc.

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""<Pending>"", Scope = ""type"", Target = ""Class"")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""{FeaturesResources.SuppressionPendingJustification}"", Scope = ""type"", Target = ""~T:Class2"")]

";

                    await TestAsync(initialMarkup, expectedText);
                }

                [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
                public async Task TestSuppressionWithExistingGlobalSuppressionsDocument2()
                {
                    // Own custom file named GlobalSuppressions.cs
                    var initialMarkup = @"<Workspace>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj1"">
        <Document FilePath=""CurrentDocument.cs""><![CDATA[
using System;

class Class { }

[|class Class2|] { }
]]>
        </Document>
        <Document FilePath=""GlobalSuppressions.cs""><![CDATA[
// My own file named GlobalSuppressions.cs.
using System;
class Class { }
]]>
        </Document>
    </Project>
</Workspace>";
                    var expectedText =
                        $@"
// This file is used by Code Analysis to maintain SuppressMessage 
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given 
// a specific target and scoped to a namespace, type, member, etc.

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""{FeaturesResources.SuppressionPendingJustification}"", Scope = ""type"", Target = ""~T:Class2"")]

";

                    await TestAsync(initialMarkup, expectedText);
                }

                [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
                public async Task TestSuppressionWithExistingGlobalSuppressionsDocument3()
                {
                    // Own custom file named GlobalSuppressions.cs + existing GlobalSuppressions2.cs with global suppressions
                    var initialMarkup = @"<Workspace>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj1"">
        <Document FilePath=""CurrentDocument.cs""><![CDATA[
using System;

class Class { }

[|class Class2|] { }
]]>
        </Document>
        <Document FilePath=""GlobalSuppressions.cs""><![CDATA[
// My own file named GlobalSuppressions.cs.
using System;
class Class { }
]]>
        </Document>
         <Document FilePath=""GlobalSuppressions2.cs""><![CDATA[
// This file is used by Code Analysis to maintain SuppressMessage 
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given 
// a specific target and scoped to a namespace, type, member, etc.

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""<Pending>"", Scope = ""type"", Target = ""Class"")]
]]>
        </Document>
    </Project>
</Workspace>";
                    var expectedText =
                        $@"
// This file is used by Code Analysis to maintain SuppressMessage 
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given 
// a specific target and scoped to a namespace, type, member, etc.

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""<Pending>"", Scope = ""type"", Target = ""Class"")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""{FeaturesResources.SuppressionPendingJustification}"", Scope = ""type"", Target = ""~T:Class2"")]

";

                    await TestAsync(initialMarkup, expectedText);
                }
            }
        }

        #endregion

        #region NoLocation Diagnostics tests

        public partial class CSharpDiagnosticWithoutLocationSuppressionTests : CSharpSuppressionTests
        {
            private class UserDiagnosticAnalyzer : DiagnosticAnalyzer
            {
                public static readonly DiagnosticDescriptor Descriptor =
                    new DiagnosticDescriptor("NoLocationDiagnostic", "NoLocationDiagnostic", "NoLocationDiagnostic", "NoLocationDiagnostic", DiagnosticSeverity.Info, isEnabledByDefault: true);

                public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
                {
                    get
                    {
                        return ImmutableArray.Create(Descriptor);
                    }
                }

                public override void Initialize(AnalysisContext context)
                {
                    context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.ClassDeclaration);
                }

                public void AnalyzeNode(SyntaxNodeAnalysisContext context)
                {
                    context.ReportDiagnostic(Diagnostic.Create(Descriptor, Location.None));
                }
            }

            internal override Tuple<DiagnosticAnalyzer, ISuppressionFixProvider> CreateDiagnosticProviderAndFixer(Workspace workspace)
            {
                return new Tuple<DiagnosticAnalyzer, ISuppressionFixProvider>(
                    new UserDiagnosticAnalyzer(), new CSharpSuppressionCodeFixProvider());
            }

            protected override int CodeActionIndex
            {
                get
                {
                    return 0;
                }
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
            [WorkItem(1073825)]
            public async Task TestDiagnosticWithoutLocationCanBeSuppressed()
            {
                await TestAsync(
        @"[||]
using System;

class Class
{
    int Method()
    {
        int x = 0;
    }
}",
            $@"
// This file is used by Code Analysis to maintain SuppressMessage 
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given 
// a specific target and scoped to a namespace, type, member, etc.

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""NoLocationDiagnostic"", ""NoLocationDiagnostic:NoLocationDiagnostic"", Justification = ""{FeaturesResources.SuppressionPendingJustification}"")]

");
            }
        }

        #endregion
    }
}
