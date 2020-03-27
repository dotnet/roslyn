' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Structure
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Outlining.MetadataAsSource
    Public Class ConstructorDeclarationStructureProviderTests
        Inherits AbstractVisualBasicSyntaxNodeStructureProviderTests(Of SubNewStatementSyntax)

        Protected Overrides ReadOnly Property WorkspaceKind As String
            Get
                Return CodeAnalysis.WorkspaceKind.MetadataAsSource
            End Get
        End Property

        Friend Overrides Function CreateProvider() As AbstractSyntaxStructureProvider
            Return New ConstructorDeclarationStructureProvider()
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)>
        Public Async Function NoCommentsOrAttributes() As Task
            Dim code = "
Class C
    {|hint:{|textspan:Sub $$New()
    End Sub|}|}
End Class
"

            Await VerifyBlockSpansAsync(code,
                Region("textspan", "hint", "Sub New() " & VisualBasicOutliningHelpers.Ellipsis, autoCollapse:=True))
        End Function



        <Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)>
        Public Async Function WithAttributes() As Task
            Dim code = "
Class C
    {|hint:{|textspan:<Goo>
    |}Sub $$New()|}
    End Sub
End Class
"

            Await VerifyBlockSpansAsync(code,
                Region("textspan", "hint", VisualBasicOutliningHelpers.Ellipsis, autoCollapse:=True),
                New BlockSpan(
                    isCollapsible:=True,
                    textSpan:=TextSpan.FromBounds(15, 48),
                    hintSpan:=TextSpan.FromBounds(26, 48),
                    type:=BlockTypes.Nonstructural,
                    bannerText:="<Goo> Sub New() " & Ellipsis,
                    autoCollapse:=True))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)>
        Public Async Function WithCommentsAndAttributes() As Task
            Dim code = "
Class C
   {|hint:{|textspan:' Summary:
    '     This is a summary.
    <Goo>
    |}Sub $$New()|}
    End Sub
End Class
"

            Await VerifyBlockSpansAsync(code,
                Region("textspan", "hint", VisualBasicOutliningHelpers.Ellipsis, autoCollapse:=True),
                New BlockSpan(
                    isCollapsible:=True,
                    textSpan:=TextSpan.FromBounds(60, 93),
                    hintSpan:=TextSpan.FromBounds(71, 93),
                    type:=BlockTypes.Nonstructural,
                    bannerText:="<Goo> Sub New() " & Ellipsis,
                    autoCollapse:=True))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)>
        Public Async Function WithCommentsAttributesAndModifiers() As Task
            Dim code = "
Class C
    {|hint:{|textspan:' Summary:
    '     This is a summary.
    <Goo>
    |}Public Sub $$New()|}
    End Sub
End Class
"

            Await VerifyBlockSpansAsync(code,
                Region("textspan", "hint", VisualBasicOutliningHelpers.Ellipsis, autoCollapse:=True),
                New BlockSpan(
                    isCollapsible:=True,
                    textSpan:=TextSpan.FromBounds(61, 101),
                    hintSpan:=TextSpan.FromBounds(72, 101),
                    type:=BlockTypes.Nonstructural,
                    bannerText:="<Goo> Public Sub New() " & Ellipsis,
                    autoCollapse:=True))
        End Function
    End Class
End Namespace
