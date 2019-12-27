' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
    <Name(NameOf(SplitCommentCommandHandler))>
    <Order(After:=PredefinedCompletionNames.CompletionCommandHandler)>
    Partial Friend Class SplitCommentCommandHandler
        Inherits AbstractSplitCommentCommandHandler

        <ImportingConstructor>
        Public Sub New(undoHistoryRegistry As ITextUndoHistoryRegistry,
                       editorOperationsFactoryService As IEditorOperationsFactoryService)
            _undoHistoryRegistry = undoHistoryRegistry
            _editorOperationsFactoryService = editorOperationsFactoryService
        End Sub

        Public Overrides Function ExecuteCommand(args As ReturnKeyCommandArgs, executionContext As CommandExecutionContext) As Boolean
            Return ExecuteCommandWorker(args)
        End Function

        Public Overrides Function GetCommandState(args As ReturnKeyCommandArgs) As CommandState
            Return CommandState.Unspecified
        End Function

        Protected Overrides Function LineContainsComment(line As ITextSnapshotLine, caretPosition As Integer) As Boolean
            Dim snapshot = line.Snapshot
            For i As Integer = line.Start To caretPosition Step 1
                If snapshot(i) = "'"c Then
                    Return True
                End If
            Next

            Return False
        End Function

        Protected Overrides Function SplitComment(document As Document, options As DocumentOptionSet, position As Integer, cancellationToken As CancellationToken) As Integer?
            Dim useTabs = options.GetOption(FormattingOptions.UseTabs)
            Dim tabSize = options.GetOption(FormattingOptions.TabSize)
            Dim indentStyle = options.GetOption(FormattingOptions.SmartIndent, LanguageNames.VisualBasic)

            Dim root = document.GetSyntaxRootSynchronously(cancellationToken)
            Dim sourceText = root.SyntaxTree.GetText(cancellationToken)


        End Function
    End Class
End Namespace
