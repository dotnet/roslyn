' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.Outlining
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports MaSOutliners = Microsoft.CodeAnalysis.Editor.VisualBasic.Outlining.MetadataAsSource
Imports Microsoft.CodeAnalysis.Editor.Implementation.Outlining

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Outlining.MetadataAsSource
    Public Class EnumMemberDeclarationOutlinerTests
        Inherits AbstractOutlinerTests(Of EnumMemberDeclarationSyntax)

        Friend Overrides Function GetRegions(node As EnumMemberDeclarationSyntax) As IEnumerable(Of OutliningSpan)
            Dim outliner = New MaSOutliners.EnumMemberDeclarationOutliner()
            Return outliner.GetOutliningSpans(node, CancellationToken.None).WhereNotNull()
        End Function

        Private Shared Function GetEnumMemberNode(code As Xml.Linq.XElement) As EnumMemberDeclarationSyntax
            Dim tree = ParseCode(code.Value)
            Dim typeDecl = tree.DigToFirstNodeOfType(Of EnumBlockSyntax)()
            Dim memberDecl = typeDecl.DigToFirstNodeOfType(Of EnumMemberDeclarationSyntax)()
            Return memberDecl
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)>
        Public Sub NoCommentsOrAttributes()
            Dim code =
<code><![CDATA[
Enum E
    Foo
    Bar
End Enum
]]></code>

            Dim enumMember = GetEnumMemberNode(code)
            Assert.Empty(GetRegions(enumMember))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)>
        Public Sub WithAttributes()
            Dim code =
<code><![CDATA[
Enum E
    <Blah>
    Foo,
    Bar
End Enum
]]></code>

            Dim enumMember = GetEnumMemberNode(code)

            Dim actualRegion = GetRegion(enumMember)
            Dim expectedRegion = New OutliningSpan(
                TextSpan.FromBounds(12, 23),
                TextSpan.FromBounds(12, 26),
                VisualBasicOutliningHelpers.Ellipsis,
                autoCollapse:=True)

            AssertRegion(expectedRegion, actualRegion)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)>
        Public Sub WithCommentsAndAttributes()
            Dim code =
<code><![CDATA[
Enum E
    ' Summary:
    '     This is a summary.
    <Blah>
    Foo,
    Bar
End Enum
]]></code>

            Dim enumMember = GetEnumMemberNode(code)

            Dim actualRegion = GetRegion(enumMember)
            Dim expectedRegion = New OutliningSpan(
                TextSpan.FromBounds(12, 67),
                TextSpan.FromBounds(12, 70),
                VisualBasicOutliningHelpers.Ellipsis,
                autoCollapse:=True)

            AssertRegion(expectedRegion, actualRegion)
        End Sub
    End Class
End Namespace
