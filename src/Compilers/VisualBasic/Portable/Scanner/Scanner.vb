' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

'-----------------------------------------------------------------------------
' Contains the definition of the Scanner, which produces tokens from text
'-----------------------------------------------------------------------------

Option Compare Binary
Option Strict On

Imports System.Collections.Immutable
Imports System.Globalization
Imports System.Runtime.InteropServices
Imports System.Text
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.SyntaxFacts
Imports Microsoft.CodeAnalysis.Collections

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax

    ''' <summary>
    ''' Creates red tokens for a stream of text
    ''' </summary>
    Friend Class Scanner
        Implements IDisposable

        Private Delegate Function ScanTriviaFunc() As SyntaxList(Of VisualBasicSyntaxNode)

        Private Shared ReadOnly s_scanNoTriviaFunc As ScanTriviaFunc = Function() Nothing
        Private ReadOnly _scanSingleLineTriviaFunc As ScanTriviaFunc = AddressOf ScanSingleLineTrivia

        Protected _lineBufferOffset As Integer ' marks the next character to read from _buffer
        Private _endOfTerminatorTrivia As Integer ' marks how far scanner may have scanned ahead for terminator trivia. This may be greater than _lineBufferOffset

        Friend Const BadTokenCountLimit As Integer = 200
        Private _badTokenCount As Integer ' cumulative count of bad tokens produced

        Private ReadOnly _sbPooled As PooledStringBuilder = PooledStringBuilder.GetInstance
        ''' <summary>
        ''' DO NOT USE DIRECTLY.
        ''' USE GetScratch()
        ''' </summary>
        Private ReadOnly _sb As StringBuilder = _sbPooled.Builder
        Private ReadOnly _triviaListPool As New SyntaxListPool
        Private ReadOnly _options As VisualBasicParseOptions

        Private ReadOnly _stringTable As StringTable = StringTable.GetInstance()
        Private ReadOnly _quickTokenTable As TextKeyedCache(Of SyntaxToken) = TextKeyedCache(Of SyntaxToken).GetInstance

        Public Const TABLE_LIMIT = 512
        Private Shared ReadOnly s_keywordKindFactory As Func(Of String, SyntaxKind) =
            Function(spelling) KeywordTable.TokenOfString(spelling)

        Private Shared ReadOnly s_keywordsObjsPool As ObjectPool(Of CachingIdentityFactory(Of String, SyntaxKind)) = CachingIdentityFactory(Of String, SyntaxKind).CreatePool(TABLE_LIMIT, s_keywordKindFactory)
        Private ReadOnly _KeywordsObjs As CachingIdentityFactory(Of String, SyntaxKind) = s_keywordsObjsPool.Allocate()

        Private Shared ReadOnly s_idTablePool As New ObjectPool(Of CachingFactory(Of TokenParts, IdentifierTokenSyntax))(
            Function() New CachingFactory(Of TokenParts, IdentifierTokenSyntax)(TABLE_LIMIT, Nothing, s_tokenKeyHasher, s_tokenKeyEquality))

        Private ReadOnly _idTable As CachingFactory(Of TokenParts, IdentifierTokenSyntax) = s_idTablePool.Allocate()

        Private Shared ReadOnly s_kwTablePool As New ObjectPool(Of CachingFactory(Of TokenParts, KeywordSyntax))(
            Function() New CachingFactory(Of TokenParts, KeywordSyntax)(TABLE_LIMIT, Nothing, s_tokenKeyHasher, s_tokenKeyEquality))

        Private ReadOnly _kwTable As CachingFactory(Of TokenParts, KeywordSyntax) = s_kwTablePool.Allocate

        Private Shared ReadOnly s_punctTablePool As New ObjectPool(Of CachingFactory(Of TokenParts, PunctuationSyntax))(
            Function() New CachingFactory(Of TokenParts, PunctuationSyntax)(TABLE_LIMIT, Nothing, s_tokenKeyHasher, s_tokenKeyEquality))

        Private ReadOnly _punctTable As CachingFactory(Of TokenParts, PunctuationSyntax) = s_punctTablePool.Allocate()

        Private Shared ReadOnly s_literalTablePool As New ObjectPool(Of CachingFactory(Of TokenParts, SyntaxToken))(
            Function() New CachingFactory(Of TokenParts, SyntaxToken)(TABLE_LIMIT, Nothing, s_tokenKeyHasher, s_tokenKeyEquality))

        Private ReadOnly _literalTable As CachingFactory(Of TokenParts, SyntaxToken) = s_literalTablePool.Allocate

        Private Shared ReadOnly s_wslTablePool As New ObjectPool(Of CachingFactory(Of SyntaxListBuilder, SyntaxList(Of VisualBasicSyntaxNode)))(
            Function() New CachingFactory(Of SyntaxListBuilder, SyntaxList(Of VisualBasicSyntaxNode))(TABLE_LIMIT, s_wsListFactory, s_wsListKeyHasher, s_wsListKeyEquality))

        Private ReadOnly _wslTable As CachingFactory(Of SyntaxListBuilder, SyntaxList(Of VisualBasicSyntaxNode)) = s_wslTablePool.Allocate

        Private Shared ReadOnly s_wsTablePool As New ObjectPool(Of CachingFactory(Of TriviaKey, SyntaxTrivia))(
            Function() CreateWsTable())

        Private ReadOnly _wsTable As CachingFactory(Of TriviaKey, SyntaxTrivia) = s_wsTablePool.Allocate

        Private ReadOnly _isScanningForExpressionCompiler As Boolean

        Private _isDisposed As Boolean

        Private Function GetScratch() As StringBuilder
            ' the normal pattern is that we clean scratch after use.
            ' hitting this assert very likely indicates that you
            ' did not release scratch content or worse trying to use
            ' scratch in two places at a time.
            Debug.Assert(_sb.Length = 0, "trying to use dirty buffer?")
            Return _sb
        End Function

#Region "Public interface"
        Friend Sub New(textToScan As SourceText, options As VisualBasicParseOptions, Optional isScanningForExpressionCompiler As Boolean = False)
            Debug.Assert(textToScan IsNot Nothing)

            _lineBufferOffset = 0
            _buffer = textToScan
            _bufferLen = textToScan.Length
            _curPage = GetPage(0)
            _options = options

            _scannerPreprocessorState = New PreprocessorState(GetPreprocessorConstants(options))
            _isScanningForExpressionCompiler = isScanningForExpressionCompiler
        End Sub

        Friend Sub Dispose() Implements IDisposable.Dispose
            If Not _isDisposed Then
                _isDisposed = True

                _KeywordsObjs.Free()
                _quickTokenTable.Free()
                _stringTable.Free()
                _sbPooled.Free()

                s_idTablePool.Free(_idTable)
                s_kwTablePool.Free(_kwTable)
                s_punctTablePool.Free(_punctTable)
                s_literalTablePool.Free(_literalTable)
                s_wslTablePool.Free(_wslTable)
                s_wsTablePool.Free(_wsTable)

                For Each p As Page In _pages
                    If p IsNot Nothing Then
                        p.Free()
                    End If
                Next

                Array.Clear(_pages, 0, _pages.Length)
            End If
        End Sub
        Friend ReadOnly Property Options As VisualBasicParseOptions
            Get
                Return _options
            End Get
        End Property

        Friend Shared Function GetPreprocessorConstants(options As VisualBasicParseOptions) As ImmutableDictionary(Of String, CConst)
            If options.PreprocessorSymbols.IsDefaultOrEmpty Then
                Return ImmutableDictionary(Of String, CConst).Empty
            End If

            Dim result = ImmutableDictionary.CreateBuilder(Of String, CConst)(IdentifierComparison.Comparer)
            For Each symbol In options.PreprocessorSymbols
                ' The values in options have already been verified
                result(symbol.Key) = CConst.CreateChecked(symbol.Value)
            Next

            Return result.ToImmutable()
        End Function

        Private Function GetNextToken(Optional allowLeadingMultilineTrivia As Boolean = False) As SyntaxToken
            ' Use quick token scanning to see if we can scan a token quickly.
            Dim quickToken = QuickScanToken(allowLeadingMultilineTrivia)

            If quickToken.Succeeded Then
                Dim token = _quickTokenTable.FindItem(quickToken.Chars, quickToken.Start, quickToken.Length, quickToken.HashCode)
                If token IsNot Nothing Then
                    AdvanceChar(quickToken.Length)
                    If quickToken.TerminatorLength <> 0 Then
                        _endOfTerminatorTrivia = _lineBufferOffset
                        _lineBufferOffset -= quickToken.TerminatorLength
                    End If

                    Return token
                End If
            End If

            Dim scannedToken = ScanNextToken(allowLeadingMultilineTrivia)

            ' If we quick-scanned a token, but didn't have a actual token cached for it, cache the token we created
            ' from the regular scanner.
            If quickToken.Succeeded Then
                Debug.Assert(quickToken.Length = scannedToken.FullWidth)

                _quickTokenTable.AddItem(quickToken.Chars, quickToken.Start, quickToken.Length, quickToken.HashCode, scannedToken)
            End If

            Return scannedToken
        End Function

        Private Function ScanNextToken(allowLeadingMultilineTrivia As Boolean) As SyntaxToken
#If DEBUG Then
            Dim oldOffset = _lineBufferOffset
#End If
            Dim leadingTrivia As SyntaxList(Of VisualBasicSyntaxNode)

            If allowLeadingMultilineTrivia Then
                leadingTrivia = ScanMultilineTrivia()
            Else
                leadingTrivia = ScanLeadingTrivia()

                ' Special case where the remainder of the line is a comment.
                Dim length = PeekStartComment(0)
                If length > 0 Then
                    Return MakeEmptyToken(leadingTrivia)
                End If
            End If

            Dim token = TryScanToken(leadingTrivia)

            If token Is Nothing Then
                token = ScanNextCharAsToken(leadingTrivia)
            End If

            If _lineBufferOffset > _endOfTerminatorTrivia Then
                _endOfTerminatorTrivia = _lineBufferOffset
            End If

#If DEBUG Then
            ' we must always consume as much as returned token's full length or things will go very bad
            Debug.Assert(oldOffset + token.FullWidth = _lineBufferOffset OrElse
                         oldOffset + token.FullWidth = _endOfTerminatorTrivia OrElse
                         token.FullWidth = 0)
#End If
            Return token
        End Function

        Private Function ScanNextCharAsToken(leadingTrivia As SyntaxList(Of VisualBasicSyntaxNode)) As SyntaxToken
            Dim token As SyntaxToken

            If Not CanGet() Then
                token = MakeEofToken(leadingTrivia)
            Else
                _badTokenCount += 1

                If _badTokenCount < BadTokenCountLimit Then
                    ' // Don't break up surrogate pairs
                    Dim c = Peek()
                    Dim length = If(IsHighSurrogate(c) AndAlso CanGet(1) AndAlso IsLowSurrogate(Peek(1)), 2, 1)
                    token = MakeBadToken(leadingTrivia, length, ERRID.ERR_IllegalChar)
                Else
                    ' If we get too many characters that we cannot make sense of, absorb the rest of the input.
                    token = MakeBadToken(leadingTrivia, RemainingLength(), ERRID.ERR_IllegalChar)
                End If
            End If

            Return token
        End Function

        ' // SkipToNextConditionalLine advances through the input stream until it finds a (logical)
        ' // line that has a '#' character as its first non-whitespace, non-continuation character.
        ' // SkipToNextConditionalLine ignores explicit line continuation.

        ' TODO: this could be vastly simplified if we could ignore line continuations.
        Public Function SkipToNextConditionalLine() As TextSpan
            ' start at current token
            ResetLineBufferOffset()

            Dim start = _lineBufferOffset

            ' if starting not from line start, skip to the next one.
            Dim prev = PrevToken
            If Not IsAtNewLine() OrElse
                (PrevToken IsNot Nothing AndAlso PrevToken.EndsWithEndOfLineOrColonTrivia) Then

                EatThroughLine()
            End If

            Dim condLineStart = _lineBufferOffset

            While (CanGet())
                Dim c As Char = Peek()

                Select Case (c)

                    Case CARRIAGE_RETURN, LINE_FEED
                        EatThroughLineBreak(c)
                        condLineStart = _lineBufferOffset
                        Continue While

                    Case SPACE, CHARACTER_TABULATION
                        Debug.Assert(IsWhitespace(Peek()))
                        EatWhitespace()
                        Continue While

                    Case _
                        "a"c, "b"c, "c"c, "d"c, "e"c, "f"c, "g"c, "h"c, "i"c, "j"c, "k"c, "l"c,
                        "m"c, "n"c, "o"c, "p"c, "q"c, "r"c, "s"c, "t"c, "u"c, "v"c, "w"c, "x"c,
                        "y"c, "z"c, "A"c, "B"c, "C"c, "D"c, "E"c, "F"c, "G"c, "H"c, "I"c, "J"c,
                        "K"c, "L"c, "M"c, "N"c, "O"c, "P"c, "Q"c, "R"c, "S"c, "T"c, "U"c, "V"c,
                        "W"c, "X"c, "Y"c, "Z"c, "'"c, "_"c

                        EatThroughLine()
                        condLineStart = _lineBufferOffset
                        Continue While

                    Case "#"c, FULLWIDTH_NUMBER_SIGN
                        Exit While

                    Case Else
                        If IsWhitespace(c) Then
                            EatWhitespace()
                            Continue While

                        ElseIf IsNewLine(c) Then
                            EatThroughLineBreak(c)
                            condLineStart = _lineBufferOffset
                            Continue While

                        End If

                        EatThroughLine()
                        condLineStart = _lineBufferOffset
                        Continue While
                End Select
            End While

            ' we did not find # or we have hit EoF.
            _lineBufferOffset = condLineStart
            Debug.Assert(_lineBufferOffset >= start AndAlso _lineBufferOffset >= 0)

            ResetTokens()
            Return TextSpan.FromBounds(start, condLineStart)
        End Function

        Private Sub EatThroughLine()
            While CanGet()
                Dim c As Char = Peek()

                If IsNewLine(c) Then
                    EatThroughLineBreak(c)
                    Return
                Else
                    AdvanceChar()
                End If
            End While
        End Sub

        ''' <summary>
        ''' Gets a chunk of text as a DisabledCode node.
        ''' </summary>
        ''' <param name="span">The range of text.</param>
        ''' <returns>The DisabledCode node.</returns>
        Friend Function GetDisabledTextAt(span As TextSpan) As SyntaxTrivia
            If span.Start >= 0 AndAlso span.End <= _bufferLen Then
                Return SyntaxFactory.DisabledTextTrivia(GetTextNotInterned(span.Start, span.Length))
            End If

            ' TODO: should this be a Require?
            Throw New ArgumentOutOfRangeException(NameOf(span))
        End Function
