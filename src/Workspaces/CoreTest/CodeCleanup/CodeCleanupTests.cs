// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.CodeCleanup.Providers;
using Microsoft.CodeAnalysis.SemanticModelWorkspaceService;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.CodeCleanup
{
    using CSharp = Microsoft.CodeAnalysis.CSharp;

    public class CodeCleanupTests
    {
#if false
        [Fact]
        public void DefaultCSharpCodeCleanups()
        {
            var codeCleanups = CodeCleaner.GetDefaultProviders(LanguageNames.CSharp);
            Assert.NotNull(codeCleanups);
            Assert.NotEmpty(codeCleanups);
        }

        [Fact]
        public void DefaultVisualBasicCodeCleanups()
        {
            var codeCleanups = CodeCleaner.GetDefaultProviders(LanguageNames.VisualBasic);
            Assert.NotNull(codeCleanups);
            Assert.NotEmpty(codeCleanups);
        }
#endif

        [Fact]
        public async Task CodeCleaners_NoSpans()
        {
            var document = CreateDocument("class C { }", LanguageNames.CSharp);
            var cleanDocument = await CodeCleaner.CleanupAsync(document, SpecializedCollections.EmptyEnumerable<TextSpan>());

            Assert.Equal(document, cleanDocument);
        }

        [Fact]
        public async Task CodeCleaners_Document()
        {
            var document = CreateDocument("class C { }", LanguageNames.CSharp);
            var cleanDocument = await CodeCleaner.CleanupAsync(document);

            Assert.Equal(document, cleanDocument);
        }

        [Fact]
        public async Task CodeCleaners_Span()
        {
            var document = CreateDocument("class C { }", LanguageNames.CSharp);
            var cleanDocument = await CodeCleaner.CleanupAsync(document, (await document.GetSyntaxRootAsync()).FullSpan);

            Assert.Equal(document, cleanDocument);
        }

        [Fact]
        public async Task CodeCleaners_Spans()
        {
            var document = CreateDocument("class C { }", LanguageNames.CSharp);
            var cleanDocument = await CodeCleaner.CleanupAsync(document, SpecializedCollections.SingletonEnumerable(
                (await document.GetSyntaxRootAsync()).FullSpan));

            Assert.Equal(document, cleanDocument);
        }

        [Fact]
        public async Task CodeCleaners_Annotation()
        {
            var document = CreateDocument("class C { }", LanguageNames.CSharp);
            var annotation = new SyntaxAnnotation();
            document = document.WithSyntaxRoot((await document.GetSyntaxRootAsync()).WithAdditionalAnnotations(annotation));

            var cleanDocument = await CodeCleaner.CleanupAsync(document, annotation);

            Assert.Equal(document, cleanDocument);
        }

        [Fact]
        public void EntireRange()
        {
            VerifyRange("{|b:{|r:class C {}|}|}");
        }

        [Fact]
        public void EntireRange_Merge()
        {
            VerifyRange("{|r:class {|b:C { }|} class {|b: B { } |}|}");
        }

        [Fact]
        public void EntireRange_EndOfFile()
        {
            VerifyRange("{|r:class {|b:C { }|} class {|b: B { } |} |}");
        }

        [Fact]
        public void EntireRangeWithTransformation_RemoveClass()
        {
            var expectedResult = default(IEnumerable<TextSpan>);
            var transformer = new SimpleCodeCleanupProvider("TransformerCleanup", async (doc, spans, cancellationToken) =>
            {
                var root = await doc.GetSyntaxRootAsync().ConfigureAwait(false);
                root = root.RemoveCSharpMember(0);

                expectedResult = SpecializedCollections.SingletonEnumerable(root.FullSpan);

                return doc.WithSyntaxRoot(root);
            });

            VerifyRange("{|b:class C {}|}", transformer, ref expectedResult);
        }

        [Fact]
        public void EntireRangeWithTransformation_AddMember()
        {
            var expectedResult = default(IEnumerable<TextSpan>);
            var transformer = new SimpleCodeCleanupProvider("TransformerCleanup", async (doc, spans, cancellationToken) =>
            {
                var root = await doc.GetSyntaxRootAsync().ConfigureAwait(false);
                var @class = root.GetMember(0);
                var classWithMember = @class.AddCSharpMember(CreateCSharpMethod(), 0);
                root = root.ReplaceNode(@class, classWithMember);

                expectedResult = SpecializedCollections.SingletonEnumerable(root.FullSpan);

                return doc.WithSyntaxRoot(root);
            });

            VerifyRange("{|b:class C {}|}", transformer, ref expectedResult);
        }

        [Fact]
        public void RangeWithTransformation_AddMember()
        {
            var expectedResult = default(IEnumerable<TextSpan>);
            var transformer = new SimpleCodeCleanupProvider("TransformerCleanup", async (doc, spans, cancellationToken) =>
            {
                var root = await doc.GetSyntaxRootAsync().ConfigureAwait(false);
                var @class = root.GetMember(0).GetMember(0);
                var classWithMember = @class.AddCSharpMember(CreateCSharpMethod(), 0);
                root = root.ReplaceNode(@class, classWithMember);

                expectedResult = SpecializedCollections.SingletonEnumerable(root.GetMember(0).GetMember(0).GetCodeCleanupSpan());

                return doc.WithSyntaxRoot(root);
            });

            VerifyRange("namespace N { {|b:class C {}|} }", transformer, ref expectedResult);
        }

        [Fact]
        public void RangeWithTransformation_RemoveMember()
        {
            var expectedResult = default(IEnumerable<TextSpan>);
            var transformer = new SimpleCodeCleanupProvider("TransformerCleanup", async (doc, spans, cancellationToken) =>
            {
                var root = await doc.GetSyntaxRootAsync().ConfigureAwait(false);
                var @class = root.GetMember(0).GetMember(0);
                var classWithMember = @class.RemoveCSharpMember(0);
                root = root.ReplaceNode(@class, classWithMember);

                expectedResult = SpecializedCollections.SingletonEnumerable(root.GetMember(0).GetMember(0).GetCodeCleanupSpan());

                return doc.WithSyntaxRoot(root);
            });

            VerifyRange("namespace N { {|b:class C { void Method() { } }|} }", transformer, ref expectedResult);
        }

        [Fact]
        public void MultipleRange_Overlapped()
        {
            VerifyRange("namespace N {|r:{ {|b:class C { {|b:void Method() { }|} }|} }|}");
        }

        [Fact]
        public void MultipleRange_Adjacent()
        {
            VerifyRange("namespace N {|r:{ {|b:class C { |}{|b:void Method() { } }|} }|}");
        }

        [Fact]
        public void MultipleRanges()
        {
            VerifyRange("namespace N { class C {|r:{ {|b:void Method() { }|} }|} class C2 {|r:{ {|b:void Method() { }|} }|} }");
        }

        [Fact]
        [WorkItem(12848, "DevDiv_Projects/Roslyn")]
        public void DontCrash_VB()
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

        [Fact]
        [WorkItem(774295, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/774295")]
        public async Task DontCrash_VB_2()
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
            var factory = new SemanticModelWorkspaceServiceFactory();
            var service = (ISemanticModelService)factory.CreateService(document.Project.Solution.Workspace.Services);
            var newSemanticModel = await service.GetSemanticModelForNodeAsync(document, accessor, CancellationToken.None);
            Assert.NotNull(newSemanticModel);
            var newDocument = CreateDocument(code, LanguageNames.VisualBasic);
            var newRoot = await newDocument.GetSyntaxRootAsync();
            var newAccessor = newRoot.DescendantNodes().OfType<VisualBasic.Syntax.AccessorBlockSyntax>().Last();
            root = root.ReplaceNode(accessor, newAccessor);
            document = document.WithSyntaxRoot(root);
            accessor = root.DescendantNodes().OfType<VisualBasic.Syntax.AccessorBlockSyntax>().Last();
            newSemanticModel = await service.GetSemanticModelForNodeAsync(document, accessor, CancellationToken.None);
            Assert.NotNull(newSemanticModel);
            var cleanDocument = await CodeCleaner.CleanupAsync(document);
            Assert.Equal(document, cleanDocument);
        }

        [Fact]
        [WorkItem(547075, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/547075")]
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
            var expectedResult = default(IEnumerable<TextSpan>);
            var transformer = new SimpleCodeCleanupProvider("TransformerCleanup", async (doc, spans, cancellationToken) =>
            {
                var root = await doc.GetSyntaxRootAsync().ConfigureAwait(false);
                var member = root.GetMember(0).GetMember(0).GetMember(0);
                var previousToken = member.GetFirstToken().GetPreviousToken().GetPreviousToken();
                var nextToken = member.GetLastToken().GetNextToken().GetNextToken();

                root = root.ReplaceToken(previousToken, CSharp.SyntaxFactory.Identifier(previousToken.LeadingTrivia, previousToken.ValueText, previousToken.TrailingTrivia));
                root = root.ReplaceToken(nextToken, CSharp.SyntaxFactory.Token(nextToken.LeadingTrivia, CSharp.CSharpExtensions.Kind(nextToken), nextToken.TrailingTrivia));

                expectedResult = SpecializedCollections.EmptyEnumerable<TextSpan>();

                return doc.WithSyntaxRoot(root);
            });

            VerifyRange("namespace N { class C { {|b:void Method() { }|} } }", transformer, ref expectedResult);
        }

        public static CSharp.Syntax.MethodDeclarationSyntax CreateCSharpMethod(string returnType = "void", string methodName = "Method")
        {
            return CSharp.SyntaxFactory.MethodDeclaration(CSharp.SyntaxFactory.ParseTypeName(returnType), CSharp.SyntaxFactory.Identifier(methodName));
        }

        private void VerifyRange(string codeWithMarker, string language = LanguageNames.CSharp)
        {
            var codeWithoutMarker = default(string);
            var namedSpans = (IDictionary<string, IList<TextSpan>>)new Dictionary<string, IList<TextSpan>>();

            MarkupTestFile.GetSpans(codeWithMarker, out codeWithoutMarker, out namedSpans);

            var expectedResult = namedSpans.ContainsKey("r") ? namedSpans["r"] as IEnumerable<TextSpan> : SpecializedCollections.EmptyEnumerable<TextSpan>();

            VerifyRange(codeWithoutMarker, SpecializedCollections.EmptyEnumerable<ICodeCleanupProvider>(), namedSpans["b"], ref expectedResult, language);
        }

        private void VerifyRange(string codeWithMarker, ICodeCleanupProvider transformer, ref IEnumerable<TextSpan> expectedResult, string language = LanguageNames.CSharp)
        {
            var codeWithoutMarker = default(string);
            var namedSpans = (IDictionary<string, IList<TextSpan>>)new Dictionary<string, IList<TextSpan>>();

            MarkupTestFile.GetSpans(codeWithMarker, out codeWithoutMarker, out namedSpans);

            VerifyRange(codeWithoutMarker, SpecializedCollections.SingletonEnumerable(transformer), namedSpans["b"], ref expectedResult, language);
        }

        private void VerifyRange(string code, IEnumerable<ICodeCleanupProvider> codeCleanups, IEnumerable<TextSpan> spans, ref IEnumerable<TextSpan> expectedResult, string language)
        {
            var result = default(IEnumerable<TextSpan>);
            var spanCodeCleanup = new SimpleCodeCleanupProvider("TestCodeCleanup", (d, s, c) =>
            {
                result = s;
                return Task.FromResult(d);
            });

            var document = CreateDocument(code, language);

            CodeCleaner.CleanupAsync(document, spans, codeCleanups.Concat(spanCodeCleanup)).Wait();

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
