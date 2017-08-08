' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Structure.MetadataAsSource
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Outlining.MetadataAsSource
    Public Class OperatorDeclarationStructureProviderTests
        Inherits AbstractVisualBasicSyntaxNodeStructureProviderTests(Of OperatorStatementSyntax)

        Protected Overrides ReadOnly Property WorkspaceKind As String
            Get
                Return CodeAnalysis.WorkspaceKind.MetadataAsSource
            End Get
        End Property

        Friend Overrides Function CreateProvider() As AbstractSyntaxStructureProvider
            Return New MetadataOperatorDeclarationStructureProvider()
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)>
        Public Async Function NoCommentsOrAttributes() As Task
            Dim code = "
Class C
    Public Shared Operator $$=(a As C, b As C) As Boolean
End Class
"

            Await VerifyNoBlockSpansAsync(code)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)>
        Public Async Function WithAttributes() As Task
            Dim code = "
Class C
    {|hint:{|textspan:<Goo>
    |}Public Shared Operator $$=(a As C, b As C) As Boolean|}
End Class
"

            Await VerifyBlockSpansAsync(code,
                Region("textspan", "hint", VisualBasicOutliningHelpers.Ellipsis, autoCollapse:=True))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)>
        Public Async Function WithCommentsAndAttributes() As Task
            Dim code = "
Class C
    {|hint:{|textspan:' Summary:
    '     This is a summary.
    <Goo>
    |}Operator $$=(a As C, b As C) As Boolean|}
End Class
"

            Await VerifyBlockSpansAsync(code,
                Region("textspan", "hint", VisualBasicOutliningHelpers.Ellipsis, autoCollapse:=True))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)>
        Public Async Function WithCommentsAttributesAndModifiers() As Task
            Dim code = "
Class C
    {|hint:{|textspan:' Summary:
    '     This is a summary.
    <Goo>
    |}Public Shared Operator $$=(a As C, b As C) As Boolean|}
End Class
"

            Await VerifyBlockSpansAsync(code,
                Region("textspan", "hint", VisualBasicOutliningHelpers.Ellipsis, autoCollapse:=True))
        End Function
    End Class
End Namespace