#End Region

#Region "Interning"
        Friend Function GetScratchTextInterned(sb As StringBuilder) As String
            Dim str = _stringTable.Add(sb)
            sb.Clear()
            Return str
        End Function

        Friend Shared Function GetScratchText(sb As StringBuilder) As String
            ' PERF: Special case for the very common case of a string containing a single space
            Dim str As String
            If sb.Length = 1 AndAlso sb(0) = " "c Then
                str = " "
            Else
                str = sb.ToString
            End If
            sb.Clear()
            Return str
        End Function

        ' This overload of GetScratchText first examines the contents of the StringBuilder to
        ' see if it matches the given string. If so, then the given string is returned, saving
        ' the allocation.
        Private Shared Function GetScratchText(sb As StringBuilder, text As String) As String
            Dim str As String
            If StringTable.TextEquals(text, sb) Then
                str = text
            Else
                str = sb.ToString
            End If
            sb.Clear()
            Return str
        End Function

        Friend Function Intern(s As String, start As Integer, length As Integer) As String
            Return _stringTable.Add(s, start, length)
        End Function

        Friend Function Intern(s As Char(), start As Integer, length As Integer) As String
            Return _stringTable.Add(s, start, length)
        End Function

        Friend Function Intern(ch As Char) As String
            Return _stringTable.Add(ch)
        End Function
        Friend Function Intern(arr As Char()) As String
            Return _stringTable.Add(arr)
        End Function
#End Region

#Region "Buffer helpers"

        Private Function NextAre(chars As String) As Boolean
            Return NextAre(0, chars)
        End Function

        Private Function NextAre(offset As Integer, chars As String) As Boolean
            Debug.Assert(Not String.IsNullOrEmpty(chars))
            Dim n = chars.Length
            If Not CanGet(offset + n - 1) Then Return False
            For i = 0 To n - 1
                If chars(i) <> Peek(offset + i) Then Return False
            Next
            Return True
        End Function

        Private Function NextIs(offset As Integer, c As Char) As Boolean
            Return CanGet(offset) AndAlso (Peek(offset) = c)
        End Function

        Private Function CanGet() As Boolean
            Return _lineBufferOffset < _bufferLen
        End Function

        Private Function CanGet(num As Integer) As Boolean
            Debug.Assert(_lineBufferOffset + num >= 0)
            Debug.Assert(num >= -MaxCharsLookBehind)

            Return _lineBufferOffset + num < _bufferLen
        End Function

        Private Function RemainingLength() As Integer
            Dim result = _bufferLen - _lineBufferOffset
            Debug.Assert(CanGet(result - 1))
            Return result
        End Function

        Private Function GetText(length As Integer) As String
            Debug.Assert(length > 0)
            Debug.Assert(CanGet(length - 1))

            If length = 1 Then
                Return GetNextChar()
            End If

            Dim str = GetText(_lineBufferOffset, length)
            AdvanceChar(length)
            Return str
        End Function

        Private Function GetTextNotInterned(length As Integer) As String
            Debug.Assert(length > 0)
            Debug.Assert(CanGet(length - 1))

            If length = 1 Then
                ' we will still intern single chars. There could not be too many.
                Return GetNextChar()
            End If

            Dim str = GetTextNotInterned(_lineBufferOffset, length)
            AdvanceChar(length)
            Return str
        End Function

        Private Sub AdvanceChar(Optional howFar As Integer = 1)
            Debug.Assert(howFar > 0)
            Debug.Assert(CanGet(howFar - 1))

            _lineBufferOffset += howFar
        End Sub

        Private Function GetNextChar() As String
            Debug.Assert(CanGet)

            Dim ch = GetChar()
            _lineBufferOffset += 1

            Return ch
        End Function

        Private Sub EatThroughLineBreak(StartCharacter As Char)
            AdvanceChar(LengthOfLineBreak(StartCharacter))
        End Sub

        Private Function SkipLineBreak(StartCharacter As Char, index As Integer) As Integer
            Return index + LengthOfLineBreak(StartCharacter, index)
        End Function

        Private Function LengthOfLineBreak(StartCharacter As Char, Optional here As Integer = 0) As Integer
            Debug.Assert(CanGet(here))
            Debug.Assert(IsNewLine(StartCharacter))

            Debug.Assert(StartCharacter = Peek(here))

            If StartCharacter = CARRIAGE_RETURN AndAlso NextIs(here + 1, LINE_FEED) Then
                Return 2
            End If
            Return 1
        End Function
#End Region

#Region "New line and explicit line continuation."
        ''' <summary>
        ''' Accept a CR/LF pair or either in isolation as a newline.
        ''' Make it a statement separator
        ''' </summary>
        Private Function ScanNewlineAsStatementTerminator(startCharacter As Char, precedingTrivia As SyntaxList(Of VisualBasicSyntaxNode)) As SyntaxToken
            If _lineBufferOffset < _endOfTerminatorTrivia Then
                Dim width = LengthOfLineBreak(startCharacter)
                Return MakeStatementTerminatorToken(precedingTrivia, width)
            Else
                Return MakeEmptyToken(precedingTrivia)
            End If
        End Function

        Private Function ScanColonAsStatementTerminator(precedingTrivia As SyntaxList(Of VisualBasicSyntaxNode), charIsFullWidth As Boolean) As SyntaxToken
            If _lineBufferOffset < _endOfTerminatorTrivia Then
                Return MakeColonToken(precedingTrivia, charIsFullWidth)
            Else
                Return MakeEmptyToken(precedingTrivia)
            End If
        End Function

        ''' <summary>
        ''' Accept a CR/LF pair or either in isolation as a newline.
        ''' Make it a whitespace
        ''' </summary>
        Private Function ScanNewlineAsTrivia(StartCharacter As Char) As SyntaxTrivia
            If LengthOfLineBreak(StartCharacter) = 2 Then
                Return MakeEndOfLineTriviaCRLF()
            End If
            Return MakeEndOfLineTrivia(GetNextChar)
        End Function

        Private Function ScanLineContinuation(tList As SyntaxListBuilder) As Boolean
            If Not CanGet() Then
                Return False
            End If

            If Not IsAfterWhitespace() Then
                Return False
            End If

            Dim ch As Char = Peek()
            If Not IsUnderscore(ch) Then
                Return False
            End If

            Dim Here = 1
            While CanGet(Here)
                ch = Peek(Here)
                If IsWhitespace(ch) Then
                    Here += 1
                Else
                    Exit While
                End If
            End While

            ' Line continuation is valid at the end of the
            ' line or at the end of file only.
            Dim atNewLine = IsNewLine(ch)
            If Not atNewLine AndAlso CanGet(Here) Then
                Return False
            End If

            tList.Add(MakeLineContinuationTrivia(GetText(1)))
            If Here > 1 Then
                tList.Add(MakeWhiteSpaceTrivia(GetText(Here - 1)))
            End If

            If atNewLine Then
                Dim newLine = SkipLineBreak(ch, 0)
                Here = GetWhitespaceLength(newLine)
                Dim spaces = Here - newLine
                Dim startComment = PeekStartComment(Here)

                ' If the line following the line continuation is blank, or blank with a comment,
                ' do not include the new line character since that would confuse code handling
                ' implicit line continuations. (See Scanner::EatLineContinuation.) Otherwise,
                ' include the new line and any additional spaces as trivia.
                If startComment = 0 AndAlso
                    CanGet(Here) AndAlso
                    Not IsNewLine(Peek(Here)) Then

                    tList.Add(MakeEndOfLineTrivia(GetText(newLine)))
                    If spaces > 0 Then
                        tList.Add(MakeWhiteSpaceTrivia(GetText(spaces)))
                    End If
                End If

            End If

            Return True
        End Function

#End Region

