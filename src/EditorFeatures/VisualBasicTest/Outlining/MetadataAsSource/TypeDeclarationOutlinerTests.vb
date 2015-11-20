' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.Outlining
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports MaSOutliners = Microsoft.CodeAnalysis.Editor.VisualBasic.Outlining.MetadataAsSource
Imports Microsoft.CodeAnalysis.Editor.Implementation.Outlining

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Outlining.MetadataAsSource
    Public Class TypeDeclarationOutlinerTests
        Inherits AbstractVisualBasicSyntaxOutlinerTests(Of TypeStatementSyntax)

        Friend Overrides Function GetRegions(node As TypeStatementSyntax) As IEnumerable(Of OutliningSpan)
            Dim outliner = New MaSOutliners.TypeDeclarationOutliner()
            Return outliner.GetOutliningSpans(node, CancellationToken.None).WhereNotNull()
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)>
        Public Sub NoCommentsOrAttributes()
            Dim code =
<code><![CDATA[
Class C
End Class
]]></code>

            Dim tree = ParseCode(code.Value)
            Dim typeDecl = tree.DigToFirstTypeBlock()
            Dim typeStatement = typeDecl.BlockStatement

            Assert.Empty(GetRegions(typeStatement))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)>
        Public Sub WithAttributes()
            Dim code =
<code><![CDATA[
<Foo>
Class C
End Class
]]></code>

            Dim tree = ParseCode(code.Value)
            Dim typeDecl = tree.DigToFirstTypeBlock()
            Dim typeStatement = typeDecl.BlockStatement

            Dim actualRegion = GetRegion(typeStatement)
            Dim expectedRegion = New OutliningSpan(
                TextSpan.FromBounds(1, 7),
                TextSpan.FromBounds(1, 14),
                VisualBasicOutliningHelpers.Ellipsis,
                autoCollapse:=True)

            AssertRegion(expectedRegion, actualRegion)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)>
        Public Sub WithCommentsAndAttributes()
            Dim code =
<code><![CDATA[
' Summary:
'     This is a summary.
<Foo>
Class C
End Class
]]></code>

            Dim tree = ParseCode(code.Value)
            Dim typeDecl = tree.DigToFirstTypeBlock()
            Dim typeStatement = typeDecl.BlockStatement

            Dim actualRegion = GetRegion(typeStatement)
            Dim expectedRegion = New OutliningSpan(
                TextSpan.FromBounds(1, 43),
                TextSpan.FromBounds(1, 50),
                VisualBasicOutliningHelpers.Ellipsis,
                autoCollapse:=True)

            AssertRegion(expectedRegion, actualRegion)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)>
        Public Sub WithCommentsAttributesAndModifiers()
            Dim code =
<code><![CDATA[
' Summary:
'     This is a summary.
<Foo>
Public Class C
End Class
]]></code>

            Dim tree = ParseCode(code.Value)
            Dim typeDecl = tree.DigToFirstTypeBlock()
            Dim typeStatement = typeDecl.BlockStatement

            Dim actualRegion = GetRegion(typeStatement)
            Dim expectedRegion = New OutliningSpan(
                TextSpan.FromBounds(1, 43),
                TextSpan.FromBounds(1, 57),
                VisualBasicOutliningHelpers.Ellipsis,
                autoCollapse:=True)

            AssertRegion(expectedRegion, actualRegion)
        End Sub
    End Class
End Namespace
