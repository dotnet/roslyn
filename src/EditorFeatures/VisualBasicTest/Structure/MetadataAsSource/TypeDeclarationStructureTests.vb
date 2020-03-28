' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Structure
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Outlining.MetadataAsSource
    Public Class TypeDeclarationStructureProviderTests
        Inherits AbstractVisualBasicSyntaxNodeStructureProviderTests(Of TypeStatementSyntax)

        Protected Overrides ReadOnly Property WorkspaceKind As String
            Get
                Return CodeAnalysis.WorkspaceKind.MetadataAsSource
            End Get
        End Property

        Friend Overrides Function CreateProvider() As AbstractSyntaxStructureProvider
            Return New TypeDeclarationStructureProvider()
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)>
        Public Async Function NoCommentsOrAttributes() As Task
            Dim code = "
{|hint:{|textspan:Class $$C
End Class|}|}
"

            Await VerifyBlockSpansAsync(code,
                Region("textspan", "hint", "Class C " & VisualBasicOutliningHelpers.Ellipsis, autoCollapse:=False))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)>
        Public Async Function WithAttributes() As Task
            Dim code = "
{|hint:{|textspan:<Goo>
|}Class $$C|}
End Class
"

            Await VerifyBlockSpansAsync(code,
                Region("textspan", "hint", VisualBasicOutliningHelpers.Ellipsis, autoCollapse:=True),
                New BlockSpan(
                    isCollapsible:=True,
                    textSpan:=TextSpan.FromBounds(2, 27),
                    hintSpan:=TextSpan.FromBounds(9, 27),
                    type:=BlockTypes.Nonstructural,
                    bannerText:="<Goo> Class C " & Ellipsis,
                    autoCollapse:=False))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)>
        Public Async Function WithCommentsAndAttributes() As Task
            Dim code = "
{|hint:{|textspan:' Summary:
'     This is a summary.
<Goo>
|}Class $$C|}
End Class
"

            Await VerifyBlockSpansAsync(code,
                Region("textspan", "hint", VisualBasicOutliningHelpers.Ellipsis, autoCollapse:=True),
                New BlockSpan(
                    isCollapsible:=True,
                    textSpan:=TextSpan.FromBounds(40, 65),
                    hintSpan:=TextSpan.FromBounds(47, 65),
                    type:=BlockTypes.Nonstructural,
                    bannerText:="<Goo> Class C " & Ellipsis,
                    autoCollapse:=False))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)>
        Public Async Function WithCommentsAttributesAndModifiers() As Task
            Dim code = "
{|hint:{|textspan:' Summary:
'     This is a summary.
<Goo>
|}Public Class $$C|}
End Class
"

            Await VerifyBlockSpansAsync(code,
                Region("textspan", "hint", VisualBasicOutliningHelpers.Ellipsis, autoCollapse:=True),
                New BlockSpan(
                    isCollapsible:=True,
                    textSpan:=TextSpan.FromBounds(40, 72),
                    hintSpan:=TextSpan.FromBounds(47, 72),
                    type:=BlockTypes.Nonstructural,
                    bannerText:="<Goo> Public Class C " & Ellipsis,
                    autoCollapse:=False))
        End Function
    End Class
End Namespace
