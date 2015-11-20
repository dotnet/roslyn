' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.Implementation.Outlining
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Outlining
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.Outlining
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Outlining
    Public Class DisabledCodeOutlinerTests
        Inherits AbstractSyntaxOutlinerTests

        Private Function GetRegions(syntaxTree As SyntaxTree, trivia As SyntaxTrivia) As IEnumerable(Of OutliningSpan)
            Dim outliner As New DisabledTextTriviaOutliner
            Dim spans = New List(Of OutliningSpan)
            outliner.CollectOutliningSpans(syntaxTree, trivia, spans, CancellationToken.None)
            Return spans.WhereNotNull()
        End Function

        Private Function GetRegion(syntaxTree As SyntaxTree, trivia As SyntaxTrivia) As OutliningSpan
            Dim regions = GetRegions(syntaxTree, trivia).ToList()
            Assert.Equal(1, regions.Count())

            Return regions(0)
        End Function


        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestWithoutWrongTrivia()
            Dim tree = ParseLines("#If False",
                                  "Blah",
                                  "Blah",
                                  "Blah",
                                  "#End If")

            Dim trivia = tree.GetCompilationUnitRoot().EndOfFileToken.LeadingTrivia.ElementAt(0)

            Dim regions = GetRegions(tree, trivia).ToList()
            Assert.Equal(0, regions.Count)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestDisabledIf()
            Dim tree = ParseLines("#If False",
                                  "Blah",
                                  "Blah",
                                  "Blah",
                                  "#End If")

            Dim trivia = tree.GetCompilationUnitRoot().EndOfFileToken.LeadingTrivia.ElementAt(1)

            Dim actualRegion = GetRegion(tree, trivia)
            Dim expectedRegion = New OutliningSpan(
                                     TextSpan.FromBounds(11, 27),
                                     "...",
                                     autoCollapse:=True)

            AssertRegion(expectedRegion, actualRegion)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestDisabledElse()
            Dim tree = ParseLines("#If True",
                                  "#Else",
                                  "Blah",
                                  "Blah",
                                  "Blah",
                                  "#End If")

            Dim trivia = tree.GetCompilationUnitRoot().EndOfFileToken.LeadingTrivia.ElementAt(2)

            Dim actualRegion = GetRegion(tree, trivia)
            Dim expectedRegion = New OutliningSpan(
                                     TextSpan.FromBounds(17, 33),
                                     "...",
                                     autoCollapse:=True)

            AssertRegion(expectedRegion, actualRegion)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestDisabledElseIf()
            Dim tree = ParseLines("#If True",
                                  "#ElseIf False",
                                  "Blah",
                                  "Blah",
                                  "Blah",
                                  "#Else",
                                  "#End If")

            Dim trivia = tree.GetCompilationUnitRoot().EndOfFileToken.LeadingTrivia.ElementAt(2)

            Dim actualRegion = GetRegion(tree, trivia)
            Dim expectedRegion = New OutliningSpan(
                                     TextSpan.FromBounds(25, 41),
                                     "...",
                                     autoCollapse:=True)

            AssertRegion(expectedRegion, actualRegion)
        End Sub

    End Class
End Namespace
