' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

'-----------------------------------------------------------------------------
' Contains the definition of the Scanner, which produces tokens from text
'-----------------------------------------------------------------------------

Option Compare Binary
Option Strict On

Imports System.Collections.Immutable
Imports System.Globalization
Imports System.Runtime.InteropServices
Imports System.Text
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.SyntaxFacts
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.Syntax.InternalSyntax
Imports CoreInternalSyntax = Microsoft.CodeAnalysis.Syntax.InternalSyntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax

    ''' <summary>
    ''' Creates red tokens for a stream of text
    ''' </summary>
    Friend Class Scanner
        Implements IDisposable

        Private Delegate Function ScanTriviaFunc() As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode)

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

        Private Shared ReadOnly s_wslTablePool As New ObjectPool(Of CachingFactory(Of SyntaxListBuilder, CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode)))(
            Function() New CachingFactory(Of SyntaxListBuilder, CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode))(TABLE_LIMIT, s_wsListFactory, s_wsListKeyHasher, s_wsListKeyEquality))

        Private ReadOnly _wslTable As CachingFactory(Of SyntaxListBuilder, CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode)) = s_wslTablePool.Allocate

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
            Dim leadingTrivia As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode)

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

        Private Function ScanNextCharAsToken(leadingTrivia As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode)) As SyntaxToken
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
        Private Function ScanNewlineAsStatementTerminator(startCharacter As Char, precedingTrivia As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode)) As SyntaxToken
            If _lineBufferOffset < _endOfTerminatorTrivia Then
                Dim width = LengthOfLineBreak(startCharacter)
                Return MakeStatementTerminatorToken(precedingTrivia, width)
            Else
                Return MakeEmptyToken(precedingTrivia)
            End If
        End Function

        Private Function ScanColonAsStatementTerminator(precedingTrivia As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode), charIsFullWidth As Boolean) As SyntaxToken
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

        Private Function TryGet(num As Integer, ByRef ch As Char) As Boolean
            If CanGet(num) Then
                ch = Peek(num)
                Return True
            End If
            Return False
        End Function

        Private Function ScanLineContinuation(tList As SyntaxListBuilder) As Boolean
            Dim ch As Char = ChrW(0)
            If Not TryGet(0, ch) Then
                Return False
            End If

            If Not IsAfterWhitespace() Then
                Return False
            End If

            If Not IsUnderscore(ch) Then
                Return False
            End If

            Dim here = GetWhitespaceLength(1)
            TryGet(here, ch)

            Dim foundComment = IsSingleQuote(ch)
            Dim atNewLine As Boolean = IsNewLine(ch)
            If Not foundComment AndAlso Not atNewLine AndAlso CanGet(here) Then
                Return False
            End If

            tList.Add(MakeLineContinuationTrivia(GetText(1)))
            If here > 1 Then
                tList.Add(MakeWhiteSpaceTrivia(GetText(here - 1)))
            End If

            If foundComment Then
                Dim comment As SyntaxTrivia = ScanComment()
                If Not CheckFeatureAvailability(Feature.CommentsAfterLineContinuation) Then
                    comment = comment.WithDiagnostics({ErrorFactory.ErrorInfo(ERRID.ERR_CommentsAfterLineContinuationNotAvailable1,
                        New VisualBasicRequiredLanguageVersion(Feature.CommentsAfterLineContinuation.GetLanguageVersion()))})
                End If
                tList.Add(comment)
                ' Need to call CanGet here to prevent Peek reading past EndOfBuffer. This can happen when file ends with comment but no New Line.
                If CanGet() Then
                    ch = Peek()
                    atNewLine = IsNewLine(ch)
                Else
                    Debug.Assert(Not atNewLine)
                End If
            End If

            If atNewLine Then
                Dim newLine = SkipLineBreak(ch, 0)
                here = GetWhitespaceLength(newLine)
                Dim spaces = here - newLine
                Dim startComment = PeekStartComment(here)

                ' If the line following the line continuation is blank, or blank with a comment,
                ' do not include the new line character since that would confuse code handling
                ' implicit line continuations. (See Scanner::EatLineContinuation.) Otherwise,
                ' include the new line and any additional spaces as trivia.
                If startComment = 0 AndAlso
                    CanGet(here) AndAlso
                    Not IsNewLine(Peek(here)) Then

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
        Friend Function ScanMultilineTrivia() As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode)
            If Not CanGet() Then
                Return Nothing
            End If

            Dim ch = Peek()

            ' optimization for a common case
            ' the ASCII range between ': and ~ , with exception of except "'", "_" and R cannot start trivia
            If ch > ":"c AndAlso
               ch <= "~"c AndAlso
               ch <> "'"c AndAlso
               ch <> "_"c AndAlso
               ch <> "R"c AndAlso
               ch <> "r"c AndAlso
               ch <> "<"c AndAlso
               ch <> "|"c AndAlso
               ch <> "="c AndAlso
               ch <> ">"c Then
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

                    If IsConflictMarkerTrivia() Then
                        ScanConflictMarker(tList)
                        Return True
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

        ' All conflict markers consist of the same character repeated seven times.  If it is
        ' a <<<<<<< or >>>>>>> marker then it is also followed by a space.
        Private Shared ReadOnly s_conflictMarkerLength As Integer = "<<<<<<<".Length

        Private Function IsConflictMarkerTrivia() As Boolean
            If CanGet() Then
                Dim ch = Peek()

                If ch = "<"c OrElse ch = ">"c OrElse ch = "|"c OrElse ch = "="c Then
                    Dim position = _lineBufferOffset
                    Dim text = _buffer

                    If position = 0 OrElse SyntaxFacts.IsNewLine(text(position - 1)) Then
                        Dim firstCh = _buffer(position)

                        If (position + s_conflictMarkerLength) <= text.Length Then
                            For i = 0 To s_conflictMarkerLength - 1
                                If text(position + i) <> firstCh Then
                                    Return False
                                End If
                            Next

                            If firstCh = "|"c OrElse firstCh = "="c Then
                                Return True
                            End If

                            Return (position + s_conflictMarkerLength) < text.Length AndAlso
                                   text(position + s_conflictMarkerLength) = " "c
                        End If
                    End If
                End If
            End If

            Return False
        End Function

        Private Sub ScanConflictMarker(tList As SyntaxListBuilder)
            Dim startCh = Peek()

            ' First create a trivia from the start of this merge conflict marker to the
            ' end of line/file (whichever comes first).
            ScanConflictMarkerHeader(tList)

            ' Now add the newlines as the next trivia.
            ScanConflictMarkerEndOfLine(tList)

            If startCh = "|"c OrElse startCh = "="c Then
                ' Consume everything from the start of the mid-conflict marker to the start of the next
                ' end-conflict marker.
                ScanConflictMarkerDisabledText(startCh = "="c, tList)
            End If
        End Sub

        Private Sub ScanConflictMarkerDisabledText(atSecondMiddleMarker As Boolean, tList As SyntaxListBuilder)
            Dim start = _lineBufferOffset
            While CanGet()
                Dim ch = Peek()

                If Not atSecondMiddleMarker AndAlso ch = "="c AndAlso IsConflictMarkerTrivia() Then
                    Exit While
                End If

                If ch = ">"c AndAlso IsConflictMarkerTrivia() Then
                    Exit While
                End If

                AdvanceChar()
            End While

            Dim width = _lineBufferOffset - start
            If width > 0 Then
                tList.Add(SyntaxFactory.DisabledTextTrivia(GetText(start, width)))
            End If
        End Sub

        Private Sub ScanConflictMarkerEndOfLine(tList As SyntaxListBuilder)
            Dim start = _lineBufferOffset

            While CanGet() AndAlso SyntaxFacts.IsNewLine(Peek())
                AdvanceChar()
            End While

            Dim width = _lineBufferOffset - start
            If width > 0 Then
                tList.Add(SyntaxFactory.EndOfLineTrivia(GetText(start, width)))
            End If
        End Sub

        Private Sub ScanConflictMarkerHeader(tList As SyntaxListBuilder)
            Dim start = _lineBufferOffset

            While CanGet()
                Dim ch = Peek()
                If SyntaxFacts.IsNewLine(ch) Then
                    Exit While
                End If

                AdvanceChar()
            End While

            Dim trivia = SyntaxFactory.ConflictMarkerTrivia(GetText(start, _lineBufferOffset - start))
            trivia = DirectCast(trivia.SetDiagnostics({ErrorFactory.ErrorInfo(ERRID.ERR_Merge_conflict_marker_encountered)}), SyntaxTrivia)
            tList.Add(trivia)
        End Sub

        ' check for '''(~')
        Private Function StartsXmlDoc(here As Integer) As Boolean
            Return _options.DocumentationMode >= DocumentationMode.Parse AndAlso
                CanGet(here + 3) AndAlso
                IsSingleQuote(Peek(here)) AndAlso
                IsSingleQuote(Peek(here + 1)) AndAlso
                IsSingleQuote(Peek(here + 2)) AndAlso
                Not IsSingleQuote(Peek(here + 3))
        End Function

        ' check for #
        Private Function StartsDirective(here As Integer) As Boolean
            If CanGet(here) Then
                Dim ch = Peek(here)
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
        Friend Function ScanSingleLineTrivia() As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode)
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

        Private Function ScanLeadingTrivia() As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode)
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

        Private Function ScanSingleLineTrivia(includeFollowingBlankLines As Boolean) As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode)
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
            If n = 0 OrElse tList(n - 1).RawKind <> SyntaxKind.EndOfLineTrivia Then
                Return False
            End If
            For i = 0 To n - 2
                If tList(i).RawKind <> SyntaxKind.WhitespaceTrivia Then
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

        Private Function ScanTokenCommon(precedingTrivia As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode), ch As Char, fullWidth As Boolean) As SyntaxToken
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
        Private Function TryScanToken(precedingTrivia As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode)) As SyntaxToken
            If CanGet() Then
                Return ScanTokenCommon(precedingTrivia, Peek(), False)
            End If
            Return MakeEofToken(precedingTrivia)
        End Function

        Private Function ScanTokenFullWidth(precedingTrivia As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode), ch As Char) As SyntaxToken
            Return ScanTokenCommon(precedingTrivia, ch, True)
        End Function

        ' // Allow whitespace between the characters of a two-character token.
        Private Function TrySkipFollowingEquals(ByRef index As Integer) As Boolean
            Debug.Assert(index > 0)
            Debug.Assert(CanGet(index - 1))

            Dim here = index
            Dim eq As Char

            While CanGet(here)
                eq = Peek(here)
                here += 1
                If Not IsWhitespace(eq) Then
                    If eq = "="c OrElse eq = FULLWIDTH_EQUALS_SIGN Then
                        index = here
                        Return True
                    Else
                        Return False
                    End If
                End If
            End While
            Return False
        End Function

        Private Function ScanRightAngleBracket(precedingTrivia As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode), charIsFullWidth As Boolean) As SyntaxToken
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

        Private Function ScanLeftAngleBracket(precedingTrivia As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode), charIsFullWidth As Boolean, scanTrailingTrivia As ScanTriviaFunc) As SyntaxToken
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

        Private Function ScanIdentifierOrKeyword(precedingTrivia As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode)) As SyntaxToken
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

            Dim baseSpelling = If(TypeCharacter = TypeCharacter.None,
                                   spelling,
                                   Intern(spelling, 0, len - 1))

            ' this can be keyword only if it has no type character, or if it is Mid$
            If TypeCharacter = TypeCharacter.None Then
                tokenType = TokenOfStringCached(spelling)
                If SyntaxFacts.IsContextualKeyword(tokenType) Then
                    contextualKind = tokenType
                    tokenType = SyntaxKind.IdentifierToken
                End If
            ElseIf TokenOfStringCached(baseSpelling) = SyntaxKind.MidKeyword Then

                contextualKind = SyntaxKind.MidKeyword
                tokenType = SyntaxKind.IdentifierToken
            End If

            If tokenType <> SyntaxKind.IdentifierToken Then
                ' KEYWORD
                Return MakeKeyword(tokenType, spelling, precedingTrivia)
            Else
                ' IDENTIFIER or CONTEXTUAL
                Dim id As SyntaxToken = MakeIdentifier(spelling, contextualKind, False, baseSpelling, TypeCharacter, precedingTrivia)
                Return id
            End If
        End Function

        Private Function TokenOfStringCached(spelling As String) As SyntaxKind
            If spelling.Length > 16 Then
                Return SyntaxKind.IdentifierToken
            End If

            Return _KeywordsObjs.GetOrMakeValue(spelling)
        End Function

        Private Function ScanBracketedIdentifier(precedingTrivia As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode)) As SyntaxToken
            Debug.Assert(CanGet)  ' [
            Debug.Assert(Peek() = "["c OrElse Peek() = FULLWIDTH_LEFT_SQUARE_BRACKET)

            Dim idStart As Integer = 1
            Dim here As Integer = idStart

            Dim invalidIdentifier As Boolean = False

            If Not CanGet(here) Then
                Return MakeBadToken(precedingTrivia, here, ERRID.ERR_MissingEndBrack)
            End If

            Dim ch = Peek(here)

            ' check if we can start an ident.
            If Not IsIdentifierStartCharacter(ch) OrElse
                (IsConnectorPunctuation(ch) AndAlso
                    Not (CanGet(here + 1) AndAlso
                         IsIdentifierPartCharacter(Peek(here + 1)))) Then

                invalidIdentifier = True
            End If

            ' check ident until ]
            While CanGet(here)
                Dim [Next] As Char = Peek(here)

                If [Next] = "]"c OrElse [Next] = FULLWIDTH_RIGHT_SQUARE_BRACKET Then
                    Dim idStringLength As Integer = here - idStart

                    If idStringLength > 0 AndAlso Not invalidIdentifier Then
                        Dim spelling = GetText(idStringLength + 2)
                        ' TODO: this should be provable?
                        Debug.Assert(spelling.Length > idStringLength + 1)

                        ' TODO: consider interning.
                        Dim baseText = spelling.Substring(1, idStringLength)
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
                        Return MakeBadToken(precedingTrivia, here + 1, ERRID.ERR_ExpectedIdentifier)
                    End If
                ElseIf IsNewLine([Next]) Then
                    Exit While
                ElseIf Not IsIdentifierPartCharacter([Next]) Then
                    invalidIdentifier = True
                    Exit While
                End If

                here += 1
            End While

            If here > 1 Then
                Return MakeBadToken(precedingTrivia, here, ERRID.ERR_MissingEndBrack)
            Else
                Return MakeBadToken(precedingTrivia, here, ERRID.ERR_ExpectedIdentifier)
            End If
        End Function

        Private Enum NumericLiteralKind
            Integral
            Float
            [Decimal]
        End Enum

        Private Function ScanNumericLiteral(precedingTrivia As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode)) As SyntaxToken
            Debug.Assert(CanGet)

            Dim here As Integer = 0
            Dim integerLiteralStart As Integer
            Dim UnderscoreInWrongPlace As Boolean
            Dim UnderscoreUsed As Boolean = False
            Dim LeadingUnderscoreUsed = False

            Dim base As LiteralBase = LiteralBase.Decimal
            Dim literalKind As NumericLiteralKind = NumericLiteralKind.Integral

            ' ####################################################
            ' // Validate literal and find where the number starts and ends.
            ' ####################################################

            ' // First read a leading base specifier, if present, followed by a sequence of zero
            ' // or more digits.
            Dim ch = Peek()
            If ch = "&"c OrElse ch = FULLWIDTH_AMPERSAND Then
                here += 1
                ch = If(CanGet(here), Peek(here), ChrW(0))

