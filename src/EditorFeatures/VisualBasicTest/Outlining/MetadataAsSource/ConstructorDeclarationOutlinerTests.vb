' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.Outlining
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports MaSOutliners = Microsoft.CodeAnalysis.Editor.VisualBasic.Outlining.MetadataAsSource
Imports Microsoft.CodeAnalysis.Editor.Implementation.Outlining

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Outlining.MetadataAsSource
    Public Class ConstructorDeclarationOutlinerTests
        Inherits AbstractOutlinerTests(Of SubNewStatementSyntax)

        Friend Overrides Function GetRegions(node As SubNewStatementSyntax) As IEnumerable(Of OutliningSpan)
            Dim outliner = New MaSOutliners.ConstructorDeclarationOutliner()
            Return outliner.GetOutliningSpans(node, CancellationToken.None).WhereNotNull()
        End Function

        Private Shared Function GetConstructor(code As Xml.Linq.XElement) As SubNewStatementSyntax
            Dim tree = ParseCode(code.Value)
            Dim typeDecl = tree.DigToFirstTypeBlock()
            Dim consDecl = typeDecl.DigToFirstNodeOfType(Of MethodBlockBaseSyntax)()
            Dim consStatement = TryCast(consDecl.BlockStatement, SubNewStatementSyntax)
            Return consStatement
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)>
        Public Sub NoCommentsOrAttributes()
            Dim code =
<code><![CDATA[
Class C
    Sub New()
    End Sub
End Class
]]></code>

            Dim consStatement As SubNewStatementSyntax = GetConstructor(code)
            Assert.Empty(GetRegions(consStatement))
        End Sub



        <WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)>
        Public Sub WithAttributes()
            Dim code =
<code><![CDATA[
Class C
    <Foo>
    Sub New()
    End Sub
End Class
]]></code>

            Dim consStatement = GetConstructor(code)

            Dim actualRegion = GetRegion(consStatement)
            Dim expectedRegion = New OutliningSpan(
                TextSpan.FromBounds(13, 23),
                TextSpan.FromBounds(13, 32),
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
    Sub New()
    End Sub
End Class
]]></code>

            Dim consStatement = GetConstructor(code)

            Dim actualRegion = GetRegion(consStatement)
            Dim expectedRegion = New OutliningSpan(
                TextSpan.FromBounds(13, 67),
                TextSpan.FromBounds(13, 76),
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
    Public Sub New()
    End Sub
End Class
]]></code>

            Dim consStatement = GetConstructor(code)

            Dim actualRegion = GetRegion(consStatement)
            Dim expectedRegion = New OutliningSpan(
                TextSpan.FromBounds(13, 67),
                TextSpan.FromBounds(13, 83),
                VisualBasicOutliningHelpers.Ellipsis,
                autoCollapse:=True)

            AssertRegion(expectedRegion, actualRegion)
        End Sub
    End Class
End Namespace

