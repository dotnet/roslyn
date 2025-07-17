// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.CodeCleanup.Providers;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.CodeCleanup;

using CSharp = Microsoft.CodeAnalysis.CSharp;

[UseExportProvider]
public sealed class CodeCleanupTests
{
    #region CSharp Code CodeCleaner Tests
    [Fact]
    public void DefaultCSharpCodeCleanups()
    {
        var document = CreateDocument("class C { }", LanguageNames.CSharp);
        var codeCleanups = CodeCleaner.GetDefaultProviders(document);
        Assert.NotEmpty(codeCleanups);
    }

    [Fact]
    public async Task CodeCleanersCSharp_NoSpans()
    {
        var document = CreateDocument("class C { }", LanguageNames.CSharp);
        var cleanDocument = await CodeCleaner.CleanupAsync(document, [], await document.GetCodeCleanupOptionsAsync(CancellationToken.None));

        Assert.Equal(document, cleanDocument);
    }

    [Fact]
    public async Task CodeCleanersCSharp_Document()
    {
        var document = CreateDocument("class C { }", LanguageNames.CSharp);
        var cleanDocument = await CodeCleaner.CleanupAsync(document, await document.GetCodeCleanupOptionsAsync(CancellationToken.None));

        Assert.Equal(document, cleanDocument);
    }

    [Fact]
    public async Task CodeCleanersCSharp_Span()
    {
        var document = CreateDocument("class C { }", LanguageNames.CSharp);
        var root = await document.GetSyntaxRootAsync();
        var cleanDocument = await CodeCleaner.CleanupAsync(document, root.FullSpan, await document.GetCodeCleanupOptionsAsync(CancellationToken.None));

        Assert.Equal(document, cleanDocument);
    }

    [Fact]
    public async Task CodeCleanersCSharp_Spans()
    {
        var document = CreateDocument("class C { }", LanguageNames.CSharp);
        var root = await document.GetSyntaxRootAsync();
        var cleanDocument = await CodeCleaner.CleanupAsync(document, [root.FullSpan], await document.GetCodeCleanupOptionsAsync(CancellationToken.None));

        Assert.Equal(document, cleanDocument);
    }

    #endregion

    #region Visual Basic Code CodeCleaner Tests

    [Fact]
    public void DefaultVisualBasicCodeCleanups()
    {
        var document = CreateDocument("""
            Class C
            End Class
            """, LanguageNames.VisualBasic);
        var codeCleanups = CodeCleaner.GetDefaultProviders(document);
        Assert.NotEmpty(codeCleanups);
    }

    [Fact]
    public async Task CodeCleanersVisualBasic_NoSpans()
    {
        var document = CreateDocument("""
            Class C
            End Class
            """, LanguageNames.VisualBasic);
        var cleanDocument = await CodeCleaner.CleanupAsync(document, [], await document.GetCodeCleanupOptionsAsync(CancellationToken.None));

        Assert.Equal(document, cleanDocument);
    }

    [Fact]
    public async Task CodeCleanersVisualBasic_Document()
    {
        var document = CreateDocument("""
            Class C
            End Class
            """, LanguageNames.VisualBasic);
        var cleanDocument = await CodeCleaner.CleanupAsync(document, await document.GetCodeCleanupOptionsAsync(CancellationToken.None));

        Assert.Equal(document, cleanDocument);
    }

    [Fact]
    public async Task CodeCleanersVisualBasic_Span()
    {
        var document = CreateDocument("""
            Class C
            End Class
            """, LanguageNames.VisualBasic);
        var root = await document.GetSyntaxRootAsync();
        var cleanDocument = await CodeCleaner.CleanupAsync(document, root.FullSpan, await document.GetCodeCleanupOptionsAsync(CancellationToken.None));

        Assert.Equal(document, cleanDocument);
    }

