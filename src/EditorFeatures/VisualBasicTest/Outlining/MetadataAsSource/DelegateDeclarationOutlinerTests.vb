' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.Outlining
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports MaSOutliners = Microsoft.CodeAnalysis.Editor.VisualBasic.Outlining.MetadataAsSource
Imports Microsoft.CodeAnalysis.Editor.Implementation.Outlining

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Outlining.MetadataAsSource
    Public Class DelegateDeclarationOutlinerTests
        Inherits AbstractVisualBasicSyntaxOutlinerTests(Of DelegateStatementSyntax)

        Friend Overrides Function GetRegions(node As DelegateStatementSyntax) As IEnumerable(Of OutliningSpan)
            Dim outliner = New MaSOutliners.DelegateDeclarationOutliner()
            Return outliner.GetOutliningSpans(node, CancellationToken.None).WhereNotNull()
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)>
        Public Sub NoCommentsOrAttributes()
            Dim code =
<code><![CDATA[
Delegate Sub Bar()
]]></code>

            Dim tree = ParseCode(code.Value)
            Dim typeDecl = tree.DigToFirstNodeOfType(Of DelegateStatementSyntax)()

            Assert.Empty(GetRegions(typeDecl))
        End Sub

        Public Delegate Sub Bar(x As Int16)

        <WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)>
        Public Sub WithAttributes()
            Dim code =
<code><![CDATA[
<Foo>
Delegate Sub Bar()
]]></code>

            Dim tree = ParseCode(code.Value)
            Dim typeDecl = tree.DigToFirstNodeOfType(Of DelegateStatementSyntax)()

            Dim actualRegion = GetRegion(typeDecl)
            Dim expectedRegion = New OutliningSpan(
                TextSpan.FromBounds(1, 7),
                TextSpan.FromBounds(1, 25),
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
Delegate Sub Bar()
]]></code>

            Dim tree = ParseCode(code.Value)
            Dim typeDecl = tree.DigToFirstNodeOfType(Of DelegateStatementSyntax)()

            Dim actualRegion = GetRegion(typeDecl)
            Dim expectedRegion = New OutliningSpan(
                TextSpan.FromBounds(1, 43),
                TextSpan.FromBounds(1, 61),
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
Public Delegate Sub Bar()
]]></code>

            Dim tree = ParseCode(code.Value)
            Dim typeDecl = tree.DigToFirstNodeOfType(Of DelegateStatementSyntax)()

            Dim actualRegion = GetRegion(typeDecl)
            Dim expectedRegion = New OutliningSpan(
                TextSpan.FromBounds(1, 43),
                TextSpan.FromBounds(1, 68),
                VisualBasicOutliningHelpers.Ellipsis,
                autoCollapse:=True)

            AssertRegion(expectedRegion, actualRegion)
        End Sub
    End Class
End Namespace
