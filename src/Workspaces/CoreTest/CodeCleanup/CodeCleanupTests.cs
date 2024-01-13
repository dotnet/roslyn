// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.CodeCleanup.Providers;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.CodeCleanup
{
    using CSharp = Microsoft.CodeAnalysis.CSharp;

    [UseExportProvider]
    public class CodeCleanupTests
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
            var cleanDocument = await CodeCleaner.CleanupAsync(document, ImmutableArray<TextSpan>.Empty, CodeCleanupOptions.GetDefault(document.Project.Services));

            Assert.Equal(document, cleanDocument);
        }

        [Fact]
        public async Task CodeCleanersCSharp_Document()
        {
            var document = CreateDocument("class C { }", LanguageNames.CSharp);
            var cleanDocument = await CodeCleaner.CleanupAsync(document, CodeCleanupOptions.GetDefault(document.Project.Services));

            Assert.Equal(document, cleanDocument);
        }

        [Fact]
        public async Task CodeCleanersCSharp_Span()
        {
            var document = CreateDocument("class C { }", LanguageNames.CSharp);
            var root = await document.GetSyntaxRootAsync();
            var cleanDocument = await CodeCleaner.CleanupAsync(document, root.FullSpan, CodeCleanupOptions.GetDefault(document.Project.Services));

            Assert.Equal(document, cleanDocument);
        }

        [Fact]
        public async Task CodeCleanersCSharp_Spans()
        {
            var document = CreateDocument("class C { }", LanguageNames.CSharp);
            var root = await document.GetSyntaxRootAsync();
            var cleanDocument = await CodeCleaner.CleanupAsync(document, ImmutableArray.Create(root.FullSpan), CodeCleanupOptions.GetDefault(document.Project.Services));

            Assert.Equal(document, cleanDocument);
        }

        #endregion

        #region Visual Basic Code CodeCleaner Tests

        [Fact]
        public void DefaultVisualBasicCodeCleanups()
        {
            var document = CreateDocument(@"Class C
End Class", LanguageNames.VisualBasic);
            var codeCleanups = CodeCleaner.GetDefaultProviders(document);
            Assert.NotEmpty(codeCleanups);
        }

        [Fact]
        public async Task CodeCleanersVisualBasic_NoSpans()
        {
            var document = CreateDocument(@"Class C
End Class", LanguageNames.VisualBasic);
            var cleanDocument = await CodeCleaner.CleanupAsync(document, ImmutableArray<TextSpan>.Empty, CodeCleanupOptions.GetDefault(document.Project.Services));

            Assert.Equal(document, cleanDocument);
        }

        [Fact]
        public async Task CodeCleanersVisualBasic_Document()
        {
            var document = CreateDocument(@"Class C
End Class", LanguageNames.VisualBasic);
            var cleanDocument = await CodeCleaner.CleanupAsync(document, CodeCleanupOptions.GetDefault(document.Project.Services));

            Assert.Equal(document, cleanDocument);
        }

        [Fact]
        public async Task CodeCleanersVisualBasic_Span()
        {
            var document = CreateDocument(@"Class C
End Class", LanguageNames.VisualBasic);
            var root = await document.GetSyntaxRootAsync();
            var cleanDocument = await CodeCleaner.CleanupAsync(document, root.FullSpan, CodeCleanupOptions.GetDefault(document.Project.Services));

            Assert.Equal(document, cleanDocument);
        }

        [Fact]
        public async Task CodeCleanersVisualBasic_Spans()
        {
            var document = CreateDocument(@"Class C
End Class", LanguageNames.VisualBasic);
            var root = await document.GetSyntaxRootAsync();
            var cleanDocument = await CodeCleaner.CleanupAsync(document, ImmutableArray.Create(root.FullSpan), CodeCleanupOptions.GetDefault(document.Project.Services));

            Assert.Equal(document, cleanDocument);
        }

        [Fact]
        public async Task CodeCleanersCSharp_Annotation()
        {
            var document = CreateDocument("class C { }", LanguageNames.CSharp);
            var annotation = new SyntaxAnnotation();
            document = document.WithSyntaxRoot((await document.GetSyntaxRootAsync()).WithAdditionalAnnotations(annotation));

            var cleanDocument = await CodeCleaner.CleanupAsync(document, annotation, CodeCleanupOptions.GetDefault(document.Project.Services));

            Assert.Equal(document, cleanDocument);
        }

        [Fact]
        public async Task CodeCleanersVisualBasic_Annotation()
        {
            var document = CreateDocument(@"Class C
End Class", LanguageNames.VisualBasic);
            var annotation = new SyntaxAnnotation();
            document = document.WithSyntaxRoot((await document.GetSyntaxRootAsync()).WithAdditionalAnnotations(annotation));

            var cleanDocument = await CodeCleaner.CleanupAsync(document, annotation, CodeCleanupOptions.GetDefault(document.Project.Services));

            Assert.Equal(document, cleanDocument);
        }

        #endregion

        [Fact]
        public void EntireRange()
            => VerifyRange("{|b:{|r:class C {}|}|}");

        [Fact]
        public void EntireRange_Merge()
            => VerifyRange("{|r:class {|b:C { }|} class {|b: B { } |}|}");

        [Fact]
        public void EntireRange_EndOfFile()
            => VerifyRange("{|r:class {|b:C { }|} class {|b: B { } |} |}");

        [Fact]
        public void EntireRangeWithTransformation_RemoveClass()
        {
            var expectedResult = (IEnumerable<TextSpan>)null;
            var transformer = new MockCodeCleanupProvider()
            {
                CleanupDocumentAsyncImpl = async (document, spans, options, cancellationToken) =>
                {
                    var root = await document.GetSyntaxRootAsync(cancellationToken);
                    root = root.RemoveCSharpMember(0);

                    expectedResult = SpecializedCollections.SingletonEnumerable(root.FullSpan);

                    return document.WithSyntaxRoot(root);
                }
            };

            VerifyRange("{|b:class C {}|}", transformer, ref expectedResult);
        }

        [Fact]
        public void EntireRangeWithTransformation_AddMember()
        {
            var expectedResult = (IEnumerable<TextSpan>)null;
            var transformer = new MockCodeCleanupProvider()
            {
                CleanupDocumentAsyncImpl = async (document, spans, options, cancellationToken) =>
                {
                    var root = await document.GetSyntaxRootAsync(cancellationToken);
                    var @class = root.GetMember(0);
                    var classWithMember = @class.AddCSharpMember(CreateCSharpMethod(), 0);
                    root = root.ReplaceNode(@class, classWithMember);

                    expectedResult = SpecializedCollections.SingletonEnumerable(root.FullSpan);

                    return document.WithSyntaxRoot(root);
                }
            };

            VerifyRange("{|b:class C {}|}", transformer, ref expectedResult);
        }

        [Fact]
        public void RangeWithTransformation_AddMember()
        {
            var expectedResult = (IEnumerable<TextSpan>)null;
            var transformer = new MockCodeCleanupProvider()
            {
                CleanupDocumentAsyncImpl = async (document, spans, options, cancellationToken) =>
                {
                    var root = await document.GetSyntaxRootAsync(cancellationToken);
                    var @class = root.GetMember(0).GetMember(0);
                    var classWithMember = @class.AddCSharpMember(CreateCSharpMethod(), 0);
                    root = root.ReplaceNode(@class, classWithMember);

                    expectedResult = SpecializedCollections.SingletonEnumerable(root.GetMember(0).GetMember(0).GetCodeCleanupSpan());

                    return document.WithSyntaxRoot(root);
                }
            };

            VerifyRange("namespace N { {|b:class C {}|} }", transformer, ref expectedResult);
        }

        [Fact]
        public void RangeWithTransformation_RemoveMember()
        {
            var expectedResult = (IEnumerable<TextSpan>)null;
            var transformer = new MockCodeCleanupProvider()
            {
                CleanupDocumentAsyncImpl = async (document, spans, options, cancellationToken) =>
                {
                    var root = await document.GetSyntaxRootAsync(cancellationToken);
                    var @class = root.GetMember(0).GetMember(0);
                    var classWithMember = @class.RemoveCSharpMember(0);
                    root = root.ReplaceNode(@class, classWithMember);

                    expectedResult = SpecializedCollections.SingletonEnumerable(root.GetMember(0).GetMember(0).GetCodeCleanupSpan());

                    return document.WithSyntaxRoot(root);
                }
            };

            VerifyRange("namespace N { {|b:class C { void Method() { } }|} }", transformer, ref expectedResult);
        }

        [Fact]
        public void MultipleRange_Overlapped()
            => VerifyRange("namespace N {|r:{ {|b:class C { {|b:void Method() { }|} }|} }|}");

        [Fact]
        public void MultipleRange_Adjacent()
            => VerifyRange("namespace N {|r:{ {|b:class C { |}{|b:void Method() { } }|} }|}");

        [Fact]
        public void MultipleRanges()
            => VerifyRange("namespace N { class C {|r:{ {|b:void Method() { }|} }|} class C2 {|r:{ {|b:void Method() { }|} }|} }");

        [Fact, WorkItem(12848, "DevDiv_Projects/Roslyn")]
        public void DoNotCrash_VB()
        {
            var code = @"#If DEBUG OrElse TRACE Then
Imports System.Diagnostics
#ElseIf SILVERLIGHT Then
Imports System.Diagnostics
#Else
Imports System.Diagnostics
#End If

{|r:# {|b: |}
    Region|} ""Region""
#Region ""more""
#End Region 
#End Region";

            VerifyRange(code, LanguageNames.VisualBasic);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/774295")]
        public async Task DoNotCrash_VB_2()
        {
            var code = @"
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
";
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

            var cleanDocument = await CodeCleaner.CleanupAsync(document, CodeCleanupOptions.GetDefault(document.Project.Services));
            Assert.Equal(document, cleanDocument);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/547075")]
        public void TestCodeCleanupWithinNonStructuredTrivia()
        {
            var code = @"
#Const ccConst = 0
#If {|b:
|}Then
Imports System
Imports System.Collections.Generic
Imports System.Linq
 
Module Program
    Sub Main(args As String())
 
    End Sub
End Module";

            VerifyRange(code, LanguageNames.VisualBasic);
        }

        [Fact]
        public void RangeWithTransformation_OutsideOfRange()
        {
            var expectedResult = (IEnumerable<TextSpan>)null;
            var transformer = new MockCodeCleanupProvider()
            {
                CleanupDocumentAsyncImpl = async (document, spans, options, cancellationToken) =>
                {
                    var root = await document.GetSyntaxRootAsync(cancellationToken);
                    var member = root.GetMember(0).GetMember(0).GetMember(0);
                    var previousToken = member.GetFirstToken().GetPreviousToken().GetPreviousToken();
                    var nextToken = member.GetLastToken().GetNextToken().GetNextToken();

                    root = root.ReplaceToken(previousToken, CSharp.SyntaxFactory.Identifier(previousToken.LeadingTrivia, previousToken.ValueText, previousToken.TrailingTrivia));
                    root = root.ReplaceToken(nextToken, CSharp.SyntaxFactory.Token(nextToken.LeadingTrivia, CSharp.CSharpExtensions.Kind(nextToken), nextToken.TrailingTrivia));

                    expectedResult = SpecializedCollections.EmptyEnumerable<TextSpan>();

                    return document.WithSyntaxRoot(root);
                }
            };

            VerifyRange("namespace N { class C { {|b:void Method() { }|} } }", transformer, ref expectedResult);
        }

        public static CSharp.Syntax.MethodDeclarationSyntax CreateCSharpMethod(string returnType = "void", string methodName = "Method")
            => CSharp.SyntaxFactory.MethodDeclaration(CSharp.SyntaxFactory.ParseTypeName(returnType), CSharp.SyntaxFactory.Identifier(methodName));

        private static void VerifyRange(string codeWithMarker, string language = LanguageNames.CSharp)
        {
            MarkupTestFile.GetSpans(codeWithMarker,
                out var codeWithoutMarker, out IDictionary<string, ImmutableArray<TextSpan>> namedSpans);

            var expectedResult = namedSpans.TryGetValue("r", out var spans) ? spans : SpecializedCollections.EmptyEnumerable<TextSpan>();

            VerifyRange(codeWithoutMarker, ImmutableArray<ICodeCleanupProvider>.Empty, namedSpans["b"], ref expectedResult, language);
        }

        private static void VerifyRange(string codeWithMarker, ICodeCleanupProvider transformer, ref IEnumerable<TextSpan> expectedResult, string language = LanguageNames.CSharp)
        {
            MarkupTestFile.GetSpans(codeWithMarker,
                out var codeWithoutMarker, out IDictionary<string, ImmutableArray<TextSpan>> namedSpans);

            VerifyRange(codeWithoutMarker, ImmutableArray.Create(transformer), namedSpans["b"], ref expectedResult, language);
        }

        private static void VerifyRange(string code, ImmutableArray<ICodeCleanupProvider> codeCleanups, ImmutableArray<TextSpan> spans, ref IEnumerable<TextSpan> expectedResult, string language)
        {
            var result = (IEnumerable<TextSpan>)null;
            var spanCodeCleanup = new MockCodeCleanupProvider()
            {
                CleanupDocumentAsyncImpl = (document, spans, options, cancellationToken) =>
                {
                    result = spans;
                    return Task.FromResult(document);
                }
            };

            var document = CreateDocument(code, language);

            CodeCleaner.CleanupAsync(document, spans, CodeCleanupOptions.GetDefault(document.Project.Services), codeCleanups.Concat(spanCodeCleanup)).Wait();

            var sortedSpans = result.ToList();
            var expectedSpans = expectedResult.ToList();

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
}
