' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.ComponentModel.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.Implementation.AutomaticCompletion
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.VisualStudio.Commanding
Imports Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion
Imports Microsoft.VisualStudio.Text.Operations
Imports Microsoft.VisualStudio.Utilities

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.AutomaticCompletion
    ' <summary>
    ' visual basic automatic line ender command handler
    ' </summary>
    <Export(GetType(ICommandHandler))>
    <ContentType(ContentTypeNames.VisualBasicContentType)>
    <Name(PredefinedCommandHandlerNames.AutomaticLineEnder)>
    <Order(Before:=PredefinedCompletionNames.CompletionCommandHandler)>
    Friend Class AutomaticLineEnderCommandHandler
        Inherits AbstractAutomaticLineEnderCommandHandler

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New(undoRegistry As ITextUndoHistoryRegistry,
                       editorOperations As IEditorOperationsFactoryService)

            MyBase.New(undoRegistry, editorOperations)
        End Sub

        Protected Overrides Sub NextAction(editorOperation As IEditorOperations, nextAction As Action)
            ' let the next action run
            nextAction()
        End Sub

        Protected Overrides Function TreatAsReturn(document As Document, position As Integer, cancellationToken As CancellationToken) As Boolean
            ' No special handling in VB.
            Return False
        End Function

        Protected Overrides Sub FormatAndApply(document As Document, position As Integer, cancellationToken As CancellationToken)
            ' vb does automatic line commit
            ' no need to do explicit formatting
        End Sub

        Protected Overrides Function GetEndingString(document As Document, position As Integer, cancellationToken As CancellationToken) As String
            ' prepare expansive information from document
            Dim root = document.GetSyntaxRootSynchronously(cancellationToken)
            Dim text = root.SyntaxTree.GetText(cancellationToken)

            ' get line where the caret is on
            Dim line = text.Lines.GetLineFromPosition(position)

            ' find line break token if there is one
            Dim lastToken = CType(root.FindTokenOnLeftOfPosition(line.End, includeSkipped:=False), SyntaxToken)
            lastToken = If(lastToken.Kind = SyntaxKind.EndOfFileToken, lastToken.GetPreviousToken(includeZeroWidth:=True), lastToken)

            ' find last token of the line
            If lastToken.Kind = SyntaxKind.None OrElse line.End < lastToken.Span.End Then
                Return Nothing
            End If

            ' properly ended
            If Not lastToken.IsMissing AndAlso lastToken.IsLastTokenOfStatementWithEndOfLine() Then
                Return Nothing
            End If

            ' so far so good. check whether we need to add explicit line continuation here
            Dim nonMissingToken = If(lastToken.IsMissing, lastToken.GetPreviousToken(), lastToken)

            ' now we have the last token, check whether it is at a valid location
            If (line.Span.Contains(nonMissingToken.Span.End)) Then
                ' make sure that there is no trailing text after last token on the line if it is not at the end of the line
                Dim endingString = text.ToString(TextSpan.FromBounds(nonMissingToken.Span.End, line.End))
                If Not String.IsNullOrWhiteSpace(endingString) Then
                    Return Nothing
                End If
            End If

            ' check whether implicit line continuation is allowed
            If SyntaxFacts.AllowsTrailingImplicitLineContinuation(CType(nonMissingToken, SyntaxToken)) Then
                Return Nothing
            End If

            Dim nextToken = nonMissingToken.GetNextToken(includeZeroWidth:=True)

            ' if there is skipped token between previous and next token, don't do anything
            If nonMissingToken.TrailingTrivia.Concat(nextToken.LeadingTrivia).Any(AddressOf HasSkippedText) Then
                Return Nothing
            End If

            If nextToken.IsLastTokenOfStatementWithEndOfLine() Then
                Return " _"
            End If

            Dim nextNonMissingToken = nextToken.GetNextNonZeroWidthTokenOrEndOfFile()
            If nextNonMissingToken.Kind = SyntaxKind.EndOfFileToken Then
                Return Nothing
            End If

            Return If(SyntaxFacts.AllowsLeadingImplicitLineContinuation(CType(nextToken, SyntaxToken)), Nothing, " _")
        End Function

        Private Shared Function HasSkippedText(trivia As SyntaxTrivia) As Boolean
            Return trivia.Kind = SyntaxKind.SkippedTokensTrivia
        End Function
    End Class
End Namespace
