' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

'-----------------------------------------------------------------------------
' Contains the definition of the Scanner, which produces tokens from text 
'-----------------------------------------------------------------------------
Option Compare Binary

Imports System.Text
Imports Microsoft.CodeAnalysis.VisualBasic.SyntaxFacts
Imports CoreInternalSyntax = Microsoft.CodeAnalysis.Syntax.InternalSyntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax
    Partial Friend Class Scanner

        Private Shared Function MakeMissingToken(precedingTrivia As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode), kind As SyntaxKind) As SyntaxToken
            Dim missing As SyntaxToken = SyntaxFactory.MissingToken(kind)
            If precedingTrivia.Any Then
                missing = DirectCast(missing.WithLeadingTrivia(precedingTrivia.Node), SyntaxToken)
            End If
            Return missing
        End Function

        Private Function XmlMakeLeftParenToken(precedingTrivia As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode)) As PunctuationSyntax
            AdvanceChar()
            Dim followingTrivia = ScanXmlWhitespace()

            Return MakePunctuationToken(SyntaxKind.OpenParenToken, "(", precedingTrivia, followingTrivia)
        End Function

        Private Function XmlMakeRightParenToken(precedingTrivia As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode)) As PunctuationSyntax
            AdvanceChar()
            Dim followingTrivia = ScanXmlWhitespace()

            Return MakePunctuationToken(SyntaxKind.CloseParenToken, ")", precedingTrivia, followingTrivia)
        End Function

        Private Function XmlMakeEqualsToken(precedingTrivia As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode)) As PunctuationSyntax
            AdvanceChar()
            Dim followingTrivia = ScanXmlWhitespace()

            Return MakePunctuationToken(SyntaxKind.EqualsToken, "=", precedingTrivia, followingTrivia)
        End Function

        Private Function XmlMakeDivToken(precedingTrivia As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode)) As PunctuationSyntax
            AdvanceChar()
            Dim followingTrivia = ScanXmlWhitespace()

            Return MakePunctuationToken(SyntaxKind.SlashToken, "/", precedingTrivia, followingTrivia)
        End Function

        Private Function XmlMakeColonToken(precedingTrivia As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode)) As PunctuationSyntax
            AdvanceChar()
            Dim followingTrivia = ScanXmlWhitespace()

            Return MakePunctuationToken(SyntaxKind.ColonToken, ":", precedingTrivia, followingTrivia)
        End Function

        Private Function XmlMakeGreaterToken(precedingTrivia As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode)) As PunctuationSyntax
            AdvanceChar()

            ' NOTE: > does not consume following trivia
            Return MakePunctuationToken(SyntaxKind.GreaterThanToken, ">", precedingTrivia, Nothing)
        End Function

        Private Function XmlMakeLessToken(precedingTrivia As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode)) As PunctuationSyntax
            AdvanceChar()
            Dim followingTrivia = ScanXmlWhitespace()

            Return MakePunctuationToken(SyntaxKind.LessThanToken, "<", precedingTrivia, followingTrivia)
        End Function

        Private Function XmlMakeBadToken(precedingTrivia As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode), length As Integer, id As ERRID) As BadTokenSyntax
            Return XmlMakeBadToken(SyntaxSubKind.None, precedingTrivia, length, id)
        End Function

        Private Function XmlMakeBadToken(subkind As SyntaxSubKind, precedingTrivia As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode), length As Integer, id As ERRID) As BadTokenSyntax
            Dim spelling = GetTextNotInterned(length)
            Dim followingTrivia = ScanXmlWhitespace()

            Dim result1 = SyntaxFactory.BadToken(subkind, spelling, precedingTrivia.Node, followingTrivia)

            Dim diagnostic As DiagnosticInfo

            Select Case id
                Case ERRID.ERR_IllegalXmlStartNameChar,
                    ERRID.ERR_IllegalXmlNameChar
                    Debug.Assert(length = 1)

                    If id = ERRID.ERR_IllegalXmlNameChar AndAlso
                        (precedingTrivia.Any OrElse
                        PrevToken Is Nothing OrElse
                        PrevToken.HasTrailingTrivia OrElse
                        PrevToken.Kind = SyntaxKind.LessThanToken OrElse
                        PrevToken.Kind = SyntaxKind.LessThanSlashToken OrElse
                        PrevToken.Kind = SyntaxKind.LessThanQuestionToken) Then
                        id = ERRID.ERR_IllegalXmlStartNameChar
                    End If
                    Dim xmlCh = spelling(0)
                    Dim xmlChAsUnicode = UTF16ToUnicode(New XmlCharResult(xmlCh))
                    diagnostic = ErrorFactory.ErrorInfo(id, xmlCh, String.Format("&H{0:X}", xmlChAsUnicode))
                Case Else
                    diagnostic = ErrorFactory.ErrorInfo(id)
            End Select

            Dim errResult1 = DirectCast(result1.SetDiagnostics({diagnostic}), BadTokenSyntax)
            Debug.Assert(errResult1 IsNot Nothing)

            Return errResult1
        End Function

        Private Function XmlMakeSingleQuoteToken(precedingTrivia As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode),
                                                 spelling As Char,
                                                 isOpening As Boolean) As PunctuationSyntax
            Debug.Assert(Peek() = spelling)

            AdvanceChar()

            Dim followingTrivia As GreenNode = Nothing
            If Not isOpening Then
                Dim ws = ScanXmlWhitespace()
                followingTrivia = ws
            End If

            Return MakePunctuationToken(SyntaxKind.SingleQuoteToken, Intern(spelling), precedingTrivia, followingTrivia)
        End Function

        Private Function XmlMakeDoubleQuoteToken(precedingTrivia As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode),
                                                 spelling As Char,
                                                 isOpening As Boolean) As PunctuationSyntax
            Debug.Assert(Peek() = spelling)

            AdvanceChar()

            Dim followingTrivia As GreenNode = Nothing
            If Not isOpening Then
                Dim ws = ScanXmlWhitespace()
                followingTrivia = ws
            End If

            Return MakePunctuationToken(SyntaxKind.DoubleQuoteToken, Intern(spelling), precedingTrivia, followingTrivia)
        End Function

        Private Function XmlMakeXmlNCNameToken(
                        precedingTrivia As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode),
                        TokenWidth As Integer
                    ) As XmlNameTokenSyntax

            Debug.Assert(TokenWidth > 0)

            Dim text = GetText(TokenWidth)

            'Xml/Version/Standalone/Encoding/DOCTYPE
            Dim contextualKind As SyntaxKind = SyntaxKind.XmlNameToken

            Select Case text.Length
                Case 3
                    If String.Equals(text, "xml", StringComparison.Ordinal) Then
                        contextualKind = SyntaxKind.XmlKeyword
                    End If
            End Select

            If contextualKind = SyntaxKind.XmlNameToken Then
                contextualKind = TokenOfStringCached(text)
                If contextualKind = SyntaxKind.IdentifierToken Then
                    contextualKind = SyntaxKind.XmlNameToken
                End If
            End If

            Dim followingTrivia = ScanXmlWhitespace()
            Return SyntaxFactory.XmlNameToken(text, contextualKind, precedingTrivia.Node, followingTrivia)
        End Function

        Private Function XmlMakeAttributeDataToken(
                       precedingTrivia As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode),
                       TokenWidth As Integer,
                       Value As String
                   ) As XmlTextTokenSyntax

            Debug.Assert(TokenWidth > 0)

            Dim text = GetTextNotInterned(TokenWidth)
            ' NOTE: XmlMakeAttributeData does not consume trailing trivia.
            Return SyntaxFactory.XmlTextLiteralToken(text, Value, precedingTrivia.Node, Nothing)

        End Function

        Private Function XmlMakeAttributeDataToken(
                       precedingTrivia As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode),
                       TokenWidth As Integer,
                       Scratch As StringBuilder
                   ) As XmlTextTokenSyntax

            ' NOTE: XmlMakeAttributeData does not consume trailing trivia.
            Return XmlMakeTextLiteralToken(precedingTrivia, TokenWidth, Scratch)
        End Function

        Private Function XmlMakeEntityLiteralToken(
                precedingTrivia As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode),
                TokenWidth As Integer,
                Value As String
          ) As XmlTextTokenSyntax

            Debug.Assert(TokenWidth > 0)
            Return SyntaxFactory.XmlEntityLiteralToken(GetText(TokenWidth), Value, precedingTrivia.Node, Nothing)
        End Function

        Private Shared ReadOnly s_xmlAmpToken As XmlTextTokenSyntax = SyntaxFactory.XmlEntityLiteralToken("&amp;", "&", Nothing, Nothing)
        Private Function XmlMakeAmpLiteralToken(
                precedingTrivia As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode)
          ) As XmlTextTokenSyntax

            AdvanceChar(5) ' "&amp;".Length
            Return If(precedingTrivia.Node Is Nothing, s_xmlAmpToken, SyntaxFactory.XmlEntityLiteralToken("&amp;", "&", precedingTrivia.Node, Nothing))
        End Function

        Private Shared ReadOnly s_xmlAposToken As XmlTextTokenSyntax = SyntaxFactory.XmlEntityLiteralToken("&apos;", "'", Nothing, Nothing)
        Private Function XmlMakeAposLiteralToken(
                precedingTrivia As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode)
          ) As XmlTextTokenSyntax

            AdvanceChar(6) ' "&apos;".Length
            Return If(precedingTrivia.Node Is Nothing, s_xmlAposToken, SyntaxFactory.XmlEntityLiteralToken("&apos;", "'", precedingTrivia.Node, Nothing))
        End Function

        Private Shared ReadOnly s_xmlGtToken As XmlTextTokenSyntax = SyntaxFactory.XmlEntityLiteralToken("&gt;", ">", Nothing, Nothing)
        Private Function XmlMakeGtLiteralToken(
                precedingTrivia As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode)
          ) As XmlTextTokenSyntax

            AdvanceChar(4) ' "&gt;".Length
            Return If(precedingTrivia.Node Is Nothing, s_xmlGtToken, SyntaxFactory.XmlEntityLiteralToken("&gt;", "&", precedingTrivia.Node, Nothing))
        End Function

        Private Shared ReadOnly s_xmlLtToken As XmlTextTokenSyntax = SyntaxFactory.XmlEntityLiteralToken("&lt;", "<", Nothing, Nothing)
        Private Function XmlMakeLtLiteralToken(
                precedingTrivia As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode)
          ) As XmlTextTokenSyntax

            AdvanceChar(4) ' "&lt;".Length
            Return If(precedingTrivia.Node Is Nothing, s_xmlLtToken, SyntaxFactory.XmlEntityLiteralToken("&lt;", "<", precedingTrivia.Node, Nothing))
        End Function

        Private Shared ReadOnly s_xmlQuotToken As XmlTextTokenSyntax = SyntaxFactory.XmlEntityLiteralToken("&quot;", """", Nothing, Nothing)
        Private Function XmlMakeQuotLiteralToken(
                precedingTrivia As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode)
          ) As XmlTextTokenSyntax

            AdvanceChar(6) ' "&quot;".Length
            Return If(precedingTrivia.Node Is Nothing, s_xmlQuotToken, SyntaxFactory.XmlEntityLiteralToken("&quot;", """", precedingTrivia.Node, Nothing))
        End Function

        Private Function XmlMakeTextLiteralToken(
                        precedingTrivia As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode),
                        TokenWidth As Integer,
                        Scratch As StringBuilder
                  ) As XmlTextTokenSyntax

            Debug.Assert(TokenWidth > 0)
            Dim text = GetTextNotInterned(TokenWidth)

            ' PERF: It's common for the text and the 'value' to be identical. If so, try to unify the
            ' two strings.
            Dim value = GetScratchText(Scratch, text)

            Return SyntaxFactory.XmlTextLiteralToken(text, value, precedingTrivia.Node, Nothing)

        End Function

        Private Shared ReadOnly s_docCommentCrLfToken As XmlTextTokenSyntax = SyntaxFactory.DocumentationCommentLineBreakToken(vbCrLf, vbLf, Nothing, Nothing)

        Private Function MakeDocCommentLineBreakToken(
                precedingTrivia As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode),
                TokenWidth As Integer
          ) As XmlTextTokenSyntax

            Dim text = GetText(TokenWidth)
            Debug.Assert(text = vbCr OrElse text = vbLf OrElse text = vbCrLf)

            If precedingTrivia.Node Is Nothing AndAlso text = vbCrLf Then
                Return s_docCommentCrLfToken
            End If

            Return SyntaxFactory.DocumentationCommentLineBreakToken(text, vbLf, precedingTrivia.Node, Nothing)

        End Function

        Private Function XmlMakeTextLiteralToken(
                precedingTrivia As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode),
                TokenWidth As Integer,
                err As ERRID
          ) As XmlTextTokenSyntax

            Debug.Assert(TokenWidth > 0)
            Dim text = GetTextNotInterned(TokenWidth)
            Return DirectCast(SyntaxFactory.XmlTextLiteralToken(text, text, precedingTrivia.Node, Nothing).SetDiagnostics({ErrorFactory.ErrorInfo(err)}), XmlTextTokenSyntax)

        End Function

        Private Function XmlMakeBeginEndElementToken(precedingTrivia As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode), scanTrailingTrivia As ScanTriviaFunc) As PunctuationSyntax
            Debug.Assert(NextAre("</"))

            AdvanceChar(2)
            Dim followingTrivia = scanTrailingTrivia()
            Return MakePunctuationToken(SyntaxKind.LessThanSlashToken, "</", precedingTrivia, followingTrivia)
        End Function

        Private Function XmlMakeEndEmptyElementToken(precedingTrivia As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode)) As PunctuationSyntax
            Debug.Assert(NextAre("/>"))

            AdvanceChar(2)
            Return MakePunctuationToken(SyntaxKind.SlashGreaterThanToken, "/>", precedingTrivia, Nothing)
        End Function