FullWidthRepeat:
                Select Case ch
                    Case "H"c, "h"c
                        here += 1
                        integerLiteralStart = here
                        base = LiteralBase.Hexadecimal

                        If CanGet(here) AndAlso Peek(here) = "_"c Then
                            LeadingUnderscoreUsed = True
                        End If

                        While CanGet(here)
                            ch = Peek(here)
                            If Not IsHexDigit(ch) AndAlso ch <> "_"c Then
                                Exit While
                            End If
                            If ch = "_"c Then
                                UnderscoreUsed = True
                            End If
                            here += 1
                        End While
                        UnderscoreInWrongPlace = UnderscoreInWrongPlace Or (Peek(here - 1) = "_"c)

                    Case "B"c, "b"c
                        here += 1
                        integerLiteralStart = here
                        base = LiteralBase.Binary

                        If CanGet(here) AndAlso Peek(here) = "_"c Then
                            LeadingUnderscoreUsed = True
                        End If

                        While CanGet(here)
                            ch = Peek(here)
                            If Not IsBinaryDigit(ch) AndAlso ch <> "_"c Then
                                Exit While
                            End If
                            If ch = "_"c Then
                                UnderscoreUsed = True
                            End If
                            here += 1
                        End While
                        UnderscoreInWrongPlace = UnderscoreInWrongPlace Or (Peek(here - 1) = "_"c)

                    Case "O"c, "o"c
                        here += 1
                        integerLiteralStart = here
                        base = LiteralBase.Octal

                        If CanGet(here) AndAlso Peek(here) = "_"c Then
                            LeadingUnderscoreUsed = True
                        End If

                        While CanGet(here)
                            ch = Peek(here)
                            If Not IsOctalDigit(ch) AndAlso ch <> "_"c Then
                                Exit While
                            End If
                            If ch = "_"c Then
                                UnderscoreUsed = True
                            End If
                            here += 1
                        End While
                        UnderscoreInWrongPlace = UnderscoreInWrongPlace Or (Peek(here - 1) = "_"c)

                    Case Else
                        If IsFullWidth(ch) Then
                            ch = MakeHalfWidth(ch)
                            GoTo FullWidthRepeat
                        End If

                        Throw ExceptionUtilities.UnexpectedValue(ch)
                End Select
            Else
                ' no base specifier - just go through decimal digits.
                integerLiteralStart = here
                UnderscoreInWrongPlace = (CanGet(here) AndAlso Peek(here) = "_"c)
                While CanGet(here)
                    ch = Peek(here)
                    If Not IsDecimalDigit(ch) AndAlso ch <> "_"c Then
                        Exit While
                    End If
                    If ch = "_"c Then
                        UnderscoreUsed = True
                    End If
                    here += 1
                End While
                If here <> integerLiteralStart Then
                    UnderscoreInWrongPlace = UnderscoreInWrongPlace Or (Peek(here - 1) = "_"c)
                End If
            End If

            ' we may have a dot, and then it is a float, but if this is an integral, then we have seen it all.
            Dim integerLiteralEnd As Integer = here

            ' // Unless there was an explicit base specifier (which indicates an integer literal),
            ' // read the rest of a float literal.
            If base = LiteralBase.Decimal AndAlso CanGet(here) Then
                ' // First read a '.' followed by a sequence of one or more digits.
                ch = Peek(here)
                If (ch = "."c Or ch = FULLWIDTH_FULL_STOP) AndAlso
                        CanGet(here + 1) AndAlso
                        IsDecimalDigit(Peek(here + 1)) Then

                    here += 2   ' skip dot and first digit

                    ' all following decimal digits belong to the literal (fractional part)
                    While CanGet(here)
                        ch = Peek(here)
                        If Not IsDecimalDigit(ch) AndAlso ch <> "_"c Then
                            Exit While
                        End If
                        here += 1
                    End While
                    UnderscoreInWrongPlace = UnderscoreInWrongPlace Or (Peek(here - 1) = "_"c)
                    literalKind = NumericLiteralKind.Float
                End If

                ' // Read an exponent symbol followed by an optional sign and a sequence of
                ' // one or more digits.
                If CanGet(here) AndAlso BeginsExponent(Peek(here)) Then
                    here += 1

                    If CanGet(here) Then
                        ch = Peek(here)

                        If MatchOneOrAnotherOrFullwidth(ch, "+"c, "-"c) Then
                            here += 1
                        End If
                    End If

                    If CanGet(here) AndAlso IsDecimalDigit(Peek(here)) Then
                        here += 1
                        While CanGet(here)
                            ch = Peek(here)
                            If Not IsDecimalDigit(ch) AndAlso ch <> "_"c Then
                                Exit While
                            End If
                            here += 1
                        End While
                        UnderscoreInWrongPlace = UnderscoreInWrongPlace Or (Peek(here - 1) = "_"c)
                    Else
                        Return MakeBadToken(precedingTrivia, here, ERRID.ERR_InvalidLiteralExponent)
                    End If

                    literalKind = NumericLiteralKind.Float
                End If
            End If

            Dim literalWithoutTypeChar = here

            ' ####################################################
            ' // Read a trailing type character.
            ' ####################################################

            Dim TypeCharacter As TypeCharacter = TypeCharacter.None

            If CanGet(here) Then
                ch = Peek(here)

