' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.Outlining
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports MaSOutliners = Microsoft.CodeAnalysis.Editor.VisualBasic.Outlining.MetadataAsSource
Imports Microsoft.CodeAnalysis.Editor.Implementation.Outlining

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Outlining.MetadataAsSource
    Public Class EnumDeclarationOutlinerTests
        Inherits AbstractVisualBasicSyntaxOutlinerTests(Of EnumStatementSyntax)

        Friend Overrides Function GetRegions(node As EnumStatementSyntax) As IEnumerable(Of OutliningSpan)
            Dim outliner = New MaSOutliners.EnumDeclarationOutliner()
            Return outliner.GetOutliningSpans(node, CancellationToken.None).WhereNotNull()
        End Function

        Private Shared Function GetEnumStatement(code As Xml.Linq.XElement) As EnumStatementSyntax
            Dim tree = ParseCode(code.Value)
            Dim enumDecl = tree.DigToFirstNodeOfType(Of EnumBlockSyntax)()
            Dim enumStatement = enumDecl.EnumStatement
            Return enumStatement
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)>
        Public Sub NoCommentsOrAttributes()
            Dim code =
<code><![CDATA[
Enum Foo
    Bar
    Baz
End Enum
]]></code>

            Dim enumStatement As EnumStatementSyntax = GetEnumStatement(code)

            Assert.Empty(GetRegions(enumStatement))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)>
        Public Sub WithAttributes()
            Dim code =
<code><![CDATA[
<Foo>
Enum Foo
    Bar
    Baz
End Enum
]]></code>

            Dim enumStatement As EnumStatementSyntax = GetEnumStatement(code)

            Dim actualRegion = GetRegion(enumStatement)
            Dim expectedRegion = New OutliningSpan(
                TextSpan.FromBounds(1, 7),
                TextSpan.FromBounds(1, 15),
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
Enum Foo
    Bar
    Baz
End Enum
]]></code>

            Dim enumStatement As EnumStatementSyntax = GetEnumStatement(code)

            Dim actualRegion = GetRegion(enumStatement)
            Dim expectedRegion = New OutliningSpan(
                TextSpan.FromBounds(1, 43),
                TextSpan.FromBounds(1, 51),
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
Public Enum Foo
    Bar
    Baz
End Enum
]]></code>

            Dim enumStatement As EnumStatementSyntax = GetEnumStatement(code)

            Dim actualRegion = GetRegion(enumStatement)
            Dim expectedRegion = New OutliningSpan(
                TextSpan.FromBounds(1, 43),
                TextSpan.FromBounds(1, 58),
                VisualBasicOutliningHelpers.Ellipsis,
                autoCollapse:=True)

            AssertRegion(expectedRegion, actualRegion)
        End Sub
    End Class
End Namespace