#Region "EmbeddedToken"
        Private Function XmlMakeBeginEmbeddedToken(precedingTrivia As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode)) As PunctuationSyntax
            Debug.Assert(NextAre("<%="))
            AdvanceChar(3)
            Return MakePunctuationToken(SyntaxKind.LessThanPercentEqualsToken, "<%=", precedingTrivia, Nothing)
        End Function

        Private Function XmlMakeEndEmbeddedToken(precedingTrivia As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode), scanTrailingTrivia As ScanTriviaFunc) As PunctuationSyntax
            Debug.Assert(Peek() = "%"c OrElse Peek() = FULLWIDTH_PERCENT_SIGN)
            Debug.Assert(Peek(1) = ">"c)

            Dim spelling As String
            If Peek() = "%"c Then
                AdvanceChar(2)
                spelling = "%>"
            Else
                spelling = GetText(2)
            End If

            Dim followingTrivia = scanTrailingTrivia()
            Return MakePunctuationToken(SyntaxKind.PercentGreaterThanToken, spelling, precedingTrivia, followingTrivia)
        End Function

#End Region

#Region "DTD"
        Private Function XmlMakeBeginDTDToken(precedingTrivia As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode)) As BadTokenSyntax
            Debug.Assert(NextAre("<!DOCTYPE"))
            Return XmlMakeBadToken(SyntaxSubKind.BeginDocTypeToken, precedingTrivia, 9, ERRID.ERR_DTDNotSupported)
        End Function

        Private Function XmlLessThanExclamationToken(state As ScannerState, precedingTrivia As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode)) As BadTokenSyntax
            Debug.Assert(NextAre("<!"))
            Return XmlMakeBadToken(SyntaxSubKind.LessThanExclamationToken, precedingTrivia, 2, If(state = ScannerState.DocType, ERRID.ERR_DTDNotSupported, ERRID.ERR_Syntax))
        End Function

        Private Function XmlMakeOpenBracketToken(state As ScannerState, precedingTrivia As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode)) As BadTokenSyntax
            Debug.Assert(Peek() = "["c)
            Return XmlMakeBadToken(SyntaxSubKind.OpenBracketToken, precedingTrivia, 1, If(state = ScannerState.DocType, ERRID.ERR_DTDNotSupported, ERRID.ERR_IllegalXmlNameChar))
        End Function

        Private Function XmlMakeCloseBracketToken(state As ScannerState, precedingTrivia As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode)) As BadTokenSyntax
            Debug.Assert(Peek() = "]"c)

            Return XmlMakeBadToken(SyntaxSubKind.CloseBracketToken, precedingTrivia, 1, If(state = ScannerState.DocType, ERRID.ERR_DTDNotSupported, ERRID.ERR_IllegalXmlNameChar))
        End Function
