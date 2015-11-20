' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Editor.Implementation.Outlining
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Outlining
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.Outlining
Imports Microsoft.CodeAnalysis.Text

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Outlining
    Public Class CommentTests
        Inherits AbstractSyntaxOutlinerTests

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestSimpleComment1()
            Dim tree = ParseLines("' Hello",
                                  "' VB!",
                                  "Class C1",
                                  "End Class")

            Dim typeBlock = tree.DigToFirstTypeBlock()
            Dim trivia = typeBlock.GetLeadingTrivia()
            Assert.Equal(4, trivia.Count)

            Dim regions = VisualBasicOutliningHelpers.CreateCommentsRegions(trivia).ToList()
            Assert.Equal(1, regions.Count)

            Dim actualRegion = regions(0)
            Dim expectedRegion = New OutliningSpan(
                         TextSpan.FromBounds(0, 14),
                         "' Hello ...",
                         autoCollapse:=True)

            AssertRegion(expectedRegion, actualRegion)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestSimpleComment2()
            Dim tree = ParseLines("' Hello",
                                  "'",
                                  "' VB!",
                                  "Class C1",
                                  "End Class")

            Dim typeBlock = tree.DigToFirstTypeBlock()
            Dim trivia = typeBlock.GetLeadingTrivia()
            Assert.Equal(6, trivia.Count)

            Dim regions = VisualBasicOutliningHelpers.CreateCommentsRegions(trivia).ToList()
            Assert.Equal(1, regions.Count)

            Dim actualRegion = regions(0)
            Dim expectedRegion = New OutliningSpan(
                         TextSpan.FromBounds(0, 17),
                         "' Hello ...",
                         autoCollapse:=True)

            AssertRegion(expectedRegion, actualRegion)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestSimpleComment3()
            Dim tree = ParseLines("' Hello",
                                  "",
                                  "' VB!",
                                  "Class C1",
                                  "End Class")

            Dim typeBlock = tree.DigToFirstTypeBlock()
            Dim trivia = typeBlock.GetLeadingTrivia()
            Assert.Equal(5, trivia.Count)

            Dim regions = VisualBasicOutliningHelpers.CreateCommentsRegions(trivia).ToList()
            Assert.Equal(1, regions.Count)

            Dim actualRegion = regions(0)
            Dim expectedRegion = New OutliningSpan(
                         TextSpan.FromBounds(0, 16),
                         "' Hello ...",
                         autoCollapse:=True)

            AssertRegion(expectedRegion, actualRegion)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestSingleLineCommentGroupFollowedByDocumentationComment()
            Dim tree = ParseLines("' Hello",
                                  "",
                                  "' VB!",
                                  "''' <summary></summary>",
                                  "Class C1",
                                  "End Class")

            Dim typeBlock = tree.DigToFirstTypeBlock()
            Dim trivia = typeBlock.GetLeadingTrivia()
            Assert.Equal(6, trivia.Count)

            Dim regions = VisualBasicOutliningHelpers.CreateCommentsRegions(trivia).ToList()
            Assert.Equal(1, regions.Count)

            Dim actualRegion = regions(0)
            Dim expectedRegion = New OutliningSpan(
                         TextSpan.FromBounds(0, 16),
                         "' Hello ...",
                         autoCollapse:=True)

            AssertRegion(expectedRegion, actualRegion)
        End Sub
    End Class
End Namespace
