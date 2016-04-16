' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Editor.Implementation.Outlining
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.Outlining
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports MaSOutliners = Microsoft.CodeAnalysis.Editor.VisualBasic.Outlining.MetadataAsSource

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Outlining.MetadataAsSource
    Public Class EnumMemberDeclarationOutlinerTests
        Inherits AbstractVisualBasicSyntaxNodeOutlinerTests(Of EnumMemberDeclarationSyntax)

        Protected Overrides ReadOnly Property WorkspaceKind As String
            Get
                Return CodeAnalysis.WorkspaceKind.MetadataAsSource
            End Get
        End Property

        Friend Overrides Function CreateOutliner() As AbstractSyntaxOutliner
            Return New MaSOutliners.EnumMemberDeclarationOutliner()
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)>
        Public Async Function NoCommentsOrAttributes() As Task
            Dim code = "
Enum E
    $$Foo
    Bar
End Enum
"

            Await VerifyNoRegionsAsync(code)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)>
        Public Async Function WithAttributes() As Task
            Dim code = "
Enum E
    {|hint:{|collapse:<Blah>
    |}$$Foo|}
    Bar
End Enum
"

            Await VerifyRegionsAsync(code,
                Region("collapse", "hint", VisualBasicOutliningHelpers.Ellipsis, autoCollapse:=True))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)>
        Public Async Function WithCommentsAndAttributes() As Task
            Dim code = "
Enum E
    {|hint:{|collapse:' Summary:
    '     This is a summary.
    <Blah>
    |}$$Foo|}
    Bar
End Enum
"

            Await VerifyRegionsAsync(code,
                Region("collapse", "hint", VisualBasicOutliningHelpers.Ellipsis, autoCollapse:=True))
        End Function

    End Class
End Namespace
