' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.Syntax.InternalSyntax
Imports InternalSyntaxFactory = Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax.SyntaxFactory

'
'============ Methods for parsing portions of executable statements ==
'

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax

    Partial Friend Class Parser

        Private Function ParseInterpolatedStringExpression() As InterpolatedStringExpressionSyntax
            Debug.Assert(CurrentToken.Kind = SyntaxKind.DollarSignDoubleQuoteToken, "ParseInterpolatedStringExpression called on the wrong token.")

            ResetCurrentToken(ScannerState.InterpolatedStringPunctuation)

            Debug.Assert(CurrentToken.Kind = SyntaxKind.DollarSignDoubleQuoteToken, "Rescanning $"" failed.")

            Dim dollarSignDoubleQuoteToken = DirectCast(CurrentToken, PunctuationSyntax)
            GetNextToken(ScannerState.InterpolatedStringContent)

            Dim contentBuilder = _pool.Allocate(Of InterpolatedStringContentSyntax)

            Dim doubleQuoteToken As PunctuationSyntax
            Dim skipped As SyntaxListBuilder(Of SyntaxToken) = Nothing
            Do
                Dim content As InterpolatedStringContentSyntax

                Select Case CurrentToken.Kind
                    Case SyntaxKind.InterpolatedStringTextToken

                        Dim textToken = DirectCast(CurrentToken, InterpolatedStringTextTokenSyntax)

                        ' At this point we're either right before an {, an } (error), or a "".
                        GetNextToken(ScannerState.InterpolatedStringPunctuation)

                        Debug.Assert(CurrentToken.Kind <> SyntaxKind.InterpolatedStringTextToken,
                                     "Two interpolated string text literal tokens encountered back-to-back. " &
                                     "Scanner should have scanned these as a single token.")

                        content = SyntaxFactory.InterpolatedStringText(textToken)

                    Case SyntaxKind.OpenBraceToken

                        content = ParseInterpolatedStringInterpolation()

                    Case SyntaxKind.CloseBraceToken

                        If skipped.IsNull Then
                            skipped = _pool.Allocate(Of SyntaxToken)
                        End If

                        skipped.Add(CurrentToken)
                        GetNextToken(ScannerState.InterpolatedStringContent)
                        Continue Do

                    Case SyntaxKind.DoubleQuoteToken

                        doubleQuoteToken = DirectCast(CurrentToken, PunctuationSyntax)
                        GetNextToken()
                        Exit Do

                    Case SyntaxKind.EndOfInterpolatedStringToken

                        doubleQuoteToken = InternalSyntaxFactory.MissingPunctuation(SyntaxKind.DoubleQuoteToken)
                        GetNextToken(ScannerState.VB)
                        Exit Do

                    Case Else

                        doubleQuoteToken = InternalSyntaxFactory.MissingPunctuation(SyntaxKind.DoubleQuoteToken)
                        Exit Do

                End Select

                If Not skipped.IsNull Then
                    content = AddLeadingSyntax(content, _pool.ToListAndFree(skipped), ERRID.ERR_Syntax)
                    skipped = Nothing
                End If

                contentBuilder.Add(content)
            Loop

            If Not skipped.IsNull Then
                doubleQuoteToken = AddLeadingSyntax(doubleQuoteToken, _pool.ToListAndFree(skipped), ERRID.ERR_Syntax)
                skipped = Nothing
            End If

            Dim node = SyntaxFactory.InterpolatedStringExpression(dollarSignDoubleQuoteToken,
                                                              _pool.ToListAndFree(contentBuilder),
                                                              doubleQuoteToken)
            Return CheckFeatureAvailability(Feature.InterpolatedStrings, node)
        End Function

        Private Function ParseInterpolatedStringInterpolation() As InterpolationSyntax
            Debug.Assert(CurrentToken.Kind = SyntaxKind.OpenBraceToken, "ParseInterpolatedStringEmbeddedExpression called on the wrong token.")

            Dim colonToken As PunctuationSyntax = Nothing
            Dim excessText As String = Nothing

            Dim openBraceToken = DirectCast(CurrentToken, PunctuationSyntax)
            GetNextToken(ScannerState.VB)

            Dim expression As ExpressionSyntax

            If CurrentToken.Kind = SyntaxKind.ColonToken Then

                openBraceToken = DirectCast(RemoveTrailingColonTriviaAndConvertToColonToken(openBraceToken, colonToken, excessText), PunctuationSyntax)
                expression = ReportSyntaxError(InternalSyntaxFactory.MissingExpression(), ERRID.ERR_ExpectedExpression)

            Else
                expression = ParseExpressionCore()

                ' Scanned this as a terminator. Fix it.
                If CurrentToken.Kind = SyntaxKind.ColonToken Then
                    expression = DirectCast(RemoveTrailingColonTriviaAndConvertToColonToken(expression, colonToken, excessText), ExpressionSyntax)
                End If
            End If

            Dim alignmentClauseOpt As InterpolationAlignmentClauseSyntax

            If CurrentToken.Kind = SyntaxKind.CommaToken Then
                Debug.Assert(colonToken Is Nothing)

                Dim commaToken = DirectCast(CurrentToken, PunctuationSyntax)
                GetNextToken(ScannerState.VB)

                If CurrentToken.Kind = SyntaxKind.ColonToken Then
                    commaToken = DirectCast(RemoveTrailingColonTriviaAndConvertToColonToken(commaToken, colonToken, excessText), PunctuationSyntax)
                End If

                Dim signTokenOpt As PunctuationSyntax

                If CurrentToken.Kind = SyntaxKind.MinusToken OrElse
                   CurrentToken.Kind = SyntaxKind.PlusToken Then

                    signTokenOpt = DirectCast(CurrentToken, PunctuationSyntax)
                    GetNextToken(ScannerState.VB)

                    If CurrentToken.Kind = SyntaxKind.ColonToken Then
                        signTokenOpt = DirectCast(RemoveTrailingColonTriviaAndConvertToColonToken(signTokenOpt, colonToken, excessText), PunctuationSyntax)
                    End If
                Else
                    signTokenOpt = Nothing
                End If

                Dim widthToken As IntegerLiteralTokenSyntax

                If CurrentToken.Kind = SyntaxKind.IntegerLiteralToken Then

                    widthToken = DirectCast(CurrentToken, IntegerLiteralTokenSyntax)
                    GetNextToken(ScannerState.VB)

                    If CurrentToken.Kind = SyntaxKind.ColonToken Then
                        widthToken = DirectCast(RemoveTrailingColonTriviaAndConvertToColonToken(widthToken, colonToken, excessText), IntegerLiteralTokenSyntax)
                    End If
                Else
                    widthToken = ReportSyntaxError(InternalSyntaxFactory.MissingIntegerLiteralToken(), ERRID.ERR_ExpectedIntLiteral)
                End If

                Dim valueExpression As ExpressionSyntax = SyntaxFactory.NumericLiteralExpression(widthToken)

                If signTokenOpt IsNot Nothing Then
                    valueExpression = SyntaxFactory.UnaryExpression(
                                                        If(signTokenOpt.Kind = SyntaxKind.PlusToken, SyntaxKind.UnaryPlusExpression, SyntaxKind.UnaryMinusExpression),
                                                        signTokenOpt,
                                                        valueExpression)
                End If

                alignmentClauseOpt = SyntaxFactory.InterpolationAlignmentClause(commaToken, valueExpression)
            Else
                alignmentClauseOpt = Nothing
            End If

            Dim formatStringClauseOpt As InterpolationFormatClauseSyntax

            If CurrentToken.Kind = SyntaxKind.ColonToken AndAlso colonToken IsNot Nothing Then
                ' If colonToken IsNot Nothing we were able to recovery.

                GetNextToken(ScannerState.InterpolatedStringFormatString)

                Dim formatStringToken As InterpolatedStringTextTokenSyntax

                If CurrentToken.Kind = SyntaxKind.InterpolatedStringTextToken Then
                    formatStringToken = DirectCast(CurrentToken, InterpolatedStringTextTokenSyntax)
                    GetNextToken(ScannerState.InterpolatedStringPunctuation)

                    If excessText IsNot Nothing Then
                        formatStringToken = InternalSyntaxFactory.InterpolatedStringTextToken(excessText & formatStringToken.Text,
                                                                                              excessText & formatStringToken.Value,
                                                                                              formatStringToken.GetLeadingTrivia(),
                                                                                              formatStringToken.GetTrailingTrivia())
                    End If
                Else
                    If excessText IsNot Nothing Then
                        formatStringToken = InternalSyntaxFactory.InterpolatedStringTextToken(excessText,
                                                                                              excessText,
                                                                                              Nothing,
                                                                                              Nothing)
                    Else
                        formatStringToken = Nothing
                    End If
                End If

                If formatStringToken Is Nothing Then
                    formatStringToken = DirectCast(InternalSyntaxFactory.MissingToken(SyntaxKind.InterpolatedStringTextToken), InterpolatedStringTextTokenSyntax)
                    formatStringToken = ReportSyntaxError(formatStringToken, ERRID.ERR_Syntax)

                ElseIf formatStringToken.GetTrailingTrivia() IsNot Nothing Then
                    formatStringToken = ReportSyntaxError(formatStringToken, ERRID.ERR_InterpolationFormatWhitespace)

                End If

                formatStringClauseOpt = SyntaxFactory.InterpolationFormatClause(colonToken, formatStringToken)

            Else
                formatStringClauseOpt = Nothing

                If CurrentToken.Kind = SyntaxKind.ColonToken Then
                    ' But if colonToken is null we weren't able to gracefully recover.
                    GetNextToken(ScannerState.InterpolatedStringFormatString)
                End If
            End If

            Dim closeBraceToken As PunctuationSyntax

            If CurrentToken.Kind = SyntaxKind.CloseBraceToken Then
                ' Must rescan this with interpolated string rules for trailing trivia.
                ' Specifically, any trailing trivia attached to the closing brace is actually interpolated string content.
                ResetCurrentToken(ScannerState.InterpolatedStringPunctuation)

                closeBraceToken = DirectCast(CurrentToken, PunctuationSyntax)
                GetNextToken(ScannerState.InterpolatedStringContent)

            ElseIf CurrentToken.Kind = SyntaxKind.EndOfInterpolatedStringToken Then
                GetNextToken(ScannerState.VB)

                closeBraceToken = DirectCast(HandleUnexpectedToken(SyntaxKind.CloseBraceToken), PunctuationSyntax)
            Else
                ' Content rules will either resync at a } or at the closing ".
                If Not IsValidStatementTerminator(CurrentToken) Then
                    ResetCurrentToken(ScannerState.InterpolatedStringFormatString)
                End If

                Debug.Assert(CurrentToken.Kind <> SyntaxKind.CloseBraceToken)
                closeBraceToken = DirectCast(HandleUnexpectedToken(SyntaxKind.CloseBraceToken), PunctuationSyntax)

                If CurrentToken.Kind = SyntaxKind.InterpolatedStringTextToken Then
                    ResetCurrentToken(ScannerState.InterpolatedStringContent)
                    GetNextToken(ScannerState.InterpolatedStringContent)
                End If
            End If

            Return SyntaxFactory.Interpolation(openBraceToken, expression, alignmentClauseOpt, formatStringClauseOpt, closeBraceToken)

        End Function

        Private Shared Function RemoveTrailingColonTriviaAndConvertToColonToken(
                             token As SyntaxToken,
                             <Out> ByRef colonToken As PunctuationSyntax,
                             <Out> ByRef excessText As String
                         ) As SyntaxToken

            If Not token.HasTrailingTrivia Then
                colonToken = Nothing
                excessText = Nothing
                Return token
            End If

            Dim triviaList As New CodeAnalysis.Syntax.InternalSyntax.SyntaxList(Of VisualBasicSyntaxNode)(token.GetTrailingTrivia())

            Dim indexOfFirstColon As Integer = -1
            Dim newTrailingTrivia As GreenNode

            If triviaList.Count = 1 Then
                indexOfFirstColon = 0
                newTrailingTrivia = Nothing
                excessText = Nothing
            ElseIf triviaList(0).Kind = SyntaxKind.ColonTrivia
                indexOfFirstColon = 0
                newTrailingTrivia = Nothing
                excessText = triviaList.GetEndOfTrivia(1).Node.ToFullString()
            Else
                For i = 0 To triviaList.Count - 1
                    If triviaList(i).Kind = SyntaxKind.ColonTrivia Then
                        indexOfFirstColon = i
                        Exit For
                    End If
                Next

                newTrailingTrivia = triviaList.GetStartOfTrivia(indexOfFirstColon).Node

                If indexOfFirstColon = triviaList.Count - 1 Then
                    excessText = Nothing
                Else
                    excessText = triviaList.GetEndOfTrivia(indexOfFirstColon + 1).Node.ToFullString()
                    Debug.Assert(triviaList.GetEndOfTrivia(indexOfFirstColon + 1).AnyAndOnly(SyntaxKind.ColonTrivia, SyntaxKind.WhitespaceTrivia))
                End If
            End If

            Dim firstColonTrivia = DirectCast(triviaList(indexOfFirstColon), SyntaxTrivia)

            colonToken = New PunctuationSyntax(SyntaxKind.ColonToken, firstColonTrivia.Text, Nothing, Nothing)

            Return DirectCast(token.WithTrailingTrivia(newTrailingTrivia), SyntaxToken)

        End Function

        Private Function RemoveTrailingColonTriviaAndConvertToColonToken(
                             node As VisualBasicSyntaxNode,
                             <Out> ByRef colonToken As PunctuationSyntax,
                             <Out> ByRef excessText As String
                         ) As VisualBasicSyntaxNode

            Dim lastNonMissing = DirectCast(node.GetLastToken(), SyntaxToken)

            Dim newLastNonMissing = RemoveTrailingColonTriviaAndConvertToColonToken(lastNonMissing, colonToken, excessText)

            Dim newNode = LastTokenReplacer.Replace(node, Function(t) If(t Is lastNonMissing, newLastNonMissing, t))

            ' If no token was replaced we have failed to recover; let high contexts deal with it.
            If newNode Is node Then
                colonToken = Nothing
                excessText = Nothing
            End If

            Return newNode
        End Function

    End Class

End Namespace