    [Fact]
    public async Task CodeCleanersVisualBasic_Spans()
    {
        var document = CreateDocument("""
            Class C
            End Class
            """, LanguageNames.VisualBasic);
        var root = await document.GetSyntaxRootAsync();
        var cleanDocument = await CodeCleaner.CleanupAsync(document, [root.FullSpan], await document.GetCodeCleanupOptionsAsync(CancellationToken.None));

        Assert.Equal(document, cleanDocument);
    }

    [Fact]
    public async Task CodeCleanersCSharp_Annotation()
    {
        var document = CreateDocument("class C { }", LanguageNames.CSharp);
        var annotation = new SyntaxAnnotation();
        document = document.WithSyntaxRoot((await document.GetSyntaxRootAsync()).WithAdditionalAnnotations(annotation));

        var cleanDocument = await CodeCleaner.CleanupAsync(document, annotation, await document.GetCodeCleanupOptionsAsync(CancellationToken.None));

        Assert.Equal(document, cleanDocument);
    }

    [Fact]
    public async Task CodeCleanersVisualBasic_Annotation()
    {
        var document = CreateDocument("""
            Class C
            End Class
            """, LanguageNames.VisualBasic);
        var annotation = new SyntaxAnnotation();
        document = document.WithSyntaxRoot((await document.GetSyntaxRootAsync()).WithAdditionalAnnotations(annotation));

        var cleanDocument = await CodeCleaner.CleanupAsync(document, annotation, await document.GetCodeCleanupOptionsAsync(CancellationToken.None));

        Assert.Equal(document, cleanDocument);
    }

    #endregion

    [Fact]
    public Task EntireRange()
        => VerifyRange("{|b:{|r:class C {}|}|}");

    [Fact]
    public Task EntireRange_Merge()
        => VerifyRange("{|r:class {|b:C { }|} class {|b: B { } |}|}");

    [Fact]
    public Task EntireRange_EndOfFile()
        => VerifyRange("{|r:class {|b:C { }|} class {|b: B { } |} |}");

    [Fact]
    public async Task EntireRangeWithTransformation_RemoveClass()
    {
        var transformer = new MockCodeCleanupProvider()
        {
            CleanupDocumentAsyncImpl = async (provider, document, spans, options, cancellationToken) =>
            {
                var root = await document.GetSyntaxRootAsync(cancellationToken);
                root = root.RemoveCSharpMember(0);

                provider.ExpectedResult = [root.FullSpan];

                return document.WithSyntaxRoot(root);
            }
        };

        await VerifyRange("{|b:class C {}|}", transformer);
    }

    [Fact]
    public async Task EntireRangeWithTransformation_AddMember()
    {
        var transformer = new MockCodeCleanupProvider()
        {
            CleanupDocumentAsyncImpl = async (provider, document, spans, options, cancellationToken) =>
            {
                var root = await document.GetSyntaxRootAsync(cancellationToken);
                var @class = root.GetMember(0);
                var classWithMember = @class.AddCSharpMember(CreateCSharpMethod(), 0);
                root = root.ReplaceNode(@class, classWithMember);

                provider.ExpectedResult = [root.FullSpan];

                return document.WithSyntaxRoot(root);
            }
        };

        await VerifyRange("{|b:class C {}|}", transformer);
    }

    [Fact]
    public async Task RangeWithTransformation_AddMember()
    {
        var transformer = new MockCodeCleanupProvider()
        {
            CleanupDocumentAsyncImpl = async (provider, document, spans, options, cancellationToken) =>
            {
                var root = await document.GetSyntaxRootAsync(cancellationToken);
                var @class = root.GetMember(0).GetMember(0);
                var classWithMember = @class.AddCSharpMember(CreateCSharpMethod(), 0);
                root = root.ReplaceNode(@class, classWithMember);

                provider.ExpectedResult = [root.GetMember(0).GetMember(0).GetCodeCleanupSpan()];

                return document.WithSyntaxRoot(root);
            }
        };

        await VerifyRange("namespace N { {|b:class C {}|} }", transformer);
    }

