' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Outlining.MetadataAsSource
    <Trait(Traits.Feature, Traits.Features.MetadataAsSource)>
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

        <Fact>
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

        <Fact>
        Public Async Function WithAttributes() As Task
            Dim code = "
{|textspan2:{|hint:{|textspan:<Goo>
|}{|#0:Enum $$Goo|}
    Bar
    Baz
End Enum|#0}|}
"

            Await VerifyBlockSpansAsync(code,
                Region("textspan", "hint", VisualBasicOutliningHelpers.Ellipsis, autoCollapse:=True),
                Region("textspan2", "#0", "<Goo> Enum Goo " & VisualBasicOutliningHelpers.Ellipsis, autoCollapse:=True))
        End Function

        <Fact>
        Public Async Function WithCommentsAndAttributes() As Task
            Dim code = "
{|hint:{|textspan:' Summary:
'     This is a summary.
{|#1:<Goo>
|}{|#0:Enum $$Goo|}
    Bar
    Baz
End Enum|#0}|#1}
"

            Await VerifyBlockSpansAsync(code,
                Region("textspan", "hint", VisualBasicOutliningHelpers.Ellipsis, autoCollapse:=True),
                Region("#1", "#0", "<Goo> Enum Goo " & VisualBasicOutliningHelpers.Ellipsis, autoCollapse:=True))
        End Function

        <Fact>
        Public Async Function WithCommentsAttributesAndModifiers() As Task
            Dim code = "
{|hint:{|textspan:' Summary:
'     This is a summary.
{|#1:<Goo>
|}{|#0:Public Enum $$Goo|}
    Bar
    Baz
End Enum|#0}|#1}
"

            Await VerifyBlockSpansAsync(code,
                Region("textspan", "hint", VisualBasicOutliningHelpers.Ellipsis, autoCollapse:=True),
                Region("#1", "#0", "<Goo> Public Enum Goo " & VisualBasicOutliningHelpers.Ellipsis, autoCollapse:=True))
        End Function
    End Class
End Namespace
