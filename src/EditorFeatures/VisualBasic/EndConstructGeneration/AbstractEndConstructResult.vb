' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.VisualStudio.Text
Imports Microsoft.VisualStudio.Text.Editor
Imports Microsoft.VisualStudio.Text.Operations

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.EndConstructGeneration
    Friend MustInherit Class AbstractEndConstructResult
        Public MustOverride Sub Apply(textView As ITextView,
                  subjectBuffer As ITextBuffer,
                  caretPosition As Integer,
                  smartIndentationService As ISmartIndentationService,
                  undoHistoryRegistry As ITextUndoHistoryRegistry,
                  editorOperationsFactoryService As IEditorOperationsFactoryService)

        Protected Shared Sub SetIndentForFirstBlankLine(textView As ITextView, subjectBuffer As ITextBuffer, smartIndentationService As ISmartIndentationService, cursorLine As ITextSnapshotLine)
            For lineNumber = cursorLine.LineNumber To subjectBuffer.CurrentSnapshot.LineCount
                Dim line = subjectBuffer.CurrentSnapshot.GetLineFromLineNumber(lineNumber)

                If String.IsNullOrWhiteSpace(line.GetText()) Then
                    Dim indent = textView.GetDesiredIndentation(smartIndentationService, line)
                    If indent.HasValue Then
                        textView.TryMoveCaretToAndEnsureVisible(New VirtualSnapshotPoint(line.Start, indent.Value))
                    Else
                        textView.TryMoveCaretToAndEnsureVisible(New VirtualSnapshotPoint(line.Start, 0))
                    End If

                    Return
                End If
            Next
        End Sub
    End Class
End Namespace
