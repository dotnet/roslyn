' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

'-----------------------------------------------------------------------------
' Contains scanner functionality related to XmlDoc comments
'-----------------------------------------------------------------------------
Option Compare Binary
Option Strict On

Imports Microsoft.CodeAnalysis.Syntax.InternalSyntax
Imports Microsoft.CodeAnalysis.VisualBasic.SyntaxFacts
Imports CoreInternalSyntax = Microsoft.CodeAnalysis.Syntax.InternalSyntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax
    Partial Friend Class Scanner
        Private _IsScanningXmlDoc As Boolean = False
        Friend Property IsScanningXmlDoc As Boolean
            Get
                Return _IsScanningXmlDoc
            End Get
            Private Set(value As Boolean)
                _IsScanningXmlDoc = value
            End Set
        End Property

        ''' <remarks>See description in TryScanXmlDocComment(...)</remarks>
        Private _endOfXmlInsteadOfLastDocCommentLineBreak As Boolean

        ' bug 903247. First line should be treated in a special way.
        Private _isStartingFirstXmlDocLine As Boolean = False

        Private _doNotRequireXmlDocCommentPrefix As Boolean = False

        ''' <summary>
        ''' This method is only to be used for parsing Cref and Name attributes as stand-alone entities
        ''' </summary>
        Friend Sub ForceScanningXmlDocMode()
            Me.IsScanningXmlDoc = True
            Me._isStartingFirstXmlDocLine = False
            Me._doNotRequireXmlDocCommentPrefix = True
        End Sub

        Private Function TryScanXmlDocComment(tList As SyntaxListBuilder) As Boolean
            Debug.Assert(IsAtNewLine)

            ' leading whitespace until we see ''' should be regular whitespace
            If CanGet() AndAlso IsWhitespace(Peek()) Then
                Dim ws = ScanWhitespace()
                tList.Add(ws)
            End If

            ' SAVE the lookahead state and clear current token
#If DEBUG Then
            Dim origPosition = _lineBufferOffset
#End If
            Dim restorePoint = CreateRestorePoint()

            ' since we do not have lookahead tokens, this just 
            ' resets current token to _lineBufferOffset 
            Me.GetNextTokenInState(ScannerState.Content)

            Dim currentNonterminal = Me.GetCurrentSyntaxNode()
            Dim docCommentSyntax = TryCast(currentNonterminal, DocumentationCommentTriviaSyntax)

            ' if we are lucky to get whole doc comment, we can just reuse it.
            If docCommentSyntax IsNot Nothing Then
                Me.MoveToNextSyntaxNodeInTrivia()

            Else
                Dim parser As New Parser(Me)

                Me.IsScanningXmlDoc = True
                Me._isStartingFirstXmlDocLine = True

                ' NOTE: Documentation comment syntax trivia must have at least one child xml node, because 
                '       all the ['''] trivia are created as leading trivia for appropriate tokens.
                '       This means that we have to create at least one XmlText having trailing
                '       EOL to represent an empty documentation comment: ['''<eol>]
                '
                '       The problem with this approach is that in presence of some errors (like
                '       not closed XML tags) we create missing tokens needed to represent the nodes
                '       *after* that last <eol> of the doc comment trivia, that means all the locations 
                '       of created diagnostics will land on the first character of the next line
                '       after documentation comment
                '
                '       To workaround this we parse XML nodes in two phases: 
                '         - in the first phase we detect the last DocCommentLineBreak and create 
                '         end-of-xml token instead; this should force all diagnostics to be 
                '         reported on the next token location;
                '         - in the second phase we continue parsing XML nodes but don't create 
                '         end-of-xml token which should just result in parsing one single node 
                '         of XmlText type containing EOL;
                '       Then we merge the results and create resulting DocumentationCommentTrivia

                ' The first phase
                Me._endOfXmlInsteadOfLastDocCommentLineBreak = True
                Dim nodes = parser.ParseXmlContent(ScannerState.Content)

                ' The second phase
                Me._endOfXmlInsteadOfLastDocCommentLineBreak = False
                If nodes.Count = 0 AndAlso parser.CurrentToken.Kind = SyntaxKind.EndOfXmlToken Then
                    ' This must be an empty documentation comment, we need to reset scanner so 
                    ' that the doc comment exterior trivia ([''']) lands on the final XmlNode

                    ResetLineBufferOffset()
                    restorePoint.RestoreTokens(includeLookAhead:=False)
                    Me._isStartingFirstXmlDocLine = True
                End If

                nodes = parser.ParseRestOfDocCommentContent(nodes)
                Me.IsScanningXmlDoc = False

                Debug.Assert(nodes.Any)
                Debug.Assert(nodes(0).FullWidth > 0, "should at least get {'''EoL} ")

                ' restore _currentToken and lookahead,
                ' but keep offset and PP state
                ResetLineBufferOffset()

                docCommentSyntax = SyntaxFactory.DocumentationCommentTrivia(nodes)

                If Me.Options.DocumentationMode < DocumentationMode.Diagnose Then
                    ' All diagnostics coming from documentation comment are ignored
                    docCommentSyntax.ClearFlags(GreenNode.NodeFlags.ContainsDiagnostics)
                End If
            End If

            ' RESTORE lookahead state and current token if there were any
            restorePoint.RestoreTokens(includeLookAhead:=True)

