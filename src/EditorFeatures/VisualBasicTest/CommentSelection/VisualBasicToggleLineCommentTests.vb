' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CommentSelection
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Test.Utilities.CommentSelection
Imports Microsoft.VisualStudio.Commanding
Imports Microsoft.VisualStudio.Composition

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CommentSelection
    <[UseExportProvider]>
    Public Class VisualBasicToggleLineCommentTests
        Inherits AbstractToggleCommentTestBase

        <WpfFact, Trait(Traits.Feature, Traits.Features.ToggleLineComment)>
        Public Sub AddComment()
            Dim markup =
<code>
Class A
    [|Function M()
        Dim a = 1

    End Function|]
End Class
</code>.Value
            Dim expected =
<code>
Class A
    [|'Function M()
    '    Dim a = 1

    'End Function|]
End Class
</code>.Value

            ToggleAndReplaceLineEndings(markup, expected)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ToggleLineComment)>
        Public Sub RemoveComment()
            Dim markup =
<code>
Class A
    [|'Function M()
    '    Dim a = 1

    'End Function|]
End Class
</code>.Value
            Dim expected =
<code>
Class A
    [|Function M()
        Dim a = 1

    End Function|]
End Class
</code>.Value

            ToggleAndReplaceLineEndings(markup, expected)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ToggleLineComment)>
        Public Sub ToggleComment_Multiple()
            Dim markup =
<code>
Class A
    [|Function M()
        Dim a = 1

    End Function|]
End Class
</code>.Value
            Dim expected As String() =
            {
<code>
Class A
    [|'Function M()
    '    Dim a = 1

    'End Function|]
End Class
</code>.Value,
<code>
Class A
    [|Function M()
        Dim a = 1

    End Function|]
End Class
</code>.Value
            }

            ToggleAndReplaceLineEndingsMultiple(markup, expected)
        End Sub

        Private Sub ToggleAndReplaceLineEndings(markup As String, expected As String)
            markup = ReplaceLineEndings(markup)
            expected = ReplaceLineEndings(expected)
            ToggleComment(markup, expected)
        End Sub

        Private Sub ToggleAndReplaceLineEndingsMultiple(markup As String, expected As String())
            markup = ReplaceLineEndings(markup)
            expected = expected.Select(Function(s) ReplaceLineEndings(s)).ToArray()
            ToggleCommentMultiple(markup, expected)
        End Sub

        Private Shared Function ReplaceLineEndings(markup As String) As String
            ' do this since xml value put only vbLf
            Return markup.Replace(vbLf, vbCrLf)
        End Function

        Friend Overrides Function GetToggleCommentCommandHandler(workspace As TestWorkspace) As AbstractCommentSelectionBase(Of ValueTuple)
            Return DirectCast(
                workspace.ExportProvider.GetExportedValues(Of ICommandHandler)().First(Function(export) TypeOf export Is ToggleLineCommentCommandHandler),
                AbstractCommentSelectionBase(Of ValueTuple))
        End Function

        Friend Overrides Function GetWorkspace(markup As String, composition As TestComposition) As TestWorkspace
            Return TestWorkspace.CreateVisualBasic(markup, composition:=composition)
        End Function
    End Class
End Namespace
