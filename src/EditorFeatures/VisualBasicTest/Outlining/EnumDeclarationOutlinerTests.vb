' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.Outlining
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Editor.Implementation.Outlining

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Outlining
    Public Class EnumDeclarationOutlinerTests
        Inherits AbstractOutlinerTests(Of EnumStatementSyntax)

        Friend Overrides Function GetRegions(enumDeclaration As EnumStatementSyntax) As IEnumerable(Of OutliningSpan)
            Dim outliner As New EnumDeclarationOutliner()
            Return outliner.GetOutliningSpans(enumDeclaration, CancellationToken.None).WhereNotNull()
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestEnum()
            Dim syntaxTree = ParseLines("Enum E1",
                                  "End Enum ' Foo")

            Dim enumBlock = syntaxTree.DigToFirstNodeOfType(Of EnumBlockSyntax)()
            Dim enumDecl = enumBlock.EnumStatement
            Assert.NotNull(enumDecl)

            Dim actualRegion = GetRegion(enumDecl)
            Dim expectedRegion = New OutliningSpan(
                                     TextSpan:=TextSpan.FromBounds(0, 17),
                                     bannerText:="Enum E1 ...",
                                     hintSpan:=TextSpan.FromBounds(0, 17),
                                     autoCollapse:=True)

            AssertRegion(expectedRegion, actualRegion)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestEnumWithLeadingComments()
            Dim syntaxTree = ParseLines("'Hello",
                                  "'World!",
                                  "Enum E1",
                                  "End Enum")

            Dim enumBlock = syntaxTree.DigToFirstNodeOfType(Of EnumBlockSyntax)()
            Dim enumDecl = enumBlock.EnumStatement
            Assert.NotNull(enumDecl)

            Dim actualRegions = GetRegions(enumDecl).ToList()
            Assert.Equal(2, actualRegions.Count)

            Dim expectedRegion1 = New OutliningSpan(
                                     TextSpan:=TextSpan.FromBounds(0, 15),
                                     bannerText:="' Hello ...",
                                     hintSpan:=TextSpan.FromBounds(0, 15),
                                     autoCollapse:=True)

            AssertRegion(expectedRegion1, actualRegions(0))

            Dim expectedRegion2 = New OutliningSpan(
                                     TextSpan:=TextSpan.FromBounds(17, 34),
                                     bannerText:="Enum E1 ...",
                                     hintSpan:=TextSpan.FromBounds(17, 34),
                                     autoCollapse:=True)

            AssertRegion(expectedRegion2, actualRegions(1))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestEnumWithNestedComments()
            Dim syntaxTree = ParseLines("Enum E1",
                                  "'Hello",
                                  "'World!",
                                  "End Enum")

            Dim enumBlock = syntaxTree.DigToFirstNodeOfType(Of EnumBlockSyntax)()
            Dim enumDecl = enumBlock.EnumStatement
            Assert.NotNull(enumDecl)

            Dim actualRegions = GetRegions(enumDecl).ToList()
            Assert.Equal(2, actualRegions.Count)

            Dim expectedRegion1 = New OutliningSpan(
                                     TextSpan:=TextSpan.FromBounds(0, 34),
                                     bannerText:="Enum E1 ...",
                                     hintSpan:=TextSpan.FromBounds(0, 34),
                                     autoCollapse:=True)

            AssertRegion(expectedRegion1, actualRegions(0))

            Dim expectedRegion2 = New OutliningSpan(
                                     TextSpan:=TextSpan.FromBounds(9, 24),
                                     bannerText:="' Hello ...",
                                     hintSpan:=TextSpan.FromBounds(9, 24),
                                     autoCollapse:=True)

            AssertRegion(expectedRegion2, actualRegions(1))
        End Sub

    End Class
End Namespace
