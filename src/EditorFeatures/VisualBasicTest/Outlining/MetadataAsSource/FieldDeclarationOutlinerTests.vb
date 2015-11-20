' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.Outlining
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports MaSOutliners = Microsoft.CodeAnalysis.Editor.VisualBasic.Outlining.MetadataAsSource
Imports Microsoft.CodeAnalysis.Editor.Implementation.Outlining

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Outlining.MetadataAsSource
    Public Class FieldDeclarationOutlinerTests
        Inherits AbstractVisualBasicSyntaxOutlinerTests(Of FieldDeclarationSyntax)

        Friend Overrides Function GetRegions(node As FieldDeclarationSyntax) As IEnumerable(Of OutliningSpan)
            Dim outliner = New MaSOutliners.FieldDeclarationOutliner()
            Return outliner.GetOutliningSpans(node, CancellationToken.None).WhereNotNull()
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)>
        Public Sub NoCommentsOrAttributes()
            Dim code =
<code><![CDATA[
Class C
    Dim x As Integer
End Class
]]></code>

            Dim tree = ParseCode(code.Value)
            Dim typeDecl = tree.DigToFirstTypeBlock()
            Dim consDecl = typeDecl.DigToFirstNodeOfType(Of FieldDeclarationSyntax)()

            Assert.Empty(GetRegions(consDecl))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)>
        Public Sub WithAttributes()
            Dim code =
<code><![CDATA[
Class C
    <Foo>
    Dim x As Integer
End Class
]]></code>

            Dim tree = ParseCode(code.Value)
            Dim typeDecl = tree.DigToFirstTypeBlock()
            Dim consDecl = typeDecl.DigToFirstNodeOfType(Of FieldDeclarationSyntax)()

            Dim actualRegion = GetRegion(consDecl)
            Dim expectedRegion = New OutliningSpan(
                TextSpan.FromBounds(13, 23),
                TextSpan.FromBounds(13, 39),
                VisualBasicOutliningHelpers.Ellipsis,
                autoCollapse:=True)

            AssertRegion(expectedRegion, actualRegion)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)>
        Public Sub WithCommentsAndAttributes()
            Dim code =
<code><![CDATA[
Class C
    ' Summary:
    '     This is a summary.
    <Foo>
    Dim x As Integer
End Class
]]></code>

            Dim tree = ParseCode(code.Value)
            Dim typeDecl = tree.DigToFirstTypeBlock()
            Dim consDecl = typeDecl.DigToFirstNodeOfType(Of FieldDeclarationSyntax)()

            Dim actualRegion = GetRegion(consDecl)
            Dim expectedRegion = New OutliningSpan(
                TextSpan.FromBounds(13, 67),
                TextSpan.FromBounds(13, 83),
                VisualBasicOutliningHelpers.Ellipsis,
                autoCollapse:=True)

            AssertRegion(expectedRegion, actualRegion)
        End Sub
    End Class
End Namespace
