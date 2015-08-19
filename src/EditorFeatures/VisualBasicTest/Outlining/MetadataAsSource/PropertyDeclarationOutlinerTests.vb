' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.Outlining
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports MaSOutliners = Microsoft.CodeAnalysis.Editor.VisualBasic.Outlining.MetadataAsSource
Imports Microsoft.CodeAnalysis.Editor.Implementation.Outlining

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Outlining.MetadataAsSource
    Public Class PropertyDeclarationOutlinerTests
        Inherits AbstractOutlinerTests(Of PropertyStatementSyntax)

        Friend Overrides Function GetRegions(node As PropertyStatementSyntax) As IEnumerable(Of OutliningSpan)
            Dim outliner = New MaSOutliners.PropertyDeclarationOutliner()
            Return outliner.GetOutliningSpans(node, CancellationToken.None).WhereNotNull()
        End Function

        Private Shared Function GetPropertyStatement(code As Xml.Linq.XElement) As PropertyStatementSyntax
            Dim tree = ParseCode(code.Value)
            Dim typeDecl = tree.DigToFirstTypeBlock()
            Return typeDecl.DigToFirstNodeOfType(Of PropertyStatementSyntax)()
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)>
        Public Sub NoCommentsOrAttributes()
            Dim code =
<code><![CDATA[
Class C
    Property Foo As Integer
End Class
]]></code>

            Dim propertyStatement As PropertyStatementSyntax = GetPropertyStatement(code)

            Assert.Empty(GetRegions(propertyStatement))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)>
        Public Sub WithAttributes()
            Dim code =
<code><![CDATA[
Class C
    <Foo>
    Property Foo As Integer
End Class
]]></code>

            Dim propertyStatement As PropertyStatementSyntax = GetPropertyStatement(code)

            Dim actualRegion = GetRegion(propertyStatement)
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
    Property Foo As Integer
End Class
]]></code>

            Dim propertyStatement As PropertyStatementSyntax = GetPropertyStatement(code)

            Dim actualRegion = GetRegion(propertyStatement)
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
    Public Property Foo As Integer
End Class
]]></code>

            Dim propertyStatement As PropertyStatementSyntax = GetPropertyStatement(code)

            Dim actualRegion = GetRegion(propertyStatement)
            Dim expectedRegion = New OutliningSpan(
                TextSpan.FromBounds(13, 67),
                TextSpan.FromBounds(13, 97),
                VisualBasicOutliningHelpers.Ellipsis,
                autoCollapse:=True)

            AssertRegion(expectedRegion, actualRegion)
        End Sub
    End Class
End Namespace