    [Fact]
    public async Task RangeWithTransformation_RemoveMember()
    {
        var transformer = new MockCodeCleanupProvider()
        {
            CleanupDocumentAsyncImpl = async (provider, document, spans, options, cancellationToken) =>
            {
                var root = await document.GetSyntaxRootAsync(cancellationToken);
                var @class = root.GetMember(0).GetMember(0);
                var classWithMember = @class.RemoveCSharpMember(0);
                root = root.ReplaceNode(@class, classWithMember);

                provider.ExpectedResult = [root.GetMember(0).GetMember(0).GetCodeCleanupSpan()];

                return document.WithSyntaxRoot(root);
            }
        };

        await VerifyRange("namespace N { {|b:class C { void Method() { } }|} }", transformer);
    }

    [Fact]
    public Task MultipleRange_Overlapped()
        => VerifyRange("namespace N {|r:{ {|b:class C { {|b:void Method() { }|} }|} }|}");

    [Fact]
    public Task MultipleRange_Adjacent()
        => VerifyRange("namespace N {|r:{ {|b:class C { |}{|b:void Method() { } }|} }|}");

    [Fact]
    public Task MultipleRanges()
        => VerifyRange("namespace N { class C {|r:{ {|b:void Method() { }|} }|} class C2 {|r:{ {|b:void Method() { }|} }|} }");

