' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.Outlining
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Editor.Implementation.Outlining

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Outlining

    Public Class CompilationUnitOutlinerTests
        Inherits AbstractVisualBasicSyntaxOutlinerTests(Of CompilationUnitSyntax)

        Friend Overrides Function GetRegions(compilationUnit As CompilationUnitSyntax) As IEnumerable(Of OutliningSpan)
            Dim outliner As New CompilationUnitOutliner
            Return outliner.GetOutliningSpans(compilationUnit, CancellationToken.None).WhereNotNull()
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestImports()
            Dim tree = ParseLines("Imports System",
                                  "Imports System.Linq",
                                  "Class C1",
                                  "End Class")

            Dim compilationUnit = DirectCast(tree.GetRoot(), CompilationUnitSyntax)
            Assert.NotNull(compilationUnit)

            Dim actualRegion = GetRegion(compilationUnit)
            Dim expectedRegion = New OutliningSpan(
                                     TextSpan.FromBounds(0, 35),
                                     bannerText:="Imports ...",
                                     autoCollapse:=True)

            AssertRegion(expectedRegion, actualRegion)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestImportsAliases()
            Dim tree = ParseLines("Imports System",
                                  "Imports linq = System.Linq",
                                  "Class C1",
                                  "End Class")

            Dim compilationUnit = tree.GetCompilationUnitRoot()
            Assert.NotNull(compilationUnit)

            Dim actualRegion = GetRegion(compilationUnit)
            Dim expectedRegion = New OutliningSpan(
                                     TextSpan.FromBounds(0, 42),
                                     bannerText:="Imports ...",
                                     autoCollapse:=True)

            AssertRegion(expectedRegion, actualRegion)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestComments()
            Dim tree = ParseLines("'Top",
                                  "'Of",
                                  "'File",
                                  "Class C",
                                  "End Class",
                                  "'Bottom",
                                  "'Of",
                                  "'File")

            Dim compilationUnit = tree.GetCompilationUnitRoot()
            Assert.NotNull(compilationUnit)

            Dim actualRegions = GetRegions(compilationUnit).ToList()
            Assert.Equal(2, actualRegions.Count)

            Dim expectedRegion1 = New OutliningSpan(
                                      TextSpan.FromBounds(0, 16),
                                      "' Top ...",
                                      autoCollapse:=True)
            AssertRegion(expectedRegion1, actualRegions(0))


            Dim expectedRegion2 = New OutliningSpan(
                                      TextSpan.FromBounds(38, 57),
                                      "' Bottom ...",
                                      autoCollapse:=True)
            AssertRegion(expectedRegion2, actualRegions(1))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestImportsAndComments()
            Dim tree = ParseLines("'Top",
                                  "'Of",
                                  "'File",
                                  "Imports System",
                                  "Imports System.Linq",
                                  "'Bottom",
                                  "'Of",
                                  "'File")

            Dim compilationUnit = tree.GetCompilationUnitRoot()
            Assert.NotNull(compilationUnit)

            Dim actualRegions = GetRegions(compilationUnit).ToList()
            Assert.Equal(3, actualRegions.Count)

            Dim expectedRegion1 = New OutliningSpan(
                                      TextSpan.FromBounds(0, 16),
                                      "' Top ...",
                                      autoCollapse:=True)
            AssertRegion(expectedRegion1, actualRegions(0))

            Dim expectedRegion2 = New OutliningSpan(
                                      TextSpan.FromBounds(18, 53),
                                      bannerText:="Imports ...",
                                      autoCollapse:=True)
            AssertRegion(expectedRegion2, actualRegions(1))

            Dim expectedRegion3 = New OutliningSpan(
                                      TextSpan.FromBounds(55, 74),
                                      "' Bottom ...",
                                      autoCollapse:=True)
            AssertRegion(expectedRegion3, actualRegions(2))
        End Sub

    End Class

End Namespace
