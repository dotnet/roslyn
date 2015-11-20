' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.Outlining
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Editor.Implementation.Outlining

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Outlining
    Public Class TypeDeclarationOutlinerTests
        Inherits AbstractVisualBasicSyntaxOutlinerTests(Of TypeStatementSyntax)

        Friend Overrides Function GetRegions(typeDeclaration As TypeStatementSyntax) As IEnumerable(Of OutliningSpan)
            Dim outliner As New TypeDeclarationOutliner
            Return outliner.GetOutliningSpans(typeDeclaration, CancellationToken.None).WhereNotNull()
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestClass()
            Dim tree = ParseLines("Class C1",
                                  "End Class")

            Dim typeBlock = tree.DigToFirstTypeBlock()
            Dim typeDecl = typeBlock.BlockStatement
            Assert.NotNull(typeDecl)

            Dim actualRegion = GetRegion(typeDecl)
            Dim expectedRegion = New OutliningSpan(
                                     TextSpan:=TextSpan.FromBounds(0, 19),
                                     bannerText:="Class C1 ...",
                                     hintSpan:=TextSpan.FromBounds(0, 19),
                                     autoCollapse:=False)

            AssertRegion(expectedRegion, actualRegion)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestFriendClass()
            Dim tree = ParseLines("Friend Class C1",
                                  "End Class")

            Dim typeBlock = tree.DigToFirstTypeBlock()
            Dim typeDecl = typeBlock.BlockStatement
            Assert.NotNull(typeDecl)

            Dim actualRegion = GetRegion(typeDecl)
            Dim expectedRegion = New OutliningSpan(
                                     TextSpan:=TextSpan.FromBounds(0, 26),
                                     bannerText:="Friend Class C1 ...",
                                     hintSpan:=TextSpan.FromBounds(0, 26),
                                     autoCollapse:=False)

            AssertRegion(expectedRegion, actualRegion)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestClassWithLeadingComments()
            Dim tree = ParseLines("'Hello",
                                  "'World!",
                                  "Class C1",
                                  "End Class")

            Dim typeBlock = tree.DigToFirstTypeBlock()
            Dim typeDecl = typeBlock.BlockStatement
            Assert.NotNull(typeDecl)

            Dim actualRegions = GetRegions(typeDecl).ToList()
            Assert.Equal(2, actualRegions.Count)

            Dim expectedRegion1 = New OutliningSpan(
                                     TextSpan:=TextSpan.FromBounds(0, 15),
                                     bannerText:="' Hello ...",
                                     hintSpan:=TextSpan.FromBounds(0, 15),
                                     autoCollapse:=True)

            AssertRegion(expectedRegion1, actualRegions(0))

            Dim expectedRegion2 = New OutliningSpan(
                                     TextSpan:=TextSpan.FromBounds(17, 36),
                                     bannerText:="Class C1 ...",
                                     hintSpan:=TextSpan.FromBounds(17, 36),
                                     autoCollapse:=False)

            AssertRegion(expectedRegion2, actualRegions(1))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestClassWithNestedComments()
            Dim tree = ParseLines("Class C1",
                                  "'Hello",
                                  "'World!",
                                  "End Class")

            Dim typeBlock = tree.DigToFirstTypeBlock()
            Dim typeDecl = typeBlock.BlockStatement
            Assert.NotNull(typeDecl)

            Dim actualRegions = GetRegions(typeDecl).ToList()
            Assert.Equal(2, actualRegions.Count)

            Dim expectedRegion1 = New OutliningSpan(
                                     TextSpan:=TextSpan.FromBounds(0, 36),
                                     bannerText:="Class C1 ...",
                                     hintSpan:=TextSpan.FromBounds(0, 36),
                                     autoCollapse:=False)

            AssertRegion(expectedRegion1, actualRegions(0))

            Dim expectedRegion2 = New OutliningSpan(
                                     TextSpan:=TextSpan.FromBounds(10, 25),
                                     bannerText:="' Hello ...",
                                     hintSpan:=TextSpan.FromBounds(10, 25),
                                     autoCollapse:=True)

            AssertRegion(expectedRegion2, actualRegions(1))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestModule()
            Dim tree = ParseLines("Module M1",
                                  "End Module")

            Dim typeBlock = tree.DigToFirstTypeBlock()
            Dim typeDecl = typeBlock.BlockStatement
            Assert.NotNull(typeDecl)

            Dim actualRegion = GetRegion(typeDecl)
            Dim expectedRegion = New OutliningSpan(
                                     TextSpan:=TextSpan.FromBounds(0, 21),
                                     bannerText:="Module M1 ...",
                                     hintSpan:=TextSpan.FromBounds(0, 21),
                                     autoCollapse:=False)

            AssertRegion(expectedRegion, actualRegion)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestModuleWithLeadingComments()
            Dim tree = ParseLines("'Hello",
                                  "'World!",
                                  "Module M1",
                                  "End Module")

            Dim typeBlock = tree.DigToFirstTypeBlock()
            Dim typeDecl = typeBlock.BlockStatement
            Assert.NotNull(typeDecl)

            Dim actualRegions = GetRegions(typeDecl).ToList()
            Assert.Equal(2, actualRegions.Count)

            Dim expectedRegion1 = New OutliningSpan(
                                     TextSpan:=TextSpan.FromBounds(0, 15),
                                     bannerText:="' Hello ...",
                                     hintSpan:=TextSpan.FromBounds(0, 15),
                                     autoCollapse:=True)

            AssertRegion(expectedRegion1, actualRegions(0))

            Dim expectedRegion2 = New OutliningSpan(
                                     TextSpan:=TextSpan.FromBounds(17, 38),
                                     bannerText:="Module M1 ...",
                                     hintSpan:=TextSpan.FromBounds(17, 38),
                                     autoCollapse:=False)

            AssertRegion(expectedRegion2, actualRegions(1))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestModuleWithNestedComments()
            Dim tree = ParseLines("Module M1",
                                  "'Hello",
                                  "'World!",
                                  "End Module")

            Dim typeBlock = tree.DigToFirstTypeBlock()
            Dim typeDecl = typeBlock.BlockStatement
            Assert.NotNull(typeDecl)

            Dim actualRegions = GetRegions(typeDecl).ToList()
            Assert.Equal(2, actualRegions.Count)

            Dim expectedRegion1 = New OutliningSpan(
                                     TextSpan:=TextSpan.FromBounds(0, 38),
                                     bannerText:="Module M1 ...",
                                     hintSpan:=TextSpan.FromBounds(0, 38),
                                     autoCollapse:=False)

            AssertRegion(expectedRegion1, actualRegions(0))

            Dim expectedRegion2 = New OutliningSpan(
                                     TextSpan:=TextSpan.FromBounds(11, 26),
                                     bannerText:="' Hello ...",
                                     hintSpan:=TextSpan.FromBounds(11, 26),
                                     autoCollapse:=True)

            AssertRegion(expectedRegion2, actualRegions(1))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestInterface()
            Dim tree = ParseLines("Interface I1",
                                  "End Interface")

            Dim typeBlock = tree.DigToFirstTypeBlock()
            Dim typeDecl = typeBlock.BlockStatement
            Assert.NotNull(typeDecl)

            Dim actualRegion = GetRegion(typeDecl)
            Dim expectedRegion = New OutliningSpan(
                                     TextSpan:=TextSpan.FromBounds(0, 27),
                                     bannerText:="Interface I1 ...",
                                     hintSpan:=TextSpan.FromBounds(0, 27),
                                     autoCollapse:=False)

            AssertRegion(expectedRegion, actualRegion)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestInterfaceWithLeadingComments()
            Dim tree = ParseLines("'Hello",
                                  "'World!",
                                  "Interface I1",
                                  "End Interface")

            Dim typeBlock = tree.DigToFirstTypeBlock()
            Dim typeDecl = typeBlock.BlockStatement
            Assert.NotNull(typeDecl)

            Dim actualRegions = GetRegions(typeDecl).ToList()
            Assert.Equal(2, actualRegions.Count)

            Dim expectedRegion1 = New OutliningSpan(
                                     TextSpan:=TextSpan.FromBounds(0, 15),
                                     bannerText:="' Hello ...",
                                     hintSpan:=TextSpan.FromBounds(0, 15),
                                     autoCollapse:=True)

            AssertRegion(expectedRegion1, actualRegions(0))

            Dim expectedRegion2 = New OutliningSpan(
                                     TextSpan:=TextSpan.FromBounds(17, 44),
                                     bannerText:="Interface I1 ...",
                                     hintSpan:=TextSpan.FromBounds(17, 44),
                                     autoCollapse:=False)

            AssertRegion(expectedRegion2, actualRegions(1))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestInterfaceWithNestedComments()
            Dim tree = ParseLines("Interface I1",
                                  "'Hello",
                                  "'World!",
                                  "End Interface")

            Dim typeBlock = tree.DigToFirstTypeBlock()
            Dim typeDecl = typeBlock.BlockStatement
            Assert.NotNull(typeDecl)

            Dim actualRegions = GetRegions(typeDecl).ToList()
            Assert.Equal(2, actualRegions.Count)

            Dim expectedRegion1 = New OutliningSpan(
                                     TextSpan:=TextSpan.FromBounds(0, 44),
                                     bannerText:="Interface I1 ...",
                                     hintSpan:=TextSpan.FromBounds(0, 44),
                                     autoCollapse:=False)

            AssertRegion(expectedRegion1, actualRegions(0))

            Dim expectedRegion2 = New OutliningSpan(
                                     TextSpan:=TextSpan.FromBounds(14, 29),
                                     bannerText:="' Hello ...",
                                     hintSpan:=TextSpan.FromBounds(14, 29),
                                     autoCollapse:=True)

            AssertRegion(expectedRegion2, actualRegions(1))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestStructure()
            Dim tree = ParseLines("Structure S1",
                                  "End Structure")

            Dim typeBlock = tree.DigToFirstTypeBlock()
            Dim typeDecl = typeBlock.BlockStatement
            Assert.NotNull(typeDecl)

            Dim actualRegion = GetRegion(typeDecl)
            Dim expectedRegion = New OutliningSpan(
                                     TextSpan:=TextSpan.FromBounds(0, 27),
                                     bannerText:="Structure S1 ...",
                                     hintSpan:=TextSpan.FromBounds(0, 27),
                                     autoCollapse:=False)

            AssertRegion(expectedRegion, actualRegion)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestStructureWithLeadingComments()
            Dim tree = ParseLines("'Hello",
                                  "'World!",
                                  "Structure S1",
                                  "End Structure")

            Dim typeBlock = tree.DigToFirstTypeBlock()
            Dim typeDecl = typeBlock.BlockStatement
            Assert.NotNull(typeDecl)

            Dim actualRegions = GetRegions(typeDecl).ToList()
            Assert.Equal(2, actualRegions.Count)

            Dim expectedRegion1 = New OutliningSpan(
                                     TextSpan:=TextSpan.FromBounds(0, 15),
                                     bannerText:="' Hello ...",
                                     hintSpan:=TextSpan.FromBounds(0, 15),
                                     autoCollapse:=True)

            AssertRegion(expectedRegion1, actualRegions(0))

            Dim expectedRegion2 = New OutliningSpan(
                                     TextSpan:=TextSpan.FromBounds(17, 44),
                                     bannerText:="Structure S1 ...",
                                     hintSpan:=TextSpan.FromBounds(17, 44),
                                     autoCollapse:=False)

            AssertRegion(expectedRegion2, actualRegions(1))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestStructureWithNestedComments()
            Dim tree = ParseLines("Structure S1",
                                  "'Hello",
                                  "'World!",
                                  "End Structure")

            Dim typeBlock = tree.DigToFirstTypeBlock()
            Dim typeDecl = typeBlock.BlockStatement
            Assert.NotNull(typeDecl)

            Dim actualRegions = GetRegions(typeDecl).ToList()
            Assert.Equal(2, actualRegions.Count)

            Dim expectedRegion1 = New OutliningSpan(
                                     TextSpan:=TextSpan.FromBounds(0, 44),
                                     bannerText:="Structure S1 ...",
                                     hintSpan:=TextSpan.FromBounds(0, 44),
                                     autoCollapse:=False)

            AssertRegion(expectedRegion1, actualRegions(0))

            Dim expectedRegion2 = New OutliningSpan(
                                     TextSpan:=TextSpan.FromBounds(14, 29),
                                     bannerText:="' Hello ...",
                                     hintSpan:=TextSpan.FromBounds(14, 29),
                                     autoCollapse:=True)

            AssertRegion(expectedRegion2, actualRegions(1))
        End Sub
    End Class
End Namespace
