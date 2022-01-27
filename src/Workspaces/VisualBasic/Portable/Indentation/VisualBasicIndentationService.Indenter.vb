' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Formatting.Rules
Imports Microsoft.CodeAnalysis.Indentation
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Formatting
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Indentation
    Partial Friend Class VisualBasicIndentationService
        Protected Overrides Function ShouldUseTokenIndenter(indenter As Indenter, ByRef token As SyntaxToken) As Boolean
            Return ShouldUseSmartTokenFormatterInsteadOfIndenter(
                indenter.Rules, indenter.Root, indenter.LineToBeIndented, indenter.Options.FormattingOptions, token)
        End Function

        Protected Overrides Function CreateSmartTokenFormatter(indenter As Indenter) As ISmartTokenFormatter
            Dim services = indenter.Document.Project.Solution.Workspace.Services
            Dim formattingRuleFactory = services.GetService(Of IHostDependentFormattingRuleFactoryService)()
            Dim rules = {New SpecialFormattingRule(indenter.Options.AutoFormattingOptions.IndentStyle), formattingRuleFactory.CreateRule(indenter.Document.Document, indenter.LineToBeIndented.Start)}.Concat(Formatter.GetDefaultFormattingRules(indenter.Document.Document))
            Return New VisualBasicSmartTokenFormatter(indenter.Options.FormattingOptions, rules, indenter.Root)
        End Function

        Protected Overrides Function GetDesiredIndentationWorker(
                indenter As Indenter,
                tokenOpt As SyntaxToken?,
                triviaOpt As SyntaxTrivia?) As IndentationResult?

            If triviaOpt.HasValue Then
                Dim trivia = triviaOpt.Value

                If trivia.Kind = SyntaxKind.CommentTrivia OrElse
                   trivia.Kind = SyntaxKind.DocumentationCommentTrivia Then

                    ' if the comment is the only thing on a line, then preserve its indentation for the next line.
                    Dim line = indenter.Text.Lines.GetLineFromPosition(trivia.FullSpan.Start)
                    If line.GetFirstNonWhitespacePosition() = trivia.FullSpan.Start Then
                        Return New IndentationResult(trivia.FullSpan.Start, 0)
                    End If
                End If

                If trivia.Kind = SyntaxKind.CommentTrivia Then
                    ' Line ends in comment
                    ' Two cases a line ending comment or _ comment
                    If tokenOpt.HasValue Then
                        Dim firstTrivia As SyntaxTrivia = indenter.Tree.GetRoot(indenter.CancellationToken).FindTrivia(tokenOpt.Value.Span.End + 1)
                        ' firstTrivia contains either an _ or a comment, this is the First trivia after the last Token on the line
                        If firstTrivia.Kind = SyntaxKind.LineContinuationTrivia Then
                            Return GetIndentationBasedOnToken(indenter, GetTokenOnLeft(firstTrivia), firstTrivia)
                        Else
                            ' This is we have just a comment
                            Return GetIndentationBasedOnToken(indenter, GetTokenOnLeft(trivia), trivia)
                        End If
                    End If
                End If

                ' if we are at invalid token (skipped token) at the end of statement, treat it like we are after line continuation
                If trivia.Kind = SyntaxKind.SkippedTokensTrivia AndAlso trivia.Token.IsLastTokenOfStatement() Then
                    Return GetIndentationBasedOnToken(indenter, GetTokenOnLeft(trivia), trivia)
                End If

                If trivia.Kind = SyntaxKind.LineContinuationTrivia Then
                    Return GetIndentationBasedOnToken(indenter, GetTokenOnLeft(trivia), trivia)
                End If
            End If

            If tokenOpt.HasValue Then
                Return GetIndentationBasedOnToken(indenter, tokenOpt.Value)
            End If

            Return Nothing
        End Function

        Private Shared Function GetTokenOnLeft(trivia As SyntaxTrivia) As SyntaxToken
            Dim token = trivia.Token
            If token.Span.End <= trivia.SpanStart AndAlso Not token.IsMissing Then
                Return token
            End If

            Return token.GetPreviousToken()
        End Function

        Private Shared Function GetIndentationBasedOnToken(indenter As Indenter, token As SyntaxToken, Optional trivia As SyntaxTrivia = Nothing) As IndentationResult
            Dim sourceText = indenter.LineToBeIndented.Text

            Dim position = indenter.GetCurrentPositionNotBelongToEndOfFileToken(indenter.LineToBeIndented.Start)

            ' lines must be blank since we got the token from the first non blank line above current position
            If HasLinesBetween(indenter.Tree.GetText().Lines.IndexOf(token.Span.End), indenter.LineToBeIndented.LineNumber) Then
                ' if there are blank lines between, return indentation of the owning statement
                Return GetIndentationOfCurrentPosition(indenter, token, position)
            End If

            Dim indentation = GetIndentationFromOperationService(indenter, token, position)
            If indentation.HasValue Then
                Return indentation.Value
            End If

            Dim queryNode = token.GetAncestor(Of QueryClauseSyntax)()
            If queryNode IsNot Nothing Then
                Dim subQuerySpaces = If(token.IsLastTokenOfStatement(), 0, indenter.Options.FormattingOptions.IndentationSize)
                Return indenter.GetIndentationOfToken(queryNode.GetFirstToken(includeZeroWidth:=True), subQuerySpaces)
            End If

            ' check one more time for query case
            If token.Kind = SyntaxKind.IdentifierToken AndAlso token.HasMatchingText(SyntaxKind.FromKeyword) Then
                Return indenter.GetIndentationOfToken(token)
            End If

            If FormattingHelpers.IsXmlTokenInXmlDeclaration(token) Then
                Dim xmlDocument = token.GetAncestor(Of XmlDocumentSyntax)()
                Return indenter.GetIndentationOfToken(xmlDocument.GetFirstToken(includeZeroWidth:=True))
            End If

            ' implicit line continuation case
            If IsLineContinuable(token, trivia) Then
                Return GetIndentationFromTokenLineAfterLineContinuation(indenter, token, trivia)
            End If

            Return GetIndentationOfCurrentPosition(indenter, token, position)
        End Function

        Private Shared Function GetIndentationOfCurrentPosition(indenter As Indenter, token As SyntaxToken, position As Integer) As IndentationResult
            Return GetIndentationOfCurrentPosition(indenter, token, position, extraSpaces:=0)
        End Function

        Private Shared Function GetIndentationOfCurrentPosition(indenter As Indenter, token As SyntaxToken, position As Integer, extraSpaces As Integer) As IndentationResult
            ' special case for multi-line string
            Dim containingToken = indenter.Tree.FindTokenOnLeftOfPosition(position, indenter.CancellationToken)
            If containingToken.IsKind(SyntaxKind.InterpolatedStringTextToken) OrElse
                containingToken.IsKind(SyntaxKind.InterpolatedStringText) OrElse
                (containingToken.IsKind(SyntaxKind.CloseBraceToken) AndAlso token.Parent.IsKind(SyntaxKind.Interpolation)) Then
                Return indenter.IndentFromStartOfLine(0)
            End If

            If containingToken.Kind = SyntaxKind.StringLiteralToken AndAlso containingToken.FullSpan.Contains(position) Then
                Return indenter.IndentFromStartOfLine(0)
            End If

            Return indenter.IndentFromStartOfLine(indenter.Finder.GetIndentationOfCurrentPosition(indenter.Tree, token, position, extraSpaces, indenter.CancellationToken))
        End Function

        Private Shared Function IsLineContinuable(lastVisibleTokenOnPreviousLine As SyntaxToken, trivia As SyntaxTrivia) As Boolean
            If trivia.Kind = SyntaxKind.LineContinuationTrivia OrElse
                trivia.Kind = SyntaxKind.SkippedTokensTrivia Then
                Return True
            End If

            If lastVisibleTokenOnPreviousLine.IsLastTokenOfStatement() Then
                Return False
            End If

            Dim visibleTokenOnCurrentLine As SyntaxToken = lastVisibleTokenOnPreviousLine.GetNextToken()
            If Not lastVisibleTokenOnPreviousLine.IsKind(SyntaxKind.OpenBraceToken) AndAlso
                Not lastVisibleTokenOnPreviousLine.IsKind(SyntaxKind.CommaToken) Then
                If IsCloseBraceOfInitializerSyntax(visibleTokenOnCurrentLine) Then
                    Return False
                End If
            Else
                If IsCloseBraceOfInitializerSyntax(visibleTokenOnCurrentLine) Then
                    Return True
                End If
            End If

            If Not ContainingStatementHasDiagnostic(lastVisibleTokenOnPreviousLine.Parent) Then
                Return True
            End If

            If lastVisibleTokenOnPreviousLine.GetNextToken(includeZeroWidth:=True).IsMissing Then
                Return True
            End If

            Return False
        End Function

        Private Shared Function IsCloseBraceOfInitializerSyntax(visibleTokenOnCurrentLine As SyntaxToken) As Boolean
            If visibleTokenOnCurrentLine.IsKind(SyntaxKind.CloseBraceToken) Then
                Dim visibleTokenOnCurrentLineParent = visibleTokenOnCurrentLine.Parent
                If TypeOf visibleTokenOnCurrentLineParent Is ObjectCreationInitializerSyntax OrElse
                TypeOf visibleTokenOnCurrentLineParent Is CollectionInitializerSyntax Then
                    Return True
                End If
            End If

            Return False
        End Function

        Private Shared Function ContainingStatementHasDiagnostic(node As SyntaxNode) As Boolean
            If node Is Nothing Then
                Return False
            End If

            If node.ContainsDiagnostics Then
                Return True
            End If

            Dim containingStatement = node.GetAncestorOrThis(Of StatementSyntax)()
            If containingStatement Is Nothing Then
                Return False
            End If

            Return containingStatement.ContainsDiagnostics()
        End Function

        Private Shared Function GetIndentationFromOperationService(indenter As Indenter, token As SyntaxToken, position As Integer) As IndentationResult?
            ' check operation service to see whether we can determine indentation from it
            If token.Kind = SyntaxKind.None Then
                Return Nothing
            End If

            Dim indentation = indenter.Finder.FromIndentBlockOperations(indenter.Tree, token, position, indenter.CancellationToken)
            If indentation.HasValue Then
                Return indenter.IndentFromStartOfLine(indentation.Value)
            End If

            ' special case xml text literal before checking alignment operation
            ' VB has different behavior around missing alignment token. for query expression, VB prefers putting
            ' caret aligned with previous query clause, but for xml literals, it prefer them to be ignored and indented
            ' based on current indentation level.
            If token.Kind = SyntaxKind.XmlTextLiteralToken OrElse
                token.Kind = SyntaxKind.XmlEntityLiteralToken Then
                Return indenter.GetIndentationOfLine(indenter.LineToBeIndented.Text.Lines.GetLineFromPosition(token.SpanStart))
            End If

            ' check alignment token indentation
            Dim alignmentTokenIndentation = indenter.Finder.FromAlignTokensOperations(indenter.Tree, token)
            If alignmentTokenIndentation.HasValue Then
                Return indenter.IndentFromStartOfLine(alignmentTokenIndentation.Value)
            End If

            Return Nothing
        End Function

        Private Shared Function GetIndentationFromTokenLineAfterLineContinuation(indenter As Indenter, token As SyntaxToken, trivia As SyntaxTrivia) As IndentationResult
            Dim sourceText = indenter.LineToBeIndented.Text
            Dim position = indenter.LineToBeIndented.Start

            position = indenter.GetCurrentPositionNotBelongToEndOfFileToken(position)

            Dim currentTokenLine = sourceText.Lines.GetLineFromPosition(token.SpanStart)

            ' error case where the line continuation belongs to a meaningless token such as empty token for skipped text
            If token.Kind = SyntaxKind.EmptyToken Then
                Dim baseLine = sourceText.Lines.GetLineFromPosition(trivia.SpanStart)
                Return indenter.GetIndentationOfLine(baseLine)
            End If

            Dim xmlEmbeddedExpression = token.GetAncestor(Of XmlEmbeddedExpressionSyntax)()
            If xmlEmbeddedExpression IsNot Nothing Then
                Dim firstExpressionLine = sourceText.Lines.GetLineFromPosition(xmlEmbeddedExpression.GetFirstToken(includeZeroWidth:=True).SpanStart)
                Return GetIndentationFromTwoLines(indenter, firstExpressionLine, currentTokenLine, token, position)
            End If

            If FormattingHelpers.IsGreaterThanInAttribute(token) Then
                Dim attribute = token.GetAncestor(Of AttributeListSyntax)()
                Dim baseLine = sourceText.Lines.GetLineFromPosition(attribute.GetFirstToken(includeZeroWidth:=True).SpanStart)
                Return indenter.GetIndentationOfLine(baseLine)
            End If

            ' if position is between "," and next token, consider the position to be belonged to the list that
            ' owns the ","
            If IsCommaInParameters(token) AndAlso (token.Span.End <= position AndAlso position <= token.GetNextToken().SpanStart) Then
                Return GetIndentationOfCurrentPosition(indenter, token, token.SpanStart)
            End If

            Dim statement = token.GetAncestor(Of StatementSyntax)()

            ' this can happen if only token in the file is End Of File Token
            If statement Is Nothing Then
                If trivia.Kind <> SyntaxKind.None Then
                    Dim triviaLine = sourceText.Lines.GetLineFromPosition(trivia.SpanStart)
                    Return indenter.GetIndentationOfLine(triviaLine, indenter.Options.FormattingOptions.IndentationSize)
                End If

                ' no base line to use to calculate the indentation
                Return indenter.IndentFromStartOfLine(0)
            End If

            ' find line where first token of statement is starting on
            Dim firstTokenLine = sourceText.Lines.GetLineFromPosition(statement.GetFirstToken(includeZeroWidth:=True).SpanStart)
            Return GetIndentationFromTwoLines(indenter, firstTokenLine, currentTokenLine, token, position)
        End Function

        Private Shared Function IsCommaInParameters(token As SyntaxToken) As Boolean
            Return token.Kind = SyntaxKind.CommaToken AndAlso
                (TypeOf token.Parent Is ParameterListSyntax OrElse
                    TypeOf token.Parent Is ArgumentListSyntax OrElse
                    TypeOf token.Parent Is TypeParameterListSyntax)
        End Function

        Private Shared Function GetIndentationFromTwoLines(indenter As Indenter, firstLine As TextLine, secondLine As TextLine, token As SyntaxToken, position As Integer) As IndentationResult
            If firstLine.LineNumber = secondLine.LineNumber Then
                ' things are on same line, put the indentation size
                Return GetIndentationOfCurrentPosition(indenter, token, position, indenter.Options.FormattingOptions.IndentationSize)
            End If

            ' multiline
            Return indenter.GetIndentationOfLine(secondLine)
        End Function

        Private Shared Function HasLinesBetween(lineNumber1 As Integer, lineNumber2 As Integer) As Boolean
            Return lineNumber1 + 1 < lineNumber2
        End Function
    End Class
End Namespace
