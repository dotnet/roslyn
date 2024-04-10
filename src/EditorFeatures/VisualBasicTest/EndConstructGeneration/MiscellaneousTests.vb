' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.EndConstructGeneration
Imports Microsoft.VisualStudio.Text
Imports Microsoft.VisualStudio.Text.Editor
Imports Microsoft.VisualStudio.Text.Operations

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.EndConstructGeneration
    <[UseExportProvider]>
    <Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
    Public Class MiscellaneousTests
        <WpfFact>
        Public Sub DoesNothingOnEmptyFile()
            VerifyStatementEndConstructNotApplied(
                text:="",
                caret:={0, -1})
        End Sub

        <WpfFact>
        Public Sub DoesNothingOnFileWithNoStatement()
            VerifyStatementEndConstructNotApplied(
                text:="'Goo
",
                caret:={0, -1})
        End Sub

        <WpfFact>
        Public Sub VerifyLineContinuationMark()
            VerifyStatementEndConstructNotApplied(
                text:="Class C
    function f(byval x as Integer,
               byref y as string) as string
        for i = 1 to 10 _
        return y
    End Function
End Class",
                caret:={3, -1})
        End Sub

        <WpfFact>
        Public Sub VerifyImplicitLineContinuation()
            VerifyStatementEndConstructNotApplied(
                text:="Class C
    function f() as string
        While 1 +
        return y
    End Function
End Class",
                caret:={2, -1})
        End Sub

        <WpfFact>
        Public Sub TestVerifyNestedDo()
            VerifyStatementEndConstructApplied(
                before:="Class C
        function f() as string
            for i = 1 to 10",
                beforeCaret:={2, -1},
                 after:="Class C
        function f() as string
            for i = 1 to 10

            Next",
                afterCaret:={3, -1})
        End Sub

        <WpfFact>
        Public Sub TestVerifyMultilinesChar()
            VerifyStatementEndConstructApplied(
                before:="Class C
    sub s
        do :do
        Loop
    End sub
End Class",
                beforeCaret:={2, -1},
                 after:="Class C
    sub s
        do :do

            Loop
        Loop
    End sub
End Class",
                afterCaret:={3, -1})
        End Sub

        <WpfFact>
        Public Sub TestVerifyInlineComments()
            VerifyStatementEndConstructApplied(
                before:="Class C
    sub s
        If true then 'here
    End sub
End Class",
                beforeCaret:={2, -1},
                 after:="Class C
    sub s
        If true then 'here

        End If
    End sub
End Class",
                afterCaret:={3, -1})
        End Sub

        <WpfFact>
        Public Sub VerifyNotAppliedWithJunkAtEndOfLine()
            ' Try this without a newline at the end of the file
            VerifyStatementEndConstructNotApplied(
                text:="Class C End Class",
                caret:={0, "Class C".Length})
        End Sub

        <WpfFact>
        Public Sub VerifyNotAppliedWithJunkAtEndOfLine2()
            ' Try this with a newline at the end of the file
            VerifyStatementEndConstructNotApplied(
                text:="Class C End Class
",
                caret:={0, "Class C".Length})
        End Sub

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539727")>
        Public Sub DeletesSelectedText()
            Using workspace = EditorTestWorkspace.CreateVisualBasic("Interface IGoo ~~")
                Dim textView = workspace.Documents.Single().GetTextView()
                Dim subjectBuffer = workspace.Documents.First().GetTextBuffer()

                ' Select the ~~ backwards, so the caret location is at the start
                Dim startPoint = New SnapshotPoint(textView.TextSnapshot, textView.TextSnapshot.Length - 2)
                textView.TryMoveCaretToAndEnsureVisible(startPoint)
                textView.SetSelection(New SnapshotSpan(startPoint, length:=2))

                Dim endConstructService As New VisualBasicEndConstructService(
                    workspace.GetService(Of ISmartIndentationService),
                    workspace.GetService(Of ITextUndoHistoryRegistry),
                    workspace.GetService(Of IEditorOperationsFactoryService),
                    workspace.GetService(Of IEditorOptionsFactoryService))

                Assert.True(endConstructService.TryDoEndConstructForEnterKey(textView, textView.TextSnapshot.TextBuffer, CancellationToken.None))

                Assert.Equal("End Interface", textView.TextSnapshot.Lines.Last().GetText())
            End Using
        End Sub
    End Class
End Namespace
