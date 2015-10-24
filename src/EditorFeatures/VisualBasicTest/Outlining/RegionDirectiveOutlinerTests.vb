' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.Outlining
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Editor.Implementation.Outlining

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Outlining
    Public Class RegionDirectiveOutlinerTests
        Inherits AbstractOutlinerTests(Of RegionDirectiveTriviaSyntax)

        Friend Overrides Function GetRegions(regionDirective As RegionDirectiveTriviaSyntax) As IEnumerable(Of OutliningSpan)
            Dim outliner As New RegionDirectiveOutliner
            Return outliner.GetOutliningSpans(regionDirective, CancellationToken.None).WhereNotNull()
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub BrokenRegion()
            Dim syntaxTree = ParseLines("#Region ""Foo""")

            Dim directiveTrivia = TryCast(syntaxTree.GetCompilationUnitRoot().EndOfFileToken.LeadingTrivia.ElementAt(0).GetStructure(), DirectiveTriviaSyntax)
            Assert.NotNull(directiveTrivia)

            Dim regionDirective = TryCast(directiveTrivia, RegionDirectiveTriviaSyntax)
            Assert.NotNull(regionDirective)

            Dim actualRegions = GetRegions(regionDirective).ToList()
            Assert.Equal(0, actualRegions.Count)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub SimpleRegion()
            Dim syntaxTree = ParseLines("#Region ""Foo""",
                                  "#End Region")

            Dim directiveTrivia = TryCast(DirectCast(syntaxTree.GetCompilationUnitRoot(), CompilationUnitSyntax).EndOfFileToken.LeadingTrivia.ElementAt(0).GetStructure(), DirectiveTriviaSyntax)
            Assert.NotNull(directiveTrivia)

            Dim regionDirective = TryCast(directiveTrivia, RegionDirectiveTriviaSyntax)
            Assert.NotNull(regionDirective)

            Dim actualRegion = GetRegion(regionDirective)
            Dim expectedRegion = New OutliningSpan(
                                     textSpan:=TextSpan.FromBounds(0, 26),
                                     bannerText:="Foo",
                                     hintSpan:=TextSpan.FromBounds(0, 26),
                                     autoCollapse:=False,
                                     isDefaultCollapsed:=True)

            AssertRegion(expectedRegion, actualRegion)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub RegionWithNoBanner1()
            Dim syntaxTree = ParseLines("#Region",
                                  "#End Region")

            Dim directiveTrivia = TryCast(syntaxTree.GetCompilationUnitRoot().EndOfFileToken.LeadingTrivia.ElementAt(0).GetStructure(), DirectiveTriviaSyntax)
            Assert.NotNull(directiveTrivia)

            Dim regionDirective = TryCast(directiveTrivia, RegionDirectiveTriviaSyntax)
            Assert.NotNull(regionDirective)

            Dim actualRegion = GetRegion(regionDirective)
            Dim expectedRegion = New OutliningSpan(
                                     textSpan:=TextSpan.FromBounds(0, 20),
                                     bannerText:="#Region",
                                     hintSpan:=TextSpan.FromBounds(0, 20),
                                     autoCollapse:=False,
                                     isDefaultCollapsed:=True)

            AssertRegion(expectedRegion, actualRegion)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub RegionWithNoBanner2()
            Dim syntaxTree = ParseLines("#Region """"",
                                  "#End Region")

            Dim directiveTrivia = TryCast(syntaxTree.GetCompilationUnitRoot().EndOfFileToken.LeadingTrivia.ElementAt(0).GetStructure(), DirectiveTriviaSyntax)
            Assert.NotNull(directiveTrivia)

            Dim regionDirective = TryCast(directiveTrivia, RegionDirectiveTriviaSyntax)
            Assert.NotNull(regionDirective)

            Dim actualRegion = GetRegion(regionDirective)
            Dim expectedRegion = New OutliningSpan(
                                     textSpan:=TextSpan.FromBounds(0, 23),
                                     bannerText:="#Region",
                                     hintSpan:=TextSpan.FromBounds(0, 23),
                                     autoCollapse:=False,
                                     isDefaultCollapsed:=True)

            AssertRegion(expectedRegion, actualRegion)
        End Sub

        <WorkItem(537984)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub RegionEndOfFile()
            Dim syntaxTree = ParseLines("Class C",
                                "End CLass",
                                "#Region",
                                "#End Region"
                                )

            Dim directiveTrivia = TryCast(syntaxTree.GetCompilationUnitRoot().EndOfFileToken.LeadingTrivia.ElementAt(0).GetStructure(), DirectiveTriviaSyntax)
            Assert.NotNull(directiveTrivia)

            Dim regionDirective = TryCast(directiveTrivia, RegionDirectiveTriviaSyntax)
            Assert.NotNull(regionDirective)

            Dim actualRegion = GetRegion(regionDirective)
            Dim expectedRegion = New OutliningSpan(
                               textSpan:=TextSpan.FromBounds(20, 40),
                               bannerText:="#Region",
                               hintSpan:=TextSpan.FromBounds(20, 40),
                               autoCollapse:=False,
                               isDefaultCollapsed:=True)

            AssertRegion(expectedRegion, actualRegion)
        End Sub
    End Class
End Namespace
