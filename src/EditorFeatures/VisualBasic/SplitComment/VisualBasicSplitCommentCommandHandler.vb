' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.ComponentModel.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.Implementation.SplitComment
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.VisualStudio.Commanding
Imports Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion
Imports Microsoft.VisualStudio.Text
Imports Microsoft.VisualStudio.Text.Editor.Commanding.Commands
Imports Microsoft.VisualStudio.Text.Operations
Imports Microsoft.VisualStudio.Utilities

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.SplitComment
    <Export(GetType(ICommandHandler))>
    <ContentType(ContentTypeNames.VisualBasicContentType)>
    <Name(NameOf(VisualBasicSplitCommentCommandHandler))>
    <Order(After:=PredefinedCompletionNames.CompletionCommandHandler)>
    Partial Friend Class VisualBasicSplitCommentCommandHandler
        Inherits AbstractSplitCommentCommandHandler

        <ImportingConstructor>
        Public Sub New(undoHistoryRegistry As ITextUndoHistoryRegistry,
                       editorOperationsFactoryService As IEditorOperationsFactoryService)
            _undoHistoryRegistry = undoHistoryRegistry
            _editorOperationsFactoryService = editorOperationsFactoryService
        End Sub

        Protected Overrides Function LineContainsComment(line As ITextSnapshotLine, caretPosition As Integer) As Boolean
            Dim snapshot = line.Snapshot
            Dim text = line.Snapshot.GetText()

            If caretPosition > line.End.Position Then
                Return False
            Else
                Return text.Contains(CommentSplitter.CommentCharacter)
            End If
        End Function

        Protected Overrides Function SplitComment(document As Document, options As DocumentOptionSet, position As Integer, cancellationToken As CancellationToken) As Integer?
            Dim useTabs = options.GetOption(FormattingOptions.UseTabs)
            Dim tabSize = options.GetOption(FormattingOptions.TabSize)
            Dim indentStyle = options.GetOption(FormattingOptions.SmartIndent, LanguageNames.VisualBasic)

            Dim root = document.GetSyntaxRootSynchronously(cancellationToken)
            Dim sourceText = root.SyntaxTree.GetText(cancellationToken)

            Dim splitter = CommentSplitter.TryCreate(
                document, position, root, sourceText,
                useTabs, tabSize, indentStyle, cancellationToken)

            If splitter Is Nothing Then
                Return Nothing
            End If

            Return splitter.TrySplit()
        End Function
    End Class
End Namespace
