' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.Outlining
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Editor.Implementation.Outlining

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Outlining
    Public Class PropertyDeclarationOutlinerTests
        Inherits AbstractOutlinerTests(Of PropertyStatementSyntax)

        Friend Overrides Function GetRegions(propertyDeclaration As PropertyStatementSyntax) As IEnumerable(Of OutliningSpan)
            Dim outliner As New PropertyDeclarationOutliner
            Return outliner.GetOutliningSpans(propertyDeclaration, CancellationToken.None).WhereNotNull()
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestReadOnlyProperty()
            Dim syntaxTree = ParseLines("Class C1",
                                  "  ReadOnly Property P1 As Integer",
                                  "    Get",
                                  "      Return 0",
                                  "    End Get",
                                  "  End Property",
                                  "End Class")

            Dim typeBlock = syntaxTree.DigToFirstTypeBlock()
            Dim propertyBlock = typeBlock.DigToFirstNodeOfType(Of PropertyBlockSyntax)()
            Dim propertyDecl = TryCast(propertyBlock.PropertyStatement, PropertyStatementSyntax)
            Assert.NotNull(propertyDecl)

            Dim actualRegion = GetRegion(propertyDecl)
            Dim expectedRegion = New OutliningSpan(
                                     TextSpan:=TextSpan.FromBounds(12, 97),
                                     bannerText:="ReadOnly Property P1 As Integer ...",
                                     hintSpan:=TextSpan.FromBounds(12, 97),
                                     autoCollapse:=True)

            AssertRegion(expectedRegion, actualRegion)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestWriteOnlyProperty()
            Dim syntaxTree = ParseLines("Class C1",
                                  "  WriteOnly Property P1 As Integer",
                                  "    Set(ByVal value As Integer)",
                                  "    End Get",
                                  "  End Property",
                                  "End Class")

            Dim typeBlock = syntaxTree.DigToFirstTypeBlock()
            Dim propertyBlock = typeBlock.DigToFirstNodeOfType(Of PropertyBlockSyntax)()
            Dim propertyDecl = TryCast(propertyBlock.PropertyStatement, PropertyStatementSyntax)
            Assert.NotNull(propertyDecl)

            Dim actualRegion = GetRegion(propertyDecl)
            Dim expectedRegion = New OutliningSpan(
                                     TextSpan:=TextSpan.FromBounds(12, 106),
                                     bannerText:="WriteOnly Property P1 As Integer ...",
                                     hintSpan:=TextSpan.FromBounds(12, 106),
                                     autoCollapse:=True)

            AssertRegion(expectedRegion, actualRegion)
        End Sub
    End Class
End Namespace