    [Fact, WorkItem(12848, "DevDiv_Projects/Roslyn")]
    public Task DoNotCrash_VB()
        => VerifyRange("""
            #If DEBUG OrElse TRACE Then
            Imports System.Diagnostics
            #ElseIf SILVERLIGHT Then
            Imports System.Diagnostics
            #Else
            Imports System.Diagnostics
            #End If

            {|r:# {|b: |}
                Region|} "Region"
            #Region "more"
            #End Region 
            #End Region
            """, LanguageNames.VisualBasic);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/774295")]
    public async Task DoNotCrash_VB_2()
    {
        var code = """
            Public Class Class1
                Public Custom Event Event2 As EventHandler
                    AddHandler(ByVal value As EventHandler)
                    End AddHandler
                    RemoveHandler(ByVal value As EventHandler)
                    End RemoveHandler
                    RaiseEvent(ByVal sender As Object, ByVal e As EventArgs)
                        e
                End Event

            End Class
            """;
        var document = CreateDocument(code, LanguageNames.VisualBasic);
        var semanticModel = await document.GetSemanticModelAsync();
        var root = await document.GetSyntaxRootAsync();
        var accessor = root.DescendantNodes().OfType<VisualBasic.Syntax.AccessorBlockSyntax>().Last();
        var newSemanticModel = await document.ReuseExistingSpeculativeModelAsync(accessor.Statements[0], CancellationToken.None);
        Assert.NotNull(newSemanticModel);
        Assert.False(newSemanticModel.IsSpeculativeSemanticModel);

        var newDocument = CreateDocument(code, LanguageNames.VisualBasic);
        var newRoot = await newDocument.GetSyntaxRootAsync();
        var newAccessor = newRoot.DescendantNodes().OfType<VisualBasic.Syntax.AccessorBlockSyntax>().Last();
        root = root.ReplaceNode(accessor, newAccessor);
        document = document.WithSyntaxRoot(root);
        accessor = root.DescendantNodes().OfType<VisualBasic.Syntax.AccessorBlockSyntax>().Last();
        newSemanticModel = await document.ReuseExistingSpeculativeModelAsync(accessor.Statements[0], CancellationToken.None);
        Assert.NotNull(newSemanticModel);
        Assert.True(newSemanticModel.IsSpeculativeSemanticModel);

        var cleanDocument = await CodeCleaner.CleanupAsync(document, await document.GetCodeCleanupOptionsAsync(CancellationToken.None));
        Assert.Equal(document, cleanDocument);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/547075")]
    public Task TestCodeCleanupWithinNonStructuredTrivia()
        => VerifyRange("""
            #Const ccConst = 0
            #If {|b:
            |}Then
            Imports System
            Imports System.Collections.Generic
            Imports System.Linq

            Module Program
                Sub Main(args As String())

                End Sub
            End Module
            """, LanguageNames.VisualBasic);

    [Fact]
    public async Task RangeWithTransformation_OutsideOfRange()
    {
        var transformer = new MockCodeCleanupProvider()
        {
            CleanupDocumentAsyncImpl = async (provider, document, spans, options, cancellationToken) =>
            {
                var root = await document.GetSyntaxRootAsync(cancellationToken);
                var member = root.GetMember(0).GetMember(0).GetMember(0);
                var previousToken = member.GetFirstToken().GetPreviousToken().GetPreviousToken();
                var nextToken = member.GetLastToken().GetNextToken().GetNextToken();

                root = root.ReplaceToken(previousToken, CSharp.SyntaxFactory.Identifier(previousToken.LeadingTrivia, previousToken.ValueText, previousToken.TrailingTrivia));
                root = root.ReplaceToken(nextToken, CSharp.SyntaxFactory.Token(nextToken.LeadingTrivia, CSharp.CSharpExtensions.Kind(nextToken), nextToken.TrailingTrivia));

                provider.ExpectedResult = [];

                return document.WithSyntaxRoot(root);
            }
        };

        await VerifyRange("namespace N { class C { {|b:void Method() { }|} } }", transformer);
    }

    public static CSharp.Syntax.MethodDeclarationSyntax CreateCSharpMethod(string returnType = "void", string methodName = "Method")
        => CSharp.SyntaxFactory.MethodDeclaration(CSharp.SyntaxFactory.ParseTypeName(returnType), CSharp.SyntaxFactory.Identifier(methodName));

    private static async Task VerifyRange(string codeWithMarker, string language = LanguageNames.CSharp)
    {
        MarkupTestFile.GetSpans(codeWithMarker,
            out var codeWithoutMarker, out IDictionary<string, ImmutableArray<TextSpan>> namedSpans);

        var expectedResult = namedSpans.TryGetValue("r", out var spans) ? spans : SpecializedCollections.EmptyEnumerable<TextSpan>();

        var transformer = new MockCodeCleanupProvider { ExpectedResult = expectedResult };

        await VerifyRange(codeWithoutMarker, [], namedSpans["b"], transformer, language);
    }

    private static async Task VerifyRange(string codeWithMarker, MockCodeCleanupProvider transformer, string language = LanguageNames.CSharp)
    {
        MarkupTestFile.GetSpans(codeWithMarker,
            out var codeWithoutMarker, out IDictionary<string, ImmutableArray<TextSpan>> namedSpans);

        await VerifyRange(codeWithoutMarker, [transformer], namedSpans["b"], transformer, language);
    }

    private static async Task VerifyRange(string code, ImmutableArray<ICodeCleanupProvider> codeCleanups, ImmutableArray<TextSpan> spans, MockCodeCleanupProvider transformer, string language)
    {
        var spanCodeCleanup = new MockCodeCleanupProvider()
        {
            CleanupDocumentAsyncImpl = (provider, document, spans, options, cancellationToken) =>
            {
                provider.ExpectedResult = spans;
                return Task.FromResult(document);
            }
        };

        var document = CreateDocument(code, language);

        await CodeCleaner.CleanupAsync(document, spans,
            await document.GetCodeCleanupOptionsAsync(CancellationToken.None),
            codeCleanups.Concat(spanCodeCleanup));

        var sortedSpans = spanCodeCleanup.ExpectedResult.ToList();
        var expectedSpans = transformer.ExpectedResult.ToList();

        sortedSpans.Sort();
        expectedSpans.Sort();

        AssertEx.Equal(expectedSpans, sortedSpans);
    }

    private static Document CreateDocument(string code, string language)
    {
        var solution = new AdhocWorkspace().CurrentSolution;
        var projectId = ProjectId.CreateNewId();
        var project = solution.AddProject(projectId, "Project", "Project.dll", language).GetProject(projectId);

        return project.AddDocument("Document", SourceText.From(code));
    }
}
