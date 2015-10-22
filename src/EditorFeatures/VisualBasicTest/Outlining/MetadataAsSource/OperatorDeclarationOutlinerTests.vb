' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.Outlining
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports MaSOutliners = Microsoft.CodeAnalysis.Editor.VisualBasic.Outlining.MetadataAsSource
Imports Microsoft.CodeAnalysis.Editor.Implementation.Outlining

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Outlining.MetadataAsSource
    Public Class OperatorDeclarationOutlinerTests
        Inherits AbstractOutlinerTests(Of OperatorStatementSyntax)

        Friend Overrides Function GetRegions(node As OperatorStatementSyntax) As IEnumerable(Of OutliningSpan)
            Dim outliner = New MaSOutliners.OperatorDeclarationOutliner()
            Return outliner.GetOutliningSpans(node, CancellationToken.None).WhereNotNull()
        End Function

        Private Shared Function GetOperatorStatement(code As Xml.Linq.XElement) As OperatorStatementSyntax
            Dim tree = ParseCode(code.Value)
            Dim typeDecl = tree.DigToFirstTypeBlock()
            Dim operatorDecl = typeDecl.DigToFirstNodeOfType(Of MethodBlockBaseSyntax)()
            Dim operatorStatement = DirectCast(operatorDecl.BlockStatement, OperatorStatementSyntax)
            Return operatorStatement
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)>
        Public Sub NoCommentsOrAttributes()
            Dim code =
<code><![CDATA[
Class C
    Public Shared Operator =(a As C, b As C) As Boolean
End Class
]]></code>
            Dim operatorStatement As OperatorStatementSyntax = GetOperatorStatement(code)

            Assert.Empty(GetRegions(operatorStatement))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)>
        Public Sub WithAttributes()
            Dim code =
<code><![CDATA[
Class C
    <Foo>
    Public Shared Operator =(a As C, b As C) As Boolean
End Class
]]></code>

            Dim operatorStatement As OperatorStatementSyntax = GetOperatorStatement(code)

            Dim actualRegion = GetRegion(operatorStatement)
            Dim expectedRegion = New OutliningSpan(
                TextSpan.FromBounds(13, 23),
                TextSpan.FromBounds(13, 74),
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
    Operator =(a As C, b As C) As Boolean
End Class
]]></code>

            Dim operatorStatement As OperatorStatementSyntax = GetOperatorStatement(code)

            Dim actualRegion = GetRegion(operatorStatement)
            Dim expectedRegion = New OutliningSpan(
                TextSpan.FromBounds(13, 67),
                TextSpan.FromBounds(13, 104),
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
    Public Shared Operator =(a As C, b As C) As Boolean
End Class
]]></code>

            Dim operatorStatement As OperatorStatementSyntax = GetOperatorStatement(code)

            Dim actualRegion = GetRegion(operatorStatement)
            Dim expectedRegion = New OutliningSpan(
                TextSpan.FromBounds(13, 67),
                TextSpan.FromBounds(13, 118),
                VisualBasicOutliningHelpers.Ellipsis,
                autoCollapse:=True)

            AssertRegion(expectedRegion, actualRegion)
        End Sub
    End Class
End Namespace
