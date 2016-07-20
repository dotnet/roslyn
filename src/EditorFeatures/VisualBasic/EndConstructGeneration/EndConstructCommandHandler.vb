' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.ComponentModel.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.CodeCleanup
Imports Microsoft.CodeAnalysis.CodeCleanup.Providers
Imports Microsoft.CodeAnalysis.Editor.Commands
Imports Microsoft.CodeAnalysis.Editor.Implementation.EndConstructGeneration
Imports Microsoft.CodeAnalysis.Editor.Shared.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.VisualStudio.Text
Imports Microsoft.VisualStudio.Text.Editor
Imports Microsoft.VisualStudio.Text.Operations
Imports Microsoft.VisualStudio.Utilities

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.EndConstructGeneration
    <ExportCommandHandler(PredefinedCommandHandlerNames.EndConstruct, ContentTypeNames.VisualBasicContentType)>
    <Order(After:=PredefinedCommandHandlerNames.Completion)>
    <Order(After:=PredefinedCommandHandlerNames.AutomaticLineEnder)>
    Friend Class EndConstructCommandHandler
        Implements ICommandHandler(Of ReturnKeyCommandArgs)
        Implements ICommandHandler(Of TypeCharCommandArgs)
        Implements ICommandHandler(Of AutomaticLineEnderCommandArgs)

        Private ReadOnly _editorOperationsFactoryService As IEditorOperationsFactoryService
        Private ReadOnly _undoHistoryRegistry As ITextUndoHistoryRegistry

        <ImportingConstructor()>
        Public Sub New(editorOperationsFactoryService As IEditorOperationsFactoryService,
                       undoHistoryRegistry As ITextUndoHistoryRegistry)

            Me._editorOperationsFactoryService = editorOperationsFactoryService
            Me._undoHistoryRegistry = undoHistoryRegistry
        End Sub

        Public Function GetCommandState_ReturnKeyCommandHandler(args As ReturnKeyCommandArgs, nextHandler As Func(Of CommandState)) As CommandState Implements ICommandHandler(Of ReturnKeyCommandArgs).GetCommandState
            Return nextHandler()
        End Function

        Public Sub ExecuteCommand_ReturnKeyCommandHandler(args As ReturnKeyCommandArgs, nextHandler As Action) Implements ICommandHandler(Of ReturnKeyCommandArgs).ExecuteCommand
            ExecuteEndConstructOnReturn(args.TextView, args.SubjectBuffer, nextHandler)
        End Sub

        Public Function GetCommandState_TypeCharCommandHandler(args As TypeCharCommandArgs, nextHandler As Func(Of CommandState)) As CommandState Implements ICommandHandler(Of TypeCharCommandArgs).GetCommandState
            Return nextHandler()
        End Function

        Public Sub ExecuteCommand_TypeCharCommandHandler(args As TypeCharCommandArgs, nextHandler As Action) Implements ICommandHandler(Of TypeCharCommandArgs).ExecuteCommand
            nextHandler()

            If Not args.SubjectBuffer.GetOption(FeatureOnOffOptions.EndConstruct) Then
                Return
            End If

            Dim textSnapshot = args.SubjectBuffer.CurrentSnapshot
            Dim document = textSnapshot.GetOpenDocumentInCurrentContextWithChanges()
            If document Is Nothing Then
                Return
            End If

            ' End construct is not cancellable.
            Dim endConstructService = document.GetLanguageService(Of IEndConstructGenerationService)()
            endConstructService.TryDo(args.TextView, args.SubjectBuffer, args.TypedChar, CancellationToken.None)
        End Sub

        Public Function GetCommandState_AutomaticLineEnderCommandHandler(args As AutomaticLineEnderCommandArgs, nextHandler As Func(Of CommandState)) As CommandState Implements ICommandHandler(Of AutomaticLineEnderCommandArgs).GetCommandState
            Return CommandState.Available
        End Function

        Public Sub ExecuteCommand_AutomaticLineEnderCommandHandler(args As AutomaticLineEnderCommandArgs, nextHandler As Action) Implements ICommandHandler(Of AutomaticLineEnderCommandArgs).ExecuteCommand
            ExecuteEndConstructOnReturn(args.TextView, args.SubjectBuffer, Sub()
                                                                               Dim operations = Me._editorOperationsFactoryService.GetEditorOperations(args.TextView)
                                                                               If operations Is Nothing Then
                                                                                   nextHandler()
                                                                               Else
                                                                                   operations.InsertNewLine()
                                                                               End If
                                                                           End Sub)
        End Sub

        Private Sub ExecuteEndConstructOnReturn(textView As ITextView, subjectBuffer As ITextBuffer, nextHandler As Action)
            If Not subjectBuffer.GetOption(FeatureOnOffOptions.EndConstruct) OrElse
               Not subjectBuffer.CanApplyChangeDocumentToWorkspace() Then
                nextHandler()
                Return
            End If

            Dim textSnapshot = subjectBuffer.CurrentSnapshot
            Dim document = textSnapshot.GetOpenDocumentInCurrentContextWithChanges()
            If document Is Nothing Then
                Return
            End If

            CleanupBeforeEndConstruct(textView, subjectBuffer, document, CancellationToken.None)

            Dim endConstructService = document.GetLanguageService(Of IEndConstructGenerationService)()
            Dim result = endConstructService.TryDo(textView, subjectBuffer, vbLf(0), CancellationToken.None)

            If Not result Then
                nextHandler()
                Return
            End If
        End Sub

        Private Sub CleanupBeforeEndConstruct(view As ITextView, buffer As ITextBuffer, document As Document, cancellationToken As CancellationToken)
            Dim position = view.GetCaretPoint(buffer)
            If Not position.HasValue Then
                Return
            End If

            Dim root = document.GetSyntaxRootSynchronously(cancellationToken)
            Dim statement = root.FindToken(position.Value).GetAncestor(Of StatementSyntax)()
            If statement Is Nothing OrElse TypeOf statement Is EmptyStatementSyntax OrElse
               Not statement.ContainsDiagnostics Then
                Return
            End If

            Dim codeCleanups = CodeCleaner.GetDefaultProviders(document) _
                        .Where(Function(p)
                                   Return p.Name = PredefinedCodeCleanupProviderNames.NormalizeModifiersOrOperators
                               End Function)

            Dim cleanDocument = CodeCleaner.CleanupAsync(document, GetSpanToCleanup(statement), codeCleanups, cancellationToken:=cancellationToken).WaitAndGetResult(cancellationToken)

            Using transaction = New CaretPreservingEditTransaction(VBEditorResources.End_Construct, view, _undoHistoryRegistry, _editorOperationsFactoryService)
                transaction.MergePolicy = AutomaticCodeChangeMergePolicy.Instance

                cleanDocument.Project.Solution.Workspace.ApplyDocumentChanges(cleanDocument, cancellationToken)
                transaction.Complete()
            End Using
        End Sub

        Private Function GetSpanToCleanup(statement As StatementSyntax) As TextSpan
            Dim firstToken = statement.GetFirstToken()
            Dim lastToken = statement.GetLastToken()

            Dim previousToken = firstToken.GetPreviousToken()
            Dim nextToken = lastToken.GetNextToken()

            Return TextSpan.FromBounds(If(previousToken.Kind <> SyntaxKind.None, previousToken, firstToken).SpanStart,
                                       If(nextToken.Kind <> SyntaxKind.None, nextToken, lastToken).Span.End)
        End Function
    End Class
End Namespace
