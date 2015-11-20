' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.Outlining
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Editor.Implementation.Outlining

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Outlining
    Public Class NamespaceDeclarationOutlinerTests
        Inherits AbstractVisualBasicSyntaxOutlinerTests(Of NamespaceStatementSyntax)

        Friend Overrides Function GetRegions(namespaceDeclaration As NamespaceStatementSyntax) As IEnumerable(Of OutliningSpan)
            Dim outliner As New NamespaceDeclarationOutliner
            Return outliner.GetOutliningSpans(namespaceDeclaration, CancellationToken.None).WhereNotNull()
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestNamespace()
            Dim syntaxTree = ParseLines("Namespace N1",
                                  "End Namespace")

            Dim namespaceBlock = syntaxTree.DigToFirstNamespace()
            Dim namespaceDecl = namespaceBlock.NamespaceStatement
            Dim actualRegion = GetRegion(namespaceDecl)
            Dim expectedRegion = New OutliningSpan(
                                     TextSpan:=TextSpan.FromBounds(0, 27),
                                     bannerText:="Namespace N1 ...",
                                     hintSpan:=TextSpan.FromBounds(0, 27),
                                     autoCollapse:=False)

            AssertRegion(expectedRegion, actualRegion)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestNamespaceWithComments()
            Dim syntaxTree = ParseLines("'My",
                                  "'Namespace",
                                  "Namespace N1",
                                  "End Namespace")

            Dim namespaceBlock = syntaxTree.DigToFirstNamespace()
            Dim namespaceDecl = namespaceBlock.NamespaceStatement

            Dim actualRegions = GetRegions(namespaceDecl).ToList()
            Assert.Equal(2, actualRegions.Count)

            Dim expectedRegion1 = New OutliningSpan(
                                     TextSpan:=TextSpan.FromBounds(0, 15),
                                     bannerText:="' My ...",
                                     hintSpan:=TextSpan.FromBounds(0, 15),
                                     autoCollapse:=True)
            AssertRegion(expectedRegion1, actualRegions(0))

            Dim expectedRegion2 = New OutliningSpan(
                                     TextSpan:=TextSpan.FromBounds(17, 44),
                                     bannerText:="Namespace N1 ...",
                                     hintSpan:=TextSpan.FromBounds(17, 44),
                                     autoCollapse:=False)
            AssertRegion(expectedRegion2, actualRegions(1))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestNamespaceWithNestedComments()
            Dim syntaxTree = ParseLines("Namespace N1",
                                  "'Hello",
                                  "'World",
                                  "End Namespace")

            Dim namespaceBlock = syntaxTree.DigToFirstNamespace()
            Dim namespaceDecl = namespaceBlock.NamespaceStatement

            Dim actualRegions = GetRegions(namespaceDecl).ToList()
            Assert.Equal(2, actualRegions.Count)

            Dim expectedRegion1 = New OutliningSpan(
                                     TextSpan:=TextSpan.FromBounds(0, 43),
                                     bannerText:="Namespace N1 ...",
                                     hintSpan:=TextSpan.FromBounds(0, 43),
                                     autoCollapse:=False)
            AssertRegion(expectedRegion1, actualRegions(0))

            Dim expectedRegion2 = New OutliningSpan(
                                     TextSpan:=TextSpan.FromBounds(14, 28),
                                     bannerText:="' Hello ...",
                                     hintSpan:=TextSpan.FromBounds(14, 28),
                                     autoCollapse:=True)
            AssertRegion(expectedRegion2, actualRegions(1))
        End Sub
    End Class
End Namespace
