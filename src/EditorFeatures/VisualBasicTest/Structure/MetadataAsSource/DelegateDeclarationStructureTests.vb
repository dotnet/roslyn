' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Structure.MetadataAsSource
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Outlining.MetadataAsSource
    Public Class DelegateDeclarationStructureProviderTests
        Inherits AbstractVisualBasicSyntaxNodeStructureProviderTests(Of DelegateStatementSyntax)

        Protected Overrides ReadOnly Property WorkspaceKind As String
            Get
                Return CodeAnalysis.WorkspaceKind.MetadataAsSource
            End Get
        End Property

        Friend Overrides Function CreateProvider() As AbstractSyntaxStructureProvider
            Return New MetadataDelegateDeclarationStructureProvider()
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)>
        Public Async Function NoCommentsOrAttributes() As Task
            Dim code = "
Delegate Sub $$Bar()
"

            Await VerifyNoBlockSpansAsync(code)
        End Function

        Public Delegate Sub Bar(x As Int16)

        <Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)>
        Public Async Function WithAttributes() As Task
            Dim code = "
{|hint:{|textspan:<Foo>
|}Delegate Sub $$Bar()|}
"

            Await VerifyBlockSpansAsync(code,
                Region("textspan", "hint", VisualBasicOutliningHelpers.Ellipsis, autoCollapse:=True))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)>
        Public Async Function WithCommentsAndAttributes() As Task
            Dim code = "
{|hint:{|textspan:' Summary:
'     This is a summary.
<Foo>
|}Delegate Sub $$Bar()|}
"

            Await VerifyBlockSpansAsync(code,
                Region("textspan", "hint", VisualBasicOutliningHelpers.Ellipsis, autoCollapse:=True))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)>
        Public Async Function WithCommentsAttributesAndModifiers() As Task
            Dim code = "
{|hint:{|textspan:' Summary:
'     This is a summary.
<Foo>
|}Public Delegate Sub $$Bar()|}
"

            Await VerifyBlockSpansAsync(code,
                Region("textspan", "hint", VisualBasicOutliningHelpers.Ellipsis, autoCollapse:=True))
        End Function
    End Class
End Namespace