FullWidthRepeat2:
                Select Case ch
                    Case "!"c
                        If base = LiteralBase.Decimal Then
                            TypeCharacter = TypeCharacter.Single
                            literalKind = NumericLiteralKind.Float
                            here += 1
                        End If

                    Case "F"c, "f"c
                        If base = LiteralBase.Decimal Then
                            TypeCharacter = TypeCharacter.SingleLiteral
                            literalKind = NumericLiteralKind.Float
                            here += 1
                        End If

                    Case "#"c
                        If base = LiteralBase.Decimal Then
                            TypeCharacter = TypeCharacter.Double
                            literalKind = NumericLiteralKind.Float
                            here += 1
                        End If

                    Case "R"c, "r"c
                        If base = LiteralBase.Decimal Then
                            TypeCharacter = TypeCharacter.DoubleLiteral
                            literalKind = NumericLiteralKind.Float
                            here += 1
                        End If

                    Case "S"c, "s"c

                        If literalKind <> NumericLiteralKind.Float Then
                            TypeCharacter = TypeCharacter.ShortLiteral
                            here += 1
                        End If

                    Case "%"c
                        If literalKind <> NumericLiteralKind.Float Then
                            TypeCharacter = TypeCharacter.Integer
                            here += 1
                        End If

                    Case "I"c, "i"c
                        If literalKind <> NumericLiteralKind.Float Then
                            TypeCharacter = TypeCharacter.IntegerLiteral
                            here += 1
                        End If

                    Case "&"c
                        If literalKind <> NumericLiteralKind.Float Then
                            TypeCharacter = TypeCharacter.Long
                            here += 1
                        End If

                    Case "L"c, "l"c
                        If literalKind <> NumericLiteralKind.Float Then
                            TypeCharacter = TypeCharacter.LongLiteral
                            here += 1
                        End If

                    Case "@"c
                        If base = LiteralBase.Decimal Then
                            TypeCharacter = TypeCharacter.Decimal
                            literalKind = NumericLiteralKind.Decimal
                            here += 1
                        End If

                    Case "D"c, "d"c
                        If base = LiteralBase.Decimal Then
                            TypeCharacter = TypeCharacter.DecimalLiteral
                            literalKind = NumericLiteralKind.Decimal

                            ' check if this was not attempt to use obsolete exponent
                            If CanGet(here + 1) Then
                                ch = Peek(here + 1)

                                If IsDecimalDigit(ch) OrElse MatchOneOrAnotherOrFullwidth(ch, "+"c, "-"c) Then
                                    Return MakeBadToken(precedingTrivia, here, ERRID.ERR_ObsoleteExponent)
                                End If
                            End If

                            here += 1
                        End If

                    Case "U"c, "u"c
                        If literalKind <> NumericLiteralKind.Float AndAlso CanGet(here + 1) Then
                            Dim NextChar As Char = Peek(here + 1)

                            'unsigned suffixes - US, UL, UI
                            If MatchOneOrAnotherOrFullwidth(NextChar, "S"c, "s"c) Then
                                TypeCharacter = TypeCharacter.UShortLiteral
                                here += 2
                            ElseIf MatchOneOrAnotherOrFullwidth(NextChar, "I"c, "i"c) Then
                                TypeCharacter = TypeCharacter.UIntegerLiteral
                                here += 2
                            ElseIf MatchOneOrAnotherOrFullwidth(NextChar, "L"c, "l"c) Then
                                TypeCharacter = TypeCharacter.ULongLiteral
                                here += 2
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

            Dim integralValue As UInt64
            Dim floatingValue As Double
            Dim decimalValue As Decimal
            Dim Overflows As Boolean = False

            If literalKind = NumericLiteralKind.Integral Then
                If integerLiteralStart = integerLiteralEnd Then
                    Return MakeBadToken(precedingTrivia, here, ERRID.ERR_Syntax)
                Else
                    integralValue = 0

                    If base = LiteralBase.Decimal Then
                        ' Init For loop
                        For LiteralCharacter As Integer = integerLiteralStart To integerLiteralEnd - 1
                            Dim LiteralCharacterValue As Char = Peek(LiteralCharacter)
                            If LiteralCharacterValue = "_"c Then
                                Continue For
                            End If
                            Dim NextCharacterValue As UInteger = IntegralLiteralCharacterValue(LiteralCharacterValue)

                            If integralValue < 1844674407370955161UL OrElse
                              (integralValue = 1844674407370955161UL AndAlso NextCharacterValue <= 5UI) Then

                                integralValue = (integralValue * 10UL) + NextCharacterValue
                            Else
                                Overflows = True
                                Exit For
                            End If
                        Next

                        If TypeCharacter <> TypeCharacter.ULongLiteral AndAlso integralValue > Long.MaxValue Then
                            Overflows = True
                        End If
                    Else
                        Dim Shift As Integer = If(base = LiteralBase.Hexadecimal, 4, If(Base = LiteralBase.Octal, 3, 1))
                        Dim OverflowMask As UInt64 = If(base = LiteralBase.Hexadecimal, &HF000000000000000UL, If(base = LiteralBase.Octal, &HE000000000000000UL, &H8000000000000000UL))

                        ' Init For loop
                        For LiteralCharacter As Integer = integerLiteralStart To integerLiteralEnd - 1
                            Dim LiteralCharacterValue As Char = Peek(LiteralCharacter)
                            If LiteralCharacterValue = "_"c Then
                                Continue For
                            End If

                            If (integralValue And OverflowMask) <> 0 Then
                                Overflows = True
                            End If

                            integralValue = (integralValue << Shift) + IntegralLiteralCharacterValue(LiteralCharacterValue)
                        Next
                    End If

                    If TypeCharacter = TypeCharacter.None Then
                        ' nothing to do
                    ElseIf TypeCharacter = TypeCharacter.Integer OrElse TypeCharacter = TypeCharacter.IntegerLiteral Then
                        If (base = LiteralBase.Decimal AndAlso integralValue > &H7FFFFFFF) OrElse
                            integralValue > &HFFFFFFFFUI Then

                            Overflows = True
                        End If

                    ElseIf TypeCharacter = TypeCharacter.UIntegerLiteral Then
                        If integralValue > &HFFFFFFFFUI Then
                            Overflows = True
                        End If

                    ElseIf TypeCharacter = TypeCharacter.ShortLiteral Then
                        If (base = LiteralBase.Decimal AndAlso integralValue > &H7FFF) OrElse
                            integralValue > &HFFFF Then

                            Overflows = True
                        End If

                    ElseIf TypeCharacter = TypeCharacter.UShortLiteral Then
                        If integralValue > &HFFFF Then
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
                    Overflows = Not GetDecimalValue(LiteralSpelling, decimalValue)
                Else
                    If TypeCharacter = TypeCharacter.Single OrElse TypeCharacter = TypeCharacter.SingleLiteral Then
                        ' // Attempt to convert to single
                        Dim SingleValue As Single
                        If Not RealParser.TryParseFloat(LiteralSpelling, SingleValue) Then
                            Overflows = True
                        Else
                            floatingValue = SingleValue
                        End If
                    Else
                        ' // Attempt to convert to double.
                        If Not RealParser.TryParseDouble(LiteralSpelling, floatingValue) Then
                            Overflows = True
                        End If
                    End If
                End If
            End If

            Dim result As SyntaxToken
            Select Case literalKind
                Case NumericLiteralKind.Integral
                    result = MakeIntegerLiteralToken(precedingTrivia, base, TypeCharacter, If(Overflows Or UnderscoreInWrongPlace, 0UL, IntegralValue), here)
                Case NumericLiteralKind.Float
                    result = MakeFloatingLiteralToken(precedingTrivia, TypeCharacter, If(Overflows Or UnderscoreInWrongPlace, 0.0F, floatingValue), here)
                Case NumericLiteralKind.Decimal
                    result = MakeDecimalLiteralToken(precedingTrivia, TypeCharacter, If(Overflows Or UnderscoreInWrongPlace, 0D, decimalValue), here)
                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(literalKind)
            End Select

            If Overflows Then
                result = DirectCast(result.AddError(ErrorFactory.ErrorInfo(ERRID.ERR_Overflow)), SyntaxToken)
            End If

            If UnderscoreInWrongPlace Then
                result = DirectCast(result.AddError(ErrorFactory.ErrorInfo(ERRID.ERR_Syntax)), SyntaxToken)
            ElseIf LeadingUnderscoreUsed Then
                result = CheckFeatureAvailability(result, Feature.LeadingDigitSeparator)
            ElseIf UnderscoreUsed Then
                result = CheckFeatureAvailability(result, Feature.DigitSeparators)
            End If

            If base = LiteralBase.Binary Then
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
               ByRef here As Integer
           ) As Boolean
            Debug.Assert(here >= 0)

            If Not CanGet(here) Then
                Return False
            End If

            Dim ch = Peek(here)
            If Not IsDecimalDigit(ch) Then
                Return False
            End If

            Dim integralValue As Integer = IntegralLiteralCharacterValue(ch)
            here += 1

            While CanGet(here)
                ch = Peek(here)

                If Not IsDecimalDigit(ch) Then
                    Exit While
                End If

                Dim nextDigit = IntegralLiteralCharacterValue(ch)
                If integralValue < 214748364 OrElse
                    (integralValue = 214748364 AndAlso nextDigit < 8) Then

                    integralValue = integralValue * 10 + nextDigit
                    here += 1
                Else
                    Return False
                End If
            End While

            ReturnValue = integralValue
            Return True
        End Function

        Private Function ScanDateLiteral(precedingTrivia As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode)) As SyntaxToken
            Debug.Assert(CanGet)
            Debug.Assert(IsHash(Peek()))

            Dim here As Integer = 1 'skip #
            Dim firstValue As Integer
            Dim YearValue, MonthValue, DayValue, HourValue, MinuteValue, SecondValue As Integer
            Dim haveDateValue As Boolean = False
            Dim haveYearValue As Boolean = False
            Dim haveTimeValue As Boolean = False
            Dim haveMinuteValue As Boolean = False
            Dim haveSecondValue As Boolean = False
            Dim haveAM As Boolean = False
            Dim havePM As Boolean = False
            Dim dateIsInvalid As Boolean = False
            Dim YearIsTwoDigits As Boolean = False
            Dim daysToMonth As Integer() = Nothing
            Dim yearIsFirst As Boolean = False

            ' // Unfortunately, we can't fall back on OLE Automation's date parsing because
            ' // they don't have the same range as the URT's DateTime class

            ' // First, eat any whitespace
            here = GetWhitespaceLength(here)

            Dim firstValueStart As Integer = here

            ' // The first thing has to be an integer, although it's not clear what it is yet
            If Not ScanIntLiteral(firstValue, here) Then
                Return Nothing

            End If

            ' // If we see a /, then it's a date

            If CanGet(here) AndAlso IsDateSeparatorCharacter(Peek(here)) Then
                Dim FirstDateSeparator As Integer = here

                ' // We've got a date
                haveDateValue = True
                here += 1

                ' Is the first value a year?
                ' It is a year if it consists of exactly 4 digits.
                ' Condition below uses 5 because we already skipped the separator.
                If here - firstValueStart = 5 Then
                    haveYearValue = True
                    yearIsFirst = True
                    YearValue = firstValue

                    ' // We have to have a month value
                    If Not ScanIntLiteral(MonthValue, here) Then
                        GoTo baddate
                    End If

                    ' Do we have a day value?
                    If CanGet(here) AndAlso IsDateSeparatorCharacter(Peek(here)) Then
                        ' // Check to see they used a consistent separator

                        If Peek(here) <> Peek(FirstDateSeparator) Then
                            GoTo baddate
                        End If

                        ' // Yes.
                        here += 1

                        If Not ScanIntLiteral(DayValue, here) Then
                            GoTo baddate
                        End If
                    End If
                Else
                    ' First value is month
                    MonthValue = firstValue

                    ' // We have to have a day value

                    If Not ScanIntLiteral(DayValue, here) Then
                        GoTo baddate
                    End If

                    ' // Do we have a year value?

                    If CanGet(here) AndAlso IsDateSeparatorCharacter(Peek(here)) Then
                        ' // Check to see they used a consistent separator

                        If Peek(here) <> Peek(FirstDateSeparator) Then
                            GoTo baddate
                        End If

                        ' // Yes.
                        haveYearValue = True
                        here += 1

                        Dim YearStart As Integer = here

                        If Not ScanIntLiteral(YearValue, here) Then
                            GoTo baddate
                        End If

                        If (here - YearStart) = 2 Then
                            YearIsTwoDigits = True
                        End If
                    End If
                End If

                here = GetWhitespaceLength(here)
            End If

            ' // If we haven't seen a date, assume it's a time value

            If Not haveDateValue Then
                haveTimeValue = True
                HourValue = firstValue
            Else
                ' // We did see a date. See if we see a time value...

                If ScanIntLiteral(HourValue, here) Then
                    ' // Yup.
                    haveTimeValue = True
                End If
            End If

            If haveTimeValue Then
                ' // Do we see a :?

                If CanGet(here) AndAlso IsColon(Peek(here)) Then
                    here += 1

                    ' // Now let's get the minute value

                    If Not ScanIntLiteral(MinuteValue, here) Then
                        GoTo baddate
                    End If

                    haveMinuteValue = True

                    ' // Do we have a second value?

                    If CanGet(here) AndAlso IsColon(Peek(here)) Then
                        ' // Yes.
                        haveSecondValue = True
                        here += 1

                        If Not ScanIntLiteral(SecondValue, here) Then
                            GoTo baddate
                        End If
                    End If
                End If

                here = GetWhitespaceLength(here)

                ' // Check AM/PM

                If CanGet(here) Then
                    If Peek(here) = "A"c OrElse Peek(here) = FULLWIDTH_LATIN_CAPITAL_LETTER_A OrElse
                        Peek(here) = "a"c OrElse Peek(here) = FULLWIDTH_LATIN_SMALL_LETTER_A Then

                        haveAM = True
                        here += 1

                    ElseIf Peek(here) = "P"c OrElse Peek(here) = FULLWIDTH_LATIN_CAPITAL_LETTER_P OrElse
                           Peek(here) = "p"c OrElse Peek(here) = FULLWIDTH_LATIN_SMALL_LETTER_P Then

                        havePM = True
                        here += 1

                    End If

                    If CanGet(here) AndAlso (haveAM OrElse havePM) Then
                        If Peek(here) = "M"c OrElse Peek(here) = FULLWIDTH_LATIN_CAPITAL_LETTER_M OrElse
                           Peek(here) = "m"c OrElse Peek(here) = FULLWIDTH_LATIN_SMALL_LETTER_M Then

                            here = GetWhitespaceLength(here + 1)

                        Else
                            GoTo baddate
                        End If
                    End If
                End If

                ' // If there's no minute/second value and no AM/PM, it's invalid

                If Not haveMinuteValue AndAlso Not haveAM AndAlso Not havePM Then
                    GoTo baddate
                End If
            End If

            If Not CanGet(here) OrElse Not IsHash(Peek(here)) Then
                GoTo baddate
            End If

            here += 1

            ' // OK, now we've got all the values, let's see if we've got a valid date
            If haveDateValue Then
                If MonthValue < 1 OrElse MonthValue > 12 Then
                    dateIsInvalid = True
                End If

                ' // We'll check Days in a moment...

                If Not haveYearValue Then
                    dateIsInvalid = True
                    YearValue = 1
                End If

                ' // Check if not a leap year

                If Not ((YearValue Mod 4 = 0) AndAlso (Not (YearValue Mod 100 = 0) OrElse (YearValue Mod 400 = 0))) Then
                    daysToMonth = DaysToMonth365
                Else
                    daysToMonth = DaysToMonth366
                End If

                If DayValue < 1 OrElse
                   (Not dateIsInvalid AndAlso DayValue > daysToMonth(MonthValue) - daysToMonth(MonthValue - 1)) Then

                    dateIsInvalid = True
                End If

                If YearIsTwoDigits Then
                    dateIsInvalid = True
                End If

                If YearValue < 1 OrElse YearValue > 9999 Then
                    dateIsInvalid = True
                End If

            Else
                MonthValue = 1
                DayValue = 1
                YearValue = 1
                daysToMonth = DaysToMonth365
            End If

            If haveTimeValue Then
                If haveAM OrElse havePM Then
                    ' // 12-hour value

                    If HourValue < 1 OrElse HourValue > 12 Then
                        dateIsInvalid = True
                    End If

                    If haveAM Then
                        HourValue = HourValue Mod 12
                    ElseIf havePM Then
                        HourValue = HourValue + 12

                        If HourValue = 24 Then
                            HourValue = 12
                        End If
                    End If

                Else
                    If HourValue < 0 OrElse HourValue > 23 Then
                        dateIsInvalid = True
                    End If
                End If

                If haveMinuteValue Then
                    If MinuteValue < 0 OrElse MinuteValue > 59 Then
                        dateIsInvalid = True
                    End If
                Else
                    MinuteValue = 0
                End If

                If haveSecondValue Then
                    If SecondValue < 0 OrElse SecondValue > 59 Then
                        dateIsInvalid = True
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

            If Not dateIsInvalid Then
                Dim DateTimeValue As New DateTime(YearValue, MonthValue, DayValue, HourValue, MinuteValue, SecondValue)
                Dim result = MakeDateLiteralToken(precedingTrivia, DateTimeValue, here)

                If yearIsFirst Then
                    result = Parser.CheckFeatureAvailability(Feature.YearFirstDateLiterals, result, Options.LanguageVersion)
                End If

                Return result
            Else
                Return MakeBadToken(precedingTrivia, here, ERRID.ERR_InvalidDate)
            End If

