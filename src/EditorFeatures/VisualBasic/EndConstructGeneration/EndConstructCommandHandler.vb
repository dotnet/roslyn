' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.ComponentModel.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.CodeCleanup
Imports Microsoft.CodeAnalysis.CodeCleanup.Providers
Imports Microsoft.CodeAnalysis.Editor.Implementation.EndConstructGeneration
Imports Microsoft.CodeAnalysis.Editor.Shared.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.VisualStudio.Commanding
Imports Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion
Imports Microsoft.VisualStudio.Text
Imports Microsoft.VisualStudio.Text.Editor
Imports Microsoft.VisualStudio.Text.Editor.Commanding.Commands
Imports Microsoft.VisualStudio.Text.Operations
Imports Microsoft.VisualStudio.Utilities
Imports VSCommanding = Microsoft.VisualStudio.Commanding

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.EndConstructGeneration
    <Export(GetType(VSCommanding.ICommandHandler))>
    <ContentType(ContentTypeNames.VisualBasicContentType)>
    <Name(PredefinedCommandHandlerNames.EndConstruct)>
    <Order(After:=PredefinedCompletionNames.CompletionCommandHandler)>
    <Order(After:=PredefinedCommandHandlerNames.AutomaticLineEnder)>
    Friend Class EndConstructCommandHandler
        Implements IChainedCommandHandler(Of ReturnKeyCommandArgs)
        Implements IChainedCommandHandler(Of TypeCharCommandArgs)
        Implements IChainedCommandHandler(Of AutomaticLineEnderCommandArgs)

        Private ReadOnly _editorOperationsFactoryService As IEditorOperationsFactoryService
        Private ReadOnly _undoHistoryRegistry As ITextUndoHistoryRegistry

        Public ReadOnly Property DisplayName As String Implements INamed.DisplayName
            Get
                Return VBEditorResources.End_Construct
            End Get
        End Property

        <ImportingConstructor()>
        Public Sub New(editorOperationsFactoryService As IEditorOperationsFactoryService,
                       undoHistoryRegistry As ITextUndoHistoryRegistry)

            Me._editorOperationsFactoryService = editorOperationsFactoryService
            Me._undoHistoryRegistry = undoHistoryRegistry
        End Sub

        Public Function GetCommandState_ReturnKeyCommandHandler(args As ReturnKeyCommandArgs, nextHandler As Func(Of VSCommanding.CommandState)) As VSCommanding.CommandState Implements IChainedCommandHandler(Of ReturnKeyCommandArgs).GetCommandState
            Return nextHandler()
        End Function

        Public Sub ExecuteCommand_ReturnKeyCommandHandler(args As ReturnKeyCommandArgs, nextHandler As Action, context As CommandExecutionContext) Implements IChainedCommandHandler(Of ReturnKeyCommandArgs).ExecuteCommand
            ExecuteEndConstructOnReturn(args.TextView, args.SubjectBuffer, nextHandler)
        End Sub

        Public Function GetCommandState_TypeCharCommandHandler(args As TypeCharCommandArgs, nextHandler As Func(Of VSCommanding.CommandState)) As VSCommanding.CommandState Implements IChainedCommandHandler(Of TypeCharCommandArgs).GetCommandState
            Return nextHandler()
        End Function

        Public Sub ExecuteCommand_TypeCharCommandHandler(args As TypeCharCommandArgs, nextHandler As Action, context As CommandExecutionContext) Implements IChainedCommandHandler(Of TypeCharCommandArgs).ExecuteCommand
            nextHandler()

            If Not args.SubjectBuffer.GetFeatureOnOffOption(FeatureOnOffOptions.EndConstruct) Then
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

        Public Function GetCommandState_AutomaticLineEnderCommandHandler(args As AutomaticLineEnderCommandArgs, nextHandler As Func(Of VSCommanding.CommandState)) As VSCommanding.CommandState Implements IChainedCommandHandler(Of AutomaticLineEnderCommandArgs).GetCommandState
            Return VSCommanding.CommandState.Available
        End Function

        Public Sub ExecuteCommand_AutomaticLineEnderCommandHandler(args As AutomaticLineEnderCommandArgs, nextHandler As Action, context As CommandExecutionContext) Implements IChainedCommandHandler(Of AutomaticLineEnderCommandArgs).ExecuteCommand
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
            If Not subjectBuffer.GetFeatureOnOffOption(FeatureOnOffOptions.EndConstruct) OrElse
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

            Dim codeCleanups = CodeCleaner.GetDefaultProviders(document).
                WhereAsArray(Function(p)
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
