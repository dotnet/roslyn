' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports System.Diagnostics.CodeAnalysis
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.Implementation.EndConstructGeneration
Imports Microsoft.CodeAnalysis.Editor.Shared.Utilities
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Internal.Log
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.VisualStudio.Text
Imports Microsoft.VisualStudio.Text.Editor
Imports Microsoft.VisualStudio.Text.Editor.OptionsExtensionMethods
Imports Microsoft.VisualStudio.Text.Operations

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.EndConstructGeneration
    <ExportLanguageService(GetType(IEndConstructGenerationService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicEndConstructService
        Implements IEndConstructGenerationService

        Private ReadOnly _smartIndentationService As ISmartIndentationService
        Private ReadOnly _undoHistoryRegistry As ITextUndoHistoryRegistry
        Private ReadOnly _editorOperationsFactoryService As IEditorOperationsFactoryService
        Private ReadOnly _editorOptionsFactoryService As IEditorOptionsFactoryService

        <ImportingConstructor()>
        <SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification:="Used in test code: https://github.com/dotnet/roslyn/issues/42814")>
        Public Sub New(
            smartIndentationService As ISmartIndentationService,
            undoHistoryRegistry As ITextUndoHistoryRegistry,
            editorOperationsFactoryService As IEditorOperationsFactoryService,
            editorOptionsFactoryService As IEditorOptionsFactoryService)

            ThrowIfNull(smartIndentationService)
            ThrowIfNull(undoHistoryRegistry)
            ThrowIfNull(editorOperationsFactoryService)
            ThrowIfNull(editorOptionsFactoryService)

            _smartIndentationService = smartIndentationService
            _undoHistoryRegistry = undoHistoryRegistry
            _editorOperationsFactoryService = editorOperationsFactoryService
            _editorOptionsFactoryService = editorOptionsFactoryService
        End Sub

        Private Shared Function IsMissingStatementError(statement As SyntaxNode, [error] As String) As Boolean
            Select Case [error]
                ' TODO(jasonmal): get rid of this. It is an open design goal to move missing end errors from the
                ' statement to the block. Besides making incremental parsing easier, it will also clean this mess up.
                ' Until then, I'm content with this hack.
                Case "BC30012" ' Missing #End If
                    Return True
                Case "BC30481" ' Missing End Class
                    Return True
                Case "BC30625" ' Missing End Module
                    Return True
                Case "BC30185" ' Missing End Enum
                    Return True
                Case "BC30253" ' Missing End Interface
                    Return True
                Case "BC30624" ' Missing End Structure
                    Return True
                Case "BC30626" ' Missing End Namespace
                    Return True
                Case "BC30026" ' Missing End Sub
                    Return True
                Case "BC30027" ' Missing End Function
                    Return True
                Case "BC30631" ' Missing End Get
                    Return True
                Case "BC30633" ' Missing End Set
                    Return True
                Case "BC30025" ' Missing End Property
                    Return True
                Case "BC30081" ' Missing End If
                    Return True
                Case "BC30082" ' Missing End While
                    Return True
                Case "BC30083" ' Missing Loop
                    Return True
                Case "BC30084" ' Missing Next
                    Return True
                Case "BC30085" ' Missing End With
                    Return True
                Case "BC30095" ' Missing End Select
                    Return True
                Case "BC36008" ' Missing End Using
                    Return True
                Case "BC30675" ' Missing End SyncLock
                    Return True
                Case "BC30681" ' Missing #End Region
                    Return True
                Case "BC33005" ' Missing End Operator
                    Return True
                Case "BC36759" ' Auto-implemented properties cannot have parameters
                    Return True
                Case "BC36673" ' Missing End Sub for Lambda
                    Return True
                Case "BC36674" ' Missing End Function for Lambda
                    Return True
                Case "BC30384" ' Missing End Try
                    Return True
                Case "BC30198", "BC30199"
                    ' These happen if I type Dim x = Function without parenthesis, so as long as we are in that content,
                    ' count this as an acceptable error.
                    Return TypeOf statement Is LambdaHeaderSyntax
                Case "BC31114" ' Missing End Event
                    Return True
            End Select

            Return False
        End Function

        Private Shared Function IsExpectedXmlNameError([error] As String) As Boolean
            Return [error] = "BC31146"
        End Function

        Private Shared Function IsMissingXmlEndTagError([error] As String) As Boolean
            Return [error] = "BC31151"
        End Function

        Private Shared Function IsExpectedXmlEndEmbeddedError([error] As String) As Boolean
            Return [error] = "BC31159"
        End Function

        Private Shared Function IsExpectedXmlEndPIError([error] As String) As Boolean
            Return [error] = "BC31160"
        End Function

        Private Shared Function IsExpectedXmlEndCommentError([error] As String) As Boolean
            Return [error] = "BC31161"
        End Function

        Private Shared Function IsExpectedXmlEndCDataError([error] As String) As Boolean
            Return [error] = "BC31162"
        End Function

        Private Function GetEndConstructState(textView As ITextView,
                                              subjectBuffer As ITextBuffer,
                                              cancellationToken As CancellationToken) As EndConstructState
            Dim caretPosition = textView.GetCaretPoint(subjectBuffer)
            If caretPosition Is Nothing Then
                Return Nothing
            End If

            Dim document = subjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges()
            If document Is Nothing Then
                Return Nothing
            End If

            Dim tree = document.GetSyntaxTreeSynchronously(cancellationToken)

            Dim tokenToLeft = tree.FindTokenOnLeftOfPosition(caretPosition.Value, cancellationToken, includeDirectives:=True, includeDocumentationComments:=True)
            If tokenToLeft.Kind = SyntaxKind.None Then
                Return Nothing
            End If

            Dim bufferOptions = _editorOptionsFactoryService.GetOptions(subjectBuffer)

            Return New EndConstructState(
                caretPosition.Value, New Lazy(Of SemanticModel)(Function() document.GetSemanticModelAsync(cancellationToken).WaitAndGetResult(cancellationToken)), tree, tokenToLeft, bufferOptions.GetNewLineCharacter())
        End Function

        Friend Overridable Function TryDoEndConstructForEnterKey(textView As ITextView,
                                                                 subjectBuffer As ITextBuffer,
                                                                 cancellationToken As CancellationToken) As Boolean
            Using Logger.LogBlock(FunctionId.EndConstruct_DoStatement, cancellationToken)
                Using transaction = New CaretPreservingEditTransaction(VBEditorResources.End_Construct, textView, _undoHistoryRegistry, _editorOperationsFactoryService)
                    transaction.MergePolicy = AutomaticCodeChangeMergePolicy.Instance

                    ' The user may have some text selected. In this scenario, we want to guarantee
                    ' two things:
                    '
                    ' 1) the text that was selected is deleted, as a normal pressing of an enter key
                    '    would do. Since we're not letting the editor do it's own thing during end
                    '    construct generation, we need to make sure the selection is deleted.
                    ' 2) that we compute what statements we should spit assuming the selected text
                    '    is no longer there. Consider a scenario where the user has something like:
                    '
                    '        If True Then ~~~~
                    '
                    '    and the completely invalid "~~~~" is selected. In VS2010, if you pressed
                    '    enter, we would still spit enter, since we effectively view that code as
                    '    "no longer there."
                    '
                    ' The fix is simple: as a part of our transaction, we'll just delete anything
                    ' under our selection. As long as our transaction goes through, the user won't
                    ' suspect anything was fishy. If we don't spit, we'll cancel the transaction
                    ' which will roll back this edit.
                    _editorOperationsFactoryService.GetEditorOperations(textView).ReplaceSelection("")

                    Dim state = GetEndConstructState(textView, subjectBuffer, cancellationToken)
                    If state Is Nothing Then
                        Return False
                    End If

                    ' Are we in the middle of XML tags?
                    If state.TokenToLeft.Kind = SyntaxKind.GreaterThanToken Then
                        Dim element = state.TokenToLeft.GetAncestor(Of XmlElementSyntax)
                        If element IsNot Nothing Then
                            If element.StartTag IsNot Nothing AndAlso element.StartTag.Span.End = state.CaretPosition AndAlso
                               element.EndTag IsNot Nothing AndAlso element.EndTag.SpanStart = state.CaretPosition Then
                                InsertBlankLineBetweenXmlTags(state, textView, subjectBuffer)
                                transaction.Complete()
                                Return True
                            End If
                        End If
                    End If

                    ' Figure out which statement that is to the left of us
                    Dim statement = state.TokenToLeft.FirstAncestorOrSelf(Function(n) TypeOf n Is StatementSyntax OrElse TypeOf n Is DirectiveTriviaSyntax)

                    ' Make sure we are after the last token of the statement or
                    ' if the statement is a single-line If statement that 
                    ' we're after the "Then" or "Else" token.
                    If statement Is Nothing Then
                        Return False
                    ElseIf statement.Kind = SyntaxKind.SingleLineIfStatement Then
                        Dim asSingleLine = DirectCast(statement, SingleLineIfStatementSyntax)

                        If state.TokenToLeft <> asSingleLine.ThenKeyword AndAlso
                           (asSingleLine.ElseClause Is Nothing OrElse
                            state.TokenToLeft <> asSingleLine.ElseClause.ElseKeyword) Then
                            Return False
                        End If
                    ElseIf statement.GetLastToken() <> state.TokenToLeft Then
                        Return False
                    End If

                    ' Make sure we were on the same line as the last token.
                    Dim caretLine = subjectBuffer.CurrentSnapshot.GetLineNumberFromPosition(state.CaretPosition)
                    Dim lineOfLastToken = subjectBuffer.CurrentSnapshot.GetLineNumberFromPosition(state.TokenToLeft.SpanStart)
                    If caretLine <> lineOfLastToken Then
                        Return False
                    End If

                    ' Make sure that we don't have any skipped trivia between our target token and
                    ' the end of the line
                    Dim nextToken = state.TokenToLeft.GetNextTokenOrEndOfFile()
                    Dim nextTokenLine = subjectBuffer.CurrentSnapshot.GetLineNumberFromPosition(nextToken.SpanStart)

                    If nextToken.IsKind(SyntaxKind.EndOfFileToken) AndAlso nextTokenLine = caretLine Then
                        If nextToken.LeadingTrivia.Any(Function(trivia) trivia.IsKind(SyntaxKind.SkippedTokensTrivia)) Then
                            Return False
                        End If
                    End If

                    ' If this is an Imports or Implements declaration, we should use the enclosing type declaration.
                    If TypeOf statement Is InheritsOrImplementsStatementSyntax Then
                        Dim baseDeclaration = DirectCast(statement, InheritsOrImplementsStatementSyntax)

                        Dim typeBlock = baseDeclaration.GetAncestor(Of TypeBlockSyntax)()
                        If typeBlock Is Nothing Then
                            Return False
                        End If

                        statement = typeBlock.BlockStatement
                    End If

                    If statement Is Nothing Then
                        Return False
                    End If

                    Dim errors = state.SyntaxTree.GetDiagnostics(statement)
                    If errors.Any(Function(e) Not IsMissingStatementError(statement, e.Id)) Then
                        If statement.Kind = SyntaxKind.SingleLineIfStatement Then
                            Dim asSingleLine = DirectCast(statement, SingleLineIfStatementSyntax)

                            Dim span = TextSpan.FromBounds(asSingleLine.IfKeyword.SpanStart, asSingleLine.ThenKeyword.Span.End)

                            If errors.Any(Function(e) span.Contains(e.Location.SourceSpan)) Then
                                Return False
                            End If
                        Else

                            Return False
                        End If
                    End If

                    ' Make sure this statement does not end with the line continuation character
                    If statement.GetLastToken(includeZeroWidth:=True).TrailingTrivia.Any(Function(t) t.Kind = SyntaxKind.LineContinuationTrivia) Then
                        Return False
                    End If

                    Dim visitor = New EndConstructStatementVisitor(textView, subjectBuffer, state, cancellationToken)
                    Dim result = visitor.Visit(statement)

                    If result Is Nothing Then
                        Return False
                    End If

                    result.Apply(textView, subjectBuffer, state.CaretPosition, _smartIndentationService, _undoHistoryRegistry, _editorOperationsFactoryService)
                    transaction.Complete()
                End Using
            End Using

            Return True
        End Function

        Private Sub InsertBlankLineBetweenXmlTags(state As EndConstructState, textView As ITextView, subjectBuffer As ITextBuffer)
            ' Add an extra newline first
            Using edit = subjectBuffer.CreateEdit()
                Dim aligningWhitespace = subjectBuffer.CurrentSnapshot.GetAligningWhitespace(state.TokenToLeft.Parent.Span.Start)
                edit.Insert(state.CaretPosition, state.NewLineCharacter + aligningWhitespace)
                edit.ApplyAndLogExceptions()
            End Using

            ' And now just send down a normal enter
            textView.TryMoveCaretToAndEnsureVisible(New SnapshotPoint(subjectBuffer.CurrentSnapshot, state.CaretPosition))
            _editorOperationsFactoryService.GetEditorOperations(textView).InsertNewLine()
        End Sub

        Private Shared Function GetNodeFromToken(Of T As SyntaxNode)(token As SyntaxToken, expectedKind As SyntaxKind) As T
            If token.Kind <> expectedKind Then
                Return Nothing
            End If

            Return TryCast(token.Parent, T)
        End Function

        Private Shared Function InsertEndTextAndUpdateCaretPosition(
            view As ITextView,
            subjectBuffer As ITextBuffer,
            insertPosition As Integer,
            caretPosition As Integer,
            endText As String
        ) As Boolean

            Dim document = subjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges()
            If document Is Nothing Then
                Return False
            End If

            subjectBuffer.ApplyChange(New TextChange(New TextSpan(insertPosition, 0), endText))

            Dim caretPosAfterEdit = New SnapshotPoint(subjectBuffer.CurrentSnapshot, caretPosition)

            view.TryMoveCaretToAndEnsureVisible(caretPosAfterEdit)

            Return True
        End Function

        Friend Function TryDoXmlCDataEndConstruct(textView As ITextView, subjectBuffer As ITextBuffer, cancellationToken As CancellationToken) As Boolean
            Using Logger.LogBlock(FunctionId.EndConstruct_XmlCData, cancellationToken)
                Dim state = GetEndConstructState(textView, subjectBuffer, cancellationToken)
                If state Is Nothing Then
                    Return False
                End If

                Dim xmlCData = GetNodeFromToken(Of XmlCDataSectionSyntax)(state.TokenToLeft, expectedKind:=SyntaxKind.BeginCDataToken)
                If xmlCData Is Nothing Then
                    Return False
                End If

                Dim errors = state.SyntaxTree.GetDiagnostics(xmlCData)

                ' Exactly one error is expected: ERRID_ExpectedXmlEndCData
                If errors.Count <> 1 Then
                    Return False
                End If

                If Not IsExpectedXmlEndCDataError(errors(0).Id) Then
                    Return False
                End If

                Dim endText = "]]>"
                Return InsertEndTextAndUpdateCaretPosition(textView, subjectBuffer, state.CaretPosition, state.TokenToLeft.Span.End, endText)
            End Using
        End Function

        Friend Function TryDoXmlCommentEndConstruct(textView As ITextView, subjectBuffer As ITextBuffer, cancellationToken As CancellationToken) As Boolean
            Using Logger.LogBlock(FunctionId.EndConstruct_XmlComment, cancellationToken)
                Dim state = GetEndConstructState(textView, subjectBuffer, cancellationToken)
                If state Is Nothing Then
                    Return False
                End If

                Dim xmlComment = GetNodeFromToken(Of XmlCommentSyntax)(state.TokenToLeft, expectedKind:=SyntaxKind.LessThanExclamationMinusMinusToken)
                If xmlComment Is Nothing Then
                    Return False
                End If

                Dim errors = state.SyntaxTree.GetDiagnostics(xmlComment)

                ' Exactly one error is expected: ERRID_ExpectedXmlEndComment
                If errors.Count <> 1 Then
                    Return False
                End If

                If Not IsExpectedXmlEndCommentError(errors(0).Id) Then
                    Return False
                End If

                Dim endText = "-->"
                Return InsertEndTextAndUpdateCaretPosition(textView, subjectBuffer, state.CaretPosition, state.TokenToLeft.Span.End, endText)
            End Using
        End Function

        Friend Function TryDoXmlElementEndConstruct(textView As ITextView, subjectBuffer As ITextBuffer, cancellationToken As CancellationToken) As Boolean
            Using Logger.LogBlock(FunctionId.EndConstruct_XmlElement, cancellationToken)
                Dim state = GetEndConstructState(textView, subjectBuffer, cancellationToken)
                If state Is Nothing Then
                    Return False
                End If

                Dim xmlStartElement = GetNodeFromToken(Of XmlElementStartTagSyntax)(state.TokenToLeft, expectedKind:=SyntaxKind.GreaterThanToken)
                If xmlStartElement Is Nothing Then
                    Return False
                End If

                Dim errors = state.SyntaxTree.GetDiagnostics(xmlStartElement)

                ' Exactly one error is expected: ERRID_MissingXmlEndTag
                If errors.Count <> 1 Then
                    Return False
                End If

                If Not IsMissingXmlEndTagError(errors(0).Id) Then
                    Return False
                End If

                Dim endTagText = "</" & xmlStartElement.Name.ToString & ">"
                Return InsertEndTextAndUpdateCaretPosition(textView, subjectBuffer, state.CaretPosition, state.TokenToLeft.Span.End, endTagText)
            End Using
        End Function

        Friend Function TryDoXmlEmbeddedExpressionEndConstruct(textView As ITextView, subjectBuffer As ITextBuffer, cancellationToken As CancellationToken) As Boolean
            Using Logger.LogBlock(FunctionId.EndConstruct_XmlEmbeddedExpression, cancellationToken)
                Dim state = GetEndConstructState(textView, subjectBuffer, cancellationToken)
                If state Is Nothing Then
                    Return False
                End If

                Dim xmlEmbeddedExpression = GetNodeFromToken(Of XmlEmbeddedExpressionSyntax)(state.TokenToLeft, expectedKind:=SyntaxKind.LessThanPercentEqualsToken)
                If xmlEmbeddedExpression Is Nothing Then
                    Return False
                End If

                Dim errors = state.SyntaxTree.GetDiagnostics(xmlEmbeddedExpression)

                ' Errors should contain ERRID_ExpectedXmlEndEmbedded
                If Not errors.Any(Function(e) IsExpectedXmlEndEmbeddedError(e.Id)) Then
                    Return False
                End If

                Dim endText = "  %>" ' NOTE: two spaces are inserted. The caret will be moved between them
                Return InsertEndTextAndUpdateCaretPosition(textView, subjectBuffer, state.CaretPosition, state.TokenToLeft.Span.End + 1, endText)
            End Using
        End Function

        Friend Function TryDoXmlProcessingInstructionEndConstruct(textView As ITextView, subjectBuffer As ITextBuffer, cancellationToken As CancellationToken) As Boolean
            Using Logger.LogBlock(FunctionId.EndConstruct_XmlProcessingInstruction, cancellationToken)
                Dim state = GetEndConstructState(textView, subjectBuffer, cancellationToken)
                If state Is Nothing Then
                    Return False
                End If

                Dim xmlProcessingInstruction = GetNodeFromToken(Of XmlProcessingInstructionSyntax)(state.TokenToLeft, expectedKind:=SyntaxKind.LessThanQuestionToken)
                If xmlProcessingInstruction Is Nothing Then
                    Return False
                End If

                Dim errors = state.SyntaxTree.GetDiagnostics(xmlProcessingInstruction)

                ' Exactly two errors are expected: ERRID_ExpectedXmlName and ERRID_ExpectedXmlEndPI
                If errors.Count <> 2 Then
                    Return False
                End If

                If Not (errors.Any(Function(e) IsExpectedXmlNameError(e.Id)) AndAlso
                        errors.Any(Function(e) IsExpectedXmlEndPIError(e.Id))) Then
                    Return False
                End If

                Dim endText = "?>"
                Return InsertEndTextAndUpdateCaretPosition(textView, subjectBuffer, state.CaretPosition, state.TokenToLeft.Span.End, endText)
            End Using
        End Function

        Public Function TryDo(textView As ITextView, subjectBuffer As ITextBuffer, typedChar As Char, cancellationToken As CancellationToken) As Boolean Implements IEndConstructGenerationService.TryDo
            Select Case typedChar
                Case vbLf(0)
                    Return Me.TryDoEndConstructForEnterKey(textView, subjectBuffer, cancellationToken)
                Case ">"c
                    Return Me.TryDoXmlElementEndConstruct(textView, subjectBuffer, cancellationToken)
                Case "-"c
                    Return Me.TryDoXmlCommentEndConstruct(textView, subjectBuffer, cancellationToken)
                Case "="c
                    Return Me.TryDoXmlEmbeddedExpressionEndConstruct(textView, subjectBuffer, cancellationToken)
                Case "["c
                    Return Me.TryDoXmlCDataEndConstruct(textView, subjectBuffer, cancellationToken)
                Case "?"c
                    Return Me.TryDoXmlProcessingInstructionEndConstruct(textView, subjectBuffer, cancellationToken)
            End Select

            Return False
        End Function
    End Class
End Namespace
