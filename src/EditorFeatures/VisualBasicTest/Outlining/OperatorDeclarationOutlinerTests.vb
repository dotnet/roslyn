' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.Outlining
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Editor.Implementation.Outlining

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Outlining
    Public Class OperatorDeclarationOutlinerTests
        Inherits AbstractOutlinerTests(Of OperatorStatementSyntax)

        Friend Overrides Function GetRegions(operatorDeclaration As OperatorStatementSyntax) As IEnumerable(Of OutliningSpan)
            Dim outliner As New OperatorDeclarationOutliner
            Return outliner.GetOutliningSpans(operatorDeclaration, CancellationToken.None).WhereNotNull()
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestOperatorDeclaration()
            Dim tree = ParseLines("Class Base",
                                  "    Public Shared Widening Operator CType(b As Base) As Integer",
                                  "        Return 0",
                                  "    End Operator",
                                  "End Class")

            Dim typeBlock = tree.DigToFirstTypeBlock()
            Dim methodBlock = typeBlock.DigToFirstNodeOfType(Of MethodBlockBaseSyntax)()
            Dim operatorDecl = TryCast(methodBlock.BlockStatement, OperatorStatementSyntax)
            Assert.NotNull(operatorDecl)

            Dim actualRegion = GetRegion(operatorDecl)
            Dim expectedRegion = New OutliningSpan(
                                     TextSpan:=TextSpan.FromBounds(16, 111),
                                     bannerText:="Public Shared Widening Operator CType(b As Base) As Integer ...",
                                     hintSpan:=TextSpan.FromBounds(16, 111),
                                     autoCollapse:=True)

            AssertRegion(expectedRegion, actualRegion)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestOperatorWithComments()
            Dim tree = ParseLines("Class Base",
                                  "    'Hello",
                                  "    'World!",
                                  "    Public Shared Widening Operator CType(b As Base) As Integer",
                                  "        Return 0",
                                  "    End Operator",
                                  "End Class")

            Dim typeBlock = tree.DigToFirstTypeBlock()
            Dim methodBlock = typeBlock.DigToFirstNodeOfType(Of MethodBlockBaseSyntax)()
            Dim operatorDecl = TryCast(methodBlock.BlockStatement, OperatorStatementSyntax)
            Assert.NotNull(operatorDecl)

            Dim actualRegions = GetRegions(operatorDecl).ToList()
            Assert.Equal(2, actualRegions.Count)

            Dim expectedRegion1 = New OutliningSpan(
                                     TextSpan:=TextSpan.FromBounds(16, 35),
                                     bannerText:="' Hello ...",
                                     hintSpan:=TextSpan.FromBounds(16, 35),
                                     autoCollapse:=True)

            AssertRegion(expectedRegion1, actualRegions(0))

            Dim expectedRegion2 = New OutliningSpan(
                                     TextSpan:=TextSpan.FromBounds(41, 136),
                                     bannerText:="Public Shared Widening Operator CType(b As Base) As Integer ...",
                                     hintSpan:=TextSpan.FromBounds(41, 136),
                                     autoCollapse:=True)

            AssertRegion(expectedRegion2, actualRegions(1))
        End Sub

    End Class
End Namespace
