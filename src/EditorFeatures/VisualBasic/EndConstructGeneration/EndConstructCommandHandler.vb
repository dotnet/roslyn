' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.ComponentModel.Composition
Imports System.Diagnostics.CodeAnalysis
Imports System.Threading
Imports Microsoft.CodeAnalysis.AddImport
Imports Microsoft.CodeAnalysis.CodeCleanup
Imports Microsoft.CodeAnalysis.CodeCleanup.Providers
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.Editor.Implementation.EndConstructGeneration
Imports Microsoft.CodeAnalysis.Editor.Shared.Utilities
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.VisualStudio.Commanding
Imports Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion
Imports Microsoft.VisualStudio.Text
Imports Microsoft.VisualStudio.Text.Editor
Imports Microsoft.VisualStudio.Text.Editor.Commanding.Commands
Imports Microsoft.VisualStudio.Text.Operations
Imports Microsoft.VisualStudio.Utilities

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.EndConstructGeneration
    <Export(GetType(ICommandHandler))>
    <ContentType(ContentTypeNames.VisualBasicContentType)>
    <Name(PredefinedCommandHandlerNames.EndConstruct)>
    <Order(After:=PredefinedCompletionNames.CompletionCommandHandler)>
    <Order(After:=PredefinedCommandHandlerNames.AutomaticLineEnder)>
    Friend Class EndConstructCommandHandler
        Implements IChainedCommandHandler(Of ReturnKeyCommandArgs)
        Implements IChainedCommandHandler(Of TypeCharCommandArgs)
        Implements IChainedCommandHandler(Of AutomaticLineEnderCommandArgs)

        Private ReadOnly _threadingContext As IThreadingContext
        Private ReadOnly _editorOperationsFactoryService As IEditorOperationsFactoryService
        Private ReadOnly _undoHistoryRegistry As ITextUndoHistoryRegistry
        Private ReadOnly _editorOptionsService As EditorOptionsService

        <ImportingConstructor()>
        <SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification:="Used in test code: https://github.com/dotnet/roslyn/issues/42814")>
        Public Sub New(
                threadingContext As IThreadingContext,
                editorOperationsFactoryService As IEditorOperationsFactoryService,
                undoHistoryRegistry As ITextUndoHistoryRegistry,
                editorOptionsService As EditorOptionsService)
            _threadingContext = threadingContext
            _editorOperationsFactoryService = editorOperationsFactoryService
            _undoHistoryRegistry = undoHistoryRegistry
            _editorOptionsService = editorOptionsService
        End Sub

        Public ReadOnly Property DisplayName As String = VBEditorResources.End_Construct Implements INamed.DisplayName

        Public Function GetCommandState_ReturnKeyCommandHandler(args As ReturnKeyCommandArgs, nextHandler As Func(Of CommandState)) As CommandState Implements IChainedCommandHandler(Of ReturnKeyCommandArgs).GetCommandState
            Return nextHandler()
        End Function

        Public Sub ExecuteCommand_ReturnKeyCommandHandler(args As ReturnKeyCommandArgs, nextHandler As Action, context As CommandExecutionContext) Implements IChainedCommandHandler(Of ReturnKeyCommandArgs).ExecuteCommand
            _threadingContext.JoinableTaskFactory.Run(Function() ExecuteEndConstructOnReturnAsync(
                args.TextView, args.SubjectBuffer, nextHandler, context.OperationContext.UserCancellationToken))
        End Sub

        Public Function GetCommandState_TypeCharCommandHandler(args As TypeCharCommandArgs, nextHandler As Func(Of CommandState)) As CommandState Implements IChainedCommandHandler(Of TypeCharCommandArgs).GetCommandState
            Return nextHandler()
        End Function

        Public Sub ExecuteCommand_TypeCharCommandHandler(args As TypeCharCommandArgs, nextHandler As Action, context As CommandExecutionContext) Implements IChainedCommandHandler(Of TypeCharCommandArgs).ExecuteCommand
            _threadingContext.JoinableTaskFactory.Run(
                Async Function()
                    nextHandler()

                    If Not _editorOptionsService.GlobalOptions.GetOption(EndConstructGenerationOptionsStorage.EndConstruct, LanguageNames.VisualBasic) Then
                        Return
                    End If

                    Dim textSnapshot = args.SubjectBuffer.CurrentSnapshot
                    Dim document = textSnapshot.GetOpenDocumentInCurrentContextWithChanges()
                    If document Is Nothing Then
                        Return
                    End If

                    ' End construct is not cancellable.
                    Dim endConstructService = document.GetLanguageService(Of IEndConstructGenerationService)()
                    Await endConstructService.TryDoAsync(
                        args.TextView, args.SubjectBuffer, args.TypedChar, context.OperationContext.UserCancellationToken).ConfigureAwait(True)
                End Function)
        End Sub

        Public Function GetCommandState_AutomaticLineEnderCommandHandler(args As AutomaticLineEnderCommandArgs, nextHandler As Func(Of CommandState)) As CommandState Implements IChainedCommandHandler(Of AutomaticLineEnderCommandArgs).GetCommandState
            Return CommandState.Available
        End Function

        Public Sub ExecuteCommand_AutomaticLineEnderCommandHandler(args As AutomaticLineEnderCommandArgs, nextHandler As Action, context As CommandExecutionContext) Implements IChainedCommandHandler(Of AutomaticLineEnderCommandArgs).ExecuteCommand
            _threadingContext.JoinableTaskFactory.Run(Function() ExecuteEndConstructOnReturnAsync(
                args.TextView,
                args.SubjectBuffer,
                Sub()
                    Dim operations = Me._editorOperationsFactoryService.GetEditorOperations(args.TextView)
                    If operations Is Nothing Then
                        nextHandler()
                    Else
                        operations.InsertNewLine()
                    End If
                End Sub,
                context.OperationContext.UserCancellationToken))
        End Sub

        Private Async Function ExecuteEndConstructOnReturnAsync(
                textView As ITextView,
                subjectBuffer As ITextBuffer,
                nextHandler As Action,
                cancellationToken As CancellationToken) As Task
            If Not _editorOptionsService.GlobalOptions.GetOption(EndConstructGenerationOptionsStorage.EndConstruct, LanguageNames.VisualBasic) OrElse
               Not subjectBuffer.CanApplyChangeDocumentToWorkspace() Then
                nextHandler()
                Return
            End If

            Dim textSnapshot = subjectBuffer.CurrentSnapshot
            Dim document = textSnapshot.GetOpenDocumentInCurrentContextWithChanges()
            If document Is Nothing Then
                Return
            End If

            Await CleanupBeforeEndConstructAsync(
                textView, subjectBuffer, document, cancellationToken).ConfigureAwait(True)

            Dim endConstructService = document.GetLanguageService(Of IEndConstructGenerationService)()
            Dim result = Await endConstructService.TryDoAsync(
                textView, subjectBuffer, vbLf(0), cancellationToken).ConfigureAwait(True)

            If Not result Then
                nextHandler()
                Return
            End If
        End Function

        Private Async Function CleanupBeforeEndConstructAsync(
                view As ITextView,
                buffer As ITextBuffer,
                document As Document,
                cancellationToken As CancellationToken) As Task
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

            Dim options = buffer.GetCodeCleanupOptions(_editorOptionsService, document.Project.GetFallbackAnalyzerOptions(), document.Project.Services, explicitFormat:=False, allowImportsInHiddenRegions:=document.AllowImportsInHiddenRegions())
            Dim cleanDocument = Await CodeCleaner.CleanupAsync(
                document, GetSpanToCleanup(statement), options, codeCleanups, cancellationToken).ConfigureAwait(True)
            Dim changes = cleanDocument.GetTextChangesSynchronously(document, cancellationToken)

            Using transaction = New CaretPreservingEditTransaction(VBEditorResources.End_Construct, view, _undoHistoryRegistry, _editorOperationsFactoryService)
                transaction.MergePolicy = AutomaticCodeChangeMergePolicy.Instance
                buffer.ApplyChanges(changes)
                transaction.Complete()
            End Using
        End Function

        Private Shared Function GetSpanToCleanup(statement As StatementSyntax) As TextSpan
            Dim firstToken = statement.GetFirstToken()
            Dim lastToken = statement.GetLastToken()

            Dim previousToken = firstToken.GetPreviousToken()
            Dim nextToken = lastToken.GetNextToken()

            Return TextSpan.FromBounds(If(previousToken.Kind <> SyntaxKind.None, previousToken, firstToken).SpanStart,
                                       If(nextToken.Kind <> SyntaxKind.None, nextToken, lastToken).Span.End)
        End Function
    End Class
End Namespace
