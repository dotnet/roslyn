' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.Outlining
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports MaSOutliners = Microsoft.CodeAnalysis.Editor.VisualBasic.Outlining.MetadataAsSource
Imports Microsoft.CodeAnalysis.Editor.Implementation.Outlining

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Outlining.MetadataAsSource
    Public Class MethodDeclarationOutlinerTests
        Inherits AbstractVisualBasicSyntaxOutlinerTests(Of MethodStatementSyntax)

        Friend Overrides Function GetRegions(node As MethodStatementSyntax) As IEnumerable(Of OutliningSpan)
            Dim outliner = New MaSOutliners.MethodDeclarationOutliner()
            Return outliner.GetOutliningSpans(node, CancellationToken.None).WhereNotNull()
        End Function

        Private Shared Function GetMethodStatement(code As Xml.Linq.XElement) As MethodStatementSyntax
            Dim tree = ParseCode(code.Value)
            Dim typeDecl = tree.DigToFirstTypeBlock()
            Dim methodBlock = typeDecl.DigToFirstNodeOfType(Of MethodBlockSyntax)()
            Dim methodStatement = DirectCast(methodBlock.BlockStatement, MethodStatementSyntax)
            Return methodStatement
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)>
        Public Sub NoCommentsOrAttributes()
            Dim code =
<code><![CDATA[
Class C
    Sub M()
    End Sub
End Class
]]></code>

            Dim methodStatement As MethodStatementSyntax = GetMethodStatement(code)

            Assert.Empty(GetRegions(methodStatement))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)>
        Public Sub WithAttributes()
            Dim code =
<code><![CDATA[
Class C
    <Foo>
    Sub M()
    End Sub
End Class
]]></code>

            Dim methodStatement As MethodStatementSyntax = GetMethodStatement(code)

            Dim actualRegion = GetRegion(methodStatement)
            Dim expectedRegion = New OutliningSpan(
                TextSpan.FromBounds(13, 23),
                TextSpan.FromBounds(13, 30),
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
    Sub M()
    End Sub
End Class
]]></code>

            Dim methodStatement As MethodStatementSyntax = GetMethodStatement(code)

            Dim actualRegion = GetRegion(methodStatement)
            Dim expectedRegion = New OutliningSpan(
                TextSpan.FromBounds(13, 57),
                TextSpan.FromBounds(13, 64),
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
    Public Sub M()
    End Sub
End Class
]]></code>

            Dim methodStatement As MethodStatementSyntax = GetMethodStatement(code)

            Dim actualRegion = GetRegion(methodStatement)
            Dim expectedRegion = New OutliningSpan(
                TextSpan.FromBounds(13, 57),
                TextSpan.FromBounds(13, 71),
                VisualBasicOutliningHelpers.Ellipsis,
                autoCollapse:=True)

            AssertRegion(expectedRegion, actualRegion)
        End Sub
    End Class
End Namespace
