// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeStyle;
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
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.Suppression
{
    public abstract partial class CSharpSuppressionTests : AbstractSuppressionDiagnosticTest
    {
        protected override ParseOptions GetScriptOptions() => Options.Script;

        protected internal override string GetLanguage() => LanguageNames.CSharp;

        #region "Pragma disable tests"

        public abstract partial class CSharpPragmaWarningDisableSuppressionTests : CSharpSuppressionTests
        {
            protected sealed override int CodeActionIndex
            {
                get { return 0; }
            }

            [Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
            public class CompilerDiagnosticSuppressionTests : CSharpPragmaWarningDisableSuppressionTests
            {
                internal override Tuple<DiagnosticAnalyzer, IConfigurationFixProvider> CreateDiagnosticProviderAndFixer(Workspace workspace)
                    => Tuple.Create<DiagnosticAnalyzer, IConfigurationFixProvider>(null, new CSharpSuppressionCodeFixProvider());

                [Fact]
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

                [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/26015")]
                public async Task TestPragmaWarningDirectiveAroundMultiLineStatement()
                {
                    await TestAsync(
        @"
class Class
{
    void Method()
    {
        [|string x = @""multi
line"";|]
    }
}",
        $@"
class Class
{{
    void Method()
    {{
#pragma warning disable CS0219 // {CSharpResources.WRN_UnreferencedVarAssg_Title}
        string x = @""multi
line"";
#pragma warning restore CS0219 // {CSharpResources.WRN_UnreferencedVarAssg_Title}
    }}
}}");
                }

                [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/56165")]
                public async Task TestPragmaWarningDirectiveAroundMultiLineInterpolatedString()
                {
                    await TestAsync(
            @"
using System;

[Obsolete]
class Session { }

class Class
{
    void Method()
    {
        var s = $@""
hi {[|new Session()|]}
"";
    }
}",
            $@"
using System;

[Obsolete]
class Session {{ }}

class Class
{{
    void Method()
    {{
#pragma warning disable CS0612 // {CSharpResources.WRN_DeprecatedSymbol_Title}
        var s = $@""
hi {{new Session()}}
"";
#pragma warning restore CS0612 // {CSharpResources.WRN_DeprecatedSymbol_Title}
    }}
}}");
                }

                [Fact]
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
              + 1;
#pragma warning restore CS0219 // {CSharpResources.WRN_UnreferencedVarAssg_Title}
    }}
}}");
                }

                [Fact]
                public async Task TestMultilineStatementPragmaWarningDirective2()
                {
                    await TestAsync(
        @"
class Class
{
    void Method()
    {
        [|int x = 0,
            y = 1;|]
    }
}",
        $@"
class Class
{{
    void Method()
    {{
#pragma warning disable CS0219 // {CSharpResources.WRN_UnreferencedVarAssg_Title}
        int x = 0,
            y = 1;
#pragma warning restore CS0219 // {CSharpResources.WRN_UnreferencedVarAssg_Title}
    }}
}}");
                }

                [Fact]
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

                [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/16681")]
                public async Task TestPragmaWarningDirectiveWithDocumentationComment1()
                {
                    await TestAsync(
        @"
sealed class Class
{
    /// <summary>Text</summary>
    [|protected void Method()|]
    {
    }
}",
        $@"
sealed class Class
{{
    /// <summary>Text</summary>
#pragma warning disable CS0628 // {CSharpResources.WRN_ProtectedInSealed_Title}
    protected void Method()
#pragma warning restore CS0628 // {CSharpResources.WRN_ProtectedInSealed_Title}
    {{
    }}
}}");
                }

                [Fact]
                public async Task TestPragmaWarningExpressionBodiedMember1()
                {
                    await TestAsync(
        @"
sealed class Class
{
    [|protected int Method()|] => 1;
}",
        $@"
sealed class Class
{{
#pragma warning disable CS0628 // {CSharpResources.WRN_ProtectedInSealed_Title}
    protected int Method() => 1;
#pragma warning restore CS0628 // {CSharpResources.WRN_ProtectedInSealed_Title}
}}");
                }

                [Fact]
                public async Task TestPragmaWarningExpressionBodiedMember2()
                {
                    await TestAsync(
            @"
using System;

[Obsolete]
class Session { }

class Class
{
    string Method()
        => @$""hi
        {[|new Session()|]}
        "";
}",
            $@"
using System;

[Obsolete]
class Session {{ }}

class Class
{{
    string Method()
#pragma warning disable CS0612 // {CSharpResources.WRN_DeprecatedSymbol_Title}
        => @$""hi
        {{new Session()}}
        "";
#pragma warning restore CS0612 // {CSharpResources.WRN_DeprecatedSymbol_Title}
}}");
                }

                [Fact]
                public async Task TestPragmaWarningExpressionBodiedLocalFunction()
                {
                    await TestAsync(
            @"
using System;

[Obsolete]
class Session { }

class Class
{
    void M()
    {
        string Method()
            => @$""hi
            {[|new Session()|]}
            "";
    }
}",
            $@"
using System;

[Obsolete]
class Session {{ }}

class Class
{{
    void M()
    {{
#pragma warning disable CS0612 // {CSharpResources.WRN_DeprecatedSymbol_Title}
        string Method()
            => @$""hi
            {{new Session()}}
            "";
#pragma warning restore CS0612 // {CSharpResources.WRN_DeprecatedSymbol_Title}
    }}
}}");
                }

                [Fact]
                public async Task TestPragmaWarningExpressionBodiedLambda()
                {
                    await TestAsync(
            @"
using System;

[Obsolete]
class Session { }

class Class
{
    void M()
    {
        new Func<string>(()
            => @$""hi
            {[|new Session()|]}
            "");
    }
}",
            $@"
using System;

[Obsolete]
class Session {{ }}

class Class
{{
    void M()
    {{
#pragma warning disable CS0612 // {CSharpResources.WRN_DeprecatedSymbol_Title}
        new Func<string>(()
            => @$""hi
            {{new Session()}}
            "");
#pragma warning restore CS0612 // {CSharpResources.WRN_DeprecatedSymbol_Title}
    }}
}}");
                }

                [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/16681")]
                public async Task TestPragmaWarningDirectiveWithDocumentationComment2()
                {
                    await TestAsync(
        @"
sealed class Class
{
    /// <summary>Text</summary>
    /// <remarks>
    /// <see cref=""[|Class2|]""/>
    /// </remarks>
    void Method()
    {
    }
}",
        $@"
sealed class Class
{{

#pragma warning disable CS1574 // {CSharpResources.WRN_BadXMLRef_Title}
    /// <summary>Text</summary>
    /// <remarks>
    /// <see cref=""Class2""/>
    /// </remarks>
    void Method()
#pragma warning restore CS1574 // {CSharpResources.WRN_BadXMLRef_Title}
    {{
    }}
}}", new CSharpParseOptions(documentationMode: DocumentationMode.Diagnose));
                }

                [Fact]
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

                [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/3311")]
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
                    var parameters = new TestParameters();
                    using var workspace = CreateWorkspaceFromOptions(source, parameters);

                    var analyzerReference = new AnalyzerImageReference(ImmutableArray.Create<DiagnosticAnalyzer>(new CSharpCompilerDiagnosticAnalyzer()));
                    workspace.TryApplyChanges(workspace.CurrentSolution.WithAnalyzerReferences(new[] { analyzerReference }));

                    Assert.IsType<MockDiagnosticUpdateSourceRegistrationService>(workspace.ExportProvider.GetExportedValue<IDiagnosticUpdateSourceRegistrationService>());
                    var diagnosticService = Assert.IsType<DiagnosticAnalyzerService>(workspace.ExportProvider.GetExportedValue<IDiagnosticAnalyzerService>());
                    var incrementalAnalyzer = diagnosticService.CreateIncrementalAnalyzer(workspace);
                    var suppressionProvider = CreateDiagnosticProviderAndFixer(workspace).Item2;
                    var suppressionProviderFactory = new Lazy<IConfigurationFixProvider, CodeChangeProviderMetadata>(() => suppressionProvider,
                        new CodeChangeProviderMetadata("SuppressionProvider", languages: new[] { LanguageNames.CSharp }));
                    var fixService = new CodeFixService(
                        diagnosticService,
                        SpecializedCollections.EmptyEnumerable<Lazy<IErrorLoggerService>>(),
                        SpecializedCollections.EmptyEnumerable<Lazy<CodeFixProvider, CodeChangeProviderMetadata>>(),
                        SpecializedCollections.SingletonEnumerable(suppressionProviderFactory));
                    var document = GetDocumentAndSelectSpan(workspace, out var span);
                    var diagnostics = await diagnosticService.GetDiagnosticsForSpanAsync(document, span, CancellationToken.None);
                    Assert.Equal(2, diagnostics.Where(d => d.Id == "CS0219").Count());

                    var allFixes = (await fixService.GetFixesAsync(document, span, CodeActionOptions.DefaultProvider, CancellationToken.None))
                        .SelectMany(fixCollection => fixCollection.Fixes);

                    var cs0219Fixes = allFixes.Where(fix => fix.PrimaryDiagnostic.Id == "CS0219").ToArray();

                    // Ensure that there are no duplicate suppression fixes.
                    Assert.Equal(1, cs0219Fixes.Length);
                    var cs0219EquivalenceKey = cs0219Fixes[0].Action.EquivalenceKey;
                    Assert.NotNull(cs0219EquivalenceKey);

                    // Ensure that there *is* a fix for the other warning and that it has a *different*
                    // equivalence key so that it *doesn't* get de-duplicated
                    Assert.Equal(1, diagnostics.Where(d => d.Id == "CS0168").Count());
                    var cs0168Fixes = allFixes.Where(fix => fix.PrimaryDiagnostic.Id == "CS0168");
                    var cs0168EquivalenceKey = cs0168Fixes.Single().Action.EquivalenceKey;
                    Assert.NotNull(cs0168EquivalenceKey);
                    Assert.NotEqual(cs0219EquivalenceKey, cs0168EquivalenceKey);
                }

                [Fact]
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

                [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/956453")]
                public async Task TestWholeFilePragmaWarningDirective()
                {
                    await TestAsync(
        @"class Class { void Method() { [|int x = 0;|] } }",
        $@"#pragma warning disable CS0219 // {CSharpResources.WRN_UnreferencedVarAssg_Title}
class Class {{ void Method() {{ int x = 0; }} }}
#pragma warning restore CS0219 // {CSharpResources.WRN_UnreferencedVarAssg_Title}");
                }

                [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/970129")]
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

                [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1066576")]
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
#pragma warning disable CS1633 // {CSharpResources.WRN_IllegalPragma_Title}
    {{

// Comment
// Comment
#pragma abcde

    }}    // Comment   
#pragma warning restore CS1633 // {CSharpResources.WRN_IllegalPragma_Title}



}}");
                }

                [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1066576")]
                public async Task TestPragmaWarningDirectiveAroundTrivia2()
                {
                    await TestAsync(
        @"[|#pragma abcde|]",
        $@"#pragma warning disable CS1633 // {CSharpResources.WRN_IllegalPragma_Title}
#pragma abcde
#pragma warning restore CS1633 // {CSharpResources.WRN_IllegalPragma_Title}");
                }

                [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1066576")]
                public async Task TestPragmaWarningDirectiveAroundTrivia3()
                {
                    await TestAsync(
        @"[|#pragma abcde|]  ",
        $@"#pragma warning disable CS1633 // {CSharpResources.WRN_IllegalPragma_Title}
#pragma abcde  
#pragma warning restore CS1633 // {CSharpResources.WRN_IllegalPragma_Title}");
                }

                [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1066576")]
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

                [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1066576")]
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

                [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1066576")]
                public async Task TestPragmaWarningDirectiveAroundTrivia6()
                {
                    await TestAsync(
        @"class C1 { }
class C2 { } /// <summary><see [|cref=""abc""|]/></summary>
class C3 { } // comment
  // comment
// comment",
$@"class C1 {{ }}
#pragma warning disable CS1574 // {CSharpResources.WRN_BadXMLRef_Title}
class C2 {{ }} /// <summary><see cref=""abc""/></summary>
class
#pragma warning restore CS1574 // {CSharpResources.WRN_BadXMLRef_Title}
C3 {{ }} // comment
  // comment
// comment", CSharpParseOptions.Default.WithDocumentationMode(DocumentationMode.Diagnose));
                }
            }

            public class UserHiddenDiagnosticSuppressionTests : CSharpPragmaWarningDisableSuppressionTests
            {
                internal override Tuple<DiagnosticAnalyzer, IConfigurationFixProvider> CreateDiagnosticProviderAndFixer(Workspace workspace)
                {
                    return new Tuple<DiagnosticAnalyzer, IConfigurationFixProvider>(
                        new CSharpSimplifyTypeNamesDiagnosticAnalyzer(), new CSharpSuppressionCodeFixProvider());
                }

                [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
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
                        => context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.ClassDeclaration);

                    public void AnalyzeNode(SyntaxNodeAnalysisContext context)
                    {
                        var classDecl = (ClassDeclarationSyntax)context.Node;
                        context.ReportDiagnostic(Diagnostic.Create(Decsciptor, classDecl.Identifier.GetLocation()));
                    }
                }

                internal override Tuple<DiagnosticAnalyzer, IConfigurationFixProvider> CreateDiagnosticProviderAndFixer(Workspace workspace)
                {
                    return new Tuple<DiagnosticAnalyzer, IConfigurationFixProvider>(
                        new UserDiagnosticAnalyzer(), new CSharpSuppressionCodeFixProvider());
                }

                [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
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

            public partial class FormattingDiagnosticSuppressionTests : CSharpPragmaWarningDisableSuppressionTests
            {
                internal override Tuple<DiagnosticAnalyzer, IConfigurationFixProvider> CreateDiagnosticProviderAndFixer(Workspace workspace)
                {
                    return new Tuple<DiagnosticAnalyzer, IConfigurationFixProvider>(
                        new CSharpFormattingAnalyzer(), new CSharpSuppressionCodeFixProvider());
                }

                protected override async Task<(ImmutableArray<CodeAction>, CodeAction actionToInvoke)> GetCodeActionsAsync(TestWorkspace workspace, TestParameters parameters)
                {
                    var solution = workspace.CurrentSolution;
                    var compilationOptions = solution.Projects.Single().CompilationOptions;
                    var specificDiagnosticOptions = new[] { KeyValuePairUtil.Create(IDEDiagnosticIds.FormattingDiagnosticId, ReportDiagnostic.Warn) };
                    compilationOptions = compilationOptions.WithSpecificDiagnosticOptions(specificDiagnosticOptions);
                    var updatedSolution = solution.WithProjectCompilationOptions(solution.ProjectIds.Single(), compilationOptions);
                    await workspace.ChangeSolutionAsync(updatedSolution);

                    return await base.GetCodeActionsAsync(workspace, parameters);
                }

                [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
                [WorkItem("https://github.com/dotnet/roslyn/issues/38587")]
                public async Task TestFormattingDiagnosticSuppressed()
                {
                    await TestAsync(
            @"
using System;

class Class
{
    int Method()
    {
        [|int x = 0 ;|]
    }
}",
            @"
using System;

class Class
{
    int Method()
    {
#pragma warning disable format
        int x = 0 ;
#pragma warning restore format
    }
}");
                }
            }

            public class UserErrorDiagnosticSuppressionTests : CSharpPragmaWarningDisableSuppressionTests
            {
                private class UserDiagnosticAnalyzer : DiagnosticAnalyzer
                {
                    private readonly DiagnosticDescriptor _descriptor =
                        new DiagnosticDescriptor("ErrorDiagnostic", "ErrorDiagnostic", "ErrorDiagnostic", "ErrorDiagnostic", DiagnosticSeverity.Error, isEnabledByDefault: true);

                    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
                    {
                        get
                        {
                            return ImmutableArray.Create(_descriptor);
                        }
                    }

                    public override void Initialize(AnalysisContext context)
                        => context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.ClassDeclaration);

                    public void AnalyzeNode(SyntaxNodeAnalysisContext context)
                    {
                        var classDecl = (ClassDeclarationSyntax)context.Node;
                        context.ReportDiagnostic(Diagnostic.Create(_descriptor, classDecl.Identifier.GetLocation()));
                    }
                }

                internal override Tuple<DiagnosticAnalyzer, IConfigurationFixProvider> CreateDiagnosticProviderAndFixer(Workspace workspace)
                {
                    return new Tuple<DiagnosticAnalyzer, IConfigurationFixProvider>(
                        new UserDiagnosticAnalyzer(), new CSharpSuppressionCodeFixProvider());
                }

                [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
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
                    private readonly DiagnosticDescriptor _descriptor =
                        new DiagnosticDescriptor("@~DiagnosticWithBadId", "DiagnosticWithBadId", "DiagnosticWithBadId", "DiagnosticWithBadId", DiagnosticSeverity.Info, isEnabledByDefault: true);

                    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
                    {
                        get
                        {
                            return ImmutableArray.Create(_descriptor);
                        }
                    }

                    public override void Initialize(AnalysisContext context)
                        => context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.ClassDeclaration);

                    public void AnalyzeNode(SyntaxNodeAnalysisContext context)
                    {
                        var classDecl = (ClassDeclarationSyntax)context.Node;
                        context.ReportDiagnostic(Diagnostic.Create(_descriptor, classDecl.Identifier.GetLocation()));
                    }
                }

                internal override Tuple<DiagnosticAnalyzer, IConfigurationFixProvider> CreateDiagnosticProviderAndFixer(Workspace workspace)
                {
                    return new Tuple<DiagnosticAnalyzer, IConfigurationFixProvider>(
                        new UserDiagnosticAnalyzer(), new CSharpSuppressionCodeFixProvider());
                }

                [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
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
                    => context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.ClassDeclaration);

                public void AnalyzeNode(SyntaxNodeAnalysisContext context)
                {
                    var classDecl = (ClassDeclarationSyntax)context.Node;
                    context.ReportDiagnostic(Diagnostic.Create(Decsciptor, classDecl.GetLocation()));
                }
            }

            internal override Tuple<DiagnosticAnalyzer, IConfigurationFixProvider> CreateDiagnosticProviderAndFixer(Workspace workspace)
            {
                return new Tuple<DiagnosticAnalyzer, IConfigurationFixProvider>(
                    new UserDiagnosticAnalyzer(), new CSharpSuppressionCodeFixProvider());
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
            [WorkItem("https://github.com/dotnet/roslyn/issues/2764")]
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
            protected sealed override int CodeActionIndex => 1;

            public class CompilerDiagnosticSuppressionTests : CSharpGlobalSuppressMessageSuppressionTests
            {
                internal override Tuple<DiagnosticAnalyzer, IConfigurationFixProvider> CreateDiagnosticProviderAndFixer(Workspace workspace)
                    => Tuple.Create<DiagnosticAnalyzer, IConfigurationFixProvider>(null, new CSharpSuppressionCodeFixProvider());

                [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
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

            public class FormattingDiagnosticSuppressionTests : CSharpGlobalSuppressMessageSuppressionTests
            {
                internal override Tuple<DiagnosticAnalyzer, IConfigurationFixProvider> CreateDiagnosticProviderAndFixer(Workspace workspace)
                {
                    return Tuple.Create<DiagnosticAnalyzer, IConfigurationFixProvider>(
                        new CSharpFormattingAnalyzer(), new CSharpSuppressionCodeFixProvider());
                }

                protected override async Task<(ImmutableArray<CodeAction>, CodeAction actionToInvoke)> GetCodeActionsAsync(TestWorkspace workspace, TestParameters parameters)
                {
                    var solution = workspace.CurrentSolution;
                    var compilationOptions = solution.Projects.Single().CompilationOptions;
                    var specificDiagnosticOptions = new[] { KeyValuePairUtil.Create(IDEDiagnosticIds.FormattingDiagnosticId, ReportDiagnostic.Warn) };
                    compilationOptions = compilationOptions.WithSpecificDiagnosticOptions(specificDiagnosticOptions);
                    var updatedSolution = solution.WithProjectCompilationOptions(solution.ProjectIds.Single(), compilationOptions);
                    await workspace.ChangeSolutionAsync(updatedSolution);

                    return await base.GetCodeActionsAsync(workspace, parameters);
                }

                [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
                [WorkItem("https://github.com/dotnet/roslyn/issues/38587")]
                public async Task TestCompilerDiagnosticsCannotBeSuppressed()
                {
                    // Another test verifies we have a pragma warning action for this source, this verifies there are no other suppression actions.
                    await TestActionCountAsync(
        @"
class Class
{
    void Method()
    {
        [|int x = 0 ;|]
    }
}", 1);
                }
            }

            public class UserHiddenDiagnosticSuppressionTests : CSharpGlobalSuppressMessageSuppressionTests
            {
                internal override Tuple<DiagnosticAnalyzer, IConfigurationFixProvider> CreateDiagnosticProviderAndFixer(Workspace workspace)
                {
                    return new Tuple<DiagnosticAnalyzer, IConfigurationFixProvider>(
                        new CSharpSimplifyTypeNamesDiagnosticAnalyzer(), new CSharpSuppressionCodeFixProvider());
                }

                [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
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

            [Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
            public partial class UserInfoDiagnosticSuppressionTests : CSharpGlobalSuppressMessageSuppressionTests
            {
                private class UserDiagnosticAnalyzer : DiagnosticAnalyzer
                {
                    public static readonly DiagnosticDescriptor Descriptor =
                        new("InfoDiagnostic", "InfoDiagnostic", "InfoDiagnostic", "InfoDiagnostic", DiagnosticSeverity.Info, isEnabledByDefault: true);

                    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
                    {
                        get
                        {
                            return ImmutableArray.Create(Descriptor);
                        }
                    }

                    public override void Initialize(AnalysisContext context)
                        => context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.ClassDeclaration, SyntaxKind.EnumDeclaration, SyntaxKind.NamespaceDeclaration, SyntaxKind.MethodDeclaration, SyntaxKind.PropertyDeclaration, SyntaxKind.FieldDeclaration, SyntaxKind.EventDeclaration);

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
                                foreach (var trivia in context.Node.DescendantTrivia().Where(t => t.Kind() is SyntaxKind.SingleLineCommentTrivia or SyntaxKind.MultiLineCommentTrivia))
                                {
                                    context.ReportDiagnostic(Diagnostic.Create(Descriptor, trivia.GetLocation()));
                                }

                                break;
                        }
                    }
                }

                internal override Tuple<DiagnosticAnalyzer, IConfigurationFixProvider> CreateDiagnosticProviderAndFixer(Workspace workspace)
                {
                    return new Tuple<DiagnosticAnalyzer, IConfigurationFixProvider>(
                        new UserDiagnosticAnalyzer(), new CSharpSuppressionCodeFixProvider());
                }

                [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/37529")]
                public async Task GeneratedCodeShouldNotHaveTrailingWhitespace()
                {
                    var expected =
$@"// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""{FeaturesResources.Pending}"", Scope = ""type"", Target = ""~T:Class"")]
";

                    Assert.All(Regex.Split(expected, "\r?\n"), line => Assert.False(HasTrailingWhitespace(line)));

                    await TestAsync(
            @"
using System;

[|class Class|]
{
    int Method()
    {
        int x = 0;
    }
}", expected);
                }

                private static bool HasTrailingWhitespace(string line)
                    => line.LastOrNull() is char last && char.IsWhiteSpace(last);

                [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/37529")]
                public async Task GeneratedCodeShouldNotHaveLeadingBlankLines()
                {
                    var expected =
$@"// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""{FeaturesResources.Pending}"", Scope = ""type"", Target = ""~T:Class"")]
";

                    var lines = Regex.Split(expected, "\r?\n");
                    Assert.False(string.IsNullOrWhiteSpace(lines.First()));

                    await TestAsync(
            @"
using System;

[|class Class|]
{
    int Method()
    {
        int x = 0;
    }
}", expected);
                }

                [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/37529")]
                public async Task GeneratedCodeShouldNotHaveMoreThanOneTrailingBlankLine()
                {
                    var expected =
$@"// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""{FeaturesResources.Pending}"", Scope = ""type"", Target = ""~T:Class"")]
";

                    var lines = Regex.Split(expected, "\r?\n");
                    Assert.False(string.IsNullOrWhiteSpace(lines[^2]));

                    await TestAsync(
            @"
using System;

[|class Class|]
{
    int Method()
    {
        int x = 0;
    }
}", expected);
                }

                [Fact]
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
$@"// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""{FeaturesResources.Pending}"", Scope = ""type"", Target = ""~T:Class"")]
");

                    // Also verify that the added attribute does indeed suppress the diagnostic.
                    await TestMissingAsync(
            @"
using System;

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""<Pending>"", Scope = ""type"", Target = ""~T:Class"")]

[|class Class|]
{
    int Method()
    {
        int x = 0;
    }
}");
                }

                [Fact]
                public async Task TestSuppressionOnNamespace()
                {
                    await TestInRegularAndScriptAsync(
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
$@"// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""{FeaturesResources.Pending}"", Scope = ""namespace"", Target = ""~N:N"")]
", index: 1);

                    // Also verify that the added attribute does indeed suppress the diagnostic.
                    await TestMissingAsync(
            @"
using System;

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""<Pending>"", Scope = ""namespace"", Target = ""~N:N"")]

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

                [Fact]
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
$@"// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""{FeaturesResources.Pending}"", Scope = ""type"", Target = ""~T:N1.N2.Class"")]
");

                    // Also verify that the added attribute does indeed suppress the diagnostic.
                    await TestMissingAsync(
            @"
using System;

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""<Pending>"", Scope = ""type"", Target = ""~T:N1.N2.Class"")]

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

                [Fact]
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
$@"// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""{FeaturesResources.Pending}"", Scope = ""type"", Target = ""~T:N.Generic`1.Class"")]
");

                    // Also verify that the added attribute does indeed suppress the diagnostic.
                    await TestMissingAsync(
            @"
using System;

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""<Pending>"", Scope = ""type"", Target = ""~T:N.Generic`1.Class"")]

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

                [Fact]
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
$@"// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""{FeaturesResources.Pending}"", Scope = ""member"", Target = ""~M:N.Generic`1.Class.Method~System.Int32"")]
");

                    // Also verify that the added attribute does indeed suppress the diagnostic.
                    await TestMissingAsync(
            @"
using System;

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""<Pending>"", Scope = ""member"", Target = ""~M:N.Generic`1.Class.Method~System.Int32"")]

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

                [Fact]
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
$@"// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""{FeaturesResources.Pending}"", Scope = ""member"", Target = ""~M:N.Generic`1.Class.Method(System.Int32,System.Char@)~System.Int32"")]
");

                    // Also verify that the added attribute does indeed suppress the diagnostic.
                    await TestMissingAsync(
            @"
using System;

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""<Pending>"", Scope = ""member"", Target = ""~M:N.Generic`1.Class.Method(System.Int32,System.Char@)~System.Int32"")]

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

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""<Pending>"", Scope = ""member"", Target = ""~M:N.Generic`1.Class.Method(System.Int32,System.Char@)~System.Int32"")]

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
$@"// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""{FeaturesResources.Pending}"", Scope = ""member"", Target = ""~M:N.Generic`1.Class.Method~System.Int32"")]
");
                }

                [Fact]
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
$@"// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""{FeaturesResources.Pending}"", Scope = ""member"", Target = ""~M:N.Generic`1.Class.Method``1(``0)~System.Int32"")]
");

                    // Also verify that the added attribute does indeed suppress the diagnostic.
                    await TestMissingAsync(
            @"
using System;

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""<Pending>"", Scope = ""member"", Target = ""~M:N.Generic`1.Class.Method``1(``0)~System.Int32"")]

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

                [Fact]
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
$@"// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""{FeaturesResources.Pending}"", Scope = ""member"", Target = ""~P:N.Generic.Class.Property"")]
");

                    // Also verify that the added attribute does indeed suppress the diagnostic.
                    await TestMissingAsync(
            @"
using System;

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""<Pending>"", Scope = ""member"", Target = ""~P:N.Generic.Class.Property"")]

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

                [Fact]
                public async Task TestSuppressionOnField()
                {
                    await TestAsync(
            @"
using System;

class Class
{
    [|int field = 0;|]
}",
$@"// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""{FeaturesResources.Pending}"", Scope = ""member"", Target = ""~F:Class.field"")]
");

                    // Also verify that the added attribute does indeed suppress the diagnostic.
                    await TestMissingAsync(
            @"
using System;

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""<Pending>"", Scope = ""member"", Target = ""~F:Class.field"")]

class Class
{
    [|int field = 0;|]
}");
                }

                [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/6379")]
                public async Task TestSuppressionOnTriviaBetweenFields()
                {
                    await TestAsync(
            @"
using System;

// suppressions on field are not relevant.
using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""{FeaturesResources.Pending}"", Scope = ""member"", Target = ""~F:E.Field1"")]
using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""{FeaturesResources.Pending}"", Scope = ""member"", Target = ""~F:E.Field2"")]

enum E
{
    [|
    Field1, // trailing trivia for comma token which doesn't belong to span of any of the fields
    Field2
    |]
}",
$@"// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""{FeaturesResources.Pending}"", Scope = ""type"", Target = ""~T:E"")]
");

                    // Also verify that the added attribute does indeed suppress the diagnostic.
                    await TestMissingAsync(
            @"
using System;

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""{FeaturesResources.Pending}"", Scope = ""type"", Target = ""~T:E"")]

enum E
{
    [|
    Field1, // trailing trivia for comma token which doesn't belong to span of any of the fields
    Field2
    |]
}");
                }

                [Fact]
                public async Task TestSuppressionOnField2()
                {
                    await TestAsync(
            @"
using System;

class Class
{
    int [|field = 0|], field2 = 1;
}",
$@"// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""{FeaturesResources.Pending}"", Scope = ""member"", Target = ""~F:Class.field"")]
");

                    // Also verify that the added attribute does indeed suppress the diagnostic.
                    await TestMissingAsync(
            @"
using System;

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""<Pending>"", Scope = ""member"", Target = ""~F:Class.field"")]

class Class
{
    int [|field|] = 0, field2 = 1;
}");
                }

                [Fact]
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
$@"// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""{FeaturesResources.Pending}"", Scope = ""member"", Target = ""~E:Class.SampleEvent"")]
");

                    // Also verify that the added attribute does indeed suppress the diagnostic.
                    await TestMissingAsync(
            @"
using System;

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""<Pending>"", Scope = ""member"", Target = ""~E:Class.SampleEvent"")]

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

                [Fact]
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
        <Document FilePath=""GlobalSuppressions.cs""><![CDATA[// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""<Pending>"", Scope = ""type"", Target = ""Class"")]
]]>
        </Document>
    </Project>
</Workspace>";
                    var expectedText =
$@"// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""<Pending>"", Scope = ""type"", Target = ""Class"")]
[assembly: SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""{FeaturesResources.Pending}"", Scope = ""type"", Target = ""~T:Class2"")]
";

                    await TestAsync(initialMarkup, expectedText);
                }

                [Fact]
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
$@"// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""{FeaturesResources.Pending}"", Scope = ""type"", Target = ""~T:Class2"")]
";

                    await TestAsync(initialMarkup, expectedText);
                }

                [Fact]
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
         <Document FilePath=""GlobalSuppressions2.cs""><![CDATA[// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""<Pending>"", Scope = ""type"", Target = ""Class"")]
]]>
        </Document>
    </Project>
</Workspace>";
                    var expectedText =
$@"// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""<Pending>"", Scope = ""type"", Target = ""Class"")]
[assembly: SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""{FeaturesResources.Pending}"", Scope = ""type"", Target = ""~T:Class2"")]
";

                    await TestAsync(initialMarkup, expectedText);
                }

                [Fact]
                public async Task TestSuppressionWithUsingDirectiveInExistingGlobalSuppressionsDocument()
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
using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""<Pending>"", Scope = ""type"", Target = ""Class"")]
]]>
        </Document>
    </Project>
</Workspace>";
                    var expectedText =
                        $@"
using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""<Pending>"", Scope = ""type"", Target = ""Class"")]
[assembly: SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""{FeaturesResources.Pending}"", Scope = ""type"", Target = ""~T:Class2"")]
";

                    await TestAsync(initialMarkup, expectedText);
                }

                [Fact]
                public async Task TestSuppressionWithoutUsingDirectiveInExistingGlobalSuppressionsDocument()
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
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""<Pending>"", Scope = ""type"", Target = ""Class"")]
]]>
        </Document>
    </Project>
</Workspace>";
                    var expectedText =
$@"
using System.Diagnostics.CodeAnalysis;

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""<Pending>"", Scope = ""type"", Target = ""Class"")]
[assembly: SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""{FeaturesResources.Pending}"", Scope = ""type"", Target = ""~T:Class2"")]
";

                    await TestAsync(initialMarkup, expectedText);
                }
            }
        }

        public abstract class CSharpLocalSuppressMessageSuppressionTests : CSharpSuppressionTests
        {
            protected sealed override int CodeActionIndex => 2;

            [Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
            public class UserInfoDiagnosticSuppressionTests : CSharpLocalSuppressMessageSuppressionTests
            {
                private class UserDiagnosticAnalyzer : DiagnosticAnalyzer
                {
                    private readonly DiagnosticDescriptor _descriptor =
                        new("InfoDiagnostic", "InfoDiagnostic", "InfoDiagnostic", "InfoDiagnostic", DiagnosticSeverity.Info, isEnabledByDefault: true);

                    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(_descriptor);

                    public override void Initialize(AnalysisContext context)
                        => context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.ClassDeclaration, SyntaxKind.NamespaceDeclaration, SyntaxKind.MethodDeclaration);

                    public void AnalyzeNode(SyntaxNodeAnalysisContext context)
                    {
                        switch (context.Node.Kind())
                        {
                            case SyntaxKind.ClassDeclaration:
                                var classDecl = (ClassDeclarationSyntax)context.Node;
                                context.ReportDiagnostic(Diagnostic.Create(_descriptor, classDecl.Identifier.GetLocation()));
                                break;

                            case SyntaxKind.NamespaceDeclaration:
                                var ns = (NamespaceDeclarationSyntax)context.Node;
                                context.ReportDiagnostic(Diagnostic.Create(_descriptor, ns.Name.GetLocation()));
                                break;

                            case SyntaxKind.MethodDeclaration:
                                var method = (MethodDeclarationSyntax)context.Node;
                                context.ReportDiagnostic(Diagnostic.Create(_descriptor, method.Identifier.GetLocation()));
                                break;
                        }
                    }
                }

                internal override Tuple<DiagnosticAnalyzer, IConfigurationFixProvider> CreateDiagnosticProviderAndFixer(Workspace workspace)
                {
                    return new Tuple<DiagnosticAnalyzer, IConfigurationFixProvider>(
                        new UserDiagnosticAnalyzer(), new CSharpSuppressionCodeFixProvider());
                }

                [Fact]
                public async Task TestSuppressionOnSimpleType()
                {
                    var initial = @"
using System;

// Some trivia
/* More Trivia */ [|class Class|]
{
    int Method()
    {
        int x = 0;
    }
}";
                    var expected = $@"
using System;

// Some trivia
/* More Trivia */
[System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""{FeaturesResources.Pending}"")]
class Class
{{
    int Method()
    {{
        int x = 0;
    }}
}}";
                    await TestAsync(initial, expected);

                    // Also verify that the added attribute does indeed suppress the diagnostic.
                    expected = expected.Replace("class Class", "[|class Class|]");
                    await TestMissingAsync(expected);
                }

                [Fact]
                public async Task TestSuppressionOnSimpleType2()
                {
                    // Type already has attributes.
                    var initial = @"
using System;

// Some trivia
/* More Trivia */
[System.Diagnostics.CodeAnalysis.SuppressMessage(""SomeOtherDiagnostic"", ""SomeOtherDiagnostic:Title"", Justification = ""<Pending>"")]
[|class Class|]
{
    int Method()
    {
        int x = 0;
    }
}";
                    var expected = $@"
using System;

// Some trivia
/* More Trivia */
[System.Diagnostics.CodeAnalysis.SuppressMessage(""SomeOtherDiagnostic"", ""SomeOtherDiagnostic:Title"", Justification = ""<Pending>"")]
[System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""{FeaturesResources.Pending}"")]
class Class
{{
    int Method()
    {{
        int x = 0;
    }}
}}";
                    await TestAsync(initial, expected);

                    // Also verify that the added attribute does indeed suppress the diagnostic.
                    expected = expected.Replace("class Class", "[|class Class|]");
                    await TestMissingAsync(expected);
                }

                [Fact]
                public async Task TestSuppressionOnSimpleType3()
                {
                    // Type already has attributes with trailing trivia.
                    var initial = @"
using System;

// Some trivia
/* More Trivia */
[System.Diagnostics.CodeAnalysis.SuppressMessage(""SomeOtherDiagnostic"", ""SomeOtherDiagnostic:Title"", Justification = ""<Pending>"")]
/* Some More Trivia */
[|class Class|]
{
    int Method()
    {
        int x = 0;
    }
}";
                    var expected = $@"
using System;

// Some trivia
/* More Trivia */
[System.Diagnostics.CodeAnalysis.SuppressMessage(""SomeOtherDiagnostic"", ""SomeOtherDiagnostic:Title"", Justification = ""<Pending>"")]
[System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""{FeaturesResources.Pending}"")]
/* Some More Trivia */
class Class
{{
    int Method()
    {{
        int x = 0;
    }}
}}";
                    await TestAsync(initial, expected);

                    // Also verify that the added attribute does indeed suppress the diagnostic.
                    expected = expected.Replace("class Class", "[|class Class|]");
                    await TestMissingAsync(expected);
                }

                [Fact]
                public async Task TestSuppressionOnTypeInsideNamespace()
                {
                    var initial = @"
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
}";
                    var expected = $@"
using System;

namespace N1
{{
    namespace N2
    {{
        [System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""{FeaturesResources.Pending}"")]
        class Class
        {{
            int Method()
            {{
                int x = 0;
            }}
        }}
    }}
}}";
                    await TestAsync(initial, expected);

                    // Also verify that the added attribute does indeed suppress the diagnostic.
                    expected = expected.Replace("class Class", "[|class Class|]");
                    await TestMissingAsync(expected);
                }

                [Fact]
                public async Task TestSuppressionOnNestedType()
                {
                    var initial = @"
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
}";
                    var expected = $@"
using System;

namespace N
{{
    class Generic<T>
    {{
        [System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""{FeaturesResources.Pending}"")]
        class Class
        {{
            int Method()
            {{
                int x = 0;
            }}
        }}
    }}
}}";
                    await TestAsync(initial, expected);

                    // Also verify that the added attribute does indeed suppress the diagnostic.
                    expected = expected.Replace("class Class", "[|class Class|]");
                    await TestMissingAsync(expected);
                }

                [Fact]
                public async Task TestSuppressionOnMethod()
                {
                    var initial = @"
using System;

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
}";
                    var expected = $@"
using System;

namespace N
{{
    class Generic<T>
    {{
        class Class
        {{
            [System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""{FeaturesResources.Pending}"")]
            int Method()
            {{
                int x = 0;
            }}
        }}
    }}
}}";
                    await TestAsync(initial, expected);

                    // Also verify that the added attribute does indeed suppress the diagnostic.
                    expected = expected.Replace("int Method()", "[|int Method()|]");
                    await TestMissingAsync(expected);
                }

                [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/47427")]
                public async Task TestSuppressionOnMethodWithXmlDoc()
                {
                    var initial = @"
using System;

namespace ClassLibrary10
{
    public class Class1
    {
        int x;

        /// <summary>
        /// This is a description
        /// </summary>
        [|public void Method(int unused)|] { }
    }
}";
                    var expected = $@"
using System;

namespace ClassLibrary10
{{
    public class Class1
    {{
        int x;

        /// <summary>
        /// This is a description
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""{FeaturesResources.Pending}"")]
        public void Method(int unused) {{ }}
    }}
}}";
                    await TestAsync(initial, expected);

                    // Also verify that the added attribute does indeed suppress the diagnostic.
                    expected = expected.Replace("public void Method(int unused)", "[|public void Method(int unused)|]");
                    await TestMissingAsync(expected);
                }

                [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/47427")]
                public async Task TestSuppressionOnMethodWithNoTrivia()
                {
                    var initial = @"
using System;

namespace ClassLibrary10
{
    public class Class1
    {
        int x;
[|public void Method(int unused)|] { }
    }
}";
                    var expected = $@"
using System;

namespace ClassLibrary10
{{
    public class Class1
    {{
        int x;
        [System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""{FeaturesResources.Pending}"")]
        public void Method(int unused) {{ }}
    }}
}}";
                    await TestAsync(initial, expected);

                    // Also verify that the added attribute does indeed suppress the diagnostic.
                    expected = expected.Replace("public void Method(int unused)", "[|public void Method(int unused)|]");
                    await TestMissingAsync(expected);
                }

                [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/47427")]
                public async Task TestSuppressionOnMethodWithTriviaStartsOnTheSameLine()
                {
                    var initial = @"
using System;

namespace ClassLibrary10
{
    public class Class1
    {
        int x;
        /*test*/[|public void Method(int unused)|] { }
    }
}";
                    var expected = $@"
using System;

namespace ClassLibrary10
{{
    public class Class1
    {{
        int x;
        /*test*/
        [System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""{FeaturesResources.Pending}"")]
        public void Method(int unused) {{ }}
    }}
}}";
                    await TestAsync(initial, expected);

                    // Also verify that the added attribute does indeed suppress the diagnostic.
                    expected = expected.Replace("public void Method(int unused)", "[|public void Method(int unused)|]");
                    await TestMissingAsync(expected);
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
                    new("NoLocationDiagnostic", "NoLocationDiagnostic", "NoLocationDiagnostic", "NoLocationDiagnostic", DiagnosticSeverity.Info, isEnabledByDefault: true);

                public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
                    => ImmutableArray.Create(Descriptor);

                public override void Initialize(AnalysisContext context)
                    => context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.ClassDeclaration);

                public void AnalyzeNode(SyntaxNodeAnalysisContext context)
                    => context.ReportDiagnostic(Diagnostic.Create(Descriptor, Location.None));
            }

            internal override Tuple<DiagnosticAnalyzer, IConfigurationFixProvider> CreateDiagnosticProviderAndFixer(Workspace workspace)
            {
                return new Tuple<DiagnosticAnalyzer, IConfigurationFixProvider>(
                    new UserDiagnosticAnalyzer(), new CSharpSuppressionCodeFixProvider());
            }

            protected override int CodeActionIndex => 0;

            [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
            [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1073825")]
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
$@"// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(""NoLocationDiagnostic"", ""NoLocationDiagnostic:NoLocationDiagnostic"", Justification = ""{FeaturesResources.Pending}"")]
");
            }
        }

        #endregion
    }
}
