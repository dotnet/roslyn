' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.VisualStudio.Text
Imports Microsoft.VisualStudio.Text.Editor
Imports Microsoft.VisualStudio.Text.Operations

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.EndConstructGeneration
    Friend Class ReplaceSpanResult
        Inherits AbstractEndConstructResult

        Private ReadOnly _snapshotSpan As SnapshotSpan
        Private ReadOnly _replacementText As String
        Private ReadOnly _newCaretPosition As Integer?

        Public Sub New(snapshotSpan As SnapshotSpan, replacementText As String, newCaretPosition As Integer?)
            ThrowIfNull(replacementText)

            _snapshotSpan = snapshotSpan
            _replacementText = replacementText
            _newCaretPosition = newCaretPosition
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

            Dim oldSolution = document.Project.Solution.Workspace.CurrentSolution
            Dim newSolution = oldSolution.UpdateDocument(
                document.Id, SpecializedCollections.SingletonEnumerable(
                    New TextChange(_snapshotSpan.TranslateTo(current, SpanTrackingMode.EdgeExclusive).Span.ToTextSpan(), _replacementText)), CancellationToken.None)

            oldSolution.Workspace.TryApplyChanges(newSolution)

            Dim startPoint = _snapshotSpan.Start.TranslateTo(subjectBuffer.CurrentSnapshot, PointTrackingMode.Negative)
            If _newCaretPosition IsNot Nothing Then
                textView.TryMoveCaretToAndEnsureVisible(startPoint + _newCaretPosition.Value)
            Else
                SetIndentForFirstBlankLine(textView, subjectBuffer, smartIndentationService, startPoint.GetContainingLine())
            End If
        End Sub
    End Class
End Namespace
