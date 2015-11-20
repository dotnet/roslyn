' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.Outlining
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Editor.Implementation.Outlining

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Outlining
    Public Class DocumentationCommentOutlinerTests
        Inherits AbstractVisualBasicSyntaxOutlinerTests(Of DocumentationCommentTriviaSyntax)

        Friend Overrides Function GetRegions(documentationComment As DocumentationCommentTriviaSyntax) As IEnumerable(Of OutliningSpan)
            Dim outliner As New DocumentationCommentOutliner
            Return outliner.GetOutliningSpans(documentationComment, CancellationToken.None).WhereNotNull()
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestDocumentationCommentWithoutSummaryTag1()
            Dim syntaxTree = ParseLines("''' XML doc comment",
                                  "''' some description",
                                  "''' of",
                                  "''' the comment",
                                  "Class C1",
                                  "End Class")

            Dim typeBlock = syntaxTree.DigToFirstTypeBlock()
            Dim trivia = typeBlock.GetLeadingTrivia().ToList()
            Assert.Equal(1, trivia.Count)

            Dim documentationComment = TryCast(trivia(0).GetStructure(), DocumentationCommentTriviaSyntax)
            Assert.NotNull(documentationComment)

            Dim actualRegion = GetRegion(documentationComment)
            Dim expectedRegion = New OutliningSpan(
                                     TextSpan.FromBounds(0, 66),
                                     "''' XML doc comment ...",
                                     autoCollapse:=True)

            AssertRegion(expectedRegion, actualRegion)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestDocumentationCommentWithoutSummaryTag2()
            Dim syntaxTree = ParseLines("''' <param name=""syntaxTree""></param>",
                                  "Class C1",
                                  "End Class")

            Dim typeBlock = syntaxTree.DigToFirstTypeBlock()
            Dim trivia = typeBlock.GetLeadingTrivia().ToList()
            Assert.Equal(1, trivia.Count)

            Dim documentationComment = TryCast(trivia(0).GetStructure(), DocumentationCommentTriviaSyntax)
            Assert.NotNull(documentationComment)

            Dim actualRegion = GetRegion(documentationComment)
            Dim expectedRegion = New OutliningSpan(
                                     TextSpan.FromBounds(0, 37),
                                     "''' <param name=""syntaxTree""></param> ...",
                                     autoCollapse:=True)

            AssertRegion(expectedRegion, actualRegion)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestDocumentationComment()
            Dim syntaxTree = ParseLines("''' <summary>",
                                  "''' Hello VB!",
                                  "''' </summary>",
                                  "Class C1",
                                  "End Class")

            Dim typeBlock = syntaxTree.DigToFirstTypeBlock()
            Dim trivia = typeBlock.GetLeadingTrivia().ToList()
            Assert.Equal(1, trivia.Count)

            Dim documentationComment = TryCast(trivia(0).GetStructure(), DocumentationCommentTriviaSyntax)
            Assert.NotNull(documentationComment)

            Dim actualRegion = GetRegion(documentationComment)
            Dim expectedRegion = New OutliningSpan(
                                     TextSpan.FromBounds(0, 44),
                                     "''' <summary> Hello VB!",
                                     autoCollapse:=True)

            AssertRegion(expectedRegion, actualRegion)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestDocumentationCommentWithLongBannerText()
            Dim syntaxTree = ParseLines("''' <summary>",
                                  "''' " & New String("x"c, 240),
                                  "''' </summary>",
                                  "Class C1",
                                  "End Class")

            Dim typeBlock = syntaxTree.DigToFirstTypeBlock()
            Dim trivia = typeBlock.GetLeadingTrivia().ToList()
            Assert.Equal(1, trivia.Count)

            Dim documentationComment = TryCast(trivia(0).GetStructure(), DocumentationCommentTriviaSyntax)
            Assert.NotNull(documentationComment)

            Dim actualRegion = GetRegion(documentationComment)
            Dim expectedCollapsedFormString = "''' <summary> " & New String("x"c, 106) & " ..."
            Dim expectedRegion = New OutliningSpan(
                                     TextSpan.FromBounds(0, 275),
                                     expectedCollapsedFormString,
                                     autoCollapse:=True)

            AssertRegion(expectedRegion, actualRegion)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestIndentedDocumentationComment()
            Dim syntaxTree = ParseLines("    ''' <summary>",
                                  "    ''' Hello VB!",
                                  "    ''' </summary>",
                                  "    Class C1",
                                  "    End Class")

            Dim typeBlock = syntaxTree.DigToFirstTypeBlock()
            Dim trivia = typeBlock.GetLeadingTrivia().ToList()
            Assert.Equal(3, trivia.Count)

            Dim documentationComment = TryCast(trivia(1).GetStructure(), DocumentationCommentTriviaSyntax)
            Assert.NotNull(documentationComment)

            Dim actualRegion = GetRegion(documentationComment)
            Dim expectedRegion = New OutliningSpan(
                                     TextSpan.FromBounds(4, 56),
                                     "''' <summary> Hello VB!",
                                     autoCollapse:=True)

            AssertRegion(expectedRegion, actualRegion)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestDocumentationCommentOnASingleLine()
            Dim syntaxTree = ParseLines("''' <summary>Hello VB!</summary>",
                                  "Class C1",
                                  "End Class")

            Dim typeBlock = syntaxTree.DigToFirstTypeBlock()
            Dim trivia = typeBlock.GetLeadingTrivia().ToList()
            Assert.Equal(1, trivia.Count)

            Dim documentationComment = TryCast(trivia(0).GetStructure(), DocumentationCommentTriviaSyntax)
            Assert.NotNull(documentationComment)

            Dim actualRegion = GetRegion(documentationComment)
            Dim expectedRegion = New OutliningSpan(
                                     TextSpan.FromBounds(0, 32),
                                     "''' <summary> Hello VB!",
                                     autoCollapse:=True)

            AssertRegion(expectedRegion, actualRegion)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestIndentedDocumentationCommentOnASingleLine()
            Dim syntaxTree = ParseLines("    ''' <summary>Hello VB!</summary>",
                                  "    Class C1",
                                  "    End Class")

            Dim typeBlock = syntaxTree.DigToFirstTypeBlock()
            Dim trivia = typeBlock.GetLeadingTrivia().ToList()
            Assert.Equal(3, trivia.Count)

            Dim documentationComment = TryCast(trivia(1).GetStructure(), DocumentationCommentTriviaSyntax)
            Assert.NotNull(documentationComment)

            Dim actualRegion = GetRegion(documentationComment)
            Dim expectedRegion = New OutliningSpan(
                                     TextSpan.FromBounds(4, 36),
                                     "''' <summary> Hello VB!",
                                     autoCollapse:=True)

            AssertRegion(expectedRegion, actualRegion)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestMultilineSummaryInDocumentationComment1()
            Dim syntaxTree = ParseLines("''' <summary>",
                                  "''' Hello",
                                  "''' VB!",
                                  "''' </summary>",
                                  "Class C1",
                                  "End Class")

            Dim typeBlock = syntaxTree.DigToFirstTypeBlock()
            Dim trivia = typeBlock.GetLeadingTrivia().ToList()
            Assert.Equal(1, trivia.Count)

            Dim documentationComment = TryCast(trivia(0).GetStructure(), DocumentationCommentTriviaSyntax)
            Assert.NotNull(documentationComment)

            Dim actualRegion = GetRegion(documentationComment)
            Dim expectedRegion = New OutliningSpan(
                                     TextSpan.FromBounds(0, 49),
                                     "''' <summary> Hello VB!",
                                     autoCollapse:=True)

            AssertRegion(expectedRegion, actualRegion)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestMultilineSummaryInDocumentationComment2()
            Dim syntaxTree = ParseLines("''' <summary>",
                                  "''' Hello",
                                  "''' ",
                                  "''' VB!",
                                  "''' </summary>",
                                  "Class C1",
                                  "End Class")

            Dim typeBlock = syntaxTree.DigToFirstTypeBlock()
            Dim trivia = typeBlock.GetLeadingTrivia().ToList()
            Assert.Equal(1, trivia.Count)

            Dim documentationComment = TryCast(trivia(0).GetStructure(), DocumentationCommentTriviaSyntax)
            Assert.NotNull(documentationComment)

            Dim actualRegion = GetRegion(documentationComment)
            Dim expectedRegion = New OutliningSpan(
                                     TextSpan.FromBounds(0, 55),
                                     "''' <summary> Hello VB!",
                                     autoCollapse:=True)

            AssertRegion(expectedRegion, actualRegion)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        <WorkItem(2129, "https://github.com/dotnet/roslyn/issues/2129")>
        Public Sub CrefInSummary()
            Dim tree = ParseLines("Class C",
                                  "    ''' <summary>",
                                  "    ''' Summary with <see cref=""SeeClass"" />, <seealso cref=""SeeAlsoClass"" />,",
                                  "    ''' <see langword=""Nothing"" />, <typeparamref name=""T"" />, <paramref name=""t"" />, and <see unsupported-attribute=""not-supported"" />.",
                                  "    ''' </summary>",
                                  "    Sub M(Of T)(t as T)",
                                  "    End Sub",
                                  "End Class")

            Dim methodBlock = tree.GetRoot().FindFirstNodeOfType(Of MethodBlockSyntax)()
            Dim trivia = methodBlock.GetLeadingTrivia()

            Dim docComment = DirectCast(trivia.Single(Function(t) t.HasStructure).GetStructure(), DocumentationCommentTriviaSyntax)
            Dim actualRegion = GetRegion(docComment)
            Dim expectedRegion = New OutliningSpan(
                         TextSpan.FromBounds(13, 264),
                         "''' <summary> Summary with SeeClass , SeeAlsoClass , Nothing , T , t , and not-supported .",
                         autoCollapse:=True)

            AssertRegion(expectedRegion, actualRegion)
        End Sub
    End Class
End Namespace
