' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.VisualStudio.Text
Imports Microsoft.VisualStudio.Text.Editor
Imports Microsoft.VisualStudio.Text.Editor.OptionsExtensionMethods
Imports Microsoft.VisualStudio.Text.Operations

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.EndConstructGeneration
    Friend Class SpitLinesResult
        Inherits AbstractEndConstructResult

        Private ReadOnly _lines As String()
        Private ReadOnly _startOnCurrentLine As Boolean

        Public Sub New(lines As IEnumerable(Of String),
                       Optional startOnCurrentLine As Boolean = False)
            _lines = lines.ToArray()

            ' At least one line must be blank for us to know where the caret should land
            Contract.ThrowIfFalse(_lines.Any(Function(line) String.IsNullOrWhiteSpace(line)))

            _startOnCurrentLine = startOnCurrentLine
        End Sub

        Public Overrides Sub Apply(textView As ITextView,
                                   subjectBuffer As ITextBuffer,
                                   caretPosition As Integer,
                                   smartIndentationService As ISmartIndentationService,
                                   undoHistoryRegistry As ITextUndoHistoryRegistry,
                                   editorOperationsFactoryService As IEditorOperationsFactoryService)

            Dim current = subjectBuffer.CurrentSnapshot
            Dim document = current.GetOpenDocumentInCurrentContextWithChanges()
            If document Is Nothing Then
                Return
            End If

            Dim bufferNewLine = textView.Options.GetNewLineCharacter()
            Dim currentLine = current.GetLineFromPosition(caretPosition)

            ' Join the lines together. As long as we aren't starting this text on the current line, we add a newline at
            ' the front, as we will be inserting this text before the newline of the line the caret is currently on.
            ' This is to guarantee that our _lines array is properly put in the next line, even if we're on the last in
            ' in the buffer
            Dim joinedLines = If(_startOnCurrentLine, "", bufferNewLine) + String.Join(bufferNewLine, _lines)

            document.Project.Solution.Workspace.ApplyTextChanges(
                document.Id, SpecializedCollections.SingletonEnumerable(New TextChange(New TextSpan(caretPosition, 0), joinedLines)), CancellationToken.None)

            SetIndentForFirstBlankLine(textView, subjectBuffer, smartIndentationService, currentLine)
        End Sub

    End Class
End Namespace
