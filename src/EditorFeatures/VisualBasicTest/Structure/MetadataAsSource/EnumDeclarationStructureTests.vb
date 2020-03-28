' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Structure
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Outlining.MetadataAsSource
    Public Class EnumDeclarationStructureProviderTests
        Inherits AbstractVisualBasicSyntaxNodeStructureProviderTests(Of EnumStatementSyntax)

        Protected Overrides ReadOnly Property WorkspaceKind As String
            Get
                Return CodeAnalysis.WorkspaceKind.MetadataAsSource
            End Get
        End Property

        Friend Overrides Function CreateProvider() As AbstractSyntaxStructureProvider
            Return New EnumDeclarationStructureProvider()
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)>
        Public Async Function NoCommentsOrAttributes() As Task
            Dim code = "
{|hint:{|textspan:Enum $$Goo
    Bar
    Baz
End Enum|}|}
"

            Await VerifyBlockSpansAsync(code,
                Region("textspan", "hint", "Enum Goo " & Ellipsis, autoCollapse:=True))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)>
        Public Async Function WithAttributes() As Task
            Dim code = "
{|hint:{|textspan:<Goo>
|}Enum $$Goo|}
    Bar
    Baz
End Enum
"

            Await VerifyBlockSpansAsync(code,
                Region("textspan", "hint", VisualBasicOutliningHelpers.Ellipsis, autoCollapse:=True),
                New BlockSpan(
                    isCollapsible:=True,
                    textSpan:=TextSpan.FromBounds(2, 45),
                    hintSpan:=TextSpan.FromBounds(9, 45),
                    type:=BlockTypes.Nonstructural,
                    bannerText:="<Goo> Enum Goo " & Ellipsis,
                    autoCollapse:=True))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)>
        Public Async Function WithCommentsAndAttributes() As Task
            Dim code = "
{|hint:{|textspan:' Summary:
'     This is a summary.
<Goo>
|}Enum $$Goo|}
    Bar
    Baz
End Enum
"

            Await VerifyBlockSpansAsync(code,
                Region("textspan", "hint", VisualBasicOutliningHelpers.Ellipsis, autoCollapse:=True),
                New BlockSpan(
                    isCollapsible:=True,
                    textSpan:=TextSpan.FromBounds(40, 83),
                    hintSpan:=TextSpan.FromBounds(47, 83),
                    type:=BlockTypes.Nonstructural,
                    bannerText:="<Goo> Enum Goo " & Ellipsis,
                    autoCollapse:=True))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)>
        Public Async Function WithCommentsAttributesAndModifiers() As Task
            Dim code = "
{|hint:{|textspan:' Summary:
'     This is a summary.
<Goo>
|}Public Enum $$Goo|}
    Bar
    Baz
End Enum
"

            Await VerifyBlockSpansAsync(code,
                Region("textspan", "hint", VisualBasicOutliningHelpers.Ellipsis, autoCollapse:=True),
                New BlockSpan(
                    isCollapsible:=True,
                    textSpan:=TextSpan.FromBounds(40, 90),
                    hintSpan:=TextSpan.FromBounds(47, 90),
                    type:=BlockTypes.Nonstructural,
                    bannerText:="<Goo> Public Enum Goo " & Ellipsis,
                    autoCollapse:=True))
        End Function
    End Class
End Namespace
