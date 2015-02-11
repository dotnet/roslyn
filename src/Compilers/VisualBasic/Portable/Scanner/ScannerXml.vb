﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

'-----------------------------------------------------------------------------
' Contains the definition of the Scanner, which produces tokens from text 
'-----------------------------------------------------------------------------
Option Compare Binary
Option Strict On

Imports System.Text
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.SyntaxFacts

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax
    Partial Friend Class Scanner

        Private Function ScanXmlTrivia(c As Char) As SyntaxList(Of VisualBasicSyntaxNode)
            Debug.Assert(Not IsScanningXmlDoc)
            Debug.Assert(c = CARRIAGE_RETURN OrElse c = LINE_FEED OrElse c = " "c OrElse c = CHARACTER_TABULATION)

            Dim builder = triviaListPool.Allocate

            Dim len = 0
            Do
                If c = " "c OrElse c = CHARACTER_TABULATION Then
                    len += 1
                ElseIf c = CARRIAGE_RETURN OrElse c = LINE_FEED Then
                    If len > 0 Then
                        builder.Add(MakeWhiteSpaceTrivia(GetText(len)))
                        len = 0
                    End If
                    builder.Add(ScanNewlineAsTrivia(c))
                Else
                    Exit Do
                End If

                If Not CanGetCharAtOffset(len) Then
                    Exit Do
                End If
                c = PeekAheadChar(len)
            Loop

            If len > 0 Then
                builder.Add(MakeWhiteSpaceTrivia(GetText(len)))
                len = 0
            End If
            Debug.Assert(builder.Count > 0)

            Dim result = builder.ToList
            triviaListPool.Free(builder)

            Return result
        End Function

        Friend Function ScanXmlElement(Optional state As ScannerState = ScannerState.Element) As SyntaxToken
            Debug.Assert(state = ScannerState.Element OrElse state = ScannerState.EndElement OrElse state = ScannerState.DocType)

            ' SHIM
            If IsScanningXmlDoc Then
                Return ScanXmlElementInXmlDoc(state)
            End If

            ' // Only legal tokens
            ' //  QName
            ' //  /
            ' //  >
            ' //  =
            ' //  Whitespace

            Dim leadingTrivia As SyntaxList(Of VisualBasicSyntaxNode) = Nothing

            While CanGetChar()
                Dim c As Char = PeekChar()

                Select Case (c)
                    ' // Whitespace
                    ' //  S    ::=    (#x20 | #x9 | #xD | #xA)+
                    Case CARRIAGE_RETURN, LINE_FEED
                        ' we should not visit this place twice
                        Debug.Assert(leadingTrivia.Node Is Nothing)

                        Dim offsets = CreateOffsetRestorePoint()
                        leadingTrivia = ScanXmlTrivia(c)

                        If ScanXmlForPossibleStatement(state) Then
                            offsets.Restore()
                            Return SyntaxFactory.Token(Nothing, SyntaxKind.EndOfXmlToken, Nothing, String.Empty)
                        End If

                    Case " "c, CHARACTER_TABULATION
                        ' we should not visit this place twice
                        Debug.Assert(leadingTrivia.Node Is Nothing)
                        leadingTrivia = ScanXmlTrivia(c)

                    Case "/"c
                        If CanGetCharAtOffset(1) AndAlso PeekAheadChar(1) = ">" Then
                            Return XmlMakeEndEmptyElementToken(leadingTrivia)
                        End If
                        Return XmlMakeDivToken(leadingTrivia)

                    Case ">"c
                        ' TODO: this will not consume trailing trivia
                        ' consider cases where this is the last element in the literal.
                        Return XmlMakeGreaterToken(leadingTrivia)

                    Case "="c
                        Return XmlMakeEqualsToken(leadingTrivia)

                    Case "'"c, LEFT_SINGLE_QUOTATION_MARK, RIGHT_SINGLE_QUOTATION_MARK
                        Return XmlMakeSingleQuoteToken(leadingTrivia, c, isOpening:=True)

                    Case """"c, LEFT_DOUBLE_QUOTATION_MARK, RIGHT_DOUBLE_QUOTATION_MARK
                        Return XmlMakeDoubleQuoteToken(leadingTrivia, c, isOpening:=True)

                    Case "<"c
                        If CanGetCharAtOffset(1) Then
                            Dim ch As Char = PeekAheadChar(1)
                            Select Case ch
                                Case "!"c
                                    If CanGetCharAtOffset(2) Then
                                        Select Case (PeekAheadChar(2))
                                            Case "-"c
                                                If CanGetCharAtOffset(3) AndAlso PeekAheadChar(3) = "-"c Then
                                                    Return XmlMakeBeginCommentToken(leadingTrivia, _scanNoTriviaFunc)
                                                End If
                                            Case "["c
                                                If CanGetCharAtOffset(8) AndAlso
                                                    PeekAheadChar(3) = "C"c AndAlso
                                                    PeekAheadChar(4) = "D"c AndAlso
                                                    PeekAheadChar(5) = "A"c AndAlso
                                                    PeekAheadChar(6) = "T"c AndAlso
                                                    PeekAheadChar(7) = "A"c AndAlso
                                                    PeekAheadChar(8) = "["c Then

                                                    Return XmlMakeBeginCDataToken(leadingTrivia, _scanNoTriviaFunc)
                                                End If
                                            Case "D"c
                                                If CanGetCharAtOffset(8) AndAlso
                                                    PeekAheadChar(3) = "O"c AndAlso
                                                    PeekAheadChar(4) = "C"c AndAlso
                                                    PeekAheadChar(5) = "T"c AndAlso
                                                    PeekAheadChar(6) = "Y"c AndAlso
                                                    PeekAheadChar(7) = "P"c AndAlso
                                                    PeekAheadChar(8) = "E"c Then
                                                    Return XmlMakeBeginDTDToken(leadingTrivia)
                                                End If
                                        End Select
                                    End If
                                    Return XmlLessThanExclamationToken(state, leadingTrivia)
                                Case "%"c
                                    If CanGetCharAtOffset(2) AndAlso
                                        PeekAheadChar(2) = "=" Then

                                        Return XmlMakeBeginEmbeddedToken(leadingTrivia)
                                    End If
                                Case "?"c
                                    Return XmlMakeBeginProcessingInstructionToken(leadingTrivia, _scanNoTriviaFunc)
                                Case "/"c
                                    Return XmlMakeBeginEndElementToken(leadingTrivia, _scanNoTriviaFunc)
                            End Select
                        End If

                        Return XmlMakeLessToken(leadingTrivia)

                    Case "?"c

                        If CanGetCharAtOffset(1) AndAlso PeekAheadChar(1) = ">"c Then
                            ' // Create token for the '?>' termination sequence
                            Return XmlMakeEndProcessingInstructionToken(leadingTrivia)
                        End If

                        Return XmlMakeBadToken(leadingTrivia, 1, ERRID.ERR_IllegalXmlNameChar)

                    Case "("c
                        Return XmlMakeLeftParenToken(leadingTrivia)

                    Case ")"c
                        Return XmlMakeRightParenToken(leadingTrivia)

                    Case "!"c,
                       ";"c,
                       "#"c,
                       ","c,
                       "}"c
                        Return XmlMakeBadToken(leadingTrivia, 1, ERRID.ERR_IllegalXmlNameChar)

                    Case ":"c
                        Return XmlMakeColonToken(leadingTrivia)

                    Case "["c
                        Return XmlMakeOpenBracketToken(state, leadingTrivia)

                    Case "]"c
                        Return XmlMakeCloseBracketToken(state, leadingTrivia)

                    Case Else
                        ' // Because of weak scanning of QName, this state must always handle
                        ' //    '=' | '\'' | '"'| '/' | '>' | '<' | '?'

                        Return ScanXmlNcName(leadingTrivia)

                End Select
            End While
            Return MakeEofToken(leadingTrivia)
        End Function

        '//
        '// This is used to detect a VB statement on the next line 
        '//
        '// NL WS* KW WS* ID | KW
        '// Example  Dim x
        '//
        '// For EndElement state only, </x followed by Sub
        '// NL WS* KW | ID
        '// Example Sub
        '//
        '// NL WS* ID WS* (
        '// Example  Console.WriteLine (
        '//
        '// NL WS* < ID WS* (
        '// Example <ClsCompliant(
        '// 
        '// NL WS* # WS* KW
        '// Example #END
        '// 
        '// NL WS* '
        '// Example ' This is a comment
        Private Function ScanXmlForPossibleStatement(state As ScannerState) As Boolean
            If Not CanGetChar() Then
                Return False
            End If

            Dim token As SyntaxToken
            Dim possibleStatement As Boolean = False
            Dim offsets = CreateOffsetRestorePoint()
            Dim c As Char = PeekChar()

            Select Case c
                Case "#"c,
                    FULLWIDTH_NUMBER_SIGN

                    ' Check for preprocessor statement, i.e. # if
                    AdvanceChar(1)
                    token = ScanNextToken(allowLeadingMultilineTrivia:=False)
                    possibleStatement = token.IsKeyword

                Case "<"c,
                    FULLWIDTH_LESS_THAN_SIGN

                    ' Check for code attribute, i.e < clscompliant (
                    AdvanceChar(1)
                    Dim leadingTrivia = ScanSingleLineTrivia()
                    ' Use ScanXmlNcName instead of ScanToken because it won't stop on "."
                    token = ScanXmlNcName(leadingTrivia)
                    Dim name As XmlNameTokenSyntax = TryCast(token, XmlNameTokenSyntax)

                    If name IsNot Nothing AndAlso Not name.IsMissing Then
                        If name.PossibleKeywordKind <> SyntaxKind.XmlNameToken Then
                            leadingTrivia = ScanSingleLineTrivia()
                            c = PeekChar()
                            possibleStatement =
                                c = "("c OrElse c = FULLWIDTH_LEFT_PARENTHESIS
                        End If
                    End If

                Case Else
                    If IsSingleQuote(c) AndAlso LastToken.Kind <> SyntaxKind.EqualsToken Then
                        ' Check for comment
                        possibleStatement = True
                    Else
                        ' Check for statement or call
                        Dim leadingTrivia = ScanSingleLineTrivia()
                        ' Use ScanXmlNcName instead of ScanToken because it won't stop on "."
                        token = ScanXmlNcName(leadingTrivia)

                        Dim name As XmlNameTokenSyntax = TryCast(token, XmlNameTokenSyntax)

                        If name IsNot Nothing AndAlso Not token.IsMissing Then

                            If state = ScannerState.EndElement Then
                                possibleStatement = token.Kind = SyntaxKind.XmlNameToken OrElse
                                                    LastToken.Kind = SyntaxKind.XmlNameToken
                                Exit Select
                            End If

                            ' If there was leading trivia, it must be trivia recognized by VB but not XML.
                            Debug.Assert(Not leadingTrivia.Any() OrElse
                                         (IsNewLine(c) AndAlso (c <> CARRIAGE_RETURN) AndAlso (c <> LINE_FEED)) OrElse
                                         (IsWhitespace(c) AndAlso Not IsXmlWhitespace(c)))

                            token = ScanNextToken(allowLeadingMultilineTrivia:=False)

                            If name.PossibleKeywordKind = SyntaxKind.XmlNameToken Then
                                possibleStatement =
                                    token.Kind = SyntaxKind.OpenParenToken
                            Else
                                possibleStatement =
                                    (token.Kind = SyntaxKind.IdentifierToken) OrElse token.IsKeyword
                            End If
                        End If

                    End If

            End Select

            offsets.Restore()
            Return possibleStatement
        End Function

        Friend Function ScanXmlContent() As SyntaxToken
            ' SHIM
            If Me.IsScanningXmlDoc Then
                Return ScanXmlContentInXmlDoc()
            End If

            ' // [14]    CharData    ::=    [^<&]* - ([^<&]* ']]>' [^<&]*)

            Dim Here As Integer = 0

            Dim IsAllWhitespace As Boolean = True
            ' lets do an unusual peek-behind to make sure we are not restarting after a non-Ws char.
            If _lineBufferOffset > 0 Then
                Dim prevChar = PeekAheadChar(-1)
                If prevChar <> ">"c AndAlso Not XmlCharType.IsWhiteSpace(prevChar) Then
                    IsAllWhitespace = False
                End If
            End If

            Dim scratch = GetScratch()

            While CanGetCharAtOffset(Here)
                Dim c As Char = PeekAheadChar(Here)

                Select Case (c)
                    Case CARRIAGE_RETURN, LINE_FEED
                        Here = SkipLineBreak(c, Here)
                        scratch.Append(LINE_FEED)

                    Case " "c, CHARACTER_TABULATION
                        scratch.Append(c)
                        Here += 1

                    Case "&"c
                        If Here <> 0 Then
                            Return XmlMakeTextLiteralToken(Nothing, Here, scratch)
                        End If

                        ' TODO: the entity could be whitespace, do we want to report it as WS?
                        Return ScanXmlReference(Nothing)

                    Case "<"c
                        Dim precedingTrivia As SyntaxList(Of VisualBasicSyntaxNode) = Nothing
                        If Here <> 0 Then
                            If Not IsAllWhitespace Then
                                Return XmlMakeTextLiteralToken(Nothing, Here, scratch)
                            Else
                                scratch.Clear() ' will not use this
                                Here = 0        ' consumed chars. 
                                precedingTrivia = ScanXmlTrivia(PeekChar)
                            End If
                        End If

                        Debug.Assert(Here = 0)
                        If CanGetCharAtOffset(1) Then
                            Dim ch As Char = PeekAheadChar(1)
                            Select Case ch
                                Case "!"c
                                    If CanGetCharAtOffset(2) Then
                                        Select Case (PeekAheadChar(2))
                                            Case "-"c
                                                If CanGetCharAtOffset(3) AndAlso PeekAheadChar(3) = "-"c Then
                                                    Return XmlMakeBeginCommentToken(precedingTrivia, _scanNoTriviaFunc)
                                                End If
                                            Case "["c
                                                If CanGetCharAtOffset(8) AndAlso _
                                                    PeekAheadChar(3) = "C"c AndAlso _
                                                    PeekAheadChar(4) = "D"c AndAlso _
                                                    PeekAheadChar(5) = "A"c AndAlso _
                                                    PeekAheadChar(6) = "T"c AndAlso _
                                                    PeekAheadChar(7) = "A"c AndAlso _
                                                    PeekAheadChar(8) = "["c Then

                                                    Return XmlMakeBeginCDataToken(precedingTrivia, _scanNoTriviaFunc)
                                                End If
                                            Case "D"c
                                                If CanGetCharAtOffset(8) AndAlso
                                                    PeekAheadChar(3) = "O"c AndAlso
                                                    PeekAheadChar(4) = "C"c AndAlso
                                                    PeekAheadChar(5) = "T"c AndAlso
                                                    PeekAheadChar(6) = "Y"c AndAlso
                                                    PeekAheadChar(7) = "P"c AndAlso
                                                    PeekAheadChar(8) = "E"c Then
                                                    Return XmlMakeBeginDTDToken(precedingTrivia)
                                                End If
                                        End Select
                                    End If
                                Case "%"c
                                    If CanGetCharAtOffset(2) AndAlso
                                        PeekAheadChar(2) = "=" Then

                                        Return XmlMakeBeginEmbeddedToken(precedingTrivia)
                                    End If
                                Case "?"c
                                    Return XmlMakeBeginProcessingInstructionToken(precedingTrivia, _scanNoTriviaFunc)
                                Case "/"c
                                    Return XmlMakeBeginEndElementToken(precedingTrivia, _scanNoTriviaFunc)
                            End Select
                        End If

                        Return XmlMakeLessToken(precedingTrivia)

                    Case "]"c
                        If CanGetCharAtOffset(Here + 2) AndAlso _
                           PeekAheadChar(Here + 1) = "]"c AndAlso _
                           PeekAheadChar(Here + 2) = ">"c Then

                            ' // If valid characters found then return them.
                            If Here <> 0 Then
                                Return XmlMakeTextLiteralToken(Nothing, Here, scratch)
                            End If

                            ' // Create an invalid character data token for the illegal ']]>' sequence
                            Return XmlMakeTextLiteralToken(Nothing, 3, ERRID.ERR_XmlEndCDataNotAllowedInContent)
                        End If
                        GoTo ScanChars

                    Case "#"c
                        ' // Even though # is valid in content, abort xml scanning if the m_State shows and error
                        ' // and the line begins with NL WS* # WS* KW

                        'TODO: error recovery - how can we do ths?

                        'If m_State.m_IsXmlError Then
                        '    MakeXmlCharToken(tokens.tkXmlCharData, Here - m_InputStreamPosition, IsAllWhitespace)
                        '    m_InputStreamPosition = Here

                        '    Dim sharp As Token = MakeToken(tokens.tkSharp, 1)
                        '    m_InputStreamPosition += 1

                        '    While (m_InputStream(m_InputStreamPosition) = " "c OrElse m_InputStream(m_InputStreamPosition) = CHARACTER_TABULATION)
                        '        m_InputStreamPosition += 1
                        '    End While

                        '    ScanXmlQName()

                        '    Dim restart As Token = CheckXmlForStatement()

                        '    If restart IsNot Nothing Then
                        '        ' // Abort Xml - Found Keyword space at the beginning of the line
                        '        AbandonTokens(restart)
                        '        m_State.Init(LexicalState.VB)
                        '        MakeToken(tokens.tkXmlAbort, 0)
                        '        Return
                        '    End If

                        '    AbandonTokens(sharp)
                        '    Here = m_InputStreamPosition
                        'End If
                        GoTo ScanChars

                    Case "%"c

                        'TODO: error recovery. We cannot do this. 
                        'If there is all whitespace after ">", it will be scanned as insignificant, 
                        'but in this case it is significant.
                        'Also as far as I can see Dev10 does not resync on "%>" text anyways.

                        '' // Even though %> is valid in pcdata.  When inside of an embedded expression
                        '' // return this sequence separately so that the xml literal completion code can
                        '' // easily detect the end of an embedded expression that may be temporarily hidden
                        '' // by a new element.  i.e. <%= <a> %>

                        'If CanGetCharAtOffset(Here + 1) AndAlso _
                        '   PeekAheadChar(Here + 1) = ">"c Then

                        '    ' // If valid characters found then return them.
                        '    If Here <> 0 Then
                        '        Return XmlMakeCharDataToken(Nothing, Here, New String(value.ToArray))
                        '    End If

                        '    ' // Create a special pcdata token for the possible tkEndXmlEmbedded
                        '    Return XmlMakeCharDataToken(Nothing, 2, "%>")
                        'Else
                        '    IsAllWhitespace = False
                        '    value.Add("%"c)
                        '    Here += 1
                        'End If
                        'Continue While
                        GoTo ScanChars
                    Case Else
ScanChars:
                        ' // Check characters are valid 
                        IsAllWhitespace = False
                        Dim xmlCh = ScanXmlChar(Here)

                        If xmlCh.Length = 0 Then
                            ' bad char
                            If Here > 0 Then
                                Return XmlMakeTextLiteralToken(Nothing, Here, scratch)
                            Else
                                Return XmlMakeBadToken(Nothing, 1, ERRID.ERR_IllegalChar)
                            End If
                        End If

                        xmlCh.AppendTo(scratch)
                        Here += xmlCh.Length
                End Select
            End While

            ' no more chars
            If Here > 0 Then
                Return XmlMakeTextLiteralToken(Nothing, Here, scratch)
            Else
                Return MakeEofToken()
            End If
        End Function

        Friend Function ScanXmlComment() As SyntaxToken
            ' // [15]    Comment    ::=    '<!--' ((Char - '-') | ('-' (Char - '-')))* '-->'

            Dim precedingTrivia As SyntaxList(Of VisualBasicSyntaxNode) = Nothing
            If IsScanningXmlDoc AndAlso IsAtNewLine() Then
                Dim xDocTrivia = ScanXmlDocTrivia()
                If xDocTrivia Is Nothing Then
                    Return MakeEofToken()  ' XmlDoc lines must start with XmlDocTrivia
                End If
                precedingTrivia = xDocTrivia
            End If

            Dim Here = 0
            While CanGetCharAtOffset(Here)
                Dim c As Char = PeekAheadChar(Here)
                Select Case (c)

                    Case CARRIAGE_RETURN, LINE_FEED
                        Return XmlMakeCommentToken(precedingTrivia, Here + LengthOfLineBreak(c, Here))

                    Case "-"c
                        If CanGetCharAtOffset(Here + 1) AndAlso _
                           PeekAheadChar(Here + 1) = "-"c Then

                            ' // --> terminates an Xml comment but otherwise -- is an illegal character sequence.
                            ' // The scanner will always returns "--" as a separate comment data string and the
                            ' // the semantics will error if '--' is ever found.

                            ' // Return valid characters up to the --
                            If Here > 0 Then
                                Return XmlMakeCommentToken(precedingTrivia, Here)
                            End If

                            If CanGetCharAtOffset(Here + 2) Then

                                c = PeekAheadChar(Here + 2)
                                Here += 2
                                ' // if > is not found then this is an error.  Return the -- string

                                If c <> ">"c Then
                                    Return XmlMakeCommentToken(precedingTrivia, 2)

                                    ' TODO: we cannot do the following

                                    ' // For better error recovery, allow -> to terminate the comment.
                                    ' // This works because the -> terminates only when the invalid --
                                    ' // is returned.

                                    'If Here + 1 < m_InputStreamEnd AndAlso _ 
                                    '   m_InputStream(Here) = "-"c AndAlso _
                                    '   m_InputStream(Here + 1) = ">"c Then 

                                    '    Here += 1
                                    'Else
                                    '    Continue While
                                    'End If
                                Else
                                    Return XmlMakeEndCommentToken(precedingTrivia)
                                End If
                            End If
                        End If
                        GoTo ScanChars

                    Case Else
ScanChars:
                        Dim xmlCh = ScanXmlChar(Here)
                        If xmlCh.Length <> 0 Then
                            Here += xmlCh.Length
                            Continue While
                        End If

                        ' bad char
                        If Here > 0 Then
                            Return XmlMakeCommentToken(precedingTrivia, Here)
                        Else
                            Return XmlMakeBadToken(precedingTrivia, 1, ERRID.ERR_IllegalChar)
                        End If
                End Select
            End While

            ' no more chars
            If Here > 0 Then
                Return XmlMakeCommentToken(precedingTrivia, Here)
            Else
                Return MakeEofToken(precedingTrivia)
            End If
        End Function

        Friend Function ScanXmlCData() As SyntaxToken
            ' // [18]    CDSect    ::=    CDStart CData CDEnd
            ' // [19]    CDStart    ::=    '<![CDATA['
            ' // [20]    CData    ::=    (Char* - (Char* ']]>' Char*))
            ' // [21]    CDEnd    ::=    ']]>'

            Dim precedingTrivia As SyntaxList(Of VisualBasicSyntaxNode) = Nothing
            If IsScanningXmlDoc AndAlso IsAtNewLine() Then
                Dim xDocTrivia = ScanXmlDocTrivia()
                If xDocTrivia Is Nothing Then
                    Return MakeEofToken()  ' XmlDoc lines must start with XmlDocTrivia
                End If
                precedingTrivia = xDocTrivia
            End If

            Dim scratch = GetScratch()
            Dim Here = 0

            While CanGetCharAtOffset(Here)
                Dim c As Char = PeekAheadChar(Here)
                Select Case (c)

                    Case CARRIAGE_RETURN, LINE_FEED
                        Here = SkipLineBreak(c, Here)
                        scratch.Append(LINE_FEED)
                        Return XmlMakeCDataToken(precedingTrivia, Here, scratch)

                    Case "]"c
                        If CanGetCharAtOffset(Here + 2) AndAlso _
                           PeekAheadChar(Here + 1) = "]"c AndAlso _
                           PeekAheadChar(Here + 2) = ">"c Then

                            '// If valid characters found then return them.
                            If Here <> 0 Then
                                Return XmlMakeCDataToken(precedingTrivia, Here, scratch)
                            End If

                            ' // Create token for ']]>' sequence
                            Return XmlMakeEndCDataToken(precedingTrivia)
                        End If
                        GoTo ScanChars

                    Case Else
ScanChars:
                        Dim xmlCh = ScanXmlChar(Here)

                        If xmlCh.Length = 0 Then
                            ' bad char
                            If Here > 0 Then
                                Return XmlMakeCDataToken(precedingTrivia, Here, scratch)
                            Else
                                Return XmlMakeBadToken(precedingTrivia, 1, ERRID.ERR_IllegalChar)
                            End If
                        End If

                        xmlCh.AppendTo(scratch)
                        Here += xmlCh.Length

                End Select
            End While

            ' no more chars
            If Here > 0 Then
                Return XmlMakeCDataToken(precedingTrivia, Here, scratch)
            Else
                Return MakeEofToken(precedingTrivia)
            End If
        End Function

        Friend Function ScanXmlPIData(state As ScannerState) As SyntaxToken
            ' SHIM
            If IsScanningXmlDoc Then
                Return ScanXmlPIDataInXmlDoc(state)
            End If

            ' // Scan the PI data after the white space
            ' // [16]    PI    ::=    '<?' PITarget (S (Char* - (Char* '?>' Char*)))? '?>'
            ' // [17]    PITarget    ::=    Name - (('X' | 'x') ('M' | 'm') ('L' | 'l'))
            Debug.Assert(state = ScannerState.StartProcessingInstruction OrElse
                         state = ScannerState.ProcessingInstruction)

            Dim precedingTrivia = triviaListPool.Allocate(Of VisualBasicSyntaxNode)()
            Dim result As SyntaxToken

            If state = ScannerState.StartProcessingInstruction AndAlso CanGetChar() Then
                ' // Whitespace
                ' //  S    ::=    (#x20 | #x9 | #xD | #xA)+
                Dim c = PeekChar()
                Select Case c
                    Case CARRIAGE_RETURN, LINE_FEED, " "c, CHARACTER_TABULATION
                        Dim wsTrivia = ScanXmlTrivia(c)
                        precedingTrivia.AddRange(wsTrivia)
                End Select
            End If

            Dim Here = 0
            While CanGetCharAtOffset(Here)
                Dim c As Char = PeekAheadChar(Here)
                Select Case (c)

                    Case CARRIAGE_RETURN, LINE_FEED
                        result = XmlMakeProcessingInstructionToken(precedingTrivia.ToList, Here + LengthOfLineBreak(c, Here))
                        GoTo CleanUp

                    Case "?"c
                        If CanGetCharAtOffset(Here + 1) AndAlso _
                           PeekAheadChar(Here + 1) = ">"c Then

                            '// If valid characters found then return them.
                            If Here <> 0 Then
                                result = XmlMakeProcessingInstructionToken(precedingTrivia.ToList, Here)
                                GoTo CleanUp
                            End If

                            ' // Create token for the '?>' termination sequence
                            result = XmlMakeEndProcessingInstructionToken(precedingTrivia.ToList)
                            GoTo CleanUp
                        End If
                        GoTo ScanChars

                    Case Else
ScanChars:
                        Dim xmlCh = ScanXmlChar(Here)
                        If xmlCh.Length > 0 Then
                            Here += xmlCh.Length
                            Continue While
                        End If

                        ' bad char
                        If Here <> 0 Then
                            result = XmlMakeProcessingInstructionToken(precedingTrivia.ToList, Here)
                            GoTo CleanUp
                        Else
                            result = XmlMakeBadToken(precedingTrivia.ToList, 1, ERRID.ERR_IllegalChar)
                            GoTo CleanUp
                        End If
                End Select
            End While

            ' no more chars
            If Here > 0 Then
                result = XmlMakeProcessingInstructionToken(precedingTrivia.ToList, Here)
            Else
                result = MakeEofToken(precedingTrivia.ToList)
            End If

CleanUp:
            triviaListPool.Free(precedingTrivia)
            Return result
        End Function

        Friend Function ScanXmlMisc() As SyntaxToken
            Debug.Assert(Not IsScanningXmlDoc)

            ' // Misc    ::=    Comment | PI | S

            Dim precedingTrivia As SyntaxList(Of VisualBasicSyntaxNode) = Nothing
            While CanGetChar()
                Dim c As Char = PeekChar()

                Select Case (c)
                    ' // Whitespace
                    ' //  S    ::=    (#x20 | #x9 | #xD | #xA)+
                    Case CARRIAGE_RETURN, LINE_FEED, " "c, CHARACTER_TABULATION
                        ' we should not visit this place twice
                        Debug.Assert(Not precedingTrivia.Any)
                        precedingTrivia = ScanXmlTrivia(c)

                    Case "<"c
                        If CanGetCharAtOffset(1) Then
                            Dim ch As Char = PeekAheadChar(1)
                            Select Case ch
                                Case "!"c
                                    If CanGetCharAtOffset(3) AndAlso
                                        PeekAheadChar(2) = "-"c AndAlso
                                        PeekAheadChar(3) = "-"c Then

                                        Return XmlMakeBeginCommentToken(precedingTrivia, _scanNoTriviaFunc)
                                    ElseIf CanGetCharAtOffset(8) AndAlso
                                            PeekAheadChar(2) = "D"c AndAlso
                                            PeekAheadChar(3) = "O"c AndAlso
                                            PeekAheadChar(4) = "C"c AndAlso
                                            PeekAheadChar(5) = "T"c AndAlso
                                            PeekAheadChar(6) = "Y"c AndAlso
                                            PeekAheadChar(7) = "P"c AndAlso
                                            PeekAheadChar(8) = "E"c Then
                                        Return XmlMakeBeginDTDToken(precedingTrivia)
                                    End If
                                Case "%"c
                                    If CanGetCharAtOffset(2) AndAlso
                                        PeekAheadChar(2) = "=" Then

                                        Return XmlMakeBeginEmbeddedToken(precedingTrivia)
                                    End If
                                Case "?"c
                                    Return XmlMakeBeginProcessingInstructionToken(precedingTrivia, _scanNoTriviaFunc)
                            End Select
                        End If

                        Return XmlMakeLessToken(precedingTrivia)

                        ' TODO: review 

                        '    If Not m_State.m_ScannedElement OrElse c = "?"c OrElse c = "!"c Then
                        '        ' // Remove tEOL from token ring if any exists

                        '        If tEOL IsNot Nothing Then
                        '            m_FirstFreeToken = tEOL
                        '        End If

                        '        m_State.m_LexicalState = LexicalState.XmlMarkup
                        '        MakeToken(tokens.tkLT, 1)
                        '        m_InputStreamPosition += 1
                        '        Return
                        '    End If
                        'End If

                        'm_State.EndXmlState()

                        'If tEOL IsNot Nothing Then
                        '    tEOL.m_EOL.m_NextLineAlreadyScanned = True
                        'Else
                        '    MakeToken(tokens.tkLT, 1)
                        '    m_InputStreamPosition += 1
                        'End If
                        'Return
                    Case Else
                        Return SyntaxFactory.Token(precedingTrivia.Node, SyntaxKind.EndOfXmlToken, Nothing, String.Empty)

                End Select
            End While
            Return MakeEofToken(precedingTrivia)
        End Function

        Friend Function ScanXmlStringUnQuoted() As SyntaxToken
            If Not CanGetChar() Then
                Return MakeEofToken()
            End If

            ' This can never happen as this token cannot cross lines.
            Debug.Assert(Not (IsScanningXmlDoc AndAlso IsAtNewLine()))

            Dim Here = 0
            Dim scratch = GetScratch()

            While CanGetCharAtOffset(Here)
                Dim c As Char = PeekAheadChar(Here)

                Select Case (c)

                    Case CARRIAGE_RETURN, LINE_FEED, " "c, CHARACTER_TABULATION
                        If Here > 0 Then
                            Return XmlMakeAttributeDataToken(Nothing, Here, scratch)
                        Else
                            Return MakeMissingToken(Nothing, SyntaxKind.SingleQuoteToken)
                        End If

                    Case "<"c, ">"c, "?"c
                        ' This cannot be in a string. terminate the string.
                        If Here <> 0 Then
                            Return XmlMakeAttributeDataToken(Nothing, Here, scratch)
                        Else
                            Return MakeMissingToken(Nothing, SyntaxKind.SingleQuoteToken)
                        End If

                    Case "&"c
                        If Here > 0 Then
                            Return XmlMakeAttributeDataToken(Nothing, Here, scratch)
                        Else
                            Return ScanXmlReference(Nothing)
                        End If

                    Case "/"c
                        If CanGetCharAtOffset(Here + 1) AndAlso PeekAheadChar(Here + 1) = ">"c Then
                            If Here <> 0 Then
                                Return XmlMakeAttributeDataToken(Nothing, Here, scratch)
                            Else
                                Return MakeMissingToken(Nothing, SyntaxKind.SingleQuoteToken)
                            End If
                        End If
                        GoTo ScanChars

                    Case Else
ScanChars:
                        ' // Check characters are valid
                        Dim xmlCh = ScanXmlChar(Here)

                        If xmlCh.Length = 0 Then
                            ' bad char
                            If Here > 0 Then
                                Return XmlMakeAttributeDataToken(Nothing, Here, scratch)
                            Else
                                Return XmlMakeBadToken(Nothing, 1, ERRID.ERR_IllegalChar)
                            End If
                        End If

                        xmlCh.AppendTo(scratch)
                        Here += xmlCh.Length
                End Select
            End While

            ' no more chars
            Return XmlMakeAttributeDataToken(Nothing, Here, scratch)
        End Function

        Friend Function ScanXmlStringSingle() As SyntaxToken
            Return ScanXmlString("'"c, "'"c, True)
        End Function

        Friend Function ScanXmlStringDouble() As SyntaxToken
            Return ScanXmlString(""""c, """"c, False)
        End Function

        Friend Function ScanXmlStringSmartSingle() As SyntaxToken
            Return ScanXmlString(RIGHT_SINGLE_QUOTATION_MARK, LEFT_SINGLE_QUOTATION_MARK, True)
        End Function

        Friend Function ScanXmlStringSmartDouble() As SyntaxToken
            Return ScanXmlString(RIGHT_DOUBLE_QUOTATION_MARK, LEFT_DOUBLE_QUOTATION_MARK, False)
        End Function

        Friend Function ScanXmlString(terminatingChar As Char, altTerminatingChar As Char, isSingle As Boolean) As SyntaxToken

            ' TODO: this trivia is used only in XmlDoc. May split the function?
            Dim precedingTrivia = triviaListPool.Allocate(Of VisualBasicSyntaxNode)()
            Dim result As SyntaxToken
            If IsScanningXmlDoc AndAlso IsAtNewLine() Then
                Dim xDocTrivia = ScanXmlDocTrivia()
                If xDocTrivia Is Nothing Then
                    result = MakeEofToken()  ' XmlDoc lines must start with XmlDocTrivia
                    GoTo CleanUp
                End If
                precedingTrivia.Add(xDocTrivia)
            End If

            Dim Here = 0
            Dim scratch = GetScratch()

            While CanGetCharAtOffset(Here)
                Dim c As Char = PeekAheadChar(Here)
                If c = terminatingChar Or c = altTerminatingChar Then
                    If Here > 0 Then
                        result = XmlMakeAttributeDataToken(precedingTrivia, Here, scratch)
                        GoTo CleanUp
                    Else
                        If isSingle Then
                            result = XmlMakeSingleQuoteToken(precedingTrivia, c, isOpening:=False)
                        Else
                            result = XmlMakeDoubleQuoteToken(precedingTrivia, c, isOpening:=False)
                        End If
                        GoTo CleanUp
                    End If
                End If

                Select Case (c)
                    Case CARRIAGE_RETURN, LINE_FEED
                        Here = SkipLineBreak(c, Here)
                        scratch.Append(SPACE)
                        result = XmlMakeAttributeDataToken(precedingTrivia, Here, scratch)
                        GoTo CleanUp

                    Case CHARACTER_TABULATION
                        scratch.Append(SPACE)
                        Here += 1

                    Case "<"c
                        ' This cannot be in a string. terminate the string.
                        If Here > 0 Then
                            result = XmlMakeAttributeDataToken(precedingTrivia, Here, scratch)
                            GoTo CleanUp
                        Else
                            ' report unexpected <%= in a special way.
                            If CanGetCharAtOffset(2) AndAlso
                                PeekAheadChar(1) = "%"c AndAlso
                                PeekAheadChar(2) = "=" Then

                                Dim errEmbedStart = XmlMakeAttributeDataToken(precedingTrivia, 3, "<%=")
                                Dim errEmberinfo = ErrorFactory.ErrorInfo(ERRID.ERR_QuotedEmbeddedExpression)
                                result = DirectCast(errEmbedStart.SetDiagnostics({errEmberinfo}), SyntaxToken)
                                GoTo CleanUp
                            End If

                            Dim data = SyntaxFactory.MissingToken(SyntaxKind.SingleQuoteToken)
                            If precedingTrivia.Count > 0 Then
                                data = DirectCast(data.WithLeadingTrivia(precedingTrivia.ToList.Node), SyntaxToken)
                            End If
                            Dim errInfo = ErrorFactory.ErrorInfo(If(isSingle, ERRID.ERR_ExpectedSQuote, ERRID.ERR_ExpectedQuote))
                            result = DirectCast(data.SetDiagnostics({errInfo}), SyntaxToken)
                            GoTo CleanUp
                        End If

                    Case "&"c
                        If Here > 0 Then
                            result = XmlMakeAttributeDataToken(precedingTrivia, Here, scratch)
                            GoTo CleanUp
                        Else
                            result = ScanXmlReference(precedingTrivia)
                            GoTo CleanUp
                        End If

                    Case Else
ScanChars:
                        ' // Check characters are valid
                        Dim xmlCh = ScanXmlChar(Here)

                        If xmlCh.Length = 0 Then
                            ' bad char
                            If Here > 0 Then
                                result = XmlMakeAttributeDataToken(precedingTrivia, Here, scratch)
                                GoTo CleanUp
                            Else
                                result = XmlMakeBadToken(precedingTrivia, 1, ERRID.ERR_IllegalChar)
                                GoTo CleanUp
                            End If
                        End If

                        xmlCh.AppendTo(scratch)
                        Here += xmlCh.Length
                End Select
            End While

            ' no more chars
            If Here > 0 Then
                result = XmlMakeAttributeDataToken(precedingTrivia, Here, scratch)
                GoTo CleanUp
            Else
                result = MakeEofToken(precedingTrivia)
                GoTo CleanUp
            End If

CleanUp:
            triviaListPool.Free(precedingTrivia)
            Return result
        End Function

        ''' <summary>
        ''' 0 - not a surrogate, 2 - is valid surrogate 
        ''' 1 is an error
        ''' </summary>
        Private Function ScanSurrogatePair(c1 As Char, Here As Integer) As XmlCharResult
            Debug.Assert(Here >= 0)
            Debug.Assert(CanGetCharAtOffset(Here))
            Debug.Assert(PeekAheadChar(Here) = c1)

            If IsHighSurrogate(c1) AndAlso CanGetCharAtOffset(Here + 1) Then
                Dim c2 = PeekAheadChar(Here + 1)

                If IsLowSurrogate(c2) Then
                    Return New XmlCharResult(c1, c2)
                End If
            End If

            Return Nothing
        End Function

        ' contains result of Xml char scanning. 
        Friend Structure XmlCharResult
            Friend ReadOnly Length As Integer
            Friend ReadOnly Char1 As Char
            Friend ReadOnly Char2 As Char

            Friend Sub New(ch As Char)
                Length = 1
                Char1 = ch
            End Sub

            Friend Sub New(ch1 As Char, ch2 As Char)
                Length = 2
                Char1 = ch1
                Char2 = ch2
            End Sub

            Friend Sub AppendTo(list As StringBuilder)
                Debug.Assert(list IsNot Nothing)
                Debug.Assert(Length <> 0)

                list.Append(Char1)
                If Length = 2 Then
                    list.Append(Char2)
                End If
            End Sub
        End Structure

        Private Function ScanXmlChar(Here As Integer) As XmlCharResult
            Debug.Assert(Here >= 0)
            Debug.Assert(CanGetCharAtOffset(Here))

            Dim c = PeekAheadChar(Here)

            If Not isValidUtf16(c) Then
                Return Nothing
            End If

            If Not IsSurrogate(c) Then
                Return New XmlCharResult(c)
            End If

            Return ScanSurrogatePair(c, Here)
        End Function

        Private Function ScanXmlNcName(precedingTrivia As SyntaxList(Of VisualBasicSyntaxNode)) As SyntaxToken
            ' // Scan a non qualified name per Xml Namespace 1.0
            ' // [4]  NCName ::=  (Letter | '_') (NCNameChar)* /*  An XML Name, minus the ":" */

            ' // This scanner is much looser than a pure Xml scanner.
            ' // Names are any character up to a separator character from
            ' //      ':' | ' ' | '\t' | '\n' | '\r' | '=' | '\'' | '"'| '/' | '<' | '>' | EOF
            ' // Each name token will be marked as to whether it contains only valid Xml name characters.

            Dim Here As Integer = 0
            Dim IsIllegalChar As Boolean = False
            Dim isFirst As Boolean = True
            Dim err As ERRID = ERRID.ERR_None
            Dim errUnicode As Integer = 0
            Dim errChar As String = Nothing

            'TODO - Fix ScanXmlNCName to conform to XML spec instead of old loose scanning.

            While CanGetCharAtOffset(Here)
                Dim c As Char = PeekAheadChar(Here)

                Select Case (c)

                    Case ":"c, _
                        " "c, _
                        CHARACTER_TABULATION, _
                        LINE_FEED, _
                        CARRIAGE_RETURN, _
                        "="c, _
                        "'"c, _
                        """"c, _
                        "/"c, _
                        ">"c, _
                        "<"c, _
                        "("c, _
                        ")"c, _
                        "?"c, _
                        ";"c, _
                        ","c, _
                        "}"c

                        GoTo CreateNCNameToken

                    Case Else
                        ' // Invalid Xml name but scan as Xml name anyway

                        ' // Check characters are valid name chars
                        Dim xmlCh = ScanXmlChar(Here)
                        If xmlCh.Length = 0 Then
                            IsIllegalChar = True
                            GoTo CreateNCNameToken
                        Else
                            If err = ERRID.ERR_None Then
                                If xmlCh.Length = 1 Then
                                    ' Non surrogate check
                                    If isFirst Then
                                        err = If(Not isStartNameChar(xmlCh.Char1), ERRID.ERR_IllegalXmlStartNameChar, ERRID.ERR_None)
                                        isFirst = False
                                    Else
                                        err = If(Not isNameChar(xmlCh.Char1), ERRID.ERR_IllegalXmlNameChar, ERRID.ERR_None)
                                    End If
                                    If err <> ERRID.ERR_None Then
                                        errChar = Convert.ToString(xmlCh.Char1)
                                        errUnicode = Convert.ToInt32(xmlCh.Char1)
                                    End If
                                Else
                                    ' Surrogate check
                                    Dim unicode = UTF16ToUnicode(xmlCh)
                                    If Not (unicode >= &H10000 AndAlso unicode <= &HEFFFF) Then
                                        err = ERRID.ERR_IllegalXmlNameChar
                                        errChar = {xmlCh.Char1, xmlCh.Char2}
                                        errUnicode = unicode
                                    End If
                                End If
                            End If
                            Here += xmlCh.Length
                        End If
                End Select
            End While

