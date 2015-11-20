' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.Outlining
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports MaSOutliners = Microsoft.CodeAnalysis.Editor.VisualBasic.Outlining.MetadataAsSource
Imports Microsoft.CodeAnalysis.Editor.Implementation.Outlining

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Outlining.MetadataAsSource
    Public Class EventDeclarationOutlinerTests
        Inherits AbstractVisualBasicSyntaxOutlinerTests(Of EventStatementSyntax)

        Friend Overrides Function GetRegions(node As EventStatementSyntax) As IEnumerable(Of OutliningSpan)
            Dim outliner = New MaSOutliners.EventDeclarationOutliner()
            Return outliner.GetOutliningSpans(node, CancellationToken.None).WhereNotNull()
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)>
        Public Sub NoCommentsOrAttributes()
            Dim code =
<code><![CDATA[
Class C
    Event foo(x As Integer)
End Class
]]></code>

            Dim tree = ParseCode(code.Value)
            Dim typeDecl = tree.DigToFirstTypeBlock()
            Dim consDecl = typeDecl.DigToFirstNodeOfType(Of EventStatementSyntax)()

            Assert.Empty(GetRegions(consDecl))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)>
        Public Sub WithAttributes()
            Dim code =
<code><![CDATA[
Class C
    <Foo>
    Event foo(x As Integer)
End Class
]]></code>

            Dim tree = ParseCode(code.Value)
            Dim typeDecl = tree.DigToFirstTypeBlock()
            Dim consDecl = typeDecl.DigToFirstNodeOfType(Of EventStatementSyntax)()

            Dim actualRegion = GetRegion(consDecl)
            Dim expectedRegion = New OutliningSpan(
                TextSpan.FromBounds(13, 23),
                TextSpan.FromBounds(13, 46),
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
    Event foo(x As Integer)
End Class
]]></code>

            Dim tree = ParseCode(code.Value)
            Dim typeDecl = tree.DigToFirstTypeBlock()
            Dim consDecl = typeDecl.DigToFirstNodeOfType(Of EventStatementSyntax)()

            Dim actualRegion = GetRegion(consDecl)
            Dim expectedRegion = New OutliningSpan(
                TextSpan.FromBounds(13, 67),
                TextSpan.FromBounds(13, 90),
                VisualBasicOutliningHelpers.Ellipsis,
                autoCollapse:=True)

            AssertRegion(expectedRegion, actualRegion)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)>
        Public Sub WithCommentsAttributesAndModifiers()
            Dim code =
<code><![CDATA[
Class C
    ' Summary:
    '     This is a summary.
    <Foo>
    Private Event foo(x As Integer)
End Class
]]></code>

            Dim tree = ParseCode(code.Value)
            Dim typeDecl = tree.DigToFirstTypeBlock()
            Dim consDecl = typeDecl.DigToFirstNodeOfType(Of EventStatementSyntax)()

            Dim actualRegion = GetRegion(consDecl)
            Dim expectedRegion = New OutliningSpan(
                TextSpan.FromBounds(13, 67),
                TextSpan.FromBounds(13, 98),
                VisualBasicOutliningHelpers.Ellipsis,
                autoCollapse:=True)

            AssertRegion(expectedRegion, actualRegion)
        End Sub

        <WpfFact(Skip:="530915"), Trait(Traits.Feature, Traits.Features.MetadataAsSource)>
        Public Sub WithCustomKeyword()
            Dim code =
<code><![CDATA[
Class C
    ' Summary:
    '     This is a summary.
    <Foo>
    Custom Event foo(x As Integer)
End Class
]]></code>

            Dim tree = ParseCode(code.Value)
            Dim typeDecl = tree.DigToFirstTypeBlock()
            Dim consDecl = typeDecl.DigToFirstNodeOfType(Of EventStatementSyntax)()

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
