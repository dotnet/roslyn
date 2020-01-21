' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.Implementation.EndConstructGeneration
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.Text.Shared.Extensions
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.VisualStudio.Commanding
Imports Microsoft.VisualStudio.Text
Imports Microsoft.VisualStudio.Text.Editor.Commanding.Commands
Imports Microsoft.VisualStudio.Text.Operations
Imports Microsoft.VisualStudio.Utilities

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.Utilities.CommandHandlers
    Friend MustInherit Class AbstractImplementAbstractClassOrInterfaceCommandHandler
        Implements ICommandHandler(Of ReturnKeyCommandArgs)

        Private ReadOnly _editorOperationsFactoryService As IEditorOperationsFactoryService

        Public ReadOnly Property DisplayName As String Implements INamed.DisplayName
            Get
                Return VBEditorResources.Implement_Abstract_Class_Or_Interface
            End Get
        End Property

        Protected Sub New(editorOperationsFactoryService As IEditorOperationsFactoryService)
            _editorOperationsFactoryService = editorOperationsFactoryService
        End Sub

        Protected MustOverride Overloads Function TryGetNewDocument(
            document As Document,
            typeSyntax As TypeSyntax,
            cancellationToken As CancellationToken) As Document

        Private Function ExecuteCommand(args As ReturnKeyCommandArgs, context As CommandExecutionContext) As Boolean Implements ICommandHandler(Of ReturnKeyCommandArgs).ExecuteCommand
            Dim caretPointOpt = args.TextView.GetCaretPoint(args.SubjectBuffer)
            If caretPointOpt Is Nothing Then
                Return False
            End If

            ' Implement interface is not cancellable.
            Dim _cancellationToken = CancellationToken.None
            If Not TryExecute(args, _cancellationToken) Then
                Return False
            End If

            ' It's possible that there may be an end construct to generate at this position.
            ' We'll go ahead and generate it before determining whether we need to move the caret
            TryGenerateEndConstruct(args, _cancellationToken)

            Dim snapshot = args.SubjectBuffer.CurrentSnapshot
            Dim caretPosition = args.TextView.GetCaretPoint(args.SubjectBuffer).Value
            Dim caretLine = snapshot.GetLineFromPosition(caretPosition)

            ' If there is any text after the caret on the same line, we'll pass through to
            ' insert a new line.
            Dim lastNonWhitespacePosition = If(caretLine.GetLastNonWhitespacePosition(), -1)
            If lastNonWhitespacePosition > caretPosition.Position Then
                Return False
            End If

            Dim nextLine = snapshot.GetLineFromLineNumber(caretLine.LineNumber + 1)
            If Not nextLine.IsEmptyOrWhitespace() Then
                ' If the next line is not whitespace, we'll go ahead and pass through to insert a new line.
                Return False
            End If

            ' If the next line *is* whitespace, we're just going to move the caret down a line.
            _editorOperationsFactoryService.GetEditorOperations(args.TextView).MoveLineDown(extendSelection:=False)

            Return True
        End Function

        Private Function TryGenerateEndConstruct(args As ReturnKeyCommandArgs, cancellationToken As CancellationToken) As Boolean
            Dim textSnapshot = args.SubjectBuffer.CurrentSnapshot

            Dim document = textSnapshot.GetOpenDocumentInCurrentContextWithChanges()
            If document Is Nothing Then
                Return False
            End If

            Dim caretPointOpt = args.TextView.GetCaretPoint(args.SubjectBuffer)
            If caretPointOpt Is Nothing Then
                Return False
            End If

            Dim caretPosition = caretPointOpt.Value.Position
            If caretPosition = 0 Then
                Return False
            End If

            Dim endConstructGenerationService = document.GetLanguageService(Of IEndConstructGenerationService)()

            Dim applicable As Boolean = False
            Return endConstructGenerationService.TryDo(args.TextView, args.SubjectBuffer, vbLf(0), cancellationToken)
        End Function

        Private Overloads Function TryExecute(args As ReturnKeyCommandArgs, cancellationToken As CancellationToken) As Boolean
            Dim textSnapshot = args.SubjectBuffer.CurrentSnapshot
            Dim text = textSnapshot.AsText()

            Dim document = textSnapshot.GetOpenDocumentInCurrentContextWithChanges()
            If document Is Nothing Then
                Return False
            End If

            If Not args.SubjectBuffer.GetFeatureOnOffOption(FeatureOnOffOptions.AutomaticInsertionOfAbstractOrInterfaceMembers) Then
                Return False
            End If

            Dim caretPointOpt = args.TextView.GetCaretPoint(args.SubjectBuffer)
            If caretPointOpt Is Nothing Then
                Return False
            End If

            Dim caretPosition = caretPointOpt.Value.Position
            If caretPosition = 0 Then
                Return False
            End If

            Dim syntaxRoot = document.GetSyntaxRootSynchronously(cancellationToken)
            Dim token = syntaxRoot.FindTokenOnLeftOfPosition(caretPosition)

            If text.Lines.IndexOf(token.SpanStart) <> text.Lines.IndexOf(caretPosition) Then
                Return False
            End If

            Dim statement = token.GetAncestor(Of InheritsOrImplementsStatementSyntax)()
            If statement Is Nothing Then
                Return False
            End If

            If statement.Span.End <> token.Span.End Then
                Return False
            End If

            ' We need to track this token into the resulting buffer so we know what to do with the cursor
            ' from there
            Dim caretOffsetFromToken = caretPosition - token.Span.End

            Dim tokenAnnotation As New SyntaxAnnotation()
            document = document.WithSyntaxRoot(syntaxRoot.ReplaceToken(token, token.WithAdditionalAnnotations(tokenAnnotation)))
            token = document.GetSyntaxRootSynchronously(cancellationToken).
                             GetAnnotatedNodesAndTokens(tokenAnnotation).First().AsToken()

            Dim typeSyntax = token.GetAncestor(Of TypeSyntax)()
            If typeSyntax Is Nothing Then
                Return False
            End If

            ' get top most identifier
            Dim identifier = DirectCast(typeSyntax.AncestorsAndSelf(ascendOutOfTrivia:=False).Where(Function(t) TypeOf t Is TypeSyntax).LastOrDefault(), TypeSyntax)
            If identifier Is Nothing Then
                Return False
            End If

            Dim newDocument = TryGetNewDocument(document, identifier, cancellationToken)

            If newDocument Is Nothing Then
                Return False
            End If

            newDocument = Simplifier.ReduceAsync(newDocument, Simplifier.Annotation, Nothing, cancellationToken).WaitAndGetResult(cancellationToken)
            newDocument = Formatter.FormatAsync(newDocument, Formatter.Annotation, cancellationToken:=cancellationToken).WaitAndGetResult(cancellationToken)

            newDocument.Project.Solution.Workspace.ApplyDocumentChanges(newDocument, cancellationToken)

            ' Place the cursor back to where it logically was after this
            token = newDocument.GetSyntaxRootSynchronously(cancellationToken).
                                GetAnnotatedNodesAndTokens(tokenAnnotation).First().AsToken()
            args.TextView.TryMoveCaretToAndEnsureVisible(
                New SnapshotPoint(args.SubjectBuffer.CurrentSnapshot,
                                  Math.Min(token.Span.End + caretOffsetFromToken, args.SubjectBuffer.CurrentSnapshot.Length)))

            Return True
        End Function

        Private Function GetCommandState(args As ReturnKeyCommandArgs) As CommandState Implements ICommandHandler(Of ReturnKeyCommandArgs).GetCommandState
            Return CommandState.Unspecified
        End Function
    End Class
End Namespace
