' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Editor.Implementation.Outlining
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.Outlining
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports MaSOutliners = Microsoft.CodeAnalysis.Editor.VisualBasic.Outlining.MetadataAsSource

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Outlining.MetadataAsSource
    Public Class ConstructorDeclarationOutlinerTests
        Inherits AbstractVisualBasicSyntaxNodeOutlinerTests(Of SubNewStatementSyntax)

        Protected Overrides ReadOnly Property WorkspaceKind As String
            Get
                Return CodeAnalysis.WorkspaceKind.MetadataAsSource
            End Get
        End Property

        Friend Overrides Function CreateOutliner() As AbstractSyntaxOutliner
            Return New MaSOutliners.ConstructorDeclarationOutliner()
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)>
        Public Async Function NoCommentsOrAttributes() As Task
            Dim code = "
Class C
    Sub $$New()
    End Sub
End Class
"

            Await VerifyNoRegionsAsync(code)
        End Function



        <Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)>
        Public Async Function WithAttributes() As Task
            Dim code = "
Class C
    {|hint:{|collapse:<Foo>
    |}Sub $$New()|}
    End Sub
End Class
"

            Await VerifyRegionsAsync(code,
                Region("collapse", "hint", VisualBasicOutliningHelpers.Ellipsis, autoCollapse:=True))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)>
        Public Async Function WithCommentsAndAttributes() As Task
            Dim code = "
Class C
   {|hint:{|collapse:' Summary:
    '     This is a summary.
    <Foo>
    |}Sub $$New()|}
    End Sub
End Class
"

            Await VerifyRegionsAsync(code,
                Region("collapse", "hint", VisualBasicOutliningHelpers.Ellipsis, autoCollapse:=True))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)>
        Public Async Function WithCommentsAttributesAndModifiers() As Task
            Dim code = "
Class C
    {|hint:{|collapse:' Summary:
    '     This is a summary.
    <Foo>
    |}Public Sub $$New()|}
    End Sub
End Class
"

            Await VerifyRegionsAsync(code,
                Region("collapse", "hint", VisualBasicOutliningHelpers.Ellipsis, autoCollapse:=True))
        End Function

    End Class
End Namespace