CreateNCNameToken:
            If Here <> 0 Then
                Dim name = XmlMakeXmlNCNameToken(precedingTrivia, Here)
                If err <> ERRID.ERR_None Then
                    name = name.WithDiagnostics(ErrorFactory.ErrorInfo(err, errChar, String.Format("&H{0:X}", errUnicode)))
                End If
                Return name
            ElseIf IsIllegalChar Then
                Return XmlMakeBadToken(precedingTrivia, 1, ERRID.ERR_IllegalChar)
            End If

            Return MakeMissingToken(precedingTrivia, SyntaxKind.XmlNameToken)
        End Function

        Private Function ScanXmlReference(precedingTrivia As SyntaxList(Of VisualBasicSyntaxNode)) As XmlTextTokenSyntax
            Debug.Assert(CanGetChar)
            Debug.Assert(PeekChar() = "&"c)

            ' skip 1 char for "&"
            If CanGetCharAtOffset(1) Then
                Dim c As Char = PeekAheadChar(1)

                Select Case (c)
                    Case "#"c
                        Dim Here = 2    ' skip "&#"
                        Dim result = ScanXmlCharRef(Here)

                        If result.Length <> 0 Then
                            Dim value As String = Nothing
                            If result.Length = 1 Then
                                value = Intern(result.Char1)
                            ElseIf result.Length = 2 Then
                                value = Intern({result.Char1, result.Char2})
                            End If

                            If CanGetCharAtOffset(Here) AndAlso PeekAheadChar(Here) = ";"c Then
                                Return XmlMakeEntityLiteralToken(precedingTrivia, Here + 1, value)
                            Else
                                Dim noSemicolon = XmlMakeEntityLiteralToken(precedingTrivia, Here, value)
                                Dim noSemicolonError = ErrorFactory.ErrorInfo(ERRID.ERR_ExpectedSColon)
                                Return DirectCast(noSemicolon.SetDiagnostics({noSemicolonError}), XmlTextTokenSyntax)
                            End If
                        End If

                    Case "a"c
                        ' // &amp;
                        ' // &apos;

                        If CanGetCharAtOffset(4) AndAlso
                           PeekAheadChar(2) = "m"c AndAlso
                           PeekAheadChar(3) = "p"c Then

                            If PeekAheadChar(4) = ";"c Then
                                Return XmlMakeAmpLiteralToken(precedingTrivia)
                            Else
                                Dim noSemicolon = XmlMakeEntityLiteralToken(precedingTrivia, 4, "&")
                                Dim noSemicolonError = ErrorFactory.ErrorInfo(ERRID.ERR_ExpectedSColon)
                                Return DirectCast(noSemicolon.SetDiagnostics({noSemicolonError}), XmlTextTokenSyntax)
                            End If

                        ElseIf CanGetCharAtOffset(5) AndAlso
                               PeekAheadChar(2) = "p"c AndAlso
                               PeekAheadChar(3) = "o"c AndAlso
                               PeekAheadChar(4) = "s"c Then

                            If PeekAheadChar(5) = ";"c Then
                                Return XmlMakeAposLiteralToken(precedingTrivia)
                            Else
                                Dim noSemicolon = XmlMakeEntityLiteralToken(precedingTrivia, 5, "'")
                                Dim noSemicolonError = ErrorFactory.ErrorInfo(ERRID.ERR_ExpectedSColon)
                                Return DirectCast(noSemicolon.SetDiagnostics({noSemicolonError}), XmlTextTokenSyntax)
                            End If
                        End If

                    Case "l"c
                        ' // &lt;

                        If CanGetCharAtOffset(3) AndAlso
                           PeekAheadChar(2) = "t"c Then

                            If PeekAheadChar(3) = ";"c Then
                                Return XmlMakeLtLiteralToken(precedingTrivia)
                            Else
                                Dim noSemicolon = XmlMakeEntityLiteralToken(precedingTrivia, 3, "<")
                                Dim noSemicolonError = ErrorFactory.ErrorInfo(ERRID.ERR_ExpectedSColon)
                                Return DirectCast(noSemicolon.SetDiagnostics({noSemicolonError}), XmlTextTokenSyntax)
                            End If
                        End If

                    Case "g"c
                        ' // &gt;

                        If CanGetCharAtOffset(3) AndAlso
                           PeekAheadChar(2) = "t"c Then

                            If PeekAheadChar(3) = ";"c Then
                                Return XmlMakeGtLiteralToken(precedingTrivia)
                            Else
                                Dim noSemicolon = XmlMakeEntityLiteralToken(precedingTrivia, 3, ">")
                                Dim noSemicolonError = ErrorFactory.ErrorInfo(ERRID.ERR_ExpectedSColon)
                                Return DirectCast(noSemicolon.SetDiagnostics({noSemicolonError}), XmlTextTokenSyntax)
                            End If
                        End If

                    Case "q"c
                        ' // &quot;

                        If CanGetCharAtOffset(5) AndAlso
                           PeekAheadChar(2) = "u"c AndAlso
                           PeekAheadChar(3) = "o"c AndAlso
                           PeekAheadChar(4) = "t"c Then

                            If PeekAheadChar(5) = ";"c Then
                                Return XmlMakeQuotLiteralToken(precedingTrivia)
                            Else
                                Dim noSemicolon = XmlMakeEntityLiteralToken(precedingTrivia, 5, """")
                                Dim noSemicolonError = ErrorFactory.ErrorInfo(ERRID.ERR_ExpectedSColon)
                                Return DirectCast(noSemicolon.SetDiagnostics({noSemicolonError}), XmlTextTokenSyntax)
                            End If
                        End If

                End Select
            End If

            Dim badEntity = XmlMakeEntityLiteralToken(precedingTrivia, 1, "")
            Dim errInfo = ErrorFactory.ErrorInfo(ERRID.ERR_XmlEntityReference)
            Return DirectCast(badEntity.SetDiagnostics({errInfo}), XmlTextTokenSyntax)

        End Function

        Private Function ScanXmlCharRef(ByRef index As Integer) As XmlCharResult
            Debug.Assert(index >= 0)

            If Not CanGetCharAtOffset(index) Then
                Return Nothing
            End If

            ' cannot reuse Scratch as this can be used in a nested call.
            Dim charRefSb As New StringBuilder
            Dim Here = index

            Dim ch = PeekAheadChar(Here)
            If ch = "x"c Then
                Here += 1

                While CanGetCharAtOffset(Here)
                    ch = PeekAheadChar(Here)
                    If XmlCharType.IsHexDigit(ch) Then
                        charRefSb.Append(ch)
                    Else
                        Exit While
                    End If
                    Here += 1
                End While
                If charRefSb.Length > 0 Then
                    Dim result = HexToUTF16(charRefSb)
                    If result.Length <> 0 Then
                        index = Here
                    End If
                    Return result
                End If
            Else
                While CanGetCharAtOffset(Here)
                    ch = PeekAheadChar(Here)
                    If XmlCharType.IsDigit(ch) Then
                        charRefSb.Append(ch)
                    Else
                        Exit While
                    End If
                    Here += 1
                End While
                If charRefSb.Length > 0 Then
                    Dim result = DecToUTF16(charRefSb)
                    If result.Length <> 0 Then
                        index = Here
                    End If
                    Return result
                End If
            End If
            Return Nothing
        End Function
    End Class
End Namespace