#If DEBUG Then
            Debug.Assert(Me._lineBufferOffset = origPosition + docCommentSyntax.FullWidth OrElse
                         Me._endOfTerminatorTrivia = origPosition + docCommentSyntax.FullWidth)
#End If

            tList.Add(docCommentSyntax)

            Return True
        End Function

        ' lexes (ws)'''
        Private Function TrySkipXmlDocMarker(ByRef len As Integer) As Boolean
            Dim Here = len
            While CanGet(Here)
                Dim c = Peek(Here)
                If IsWhitespace(c) Then
                    Here += 1
                Else
                    Exit While
                End If
            End While

            If StartsXmlDoc(Here) Then
                len = Here + 3
                Return True
            Else
                Return False
            End If
        End Function

        ' scans  (ws)'''
        Private Function ScanXmlDocTrivia() As VisualBasicSyntaxNode
            Debug.Assert(IsAtNewLine() OrElse _isStartingFirstXmlDocLine)

            Dim len = 0
            If TrySkipXmlDocMarker(len) Then
                Return MakeDocumentationCommentExteriorTrivia(GetText(len))
            Else
                Return Nothing
            End If
        End Function

        ''' <summary>
        ''' Returns False if trivia ends line.
        ''' </summary>
        Private Function ScanXmlTriviaInXmlDoc(c As Char, triviaList As SyntaxListBuilder(Of VisualBasicSyntaxNode)) As Boolean
            Debug.Assert(IsScanningXmlDoc)
            Debug.Assert(c = CARRIAGE_RETURN OrElse c = LINE_FEED OrElse c = " "c OrElse c = CHARACTER_TABULATION)

            Dim len = 0
            Do
                If c = " "c OrElse c = CHARACTER_TABULATION Then
                    len += 1

                ElseIf IsNewLine(c) Then
                    If len > 0 Then
                        triviaList.Add(MakeWhiteSpaceTrivia(GetText(len)))
                        len = 0
                    End If

                    ' Only consume the end of line if the XML doc
                    ' comment continues on the following line.
                    Dim offsets = CreateOffsetRestorePoint()
                    Dim endOfLineTrivia = ScanNewlineAsTrivia(c)
                    Dim ws = GetXmlWhitespaceLength(0)
                    If TrySkipXmlDocMarker(ws) Then
                        triviaList.Add(endOfLineTrivia)
                        triviaList.Add(MakeDocumentationCommentExteriorTrivia(GetText(ws)))
                    Else
                        offsets.Restore()
                        Return False
                    End If

                Else
                    Exit Do
                End If

                c = Peek(len)
            Loop

            If len > 0 Then
                triviaList.Add(MakeWhiteSpaceTrivia(GetText(len)))
            End If

            Return True

        End Function

        Private Function ScanXmlContentInXmlDoc() As SyntaxToken
            Debug.Assert(IsScanningXmlDoc)

            ' // [14]    CharData    ::=    [^<&]* - ([^<&]* ']]>' [^<&]*)

            Dim precedingTrivia As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode) = Nothing
            If IsAtNewLine() OrElse _isStartingFirstXmlDocLine Then
                Dim xDocTrivia = ScanXmlDocTrivia()
                _isStartingFirstXmlDocLine = False           ' no longer starting
                If xDocTrivia Is Nothing Then
                    Return MakeEofToken()  ' XmlDoc lines must start with XmlDocTrivia
                End If
                precedingTrivia = New CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode)(xDocTrivia)
            End If

            Dim Here As Integer = 0
            Dim scratch = GetScratch()

            While CanGet(Here)
                Dim c As Char = Peek(Here)

                Select Case (c)
                    Case CARRIAGE_RETURN, LINE_FEED
                        If Here <> 0 Then
                            Return XmlMakeTextLiteralToken(precedingTrivia, Here, scratch)
                        End If

                        Here = SkipLineBreak(c, Here)

                        If _endOfXmlInsteadOfLastDocCommentLineBreak Then
                            Dim tempHere As Integer = Here
                            If Not TrySkipXmlDocMarker(tempHere) Then
                                ' NOTE: we need to reset the buffer so that precedingTrivia 
                                '       lands on the next token
                                ResetLineBufferOffset()
                                Return SyntaxFactory.Token(Nothing, SyntaxKind.EndOfXmlToken, Nothing, String.Empty)
                            End If
                        End If

                        ' line breaks in Doc comments are separate tokens.
                        Return MakeDocCommentLineBreakToken(precedingTrivia, Here)

                    Case " "c, CHARACTER_TABULATION
                        scratch.Append(c)
                        Here += 1

                    Case "&"c
                        If Here <> 0 Then
                            Return XmlMakeTextLiteralToken(precedingTrivia, Here, scratch)
                        End If

                        Return ScanXmlReference(precedingTrivia)

                    Case "<"c
                        If Here <> 0 Then
                            Return XmlMakeTextLiteralToken(precedingTrivia, Here, scratch)
                        End If

                        Debug.Assert(Here = 0)
                        If CanGet(1) Then
                            Dim ch As Char = Peek(1)
                            Select Case ch
                                Case "!"c
                                    If CanGet(2) Then
                                        Select Case (Peek(2))
                                            Case "-"c
                                                If NextIs(3, "-"c) Then
                                                    Return XmlMakeBeginCommentToken(precedingTrivia, s_scanNoTriviaFunc)
                                                End If
                                            Case "["c
                                                If NextAre(3, "CDATA[") Then
                                                    Return XmlMakeBeginCDataToken(precedingTrivia, s_scanNoTriviaFunc)
                                                End If
                                            Case "D"c
                                                If NextAre(3, "OCTYPE") Then
                                                    Return XmlMakeBeginDTDToken(precedingTrivia)
                                                End If
                                        End Select
                                    End If
                                Case "?"c
                                    Return XmlMakeBeginProcessingInstructionToken(precedingTrivia, s_scanNoTriviaFunc)
                                Case "/"c
                                    Return XmlMakeBeginEndElementToken(precedingTrivia, s_scanNoTriviaFunc)
                            End Select
                        End If

                        Return XmlMakeLessToken(precedingTrivia)

                    Case "]"c
                        If NextAre(Here + 1, "]>") Then

                            ' // If valid characters found then return them.
                            If Here <> 0 Then
                                Return XmlMakeTextLiteralToken(precedingTrivia, Here, scratch)
                            End If

                            ' // Create an invalid character data token for the illegal ']]>' sequence
                            Return XmlMakeTextLiteralToken(precedingTrivia, 3, ERRID.ERR_XmlEndCDataNotAllowedInContent)
                        End If
                        GoTo ScanChars
                    Case Else
ScanChars:
                        ' // Check characters are valid 
                        Dim xmlCh = ScanXmlChar(Here)

                        If xmlCh.Length = 0 Then
                            ' bad char
                            If Here > 0 Then
                                Return XmlMakeTextLiteralToken(precedingTrivia, Here, scratch)
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
                Return XmlMakeTextLiteralToken(precedingTrivia, Here, scratch)
            Else
                Return MakeEofToken(precedingTrivia)
            End If
        End Function

        Friend Function ScanXmlPIDataInXmlDoc(state As ScannerState) As SyntaxToken
            Debug.Assert(IsScanningXmlDoc)

            ' // Scan the PI data after the white space
            ' // [16]    PI    ::=    '<?' PITarget (S (Char* - (Char* '?>' Char*)))? '?>'
            ' // [17]    PITarget    ::=    Name - (('X' | 'x') ('M' | 'm') ('L' | 'l'))
            Debug.Assert(state = ScannerState.StartProcessingInstruction OrElse
                         state = ScannerState.ProcessingInstruction)

            Dim precedingTrivia = _triviaListPool.Allocate(Of VisualBasicSyntaxNode)()
            Dim result As SyntaxToken

            If IsAtNewLine() Then
                Dim xDocTrivia = ScanXmlDocTrivia()
                If xDocTrivia Is Nothing Then
                    Return MakeEofToken()  ' XmlDoc lines must start with XmlDocTrivia
                End If
                precedingTrivia.Add(xDocTrivia)
            End If

            If state = ScannerState.StartProcessingInstruction AndAlso CanGet() Then
                ' // Whitespace
                ' //  S    ::=    (#x20 | #x9 | #xD | #xA)+
                Dim c = Peek()
                Select Case c
                    Case CARRIAGE_RETURN, LINE_FEED, " "c, CHARACTER_TABULATION
                        Dim offsets = CreateOffsetRestorePoint()
                        Dim continueLine = ScanXmlTriviaInXmlDoc(c, precedingTrivia)
                        If Not continueLine Then
                            offsets.Restore()
                            result = SyntaxFactory.Token(precedingTrivia.ToList.Node, SyntaxKind.EndOfXmlToken, Nothing, String.Empty)
                            GoTo CleanUp
                        End If
                End Select
            End If

            Dim Here = 0
            While CanGet(Here)
                Dim c As Char = Peek(Here)
                Select Case (c)

                    Case CARRIAGE_RETURN, LINE_FEED
                        result = XmlMakeProcessingInstructionToken(precedingTrivia.ToList, Here + LengthOfLineBreak(c, Here))
                        GoTo CleanUp

                    Case "?"c
                        If NextIs(Here + 1, ">"c) Then

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
            _triviaListPool.Free(precedingTrivia)
            Return result
        End Function

        Private Function ScanXmlElementInXmlDoc(state As ScannerState) As SyntaxToken
            Debug.Assert(IsScanningXmlDoc)

            ' // Only legal tokens
            ' //  QName
            ' //  /
            ' //  >
            ' //  =
            ' //  Whitespace

            Dim precedingTrivia As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode) = Nothing

            If IsAtNewLine() AndAlso Not Me._doNotRequireXmlDocCommentPrefix Then
                Dim xDocTrivia = ScanXmlDocTrivia()
                If xDocTrivia Is Nothing Then
                    Return MakeEofToken()  ' XmlDoc lines must start with XmlDocTrivia
                End If
                precedingTrivia = New CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode)(xDocTrivia)
            End If

            While CanGet()
                If Not precedingTrivia.Any AndAlso IsAtNewLine() AndAlso Not Me._doNotRequireXmlDocCommentPrefix Then
                    ' this would indicate that we looked at Trivia, but did not find
                    ' XmlDoc prefix (or we would not be at the line start)
                    ' must terminate XmlDoc scanning
                    Return MakeEofToken(precedingTrivia)
                End If

                Dim c As Char = Peek()

                Select Case (c)
                    ' // Whitespace
                    ' //  S    ::=    (#x20 | #x9 | #xD | #xA)+
                    Case CARRIAGE_RETURN, LINE_FEED, " "c, CHARACTER_TABULATION
                        ' we should not visit this place twice
                        Debug.Assert(Not precedingTrivia.Any)
                        Dim offsets = CreateOffsetRestorePoint()
                        Dim triviaList = _triviaListPool.Allocate(Of VisualBasicSyntaxNode)()
                        Dim continueLine = ScanXmlTriviaInXmlDoc(c, triviaList)
                        precedingTrivia = triviaList.ToList()
                        _triviaListPool.Free(triviaList)
                        If Not continueLine Then
                            offsets.Restore()
                            Return SyntaxFactory.Token(precedingTrivia.Node, SyntaxKind.EndOfXmlToken, Nothing, String.Empty)
                        End If

                    Case "/"c
                        If NextIs(1, ">"c) Then
                            Return XmlMakeEndEmptyElementToken(precedingTrivia)
                        End If
                        Return XmlMakeDivToken(precedingTrivia)

                    Case ">"c
                        Return XmlMakeGreaterToken(precedingTrivia)

                    Case "="c
                        Return XmlMakeEqualsToken(precedingTrivia)

                    Case "'"c, LEFT_SINGLE_QUOTATION_MARK, RIGHT_SINGLE_QUOTATION_MARK
                        Return XmlMakeSingleQuoteToken(precedingTrivia, c, isOpening:=True)

                    Case """"c, LEFT_DOUBLE_QUOTATION_MARK, RIGHT_DOUBLE_QUOTATION_MARK
                        Return XmlMakeDoubleQuoteToken(precedingTrivia, c, isOpening:=True)

                    Case "<"c
                        If CanGet(1) Then
                            Dim ch As Char = Peek(1)
                            Select Case ch
                                Case "!"c
                                    If CanGet(2) Then
                                        Select Case (Peek(2))
                                            Case "-"c
                                                If NextIs(3, "-"c) Then
                                                    Return XmlMakeBeginCommentToken(precedingTrivia, s_scanNoTriviaFunc)
                                                End If
                                            Case "["c
                                                If NextAre(3, "CDATA[") Then
                                                    Return XmlMakeBeginCDataToken(precedingTrivia, s_scanNoTriviaFunc)
                                                End If
                                            Case "D"c
                                                If NextAre(3, "OCTYPE") Then
                                                    Return XmlMakeBeginDTDToken(precedingTrivia)
                                                End If
                                        End Select
                                    End If
                                    Return XmlLessThanExclamationToken(state, precedingTrivia)
                                Case "?"c
                                    Return XmlMakeBeginProcessingInstructionToken(precedingTrivia, s_scanNoTriviaFunc)
                                Case "/"c
                                    Return XmlMakeBeginEndElementToken(precedingTrivia, s_scanNoTriviaFunc)
                            End Select
                        End If

                        Return XmlMakeLessToken(precedingTrivia)

                    Case "?"c
                        If NextIs(1, ">"c) Then
                            ' // Create token for the '?>' termination sequence
                            Return XmlMakeEndProcessingInstructionToken(precedingTrivia)
                        End If

                        Return MakeQuestionToken(precedingTrivia, False)

                    Case "("c
                        Return XmlMakeLeftParenToken(precedingTrivia)

                    Case ")"c
                        Return XmlMakeRightParenToken(precedingTrivia)

                    Case "!"c,
                        ";"c,
                        "#"c,
                        ","c,
                        "}"c
                        Return XmlMakeBadToken(precedingTrivia, 1, ERRID.ERR_IllegalXmlNameChar)

                    Case ":"c
                        Return XmlMakeColonToken(precedingTrivia)

                    Case "["c
                        Return XmlMakeOpenBracketToken(state, precedingTrivia)

                    Case "]"c
                        Return XmlMakeCloseBracketToken(state, precedingTrivia)

                    Case Else
                        ' // Because of weak scanning of QName, this state must always handle
                        ' //    '=' | '\'' | '"'| '/' | '>' | '<' | '?'

                        Return ScanXmlNcName(precedingTrivia)
                End Select
            End While
            Return MakeEofToken(precedingTrivia)
        End Function

        'TODO: It makes sense to split Xml scanning functions that have XmlDoc functionality
        ' and place here (see ScanXmlContentInXmlDoc).
        ' may actually make it a derived XmlDocScanner class (consider caches).

    End Class
End Namespace
