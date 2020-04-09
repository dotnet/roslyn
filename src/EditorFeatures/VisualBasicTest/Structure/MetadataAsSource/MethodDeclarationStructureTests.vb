' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Structure
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Outlining.MetadataAsSource
    Public Class MethodDeclarationStructureProviderTests
        Inherits AbstractVisualBasicSyntaxNodeStructureProviderTests(Of MethodStatementSyntax)

        Protected Overrides ReadOnly Property WorkspaceKind As String
            Get
                Return CodeAnalysis.WorkspaceKind.MetadataAsSource
            End Get
        End Property

        Friend Overrides Function CreateProvider() As AbstractSyntaxStructureProvider
            Return New MethodDeclarationStructureProvider()
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)>
        Public Async Function NoCommentsOrAttributes() As Task
            Dim code = "
Class C
    {|hint:{|textspan:Sub $$M()
    End Sub|}|}
End Class
"

            Await VerifyBlockSpansAsync(code,
                Region("textspan", "hint", "Sub M() " & VisualBasicOutliningHelpers.Ellipsis, autoCollapse:=True))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)>
        Public Async Function WithAttributes() As Task
            Dim code = "
Class C
    {|hint:{|textspan:<Goo>
    |}Sub $$M()|}
    End Sub
End Class
"

            Await VerifyBlockSpansAsync(code,
                Region("textspan", "hint", VisualBasicOutliningHelpers.Ellipsis, autoCollapse:=True),
                New BlockSpan(
                    isCollapsible:=True,
                    textSpan:=TextSpan.FromBounds(15, 46),
                    hintSpan:=TextSpan.FromBounds(26, 46),
                    type:=BlockTypes.Nonstructural,
                    bannerText:="<Goo> Sub M() " & Ellipsis,
                    autoCollapse:=True))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)>
        Public Async Function WithCommentsAndAttributes() As Task
            Dim code = "
Class C
    {|hint:{|textspan:' Summary:
    '     This is a summary.
    <Goo>
    |}Sub $$M()|}
    End Sub
End Class
"

            Await VerifyBlockSpansAsync(code,
                Region("textspan", "hint", VisualBasicOutliningHelpers.Ellipsis, autoCollapse:=True),
                New BlockSpan(
                    isCollapsible:=True,
                    textSpan:=TextSpan.FromBounds(61, 92),
                    hintSpan:=TextSpan.FromBounds(72, 92),
                    type:=BlockTypes.Nonstructural,
                    bannerText:="<Goo> Sub M() " & Ellipsis,
                    autoCollapse:=True))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)>
        Public Async Function WithCommentsAttributesAndModifiers() As Task
            Dim code = "
Class C
    {|hint:{|textspan:' Summary:
    '     This is a summary.
    <Goo>
    |}Public Sub $$M()|}
    End Sub
End Class
"

            Await VerifyBlockSpansAsync(code,
                Region("textspan", "hint", VisualBasicOutliningHelpers.Ellipsis, autoCollapse:=True),
                New BlockSpan(
                    isCollapsible:=True,
                    textSpan:=TextSpan.FromBounds(61, 99),
                    hintSpan:=TextSpan.FromBounds(72, 99),
                    type:=BlockTypes.Nonstructural,
                    bannerText:="<Goo> Public Sub M() " & Ellipsis,
                    autoCollapse:=True))
        End Function
    End Class
End Namespace