#Region "Trivia"

        ''' <summary>
        ''' Consumes all trivia until a nontrivia char is found
        ''' </summary>
        Friend Function ScanMultilineTrivia() As SyntaxList(Of VisualBasicSyntaxNode)
            If Not CanGet() Then
                Return Nothing
            End If

            Dim ch = Peek()

            ' optimization for a common case
            ' the ASCII range between ': and ~ , with exception of except "'", "_" and R cannot start trivia
            If ch > ":"c AndAlso ch <= "~"c AndAlso ch <> "'"c AndAlso ch <> "_"c AndAlso ch <> "R"c AndAlso ch <> "r"c Then
                Return Nothing
            End If

            Dim triviaList = _triviaListPool.Allocate()
            While TryScanSinglePieceOfMultilineTrivia(triviaList)
            End While

            Dim result = MakeTriviaArray(triviaList)
            _triviaListPool.Free(triviaList)
            Return result
        End Function

        ''' <summary>
        ''' Scans a single piece of trivia
        ''' </summary>
        Private Function TryScanSinglePieceOfMultilineTrivia(tList As SyntaxListBuilder) As Boolean
            If CanGet() Then

                Dim atNewLine = IsAtNewLine()

                ' check for XmlDocComment and directives
                If atNewLine Then
                    If StartsXmlDoc(0) Then
                        Return TryScanXmlDocComment(tList)
                    End If

                    If StartsDirective(0) Then
                        Return TryScanDirective(tList)
                    End If
                End If

                Dim ch = Peek()
                If IsWhitespace(ch) Then
                    ' eat until linebreak or non-whitespace
                    Dim wslen = GetWhitespaceLength(1)

                    If atNewLine Then
                        If StartsXmlDoc(wslen) Then
                            Return TryScanXmlDocComment(tList)
                        End If

                        If StartsDirective(wslen) Then
                            Return TryScanDirective(tList)
                        End If
                    End If
                    tList.Add(MakeWhiteSpaceTrivia(GetText(wslen)))
                    Return True
                ElseIf IsNewLine(ch) Then
                    tList.Add(ScanNewlineAsTrivia(ch))
                    Return True
                ElseIf IsUnderscore(ch) Then
                    Return ScanLineContinuation(tList)
                ElseIf IsColonAndNotColonEquals(ch, offset:=0) Then
                    tList.Add(ScanColonAsTrivia())
                    Return True
                End If

                ' try get a comment
                Return ScanCommentIfAny(tList)
            End If

            Return False
        End Function

        ' check for '''(~')
        Private Function StartsXmlDoc(Here As Integer) As Boolean
            Return _options.DocumentationMode >= DocumentationMode.Parse AndAlso
                CanGet(Here + 3) AndAlso
                IsSingleQuote(Peek(Here)) AndAlso
                IsSingleQuote(Peek(Here + 1)) AndAlso
                IsSingleQuote(Peek(Here + 2)) AndAlso
                Not IsSingleQuote(Peek(Here + 3))
        End Function

        ' check for #
        Private Function StartsDirective(Here As Integer) As Boolean
            If CanGet(Here) Then
                Dim ch = Peek(Here)
                Return IsHash(ch)
            End If
            Return False
        End Function

        Private Function IsAtNewLine() As Boolean
            Return _lineBufferOffset = 0 OrElse IsNewLine(Peek(-1))
        End Function

        Private Function IsAfterWhitespace() As Boolean
            If _lineBufferOffset = 0 Then
                Return True
            End If

            Dim prevChar = Peek(-1)
            Return IsWhitespace(prevChar)
        End Function

        ''' <summary>
        ''' Scan trivia on one LOGICAL line
        ''' Will check for whitespace, comment, EoL, implicit line break
        ''' EoL may be consumed as whitespace only as a part of line continuation ( _ )
        ''' </summary>
        Friend Function ScanSingleLineTrivia() As SyntaxList(Of VisualBasicSyntaxNode)
            Dim tList = _triviaListPool.Allocate()
            ScanSingleLineTrivia(tList)
            Dim result = MakeTriviaArray(tList)
            _triviaListPool.Free(tList)
            Return result
        End Function

        Private Sub ScanSingleLineTrivia(tList As SyntaxListBuilder)
            If IsScanningXmlDoc Then
                ScanSingleLineTriviaInXmlDoc(tList)
            Else
                ScanWhitespaceAndLineContinuations(tList)
                ScanCommentIfAny(tList)
                ScanTerminatorTrivia(tList)
            End If
        End Sub

        Private Sub ScanSingleLineTriviaInXmlDoc(tList As SyntaxListBuilder)
            If CanGet() Then
                Dim c As Char = Peek()
                Select Case (c)
                    ' // Whitespace
                    ' //  S    ::=    (#x20 | #x9 | #xD | #xA)+
                    Case CARRIAGE_RETURN, LINE_FEED, " "c, CHARACTER_TABULATION
                        Dim offsets = CreateOffsetRestorePoint()
                        Dim triviaList = _triviaListPool.Allocate(Of VisualBasicSyntaxNode)()
                        Dim continueLine = ScanXmlTriviaInXmlDoc(c, triviaList)
                        If Not continueLine Then
                            _triviaListPool.Free(triviaList)
                            offsets.Restore()
                            Return
                        End If

                        For i = 0 To triviaList.Count - 1
                            tList.Add(triviaList(i))
                        Next
                        _triviaListPool.Free(triviaList)

                End Select
            End If
        End Sub

        Private Function ScanLeadingTrivia() As SyntaxList(Of VisualBasicSyntaxNode)
            Dim tList = _triviaListPool.Allocate()
            ScanWhitespaceAndLineContinuations(tList)
            Dim result = MakeTriviaArray(tList)
            _triviaListPool.Free(tList)
            Return result
        End Function

        Private Sub ScanWhitespaceAndLineContinuations(tList As SyntaxListBuilder)
            If CanGet() AndAlso IsWhitespace(Peek()) Then
                tList.Add(ScanWhitespace(1))
                ' collect { lineCont, ws }
                While ScanLineContinuation(tList)
                End While
            End If
        End Sub

        Private Function ScanSingleLineTrivia(includeFollowingBlankLines As Boolean) As SyntaxList(Of VisualBasicSyntaxNode)
            Dim tList = _triviaListPool.Allocate()
            ScanSingleLineTrivia(tList)

            If includeFollowingBlankLines AndAlso IsBlankLine(tList) Then
                Dim more = _triviaListPool.Allocate()

                While True
                    Dim offsets = CreateOffsetRestorePoint()

                    _lineBufferOffset = _endOfTerminatorTrivia
                    ScanSingleLineTrivia(more)

                    If Not IsBlankLine(more) Then
                        offsets.Restore()
                        Exit While
                    End If

                    Dim n = more.Count
                    For i = 0 To n - 1
                        tList.Add(more(i))
                    Next
                    more.Clear()
                End While

                _triviaListPool.Free(more)
            End If

            Dim result = tList.ToList()
            _triviaListPool.Free(tList)
            Return result
        End Function

        ''' <summary>
        ''' Return True if the builder is a (possibly empty) list of
        ''' WhitespaceTrivia followed by an EndOfLineTrivia.
        ''' </summary>
        Private Shared Function IsBlankLine(tList As SyntaxListBuilder) As Boolean
            Dim n = tList.Count
            If n = 0 OrElse tList(n - 1).Kind <> SyntaxKind.EndOfLineTrivia Then
                Return False
            End If
            For i = 0 To n - 2
                If tList(i).Kind <> SyntaxKind.WhitespaceTrivia Then
                    Return False
                End If
            Next
            Return True
        End Function

        Private Sub ScanTerminatorTrivia(tList As SyntaxListBuilder)
            ' Check for statement terminators
            ' There are 4 special cases

            '   1. [colon ws+]* colon -> colon terminator
            '   2. new line -> new line terminator
            '   3. colon followed by new line -> colon terminator + new line terminator
            '   4. new line followed by new line -> new line terminator + new line terminator

            ' Case 3 is required to parse single line if's and numeric labels.
            ' Case 4 is required to limit explicit line continuations to single new line

            If CanGet() Then

                Dim ch As Char = Peek()
                Dim startOfTerminatorTrivia = _lineBufferOffset

                If IsNewLine(ch) Then
                    tList.Add(ScanNewlineAsTrivia(ch))

                ElseIf IsColonAndNotColonEquals(ch, offset:=0) Then
                    tList.Add(ScanColonAsTrivia())

                    ' collect { ws, colon }
                    Do
                        Dim len = GetWhitespaceLength(0)
                        If Not CanGet(len) Then
                            Exit Do
                        End If

                        ch = Peek(len)
                        If Not IsColonAndNotColonEquals(ch, offset:=len) Then
                            Exit Do
                        End If

                        If len > 0 Then
                            tList.Add(MakeWhiteSpaceTrivia(GetText(len)))
                        End If

                        startOfTerminatorTrivia = _lineBufferOffset
                        tList.Add(ScanColonAsTrivia())
                    Loop
                End If

                _endOfTerminatorTrivia = _lineBufferOffset
                ' Reset _lineBufferOffset to the start of the terminator trivia.
                ' When the scanner is asked for the next token, it will return a 0 length terminator or colon token.
                _lineBufferOffset = startOfTerminatorTrivia
            End If

        End Sub

        Private Function ScanCommentIfAny(tList As SyntaxListBuilder) As Boolean
            If CanGet() Then
                ' check for comment
                Dim comment = ScanComment()
                If comment IsNot Nothing Then
                    tList.Add(comment)
                    Return True
                End If
            End If
            Return False
        End Function

        Private Function GetWhitespaceLength(len As Integer) As Integer
            ' eat until linebreak or non-whitespace
            While CanGet(len) AndAlso IsWhitespace(Peek(len))
                len += 1
            End While
            Return len
        End Function

        Private Function GetXmlWhitespaceLength(len As Integer) As Integer
            ' eat until linebreak or non-whitespace
            While CanGet(len) AndAlso IsXmlWhitespace(Peek(len))
                len += 1
            End While
            Return len
        End Function

        Private Function ScanWhitespace(Optional len As Integer = 0) As VisualBasicSyntaxNode
            len = GetWhitespaceLength(len)
            If len > 0 Then
                Return MakeWhiteSpaceTrivia(GetText(len))
            End If
            Return Nothing
        End Function

        Private Function ScanXmlWhitespace(Optional len As Integer = 0) As VisualBasicSyntaxNode
            len = GetXmlWhitespaceLength(len)
            If len > 0 Then
                Return MakeWhiteSpaceTrivia(GetText(len))
            End If
            Return Nothing
        End Function

        Private Sub EatWhitespace()
            Debug.Assert(CanGet)
            Debug.Assert(IsWhitespace(Peek()))

            AdvanceChar()

            ' eat until linebreak or non-whitespace
            While CanGet() AndAlso IsWhitespace(Peek)
                AdvanceChar()
            End While
        End Sub

        Private Function PeekStartComment(i As Integer) As Integer

            If CanGet(i) Then
                Dim ch = Peek(i)

                If IsSingleQuote(ch) Then
                    Return 1
                ElseIf MatchOneOrAnotherOrFullwidth(ch, "R"c, "r"c) AndAlso
                    CanGet(i + 2) AndAlso MatchOneOrAnotherOrFullwidth(Peek(i + 1), "E"c, "e"c) AndAlso
                    MatchOneOrAnotherOrFullwidth(Peek(i + 2), "M"c, "m"c) Then

                    If Not CanGet(i + 3) OrElse IsNewLine(Peek(i + 3)) Then
                        ' have only 'REM'
                        Return 3
                    ElseIf Not IsIdentifierPartCharacter(Peek(i + 3)) Then
                        ' have 'REM '
                        Return 4
                    End If
                End If
            End If

            Return 0
        End Function

        Private Function ScanComment() As SyntaxTrivia
            Debug.Assert(CanGet())

            Dim length = PeekStartComment(0)
            If length > 0 Then
                Dim looksLikeDocComment As Boolean = StartsXmlDoc(0)

                ' eat all chars until EoL
                While CanGet(length) AndAlso
                    Not IsNewLine(Peek(length))

                    length += 1
                End While

                Dim commentTrivia As SyntaxTrivia = MakeCommentTrivia(GetTextNotInterned(length))

                If looksLikeDocComment AndAlso _options.DocumentationMode >= DocumentationMode.Diagnose Then
                    commentTrivia = commentTrivia.WithDiagnostics(ErrorFactory.ErrorInfo(ERRID.WRN_XMLDocNotFirstOnLine))
                End If

                Return commentTrivia
            End If

            Return Nothing
        End Function

        ''' <summary>
        ''' Return True if the character is a colon, and not part of ":=".
        ''' </summary>
        Private Function IsColonAndNotColonEquals(ch As Char, offset As Integer) As Boolean
            Return IsColon(ch) AndAlso Not TrySkipFollowingEquals(offset + 1)
        End Function

        Private Function ScanColonAsTrivia() As SyntaxTrivia
            Debug.Assert(CanGet())
            Debug.Assert(IsColonAndNotColonEquals(Peek(), offset:=0))

            Return MakeColonTrivia(GetText(1))
        End Function

#End Region

        Private Function ScanTokenCommon(precedingTrivia As SyntaxList(Of VisualBasicSyntaxNode), ch As Char, fullWidth As Boolean) As SyntaxToken
            Dim lengthWithMaybeEquals As Integer = 1
            Select Case ch
                Case CARRIAGE_RETURN, LINE_FEED
                    Return ScanNewlineAsStatementTerminator(ch, precedingTrivia)

                Case NEXT_LINE, LINE_SEPARATOR, PARAGRAPH_SEPARATOR
                    If Not fullWidth Then
                        Return ScanNewlineAsStatementTerminator(ch, precedingTrivia)
                    End If

                Case " "c, CHARACTER_TABULATION, "'"c
                    Debug.Assert(False, $"Unexpected char: &H{AscW(ch):x}")
                    Return Nothing ' trivia cannot start a token

                Case "@"c
                    Return MakeAtToken(precedingTrivia, fullWidth)

                Case "("c
                    Return MakeOpenParenToken(precedingTrivia, fullWidth)

                Case ")"c
                    Return MakeCloseParenToken(precedingTrivia, fullWidth)

                Case "{"c
                    Return MakeOpenBraceToken(precedingTrivia, fullWidth)

                Case "}"c
                    Return MakeCloseBraceToken(precedingTrivia, fullWidth)

                Case ","c
                    Return MakeCommaToken(precedingTrivia, fullWidth)

                Case "#"c
                    Dim dl = ScanDateLiteral(precedingTrivia)
                    Return If(dl, MakeHashToken(precedingTrivia, fullWidth))

                Case "&"c
                    If CanGet(1) AndAlso BeginsBaseLiteral(Peek(1)) Then
                        Return ScanNumericLiteral(precedingTrivia)
                    End If

                    If TrySkipFollowingEquals(lengthWithMaybeEquals) Then
                        Return MakeAmpersandEqualsToken(precedingTrivia, lengthWithMaybeEquals)
                    Else
                        Return MakeAmpersandToken(precedingTrivia, fullWidth)
                    End If

                Case "="c
                    Return MakeEqualsToken(precedingTrivia, fullWidth)

                Case "<"c
                    Return ScanLeftAngleBracket(precedingTrivia, fullWidth, _scanSingleLineTriviaFunc)

                Case ">"c
                    Return ScanRightAngleBracket(precedingTrivia, fullWidth)

                Case ":"c
                    If TrySkipFollowingEquals(lengthWithMaybeEquals) Then
                        Return MakeColonEqualsToken(precedingTrivia, lengthWithMaybeEquals)
                    Else
                        Return ScanColonAsStatementTerminator(precedingTrivia, fullWidth)
                    End If

                Case "+"c
                    If TrySkipFollowingEquals(lengthWithMaybeEquals) Then
                        Return MakePlusEqualsToken(precedingTrivia, lengthWithMaybeEquals)
                    Else
                        Return MakePlusToken(precedingTrivia, fullWidth)
                    End If

                Case "-"c
                    If TrySkipFollowingEquals(lengthWithMaybeEquals) Then
                        Return MakeMinusEqualsToken(precedingTrivia, lengthWithMaybeEquals)
                    Else
                        Return MakeMinusToken(precedingTrivia, fullWidth)
                    End If

                Case "*"c
                    If TrySkipFollowingEquals(lengthWithMaybeEquals) Then
                        Return MakeAsteriskEqualsToken(precedingTrivia, lengthWithMaybeEquals)
                    Else
                        Return MakeAsteriskToken(precedingTrivia, fullWidth)
                    End If

                Case "/"c
                    If TrySkipFollowingEquals(lengthWithMaybeEquals) Then
                        Return MakeSlashEqualsToken(precedingTrivia, lengthWithMaybeEquals)
                    Else
                        Return MakeSlashToken(precedingTrivia, fullWidth)
                    End If

                Case "\"c
                    If TrySkipFollowingEquals(lengthWithMaybeEquals) Then
                        Return MakeBackSlashEqualsToken(precedingTrivia, lengthWithMaybeEquals)
                    Else
                        Return MakeBackslashToken(precedingTrivia, fullWidth)
                    End If

                Case "^"c
                    If TrySkipFollowingEquals(lengthWithMaybeEquals) Then
                        Return MakeCaretEqualsToken(precedingTrivia, lengthWithMaybeEquals)
                    Else
                        Return MakeCaretToken(precedingTrivia, fullWidth)
                    End If

                Case "!"c
                    Return MakeExclamationToken(precedingTrivia, fullWidth)

                Case "."c
                    If CanGet(1) AndAlso IsDecimalDigit(Peek(1)) Then
                        Return ScanNumericLiteral(precedingTrivia)
                    Else
                        Return MakeDotToken(precedingTrivia, fullWidth)
                    End If

                Case "0"c, "1"c, "2"c, "3"c, "4"c, "5"c, "6"c, "7"c, "8"c, "9"c
                    Return ScanNumericLiteral(precedingTrivia)

                Case """"c
                    Return ScanStringLiteral(precedingTrivia)

                Case "A"c
                    If NextAre(1, "s ") Then
                        ' TODO: do we allow widechars in keywords?
                        AdvanceChar(2)
                        Return MakeKeyword(SyntaxKind.AsKeyword, "As", precedingTrivia)
                    Else
                        Return ScanIdentifierOrKeyword(precedingTrivia)
                    End If

                Case "E"c
                    If NextAre(1, "nd ") Then
                        ' TODO: do we allow widechars in keywords?
                        AdvanceChar(3)
                        Return MakeKeyword(SyntaxKind.EndKeyword, "End", precedingTrivia)
                    Else
                        Return ScanIdentifierOrKeyword(precedingTrivia)
                    End If

                Case "I"c
                    If NextAre(1, "f ") Then
                        ' TODO: do we allow widechars in keywords?
                        AdvanceChar(2)
                        Return MakeKeyword(SyntaxKind.IfKeyword, "If", precedingTrivia)
                    Else
                        Return ScanIdentifierOrKeyword(precedingTrivia)
                    End If

                Case "a"c, "b"c, "c"c, "d"c, "e"c, "f"c, "g"c, "h"c, "i"c, "j"c, "k"c, "l"c, "m"c,
                     "n"c, "o"c, "p"c, "q"c, "r"c, "s"c, "t"c, "u"c, "v"c, "w"c, "x"c, "y"c, "z"c
                    Return ScanIdentifierOrKeyword(precedingTrivia)

                Case "B"c, "C"c, "D"c, "F"c, "G"c, "H"c, "J"c, "K"c, "L"c, "M"c, "N"c, "O"c, "P"c, "Q"c,
                      "R"c, "S"c, "T"c, "U"c, "V"c, "W"c, "X"c, "Y"c, "Z"c
                    Return ScanIdentifierOrKeyword(precedingTrivia)

                Case "_"c
                    If CanGet(1) AndAlso IsIdentifierPartCharacter(Peek(1)) Then
                        Return ScanIdentifierOrKeyword(precedingTrivia)
                    End If

                    Dim err As ERRID = ERRID.ERR_ExpectedIdentifier
                    Dim len = GetWhitespaceLength(1)
                    If Not CanGet(len) OrElse IsNewLine(Peek(len)) OrElse PeekStartComment(len) > 0 Then
                        err = ERRID.ERR_LineContWithCommentOrNoPrecSpace
                    End If

                    ' not a line continuation and cannot start identifier.
                    Return MakeBadToken(precedingTrivia, 1, err)

                Case "["c
                    Return ScanBracketedIdentifier(precedingTrivia)

                Case "?"c
                    Return MakeQuestionToken(precedingTrivia, fullWidth)

                Case "%"c
                    If NextIs(1, ">"c) Then
                        Return XmlMakeEndEmbeddedToken(precedingTrivia, _scanSingleLineTriviaFunc)
                    End If

                Case "$"c, FULLWIDTH_DOLLAR_SIGN
                    If Not fullWidth AndAlso CanGet(1) AndAlso IsDoubleQuote(Peek(1)) Then
                        Return MakePunctuationToken(precedingTrivia, 2, SyntaxKind.DollarSignDoubleQuoteToken)
                    End If

            End Select
            If IsIdentifierStartCharacter(ch) Then
                Return ScanIdentifierOrKeyword(precedingTrivia)
            End If
            Debug.Assert(Not IsNewLine(ch))
            If fullWidth Then
                Debug.Assert(Not IsDoubleQuote(ch))
                Return Nothing
            End If
            If IsDoubleQuote(ch) Then
                Return ScanStringLiteral(precedingTrivia)
            End If
            If IsFullWidth(ch) Then
                ch = MakeHalfWidth(ch)
                Return ScanTokenFullWidth(precedingTrivia, ch)
            End If
            Return Nothing
        End Function

        ' at this point it is very likely that we are located at the beginning of a token
        Private Function TryScanToken(precedingTrivia As SyntaxList(Of VisualBasicSyntaxNode)) As SyntaxToken
            If CanGet() Then
                Return ScanTokenCommon(precedingTrivia, Peek(), False)
            End If
            Return MakeEofToken(precedingTrivia)
        End Function

        Private Function ScanTokenFullWidth(precedingTrivia As SyntaxList(Of VisualBasicSyntaxNode), ch As Char) As SyntaxToken
            Return ScanTokenCommon(precedingTrivia, ch, True)
        End Function

        ' // Allow whitespace between the characters of a two-character token.
        Private Function TrySkipFollowingEquals(ByRef Index As Integer) As Boolean
            Debug.Assert(Index > 0)
            Debug.Assert(CanGet(Index - 1))

            Dim Here = Index
            Dim eq As Char

            While CanGet(Here)
                eq = Peek(Here)
                Here += 1
                If Not IsWhitespace(eq) Then
                    If eq = "="c OrElse eq = FULLWIDTH_EQUALS_SIGN Then
                        Index = Here
                        Return True
                    Else
                        Return False
                    End If
                End If
            End While
            Return False
        End Function

        Private Function ScanRightAngleBracket(precedingTrivia As SyntaxList(Of VisualBasicSyntaxNode), charIsFullWidth As Boolean) As SyntaxToken
            Debug.Assert(CanGet)  ' >
            Debug.Assert(Peek() = ">"c OrElse Peek() = FULLWIDTH_GREATER_THAN_SIGN)

            Dim length As Integer = 1

            ' // Allow whitespace between the characters of a two-character token.
            length = GetWhitespaceLength(length)

            If CanGet(length) Then
                Dim c As Char = Peek(length)

                If c = "="c OrElse c = FULLWIDTH_EQUALS_SIGN Then
                    length += 1
                    Return MakeGreaterThanEqualsToken(precedingTrivia, length)
                ElseIf c = ">"c OrElse c = FULLWIDTH_GREATER_THAN_SIGN Then
                    length += 1
                    If TrySkipFollowingEquals(length) Then
                        Return MakeGreaterThanGreaterThanEqualsToken(precedingTrivia, length)
                    Else
                        Return MakeGreaterThanGreaterThanToken(precedingTrivia, length)
                    End If
                End If
            End If
            Return MakeGreaterThanToken(precedingTrivia, charIsFullWidth)
        End Function

        Private Function ScanLeftAngleBracket(precedingTrivia As SyntaxList(Of VisualBasicSyntaxNode), charIsFullWidth As Boolean, scanTrailingTrivia As ScanTriviaFunc) As SyntaxToken
            Debug.Assert(CanGet)  ' <
            Debug.Assert(Peek() = "<"c OrElse Peek() = FULLWIDTH_LESS_THAN_SIGN)

            Dim length As Integer = 1

            ' Check for XML tokens
            If Not charIsFullWidth AndAlso CanGet(length) Then
                Dim c As Char = Peek(length)
                Select Case c
                    Case "!"c
                        If CanGet(length + 2) Then
                            Select Case (Peek(length + 1))
                                Case "-"c
                                    If CanGet(length + 3) AndAlso Peek(length + 2) = "-"c Then
                                        Return XmlMakeBeginCommentToken(precedingTrivia, scanTrailingTrivia)
                                    End If
                                Case "["c

                                    If NextAre(length + 2, "CDATA[") Then

                                        Return XmlMakeBeginCDataToken(precedingTrivia, scanTrailingTrivia)
                                    End If
                            End Select
                        End If
                    Case "?"c
                        Return XmlMakeBeginProcessingInstructionToken(precedingTrivia, scanTrailingTrivia)

                    Case "/"c
                        Return XmlMakeBeginEndElementToken(precedingTrivia, _scanSingleLineTriviaFunc)
                End Select
            End If

            ' // Allow whitespace between the characters of a two-character token.
            length = GetWhitespaceLength(length)

            If CanGet(length) Then
                Dim c As Char = Peek(length)

                If c = "="c OrElse c = FULLWIDTH_EQUALS_SIGN Then
                    length += 1
                    Return MakeLessThanEqualsToken(precedingTrivia, length)
                ElseIf c = ">"c OrElse c = FULLWIDTH_GREATER_THAN_SIGN Then
                    length += 1
                    Return MakeLessThanGreaterThanToken(precedingTrivia, length)
                ElseIf c = "<"c OrElse c = FULLWIDTH_LESS_THAN_SIGN Then
                    length += 1

                    If CanGet(length) Then
                        c = Peek(length)

                        'if the second "<" is a part of "<%" - like in "<<%" , we do not want to use it.
                        If c <> "%"c AndAlso c <> FULLWIDTH_PERCENT_SIGN Then
                            If TrySkipFollowingEquals(length) Then
                                Return MakeLessThanLessThanEqualsToken(precedingTrivia, length)
                            Else
                                Return MakeLessThanLessThanToken(precedingTrivia, length)
                            End If
                        End If
                    End If
                End If
            End If

            Return MakeLessThanToken(precedingTrivia, charIsFullWidth)
        End Function

        ''' <remarks>
        ''' Not intended for use in Expression Compiler scenarios.
        ''' </remarks>
        Friend Shared Function IsIdentifier(spelling As String) As Boolean
            Dim spellingLength As Integer = spelling.Length
            If spellingLength = 0 Then
                Return False
            End If

            Dim c = spelling(0)
            If SyntaxFacts.IsIdentifierStartCharacter(c) Then
                '  SPEC: ... Visual Basic identifiers conform to the Unicode Standard Annex 15 with one
                '  SPEC:     exception: identifiers may begin with an underscore (connector) character.
                '  SPEC:     If an identifier begins with an underscore, it must contain at least one other
                '  SPEC:     valid identifier character to disambiguate it from a line continuation.
                If IsConnectorPunctuation(c) AndAlso spellingLength = 1 Then
                    Return False
                End If

                For i = 1 To spellingLength - 1
                    If Not IsIdentifierPartCharacter(spelling(i)) Then
                        Return False
                    End If
                Next
            End If

            Return True
        End Function

        Private Function ScanIdentifierOrKeyword(precedingTrivia As SyntaxList(Of VisualBasicSyntaxNode)) As SyntaxToken
            Debug.Assert(CanGet)
            Debug.Assert(IsIdentifierStartCharacter(Peek))
            Debug.Assert(PeekStartComment(0) = 0) ' comment should be handled by caller

            Dim ch = Peek()
            If CanGet(1) Then
                Dim ch1 = Peek(1)
                If IsConnectorPunctuation(ch) AndAlso Not IsIdentifierPartCharacter(ch1) Then
                    Return MakeBadToken(precedingTrivia, 1, ERRID.ERR_ExpectedIdentifier)
                End If
            End If

            Dim len = 1 ' we know that the first char was good

            ' // The C++ compiler refuses to inline IsIdentifierCharacter, so the
            ' // < 128 test is inline here. (This loop gets a *lot* of traffic.)
            ' TODO: make sure we get good perf here
            While CanGet(len)
                ch = Peek(len)

                Dim code = Convert.ToUInt16(ch)
                If code < 128US AndAlso IsNarrowIdentifierCharacter(code) OrElse
                    IsWideIdentifierCharacter(ch) Then

                    len += 1
                Else
                    Exit While
                End If
            End While

            'Check for a type character
            Dim TypeCharacter As TypeCharacter = TypeCharacter.None
            If CanGet(len) Then
                ch = Peek(len)

FullWidthRepeat:
                Select Case ch
                    Case "!"c
                        ' // If the ! is followed by an identifier it is a dictionary lookup operator, not a type character.
                        If CanGet(len + 1) Then
                            Dim NextChar As Char = Peek(len + 1)

                            If IsIdentifierStartCharacter(NextChar) OrElse
                                MatchOneOrAnotherOrFullwidth(NextChar, "["c, "]"c) Then
                                Exit Select
                            End If
                        End If
                        TypeCharacter = TypeCharacter.Single  'typeChars.chType_sR4
                        len += 1

                    Case "#"c
                        TypeCharacter = TypeCharacter.Double ' typeChars.chType_sR8
                        len += 1

                    Case "$"c
                        TypeCharacter = TypeCharacter.String 'typeChars.chType_String
                        len += 1

                    Case "%"c
                        TypeCharacter = TypeCharacter.Integer ' typeChars.chType_sI4
                        len += 1

                    Case "&"c
                        TypeCharacter = TypeCharacter.Long 'typeChars.chType_sI8
                        len += 1

                    Case "@"c
                        TypeCharacter = TypeCharacter.Decimal 'chType_sDecimal
                        len += 1

                    Case Else
                        If IsFullWidth(ch) Then
                            ch = MakeHalfWidth(ch)
                            GoTo FullWidthRepeat
                        End If
                End Select
            End If

            Dim tokenType As SyntaxKind = SyntaxKind.IdentifierToken
            Dim contextualKind As SyntaxKind = SyntaxKind.IdentifierToken
            Dim spelling = GetText(len)

            Dim BaseSpelling = If(TypeCharacter = TypeCharacter.None,
                                   spelling,
                                   Intern(spelling, 0, len - 1))

            ' this can be keyword only if it has no type character, or if it is Mid$
            If TypeCharacter = TypeCharacter.None Then
                tokenType = TokenOfStringCached(spelling)
                If SyntaxFacts.IsContextualKeyword(tokenType) Then
                    contextualKind = tokenType
                    tokenType = SyntaxKind.IdentifierToken
                End If
            ElseIf TokenOfStringCached(BaseSpelling) = SyntaxKind.MidKeyword Then

                contextualKind = SyntaxKind.MidKeyword
                tokenType = SyntaxKind.IdentifierToken
            End If

            If tokenType <> SyntaxKind.IdentifierToken Then
                ' KEYWORD
                Return MakeKeyword(tokenType, spelling, precedingTrivia)
            Else
                ' IDENTIFIER or CONTEXTUAL
                Dim id As SyntaxToken = MakeIdentifier(spelling, contextualKind, False, BaseSpelling, TypeCharacter, precedingTrivia)
                Return id
            End If
        End Function

        Private Function TokenOfStringCached(spelling As String) As SyntaxKind
            If spelling.Length > 16 Then
                Return SyntaxKind.IdentifierToken
            End If

            Return _KeywordsObjs.GetOrMakeValue(spelling)
        End Function

        Private Function ScanBracketedIdentifier(precedingTrivia As SyntaxList(Of VisualBasicSyntaxNode)) As SyntaxToken
            Debug.Assert(CanGet)  ' [
            Debug.Assert(Peek() = "["c OrElse Peek() = FULLWIDTH_LEFT_SQUARE_BRACKET)

            Dim IdStart As Integer = 1
            Dim Here As Integer = IdStart

            Dim InvalidIdentifier As Boolean = False

            If Not CanGet(Here) Then
                Return MakeBadToken(precedingTrivia, Here, ERRID.ERR_MissingEndBrack)
            End If

            Dim ch = Peek(Here)

            ' check if we can start an ident.
            If Not IsIdentifierStartCharacter(ch) OrElse
                (IsConnectorPunctuation(ch) AndAlso
                    Not (CanGet(Here + 1) AndAlso
                         IsIdentifierPartCharacter(Peek(Here + 1)))) Then

                InvalidIdentifier = True
            End If

            ' check ident until ]
            While CanGet(Here)
                Dim [Next] As Char = Peek(Here)

                If [Next] = "]"c OrElse [Next] = FULLWIDTH_RIGHT_SQUARE_BRACKET Then
                    Dim IdStringLength As Integer = Here - IdStart

                    If IdStringLength > 0 AndAlso Not InvalidIdentifier Then
                        Dim spelling = GetText(IdStringLength + 2)
                        ' TODO: this should be provable?
                        Debug.Assert(spelling.Length > IdStringLength + 1)

                        ' TODO: consider interning.
                        Dim baseText = spelling.Substring(1, IdStringLength)
                        Dim id As SyntaxToken = MakeIdentifier(
                            spelling,
                            SyntaxKind.IdentifierToken,
                            True,
                            baseText,
                            TypeCharacter.None,
                            precedingTrivia)
                        Return id
                    Else
                        ' // The sequence "[]" does not define a valid identifier.
                        Return MakeBadToken(precedingTrivia, Here + 1, ERRID.ERR_ExpectedIdentifier)
                    End If
                ElseIf IsNewLine([Next]) Then
                    Exit While
                ElseIf Not IsIdentifierPartCharacter([Next]) Then
                    InvalidIdentifier = True
                    Exit While
                End If

                Here += 1
            End While

            If Here > 1 Then
                Return MakeBadToken(precedingTrivia, Here, ERRID.ERR_MissingEndBrack)
            Else
                Return MakeBadToken(precedingTrivia, Here, ERRID.ERR_ExpectedIdentifier)
            End If
        End Function

        Private Enum NumericLiteralKind
            Integral
            Float
            [Decimal]
        End Enum

        Private Function ScanNumericLiteral(precedingTrivia As SyntaxList(Of VisualBasicSyntaxNode)) As SyntaxToken
            Debug.Assert(CanGet)

            Dim Here As Integer = 0
            Dim IntegerLiteralStart As Integer
            Dim UnderscoreInWrongPlace As Boolean
            Dim UnderscoreUsed As Boolean = False

            Dim Base As LiteralBase = LiteralBase.Decimal
            Dim literalKind As NumericLiteralKind = NumericLiteralKind.Integral

            ' ####################################################
            ' // Validate literal and find where the number starts and ends.
            ' ####################################################

            ' // First read a leading base specifier, if present, followed by a sequence of zero
            ' // or more digits.
            Dim ch = Peek()
            If ch = "&"c OrElse ch = FULLWIDTH_AMPERSAND Then
                Here += 1
                ch = If(CanGet(Here), Peek(Here), ChrW(0))

FullWidthRepeat:
                Select Case ch
                    Case "H"c, "h"c
                        Here += 1
                        IntegerLiteralStart = Here
                        Base = LiteralBase.Hexadecimal

                        UnderscoreInWrongPlace = (CanGet(Here) AndAlso Peek(Here) = "_"c)
                        While CanGet(Here)
                            ch = Peek(Here)
                            If Not IsHexDigit(ch) AndAlso ch <> "_"c Then
                                Exit While
                            End If
                            If ch = "_"c Then
                                UnderscoreUsed = True
                            End If
                            Here += 1
                        End While
                        UnderscoreInWrongPlace = UnderscoreInWrongPlace Or (Peek(Here - 1) = "_"c)

                    Case "B"c, "b"c
                        Here += 1
                        IntegerLiteralStart = Here
                        Base = LiteralBase.Binary

                        UnderscoreInWrongPlace = (CanGet(Here) AndAlso Peek(Here) = "_"c)
                        While CanGet(Here)
                            ch = Peek(Here)
                            If Not IsBinaryDigit(ch) AndAlso ch <> "_"c Then
                                Exit While
                            End If
                            If ch = "_"c Then
                                UnderscoreUsed = True
                            End If
                            Here += 1
                        End While
                        UnderscoreInWrongPlace = UnderscoreInWrongPlace Or (Peek(Here - 1) = "_"c)

                    Case "O"c, "o"c
                        Here += 1
                        IntegerLiteralStart = Here
                        Base = LiteralBase.Octal

                        UnderscoreInWrongPlace = (CanGet(Here) AndAlso Peek(Here) = "_"c)
                        While CanGet(Here)
                            ch = Peek(Here)
                            If Not IsOctalDigit(ch) AndAlso ch <> "_"c Then
                                Exit While
                            End If
                            If ch = "_"c Then
                                UnderscoreUsed = True
                            End If
                            Here += 1
                        End While
                        UnderscoreInWrongPlace = UnderscoreInWrongPlace Or (Peek(Here - 1) = "_"c)

                    Case Else
                        If IsFullWidth(ch) Then
                            ch = MakeHalfWidth(ch)
                            GoTo FullWidthRepeat
                        End If

                        Throw ExceptionUtilities.UnexpectedValue(ch)
                End Select
            Else
                ' no base specifier - just go through decimal digits.
                IntegerLiteralStart = Here
                UnderscoreInWrongPlace = (CanGet(Here) AndAlso Peek(Here) = "_"c)
                While CanGet(Here)
                    ch = Peek(Here)
                    If Not IsDecimalDigit(ch) AndAlso ch <> "_"c Then
                        Exit While
                    End If
                    If ch = "_"c Then
                        UnderscoreUsed = True
                    End If
                    Here += 1
                End While
                If Here <> IntegerLiteralStart Then
                    UnderscoreInWrongPlace = UnderscoreInWrongPlace Or (Peek(Here - 1) = "_"c)
                End If
            End If

            ' we may have a dot, and then it is a float, but if this is an integral, then we have seen it all.
            Dim IntegerLiteralEnd As Integer = Here

            ' // Unless there was an explicit base specifier (which indicates an integer literal),
            ' // read the rest of a float literal.
            If Base = LiteralBase.Decimal AndAlso CanGet(Here) Then
                ' // First read a '.' followed by a sequence of one or more digits.
                ch = Peek(Here)
                If (ch = "."c Or ch = FULLWIDTH_FULL_STOP) AndAlso
                        CanGet(Here + 1) AndAlso
                        IsDecimalDigit(Peek(Here + 1)) Then

                    Here += 2   ' skip dot and first digit

                    ' all following decimal digits belong to the literal (fractional part)
                    While CanGet(Here)
                        ch = Peek(Here)
                        If Not IsDecimalDigit(ch) AndAlso ch <> "_"c Then
                            Exit While
                        End If
                        Here += 1
                    End While
                    UnderscoreInWrongPlace = UnderscoreInWrongPlace Or (Peek(Here - 1) = "_"c)
                    literalKind = NumericLiteralKind.Float
                End If

                ' // Read an exponent symbol followed by an optional sign and a sequence of
                ' // one or more digits.
                If CanGet(Here) AndAlso BeginsExponent(Peek(Here)) Then
                    Here += 1

                    If CanGet(Here) Then
                        ch = Peek(Here)

                        If MatchOneOrAnotherOrFullwidth(ch, "+"c, "-"c) Then
                            Here += 1
                        End If
                    End If

                    If CanGet(Here) AndAlso IsDecimalDigit(Peek(Here)) Then
                        Here += 1
                        While CanGet(Here)
                            ch = Peek(Here)
                            If Not IsDecimalDigit(ch) AndAlso ch <> "_"c Then
                                Exit While
                            End If
                            Here += 1
                        End While
                        UnderscoreInWrongPlace = UnderscoreInWrongPlace Or (Peek(Here - 1) = "_"c)
                    Else
                        Return MakeBadToken(precedingTrivia, Here, ERRID.ERR_InvalidLiteralExponent)
                    End If

                    literalKind = NumericLiteralKind.Float
                End If
            End If

            Dim literalWithoutTypeChar = Here

            ' ####################################################
            ' // Read a trailing type character.
            ' ####################################################

            Dim TypeCharacter As TypeCharacter = TypeCharacter.None

            If CanGet(Here) Then
                ch = Peek(Here)

FullWidthRepeat2:
                Select Case ch
                    Case "!"c
                        If Base = LiteralBase.Decimal Then
                            TypeCharacter = TypeCharacter.Single
                            literalKind = NumericLiteralKind.Float
                            Here += 1
                        End If

                    Case "F"c, "f"c
                        If Base = LiteralBase.Decimal Then
                            TypeCharacter = TypeCharacter.SingleLiteral
                            literalKind = NumericLiteralKind.Float
                            Here += 1
                        End If

                    Case "#"c
                        If Base = LiteralBase.Decimal Then
                            TypeCharacter = TypeCharacter.Double
                            literalKind = NumericLiteralKind.Float
                            Here += 1
                        End If

                    Case "R"c, "r"c
                        If Base = LiteralBase.Decimal Then
                            TypeCharacter = TypeCharacter.DoubleLiteral
                            literalKind = NumericLiteralKind.Float
                            Here += 1
                        End If

                    Case "S"c, "s"c

                        If literalKind <> NumericLiteralKind.Float Then
                            TypeCharacter = TypeCharacter.ShortLiteral
                            Here += 1
                        End If

                    Case "%"c
                        If literalKind <> NumericLiteralKind.Float Then
                            TypeCharacter = TypeCharacter.Integer
                            Here += 1
                        End If

                    Case "I"c, "i"c
                        If literalKind <> NumericLiteralKind.Float Then
                            TypeCharacter = TypeCharacter.IntegerLiteral
                            Here += 1
                        End If

                    Case "&"c
                        If literalKind <> NumericLiteralKind.Float Then
                            TypeCharacter = TypeCharacter.Long
                            Here += 1
                        End If

                    Case "L"c, "l"c
                        If literalKind <> NumericLiteralKind.Float Then
                            TypeCharacter = TypeCharacter.LongLiteral
                            Here += 1
                        End If

                    Case "@"c
                        If Base = LiteralBase.Decimal Then
                            TypeCharacter = TypeCharacter.Decimal
                            literalKind = NumericLiteralKind.Decimal
                            Here += 1
                        End If

                    Case "D"c, "d"c
                        If Base = LiteralBase.Decimal Then
                            TypeCharacter = TypeCharacter.DecimalLiteral
                            literalKind = NumericLiteralKind.Decimal

                            ' check if this was not attempt to use obsolete exponent
                            If CanGet(Here + 1) Then
                                ch = Peek(Here + 1)

                                If IsDecimalDigit(ch) OrElse MatchOneOrAnotherOrFullwidth(ch, "+"c, "-"c) Then
                                    Return MakeBadToken(precedingTrivia, Here, ERRID.ERR_ObsoleteExponent)
                                End If
                            End If

                            Here += 1
                        End If

                    Case "U"c, "u"c
                        If literalKind <> NumericLiteralKind.Float AndAlso CanGet(Here + 1) Then
                            Dim NextChar As Char = Peek(Here + 1)

                            'unsigned suffixes - US, UL, UI
                            If MatchOneOrAnotherOrFullwidth(NextChar, "S"c, "s"c) Then
                                TypeCharacter = TypeCharacter.UShortLiteral
                                Here += 2
                            ElseIf MatchOneOrAnotherOrFullwidth(NextChar, "I"c, "i"c) Then
                                TypeCharacter = TypeCharacter.UIntegerLiteral
                                Here += 2
                            ElseIf MatchOneOrAnotherOrFullwidth(NextChar, "L"c, "l"c) Then
                                TypeCharacter = TypeCharacter.ULongLiteral
                                Here += 2
                            End If
                        End If

                    Case Else
                        If IsFullWidth(ch) Then
                            ch = MakeHalfWidth(ch)
                            GoTo FullWidthRepeat2
                        End If
                End Select
            End If

            ' ####################################################
            ' //  Produce a value for the literal.
            ' ####################################################

            Dim IntegralValue As UInt64
            Dim FloatingValue As Double
            Dim DecimalValue As Decimal
            Dim Overflows As Boolean = False

            If literalKind = NumericLiteralKind.Integral Then
                If IntegerLiteralStart = IntegerLiteralEnd Then
                    Return MakeBadToken(precedingTrivia, Here, ERRID.ERR_Syntax)
                Else
                    IntegralValue = 0

                    If Base = LiteralBase.Decimal Then
                        ' Init For loop
                        For LiteralCharacter As Integer = IntegerLiteralStart To IntegerLiteralEnd - 1
                            Dim LiteralCharacterValue As Char = Peek(LiteralCharacter)
                            If LiteralCharacterValue = "_"c Then
                                Continue For
                            End If
                            Dim NextCharacterValue As UInteger = IntegralLiteralCharacterValue(LiteralCharacterValue)

                            If IntegralValue < 1844674407370955161UL OrElse
                              (IntegralValue = 1844674407370955161UL AndAlso NextCharacterValue <= 5UI) Then

                                IntegralValue = (IntegralValue * 10UL) + NextCharacterValue
                            Else
                                Overflows = True
                                Exit For
                            End If
                        Next

                        If TypeCharacter <> TypeCharacter.ULongLiteral AndAlso IntegralValue > Long.MaxValue Then
                            Overflows = True
                        End If
                    Else
                        Dim Shift As Integer = If(Base = LiteralBase.Hexadecimal, 4, If(Base = LiteralBase.Octal, 3, 1))
                        Dim OverflowMask As UInt64 = If(Base = LiteralBase.Hexadecimal, &HF000000000000000UL, If(Base = LiteralBase.Octal, &HE000000000000000UL, &H8000000000000000UL))

                        ' Init For loop
                        For LiteralCharacter As Integer = IntegerLiteralStart To IntegerLiteralEnd - 1
                            Dim LiteralCharacterValue As Char = Peek(LiteralCharacter)
                            If LiteralCharacterValue = "_"c Then
                                Continue For
                            End If

                            If (IntegralValue And OverflowMask) <> 0 Then
                                Overflows = True
                            End If

                            IntegralValue = (IntegralValue << Shift) + IntegralLiteralCharacterValue(LiteralCharacterValue)
                        Next
                    End If

                    If TypeCharacter = TypeCharacter.None Then
                        ' nothing to do
                    ElseIf TypeCharacter = TypeCharacter.Integer OrElse TypeCharacter = TypeCharacter.IntegerLiteral Then
                        If (Base = LiteralBase.Decimal AndAlso IntegralValue > &H7FFFFFFF) OrElse
                            IntegralValue > &HFFFFFFFFUI Then

                            Overflows = True
                        End If

                    ElseIf TypeCharacter = TypeCharacter.UIntegerLiteral Then
                        If IntegralValue > &HFFFFFFFFUI Then
                            Overflows = True
                        End If

                    ElseIf TypeCharacter = TypeCharacter.ShortLiteral Then
                        If (Base = LiteralBase.Decimal AndAlso IntegralValue > &H7FFF) OrElse
                            IntegralValue > &HFFFF Then

                            Overflows = True
                        End If

                    ElseIf TypeCharacter = TypeCharacter.UShortLiteral Then
                        If IntegralValue > &HFFFF Then
                            Overflows = True
                        End If

                    Else
                        Debug.Assert(TypeCharacter = TypeCharacter.Long OrElse
                                 TypeCharacter = TypeCharacter.LongLiteral OrElse
                                 TypeCharacter = TypeCharacter.ULongLiteral,
                        "Integral literal value computation is lost.")
                    End If
                End If

            Else
                ' // Copy the text of the literal to deal with fullwidth
                Dim scratch = GetScratch()
                For i = 0 To literalWithoutTypeChar - 1
                    Dim curCh = Peek(i)
                    If curCh <> "_"c Then
                        scratch.Append(If(IsFullWidth(curCh), MakeHalfWidth(curCh), curCh))
                    End If
                Next
                Dim LiteralSpelling = GetScratchTextInterned(scratch)

                If literalKind = NumericLiteralKind.Decimal Then
                    ' Attempt to convert to Decimal.
                    Overflows = Not GetDecimalValue(LiteralSpelling, DecimalValue)
                Else
                    If TypeCharacter = TypeCharacter.Single OrElse TypeCharacter = TypeCharacter.SingleLiteral Then
                        ' // Attempt to convert to single
                        Dim SingleValue As Single
                        If Not RealParser.TryParseFloat(LiteralSpelling, SingleValue) Then
                            Overflows = True
                        Else
                            FloatingValue = SingleValue
                        End If
                    Else
                        ' // Attempt to convert to double.
                        If Not RealParser.TryParseDouble(LiteralSpelling, FloatingValue) Then
                            Overflows = True
                        End If
                    End If
                End If
            End If

            Dim result As SyntaxToken
            Select Case literalKind
                Case NumericLiteralKind.Integral
                    result = MakeIntegerLiteralToken(precedingTrivia, Base, TypeCharacter, If(Overflows Or UnderscoreInWrongPlace, 0UL, IntegralValue), Here)
                Case NumericLiteralKind.Float
                    result = MakeFloatingLiteralToken(precedingTrivia, TypeCharacter, If(Overflows Or UnderscoreInWrongPlace, 0.0F, FloatingValue), Here)
                Case NumericLiteralKind.Decimal
                    result = MakeDecimalLiteralToken(precedingTrivia, TypeCharacter, If(Overflows Or UnderscoreInWrongPlace, 0D, DecimalValue), Here)
                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(literalKind)
            End Select

            If Overflows Then
                result = DirectCast(result.AddError(ErrorFactory.ErrorInfo(ERRID.ERR_Overflow)), SyntaxToken)
            ElseIf UnderscoreInWrongPlace Then
                result = DirectCast(result.AddError(ErrorFactory.ErrorInfo(ERRID.ERR_Syntax)), SyntaxToken)
            End If

            If UnderscoreUsed Then
                result = CheckFeatureAvailability(result, Feature.DigitSeparators)
            End If
            If Base = LiteralBase.Binary Then
                result = CheckFeatureAvailability(result, Feature.BinaryLiterals)
            End If

            Return result
        End Function

        Private Shared Function GetDecimalValue(text As String, <Out()> ByRef value As Decimal) As Boolean

            ' Use Decimal.TryParse to parse value. Note: the behavior of
            ' Decimal.TryParse differs from Dev11 in the following cases:
            '
            ' 1. [-]0eNd where N > 0
            '     The native compiler ignores sign and scale and treats such cases
            '     as 0e0d. Decimal.TryParse fails so these cases are compile errors.
            '     [Bug #568475]
            ' 2. Decimals with significant digits below 1e-49
            '     The native compiler considers digits below 1e-49 when rounding.
            '     Decimal.TryParse ignores digits below 1e-49 when rounding. This
            '     difference is perhaps the most significant since existing code will
            '     continue to compile but constant values may be rounded differently.
            '     [Bug #568494]

            Return Decimal.TryParse(text, NumberStyles.AllowDecimalPoint Or NumberStyles.AllowExponent, CultureInfo.InvariantCulture, value)
        End Function

        Private Function ScanIntLiteral(
               ByRef ReturnValue As Integer,
               ByRef Here As Integer
           ) As Boolean
            Debug.Assert(Here >= 0)

            If Not CanGet(Here) Then
                Return False
            End If

            Dim ch = Peek(Here)
            If Not IsDecimalDigit(ch) Then
                Return False
            End If

            Dim IntegralValue As Integer = IntegralLiteralCharacterValue(ch)
            Here += 1

            While CanGet(Here)
                ch = Peek(Here)

                If Not IsDecimalDigit(ch) Then
                    Exit While
                End If

                Dim nextDigit = IntegralLiteralCharacterValue(ch)
                If IntegralValue < 214748364 OrElse
                    (IntegralValue = 214748364 AndAlso nextDigit < 8) Then

                    IntegralValue = IntegralValue * 10 + nextDigit
                    Here += 1
                Else
                    Return False
                End If
            End While

            ReturnValue = IntegralValue
            Return True
        End Function

        Private Function ScanDateLiteral(precedingTrivia As SyntaxList(Of VisualBasicSyntaxNode)) As SyntaxToken
            Debug.Assert(CanGet)
            Debug.Assert(IsHash(Peek()))

            Dim Here As Integer = 1 'skip #
            Dim FirstValue As Integer
            Dim YearValue, MonthValue, DayValue, HourValue, MinuteValue, SecondValue As Integer
            Dim HaveDateValue As Boolean = False
            Dim HaveYearValue As Boolean = False
            Dim HaveTimeValue As Boolean = False
            Dim HaveMinuteValue As Boolean = False
            Dim HaveSecondValue As Boolean = False
            Dim HaveAM As Boolean = False
            Dim HavePM As Boolean = False
            Dim DateIsInvalid As Boolean = False
            Dim YearIsTwoDigits As Boolean = False
            Dim DaysToMonth As Integer() = Nothing
            Dim yearIsFirst As Boolean = False

            ' // Unfortunately, we can't fall back on OLE Automation's date parsing because
            ' // they don't have the same range as the URT's DateTime class

            ' // First, eat any whitespace
            Here = GetWhitespaceLength(Here)

            Dim FirstValueStart As Integer = Here

            ' // The first thing has to be an integer, although it's not clear what it is yet
            If Not ScanIntLiteral(FirstValue, Here) Then
                Return Nothing

            End If

            ' // If we see a /, then it's a date

            If CanGet(Here) AndAlso IsDateSeparatorCharacter(Peek(Here)) Then
                Dim FirstDateSeparator As Integer = Here

                ' // We've got a date
                HaveDateValue = True
                Here += 1

                ' Is the first value a year?
                ' It is a year if it consists of exactly 4 digits.
                ' Condition below uses 5 because we already skipped the separator.
                If Here - FirstValueStart = 5 Then
                    HaveYearValue = True
                    yearIsFirst = True
                    YearValue = FirstValue

                    ' // We have to have a month value
                    If Not ScanIntLiteral(MonthValue, Here) Then
                        GoTo baddate
                    End If

                    ' Do we have a day value?
                    If CanGet(Here) AndAlso IsDateSeparatorCharacter(Peek(Here)) Then
                        ' // Check to see they used a consistent separator

                        If Peek(Here) <> Peek(FirstDateSeparator) Then
                            GoTo baddate
                        End If

                        ' // Yes.
                        Here += 1

                        If Not ScanIntLiteral(DayValue, Here) Then
                            GoTo baddate
                        End If
                    End If
                Else
                    ' First value is month
                    MonthValue = FirstValue

                    ' // We have to have a day value

                    If Not ScanIntLiteral(DayValue, Here) Then
                        GoTo baddate
                    End If

                    ' // Do we have a year value?

                    If CanGet(Here) AndAlso IsDateSeparatorCharacter(Peek(Here)) Then
                        ' // Check to see they used a consistent separator

                        If Peek(Here) <> Peek(FirstDateSeparator) Then
                            GoTo baddate
                        End If

                        ' // Yes.
                        HaveYearValue = True
                        Here += 1

                        Dim YearStart As Integer = Here

                        If Not ScanIntLiteral(YearValue, Here) Then
                            GoTo baddate
                        End If

                        If (Here - YearStart) = 2 Then
                            YearIsTwoDigits = True
                        End If
                    End If
                End If

                Here = GetWhitespaceLength(Here)
            End If

            ' // If we haven't seen a date, assume it's a time value

            If Not HaveDateValue Then
                HaveTimeValue = True
                HourValue = FirstValue
            Else
                ' // We did see a date. See if we see a time value...

                If ScanIntLiteral(HourValue, Here) Then
                    ' // Yup.
                    HaveTimeValue = True
                End If
            End If

            If HaveTimeValue Then
                ' // Do we see a :?

                If CanGet(Here) AndAlso IsColon(Peek(Here)) Then
                    Here += 1

                    ' // Now let's get the minute value

                    If Not ScanIntLiteral(MinuteValue, Here) Then
                        GoTo baddate
                    End If

                    HaveMinuteValue = True

                    ' // Do we have a second value?

                    If CanGet(Here) AndAlso IsColon(Peek(Here)) Then
                        ' // Yes.
                        HaveSecondValue = True
                        Here += 1

                        If Not ScanIntLiteral(SecondValue, Here) Then
                            GoTo baddate
                        End If
                    End If
                End If

                Here = GetWhitespaceLength(Here)

                ' // Check AM/PM

                If CanGet(Here) Then
                    If Peek(Here) = "A"c OrElse Peek(Here) = FULLWIDTH_LATIN_CAPITAL_LETTER_A OrElse
                        Peek(Here) = "a"c OrElse Peek(Here) = FULLWIDTH_LATIN_SMALL_LETTER_A Then

                        HaveAM = True
                        Here += 1

                    ElseIf Peek(Here) = "P"c OrElse Peek(Here) = FULLWIDTH_LATIN_CAPITAL_LETTER_P OrElse
                           Peek(Here) = "p"c OrElse Peek(Here) = FULLWIDTH_LATIN_SMALL_LETTER_P Then

                        HavePM = True
                        Here += 1

                    End If

                    If CanGet(Here) AndAlso (HaveAM OrElse HavePM) Then
                        If Peek(Here) = "M"c OrElse Peek(Here) = FULLWIDTH_LATIN_CAPITAL_LETTER_M OrElse
                           Peek(Here) = "m"c OrElse Peek(Here) = FULLWIDTH_LATIN_SMALL_LETTER_M Then

                            Here = GetWhitespaceLength(Here + 1)

                        Else
                            GoTo baddate
                        End If
                    End If
                End If

                ' // If there's no minute/second value and no AM/PM, it's invalid

                If Not HaveMinuteValue AndAlso Not HaveAM AndAlso Not HavePM Then
                    GoTo baddate
                End If
            End If

            If Not CanGet(Here) OrElse Not IsHash(Peek(Here)) Then
                GoTo baddate
            End If

            Here += 1

            ' // OK, now we've got all the values, let's see if we've got a valid date
            If HaveDateValue Then
                If MonthValue < 1 OrElse MonthValue > 12 Then
                    DateIsInvalid = True
                End If

                ' // We'll check Days in a moment...

                If Not HaveYearValue Then
                    DateIsInvalid = True
                    YearValue = 1
                End If

                ' // Check if not a leap year

                If Not ((YearValue Mod 4 = 0) AndAlso (Not (YearValue Mod 100 = 0) OrElse (YearValue Mod 400 = 0))) Then
                    DaysToMonth = DaysToMonth365
                Else
                    DaysToMonth = DaysToMonth366
                End If

                If DayValue < 1 OrElse
                   (Not DateIsInvalid AndAlso DayValue > DaysToMonth(MonthValue) - DaysToMonth(MonthValue - 1)) Then

                    DateIsInvalid = True
                End If

                If YearIsTwoDigits Then
                    DateIsInvalid = True
                End If

                If YearValue < 1 OrElse YearValue > 9999 Then
                    DateIsInvalid = True
                End If

            Else
                MonthValue = 1
                DayValue = 1
                YearValue = 1
                DaysToMonth = DaysToMonth365
            End If

            If HaveTimeValue Then
                If HaveAM OrElse HavePM Then
                    ' // 12-hour value

                    If HourValue < 1 OrElse HourValue > 12 Then
                        DateIsInvalid = True
                    End If

                    If HaveAM Then
                        HourValue = HourValue Mod 12
                    ElseIf HavePM Then
                        HourValue = HourValue + 12

                        If HourValue = 24 Then
                            HourValue = 12
                        End If
                    End If

                Else
                    If HourValue < 0 OrElse HourValue > 23 Then
                        DateIsInvalid = True
                    End If
                End If

                If HaveMinuteValue Then
                    If MinuteValue < 0 OrElse MinuteValue > 59 Then
                        DateIsInvalid = True
                    End If
                Else
                    MinuteValue = 0
                End If

                If HaveSecondValue Then
                    If SecondValue < 0 OrElse SecondValue > 59 Then
                        DateIsInvalid = True
                    End If
                Else
                    SecondValue = 0
                End If
            Else
                HourValue = 0
                MinuteValue = 0
                SecondValue = 0
            End If

            ' // Ok, we've got a valid value. Now make into an i8.

            If Not DateIsInvalid Then
                Dim DateTimeValue As New DateTime(YearValue, MonthValue, DayValue, HourValue, MinuteValue, SecondValue)
                Dim result = MakeDateLiteralToken(precedingTrivia, DateTimeValue, Here)

                If yearIsFirst Then
                    result = Parser.CheckFeatureAvailability(Feature.YearFirstDateLiterals, result, Options.LanguageVersion)
                End If

                Return result
            Else
                Return MakeBadToken(precedingTrivia, Here, ERRID.ERR_InvalidDate)
            End If

baddate:
            ' // If we can find a closing #, then assume it's a malformed date,
            ' // otherwise, it's not a date

            While CanGet(Here)
                Dim ch As Char = Peek(Here)
                If IsHash(ch) OrElse IsNewLine(ch) Then
                    Exit While
                End If
                Here += 1
            End While

            If Not CanGet(Here) OrElse IsNewLine(Peek(Here)) Then
                ' // No closing #
                Return Nothing
            Else
                Debug.Assert(IsHash(Peek(Here)))
                Here += 1  ' consume trailing #
                Return MakeBadToken(precedingTrivia, Here, ERRID.ERR_InvalidDate)
            End If
        End Function

        Private Function ScanStringLiteral(precedingTrivia As SyntaxList(Of VisualBasicSyntaxNode)) As SyntaxToken
            Debug.Assert(CanGet)
            Debug.Assert(IsDoubleQuote(Peek))

            Dim length As Integer = 1
            Dim ch As Char
            Dim followingTrivia As SyntaxList(Of VisualBasicSyntaxNode)

            ' // Check for a Char literal, which can be of the form:
            ' // """"c or "<anycharacter-except-">"c

            If CanGet(3) AndAlso IsDoubleQuote(Peek(2)) Then
                If IsDoubleQuote(Peek(1)) Then
                    If IsDoubleQuote(Peek(3)) AndAlso
                       CanGet(4) AndAlso
                       IsLetterC(Peek(4)) Then

                        ' // Double-quote Char literal: """"c
                        Return MakeCharacterLiteralToken(precedingTrivia, """"c, 5)
                    End If

                ElseIf IsLetterC(Peek(3)) Then
                    ' // Char literal.  "x"c
                    Return MakeCharacterLiteralToken(precedingTrivia, Peek(1), 4)
                End If
            End If

            If CanGet(2) AndAlso
               IsDoubleQuote(Peek(1)) AndAlso
               IsLetterC(Peek(2)) Then

                ' // Error. ""c is not a legal char constant
                Return MakeBadToken(precedingTrivia, 3, ERRID.ERR_IllegalCharConstant)
            End If

            Dim haveNewLine As Boolean = False

            Dim scratch = GetScratch()
            While CanGet(length)
                ch = Peek(length)

                If IsDoubleQuote(ch) Then
                    If CanGet(length + 1) Then
                        ch = Peek(length + 1)

                        If IsDoubleQuote(ch) Then
                            ' // An escaped double quote
                            scratch.Append(""""c)
                            length += 2
                            Continue While
                        Else
                            ' // The end of the char literal.
                            If IsLetterC(ch) Then
                                ' // Error. "aad"c is not a legal char constant

                                ' // +2 to include both " and c in the token span
                                scratch.Clear()
                                Return MakeBadToken(precedingTrivia, length + 2, ERRID.ERR_IllegalCharConstant)
                            End If
                        End If
                    End If

                    ' the double quote was a valid string terminator.
                    length += 1
                    Dim spelling = GetTextNotInterned(length)
                    followingTrivia = ScanSingleLineTrivia()

                    ' NATURAL TEXT, NO INTERNING
                    Dim result As SyntaxToken = SyntaxFactory.StringLiteralToken(spelling, GetScratchText(scratch), precedingTrivia.Node, followingTrivia.Node)

                    If haveNewLine Then
                        result = Parser.CheckFeatureAvailability(Feature.MultilineStringLiterals, result, Options.LanguageVersion)
                    End If

                    Return result

                ElseIf IsNewLine(ch) Then
                    If _isScanningDirective Then
                        Exit While
                    End If

                    haveNewLine = True
                End If

                scratch.Append(ch)
                length += 1
            End While

            ' CC has trouble to prove this after the loop
            Debug.Assert(CanGet(length - 1))

            '// The literal does not have an explicit termination.
            ' DIFFERENT: here in IDE we used to report string token marked as unterminated

            Dim sp = GetTextNotInterned(length)
            followingTrivia = ScanSingleLineTrivia()
            Dim strTk = SyntaxFactory.StringLiteralToken(sp, GetScratchText(scratch), precedingTrivia.Node, followingTrivia.Node)
            Dim StrTkErr = strTk.SetDiagnostics({ErrorFactory.ErrorInfo(ERRID.ERR_UnterminatedStringLiteral)})

            Debug.Assert(StrTkErr IsNot Nothing)
            Return DirectCast(StrTkErr, SyntaxToken)
        End Function

        Friend Shared Function TryIdentifierAsContextualKeyword(id As IdentifierTokenSyntax, ByRef k As SyntaxKind) As Boolean
            Debug.Assert(id IsNot Nothing)

            If id.PossibleKeywordKind <> SyntaxKind.IdentifierToken Then
                k = id.PossibleKeywordKind
                Return True
            End If

            Return False
        End Function

        ''' <summary>
        ''' Try to convert an Identifier to a Keyword.  Called by the parser when it wants to force
        ''' an identifier to be a keyword.
        ''' </summary>
        Friend Function TryIdentifierAsContextualKeyword(id As IdentifierTokenSyntax, ByRef k As KeywordSyntax) As Boolean
            Debug.Assert(id IsNot Nothing)

            Dim kind As SyntaxKind = SyntaxKind.IdentifierToken
            If TryIdentifierAsContextualKeyword(id, kind) Then
                k = MakeKeyword(id)
                Return True
            End If

            Return False
        End Function

        Friend Function TryTokenAsContextualKeyword(t As SyntaxToken, ByRef k As KeywordSyntax) As Boolean
            If t Is Nothing Then
                Return False
            End If

            If t.Kind = SyntaxKind.IdentifierToken Then
                Return TryIdentifierAsContextualKeyword(DirectCast(t, IdentifierTokenSyntax), k)
            End If

            Return False
        End Function

        Friend Shared Function TryTokenAsKeyword(t As SyntaxToken, ByRef kind As SyntaxKind) As Boolean

            If t Is Nothing Then
                Return False
            End If

            If t.IsKeyword Then
                kind = t.Kind
                Return True
            End If

            If t.Kind = SyntaxKind.IdentifierToken Then
                Return TryIdentifierAsContextualKeyword(DirectCast(t, IdentifierTokenSyntax), kind)
            End If

            Return False
        End Function

        Friend Shared Function IsContextualKeyword(t As SyntaxToken, ParamArray kinds As SyntaxKind()) As Boolean
            Dim kind As SyntaxKind = Nothing
            If TryTokenAsKeyword(t, kind) Then
                Return Array.IndexOf(kinds, kind) >= 0
            End If
            Return False
        End Function

        Private Function IsIdentifierStartCharacter(c As Char) As Boolean
            Return (_isScanningForExpressionCompiler AndAlso c = "$"c) OrElse SyntaxFacts.IsIdentifierStartCharacter(c)
        End Function

        Private Function CheckFeatureAvailability(token As SyntaxToken, feature As Feature) As SyntaxToken
            If CheckFeatureAvailability(feature) Then
                Return token
            End If
            Dim errorInfo = ErrorFactory.ErrorInfo(ERRID.ERR_LanguageVersion, _options.LanguageVersion.GetErrorName(), ErrorFactory.ErrorInfo(feature.GetResourceId()))
            Return DirectCast(token.AddError(errorInfo), SyntaxToken)
        End Function

        Friend Function CheckFeatureAvailability(feature As Feature) As Boolean
            Return CheckFeatureAvailability(Me.Options, feature)
        End Function

        Private Shared Function CheckFeatureAvailability(parseOptions As VisualBasicParseOptions, feature As Feature) As Boolean
            Dim featureFlag = feature.GetFeatureFlag()
            If featureFlag IsNot Nothing Then
                Return parseOptions.Features.ContainsKey(featureFlag)
            End If

            Dim required = feature.GetLanguageVersion()
            Dim actual = parseOptions.LanguageVersion
            Return CInt(required) <= CInt(actual)
        End Function
    End Class
End Namespace
