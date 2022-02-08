' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports InternalSyntaxFactory = Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax.SyntaxFactory

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax

    ' Deal with the case where a token is not what is expected.
    ' Produces an error unless the construct is already in error.
    ' Always returns false.

    Partial Friend Class Parser
        ' File: Parser.cpp
        ' Lines: 16764 - 16764
        ' bool .Parser::HandleUnexpectedToken( [ tokens TokenType ] [ _Inout_ bool& ErrorInConstruct ] )

        Private Shared Function HandleUnexpectedToken(kind As SyntaxKind) As SyntaxToken
            Dim errorId = GetUnexpectedTokenErrorId(kind)
            Dim t = InternalSyntaxFactory.MissingToken(kind)
            Return ReportSyntaxError(t, errorId)
        End Function

        Private Shared Function HandleUnexpectedKeyword(kind As SyntaxKind) As KeywordSyntax
            Dim errorId = GetUnexpectedTokenErrorId(kind)
            Dim t = InternalSyntaxFactory.MissingKeyword(kind)
            Return ReportSyntaxError(t, errorId)
        End Function

        Private Shared Function GetUnexpectedTokenErrorId(kind As SyntaxKind) As ERRID

            Select Case (kind)
                Case SyntaxKind.AsKeyword
                    Return ERRID.ERR_ExpectedAs

                Case SyntaxKind.ByKeyword
                    Return ERRID.ERR_ExpectedBy

                Case SyntaxKind.CloseBraceToken
                    Return ERRID.ERR_ExpectedRbrace

                Case SyntaxKind.CloseParenToken
                    Return ERRID.ERR_ExpectedRparen

                Case SyntaxKind.CommaToken
                    Return ERRID.ERR_ExpectedComma

                Case SyntaxKind.DoubleQuoteToken
                    Return ERRID.ERR_ExpectedQuote

                Case SyntaxKind.DotToken
                    Return ERRID.ERR_ExpectedDot

                Case SyntaxKind.EndCDataToken
                    Return ERRID.ERR_ExpectedXmlEndCData

                Case SyntaxKind.EqualsKeyword
                    Return ERRID.ERR_ExpectedEquals

                Case SyntaxKind.EqualsToken
                    Return ERRID.ERR_ExpectedEQ

                Case SyntaxKind.GreaterThanToken
                    Return ERRID.ERR_ExpectedGreater

                Case SyntaxKind.IdentifierToken
                    Return ERRID.ERR_ExpectedIdentifier

                Case SyntaxKind.IntegerLiteralToken
                    Return ERRID.ERR_ExpectedIntLiteral

                Case SyntaxKind.InKeyword
                    Return ERRID.ERR_ExpectedIn

                Case SyntaxKind.IntoKeyword
                    Return ERRID.ERR_ExpectedInto

                Case SyntaxKind.IsKeyword
                    Return ERRID.ERR_MissingIsInTypeOf

                Case SyntaxKind.JoinKeyword
                    Return ERRID.ERR_ExpectedJoin

                Case SyntaxKind.LessThanToken,
                    SyntaxKind.LessThanSlashToken
                    Return ERRID.ERR_ExpectedLT

                Case SyntaxKind.LessThanPercentEqualsToken
                    Return ERRID.ERR_ExpectedXmlBeginEmbedded

                Case SyntaxKind.LibKeyword
                    Return ERRID.ERR_MissingLibInDeclare

                Case SyntaxKind.MinusToken
                    Return ERRID.ERR_ExpectedMinus

                Case SyntaxKind.MinusMinusGreaterThanToken
                    Return ERRID.ERR_ExpectedXmlEndComment

                Case SyntaxKind.NextKeyword
                    Return ERRID.ERR_MissingNext

                Case SyntaxKind.OfKeyword
                    Return ERRID.ERR_OfExpected

                Case SyntaxKind.OnKeyword
                    Return ERRID.ERR_ExpectedOn

                Case SyntaxKind.OpenBraceToken
                    Return ERRID.ERR_ExpectedLbrace

                Case SyntaxKind.OpenParenToken
                    Return ERRID.ERR_ExpectedLparen

                Case SyntaxKind.PercentGreaterThanToken
                    Return ERRID.ERR_ExpectedXmlEndEmbedded

                Case SyntaxKind.QuestionGreaterThanToken
                    Return ERRID.ERR_ExpectedXmlEndPI

                Case SyntaxKind.SemicolonToken
                    Return ERRID.ERR_ExpectedSColon

                Case SyntaxKind.SingleQuoteToken
                    Return ERRID.ERR_ExpectedSQuote

                Case SyntaxKind.SlashToken
                    Return ERRID.ERR_ExpectedDiv

                Case SyntaxKind.StringLiteralToken
                    Return ERRID.ERR_ExpectedStringLiteral

                Case SyntaxKind.XmlNameToken
                    Return ERRID.ERR_ExpectedXmlName

                Case SyntaxKind.WarningKeyword
                    Return ERRID.ERR_ExpectedWarningKeyword

                Case Else
                    Return ERRID.ERR_Syntax
            End Select
        End Function

        ' Produce an error message if the current token is not the expected TokenType.

        ' File: Parser.cpp
        ' Lines: 1021 - 1021
        ' inline bool .Parser::VerifyExpectedToken( [ tokens TokenType ] [ _Inout_ bool& ErrorInConstruct ] )

        ''' <summary>
        ''' Check that the current token is the expected kind, the current node is consumed and optionally a new line
        ''' after the token.
        ''' </summary>
        ''' <param name="kind">The expected node kind.</param>
        ''' <returns>A token of the expected kind.  This node may be an empty token with an error attached to it</returns>
        ''' <remarks>Since nodes are immutable, the only way to create nodes with errors attached is to create a node without an error,
        ''' then add an error with this method to create another node.</remarks>
        Private Function VerifyExpectedToken(Of T As SyntaxToken)(
                kind As SyntaxKind,
                ByRef token As T,
                Optional state As ScannerState = ScannerState.VB
            ) As Boolean

            Dim current As SyntaxToken = CurrentToken

            If current.Kind = kind Then
                token = DirectCast(current, T)
                GetNextToken(state)
                Return True
            Else
                token = DirectCast(HandleUnexpectedToken(kind), T)
                Return False
            End If
        End Function

    End Class

End Namespace

