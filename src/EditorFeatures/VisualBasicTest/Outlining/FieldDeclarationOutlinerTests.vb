' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.Outlining
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Editor.Implementation.Outlining

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Outlining
    Public Class FieldDeclarationOutlinerTests
        Inherits AbstractVisualBasicSyntaxOutlinerTests(Of FieldDeclarationSyntax)

        Friend Overrides Function GetRegions(fieldDeclaration As FieldDeclarationSyntax) As IEnumerable(Of OutliningSpan)
            Dim outliner As New FieldDeclarationOutliner
            Return outliner.GetOutliningSpans(fieldDeclaration, CancellationToken.None).WhereNotNull()
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestVariableMemberDeclarationWithComments()
            Dim tree = ParseLines("Class C",
                                  "  'Hello",
                                  "  'World",
                                  "  Dim x As Integer",
                                  "End Class")

            Dim typeBlock = tree.DigToFirstTypeBlock()
            Dim fieldDecl = typeBlock.DigToFirstNodeOfType(Of FieldDeclarationSyntax)()
            Assert.NotNull(fieldDecl)

            Dim actualRegion = GetRegion(fieldDecl)
            Dim expectedRegion = New OutliningSpan(
                                     TextSpan:=TextSpan.FromBounds(11, 27),
                                     bannerText:="' Hello ...",
                                     hintSpan:=TextSpan.FromBounds(11, 27),
                                     autoCollapse:=True)

            AssertRegion(expectedRegion, actualRegion)
        End Sub

    End Class
End Namespace