#End Region

#Region "ProcessingInstruction"

        Private Function XmlMakeBeginProcessingInstructionToken(precedingTrivia As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode), scanTrailingTrivia As ScanTriviaFunc) As PunctuationSyntax
            Debug.Assert(NextAre("<?"))
            AdvanceChar(2)
            Dim followingTrivia = scanTrailingTrivia()
            Return MakePunctuationToken(SyntaxKind.LessThanQuestionToken, "<?", precedingTrivia, followingTrivia)
        End Function

        Private Function XmlMakeProcessingInstructionToken(precedingTrivia As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode), TokenWidth As Integer) As XmlTextTokenSyntax
            Debug.Assert(TokenWidth > 0)

            'TODO - Normalize new lines.
            Dim text = GetTextNotInterned(TokenWidth)
            Return SyntaxFactory.XmlTextLiteralToken(text, text, precedingTrivia.Node, Nothing)

        End Function

        Private Function XmlMakeEndProcessingInstructionToken(precedingTrivia As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode)) As PunctuationSyntax
            Debug.Assert(NextAre("?>"))
            AdvanceChar(2)
            Return MakePunctuationToken(SyntaxKind.QuestionGreaterThanToken, "?>", precedingTrivia, Nothing)
        End Function
#End Region

#Region "Comment"

        Private Function XmlMakeBeginCommentToken(precedingTrivia As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode), scanTrailingTrivia As ScanTriviaFunc) As PunctuationSyntax
            Debug.Assert(NextAre("<!--"))
            AdvanceChar(4)
            Dim followingTrivia = scanTrailingTrivia()
            Return MakePunctuationToken(SyntaxKind.LessThanExclamationMinusMinusToken, "<!--", precedingTrivia, followingTrivia)
        End Function

        Private Function XmlMakeCommentToken(precedingTrivia As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode), TokenWidth As Integer) As XmlTextTokenSyntax
            Debug.Assert(TokenWidth > 0)

            'TODO - Normalize new lines.
            Dim text = GetTextNotInterned(TokenWidth)
            Return SyntaxFactory.XmlTextLiteralToken(text, text, precedingTrivia.Node, Nothing)

        End Function

        Private Function XmlMakeEndCommentToken(precedingTrivia As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode)) As PunctuationSyntax
            Debug.Assert(NextAre("-->"))
            AdvanceChar(3)
            Return MakePunctuationToken(SyntaxKind.MinusMinusGreaterThanToken, "-->", precedingTrivia, Nothing)
        End Function

#End Region

#Region "CData"
        Private Function XmlMakeBeginCDataToken(precedingTrivia As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode), scanTrailingTrivia As ScanTriviaFunc) As PunctuationSyntax
            Debug.Assert(NextAre("<![CDATA["))

            AdvanceChar(9)
            Dim followingTrivia = scanTrailingTrivia()
            Return MakePunctuationToken(SyntaxKind.BeginCDataToken, "<![CDATA[", precedingTrivia, followingTrivia)
        End Function

        Private Function XmlMakeCDataToken(precedingTrivia As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode), TokenWidth As Integer, scratch As StringBuilder) As XmlTextTokenSyntax
            Return XmlMakeTextLiteralToken(precedingTrivia, TokenWidth, scratch)
        End Function

        Private Function XmlMakeEndCDataToken(precedingTrivia As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode)) As PunctuationSyntax
            Debug.Assert(NextAre("]]>"))
            AdvanceChar(3)
            Return MakePunctuationToken(SyntaxKind.EndCDataToken, "]]>", precedingTrivia, Nothing)
        End Function
#End Region

    End Class
End Namespace