baddate:
            ' // If we can find a closing #, then assume it's a malformed date,
            ' // otherwise, it's not a date

            While CanGet(here)
                Dim ch As Char = Peek(here)
                If IsHash(ch) OrElse IsNewLine(ch) Then
                    Exit While
                End If
                here += 1
            End While

            If Not CanGet(here) OrElse IsNewLine(Peek(here)) Then
                ' // No closing #
                Return Nothing
            Else
                Debug.Assert(IsHash(Peek(here)))
                here += 1  ' consume trailing #
                Return MakeBadToken(precedingTrivia, here, ERRID.ERR_InvalidDate)
            End If
        End Function

        Private Function ScanStringLiteral(precedingTrivia As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode)) As SyntaxToken
            Debug.Assert(CanGet)
            Debug.Assert(IsDoubleQuote(Peek))

            Dim length As Integer = 1
            Dim ch As Char
            Dim followingTrivia As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode)

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
            Dim requiredVersion = New VisualBasicRequiredLanguageVersion(feature.GetLanguageVersion())
            Dim errorInfo = ErrorFactory.ErrorInfo(ERRID.ERR_LanguageVersion,
                                                   _options.LanguageVersion.GetErrorName(),
                                                   ErrorFactory.ErrorInfo(feature.GetResourceId()),
                                                   requiredVersion)
            Return DirectCast(token.AddError(errorInfo), SyntaxToken)
        End Function

        Friend Function CheckFeatureAvailability(feature As Feature) As Boolean
            Return CheckFeatureAvailability(Me.Options, feature)
        End Function

        Private Shared Function CheckFeatureAvailability(parseOptions As VisualBasicParseOptions, feature As Feature) As Boolean
            Dim featureFlag = feature.GetFeatureFlag()
            If featureFlag IsNot Nothing Then
                Return parseOptions.HasFeature(featureFlag)
            End If

            Dim required = feature.GetLanguageVersion()
            Dim actual = parseOptions.LanguageVersion
            Return CInt(required) <= CInt(actual)
        End Function
    End Class
End Namespace
