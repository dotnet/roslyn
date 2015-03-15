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
Imports Roslyn.Exts

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax

  ''' <summary>
  ''' Creates red tokens for a stream of text
  ''' </summary>
  Friend Class Scanner
    Implements IDisposable

    Private Delegate Function ScanTriviaFunc() As SyntaxList(Of VisualBasicSyntaxNode)

    Private Shared ReadOnly _scanNoTriviaFunc As ScanTriviaFunc = Function() Nothing
    Private ReadOnly _scanSingleLineTriviaFunc As ScanTriviaFunc = AddressOf ScanSingleLineTrivia

    Protected _lineBufferOffset As Integer ' marks the next character to read from _buffer
    Private _endOfTerminatorTrivia As Integer ' marks how far scanner may have scanned ahead for terminator trivia. This may be greater than _lineBufferOffset

    Private ReadOnly _sbPooled As PooledStringBuilder = PooledStringBuilder.GetInstance
    ''' <summary>
    ''' DO NOT USE DIRECTLY. 
    ''' USE GetScratch() 
    ''' </summary>
    Private ReadOnly _sb As StringBuilder = _sbPooled.Builder
    Private ReadOnly triviaListPool As New SyntaxListPool
    Private ReadOnly _options As VisualBasicParseOptions

    Private ReadOnly _stringTable As StringTable = StringTable.GetInstance()
    Private ReadOnly _quickTokenTable As TextKeyedCache(Of SyntaxToken) = TextKeyedCache(Of SyntaxToken).GetInstance

    Public Const TABLE_LIMIT = 512
    Private Shared ReadOnly keywordKindFactory As Func(Of String, SyntaxKind) = Function(spelling) KeywordTable.TokenOfString(spelling)

    Private Shared ReadOnly _KeywordsObjsPool As ObjectPool(Of CachingIdentityFactory(Of String, SyntaxKind)) = CachingIdentityFactory(Of String, SyntaxKind).CreatePool(TABLE_LIMIT, keywordKindFactory)
    Private ReadOnly _KeywordsObjs As CachingIdentityFactory(Of String, SyntaxKind) = _KeywordsObjsPool.Allocate()

    Private Shared ReadOnly _idTablePool As New ObjectPool(Of CachingFactory(Of TokenParts, IdentifierTokenSyntax))(
        Function() New CachingFactory(Of TokenParts, IdentifierTokenSyntax)(TABLE_LIMIT, Nothing, tokenKeyHasher, tokenKeyEquality))

    Private ReadOnly _idTable As CachingFactory(Of TokenParts, IdentifierTokenSyntax) = _idTablePool.Allocate()

    Private Shared ReadOnly _kwTablePool As New ObjectPool(Of CachingFactory(Of TokenParts, KeywordSyntax))(
        Function() New CachingFactory(Of TokenParts, KeywordSyntax)(TABLE_LIMIT, Nothing, tokenKeyHasher, tokenKeyEquality))

    Private ReadOnly _kwTable As CachingFactory(Of TokenParts, KeywordSyntax) = _kwTablePool.Allocate

    Private Shared ReadOnly _punctTablePool As New ObjectPool(Of CachingFactory(Of TokenParts, PunctuationSyntax))(
        Function() New CachingFactory(Of TokenParts, PunctuationSyntax)(TABLE_LIMIT, Nothing, tokenKeyHasher, tokenKeyEquality))

    Private ReadOnly _punctTable As CachingFactory(Of TokenParts, PunctuationSyntax) = _punctTablePool.Allocate()

    Private Shared ReadOnly _literalTablePool As New ObjectPool(Of CachingFactory(Of TokenParts, SyntaxToken))(
        Function() New CachingFactory(Of TokenParts, SyntaxToken)(TABLE_LIMIT, Nothing, tokenKeyHasher, tokenKeyEquality))

    Private ReadOnly _literalTable As CachingFactory(Of TokenParts, SyntaxToken) = _literalTablePool.Allocate

    Private Shared ReadOnly _wslTablePool As New ObjectPool(Of CachingFactory(Of SyntaxListBuilder, SyntaxList(Of VisualBasicSyntaxNode)))(
        Function() New CachingFactory(Of SyntaxListBuilder, SyntaxList(Of VisualBasicSyntaxNode))(TABLE_LIMIT, wsListFactory, wsListKeyHasher, wsListKeyEquality))

    Private ReadOnly _wslTable As CachingFactory(Of SyntaxListBuilder, SyntaxList(Of VisualBasicSyntaxNode)) = _wslTablePool.Allocate

    Private Shared ReadOnly _wsTablePool As New ObjectPool(Of CachingFactory(Of TriviaKey, SyntaxTrivia))(
        Function() CreateWsTable())

    Private ReadOnly _wsTable As CachingFactory(Of TriviaKey, SyntaxTrivia) = _wsTablePool.Allocate

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

        _idTablePool.Free(_idTable)
        _kwTablePool.Free(_kwTable)
        _punctTablePool.Free(_punctTable)
        _literalTablePool.Free(_literalTable)
        _wslTablePool.Free(_wslTable)
        _wsTablePool.Free(_wsTable)

        For Each p As Page In Me._pages
          If p IsNot Nothing Then
            p.Free()
          End If
        Next

        Array.Clear(Me._pages, 0, Me._pages.Length)
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
            Me._endOfTerminatorTrivia = Me._lineBufferOffset
            Me._lineBufferOffset -= quickToken.TerminatorLength
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
        If length > 0 Then Return MakeEmptyToken(leadingTrivia)
      End If

      Dim token = TryScanToken(leadingTrivia)

      If token Is Nothing Then token = ScanNextCharAsToken(leadingTrivia)
      If _lineBufferOffset > _endOfTerminatorTrivia Then _endOfTerminatorTrivia = _lineBufferOffset

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
      Dim c As Char
      If Not TryPeek(c) Then
        token = MakeEofToken(leadingTrivia)
      Else
        ' // Don't break up surrogate pairs
        Dim length = If(IsHighSurrogate(c) AndAlso TryPeek(1, c) AndAlso IsLowSurrogate(c), 2, 1)
        token = MakeBadToken(leadingTrivia, length, ERRID.ERR_IllegalChar)
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
      Dim c As Char
      While (TryPeek(c))
        Select Case (c)

          Case CARRIAGE_RETURN, LINE_FEED
            EatThroughLineBreak(c)
            condLineStart = _lineBufferOffset
            Continue While

          Case SPACE, CHARACTER_TABULATION
            Debug.Assert(IsWhitespace(c))
            EatWhitespace()
            Continue While

          Case "a"c, "b"c, "c"c, "d"c, "e"c, "f"c, "g"c, "h"c, "i"c, "j"c, "k"c, "l"c,
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
      Dim c As Char
      While TryPeek(c)
        iF  IsNewLine(c) Then
          EatThroughLineBreak(c)
      Exit Sub
        End If
        AdvanceChar()
      End While
    End Sub

    ''' <summary>
    ''' Gets a chunk of text as a DisabledCode node.
    ''' </summary>
    ''' <param name="span">The range of text.</param>
    ''' <returns>The DisabledCode node.</returns> 
    Friend Function GetDisabledTextAt(span As TextSpan) As SyntaxTrivia
      If 0 <= span.Start  AndAlso span.End <= _bufferLen Then
          Return SyntaxFactory.DisabledTextTrivia(GetTextNotInterned(span.Start, span.Length))
      End If
      ' TODO: should this be a Require?
      Throw New ArgumentOutOfRangeException("span")
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

    Private Function NextAre(offset As Integer, chars As String) As Boolean
      Debug.Assert(Not String.IsNullOrEmpty(chars))
      Dim n = chars.Length
      Dim i = -1
      If CanGet(offset + n) Then
        Do
          i += 1
        Loop While i < n AndAlso chars(i) = Peek(offset + i)
      End If
      Return i = n
    End Function

    Private Function CanGet() As Boolean
      Return _lineBufferOffset < _bufferLen
    End Function

    Private Function CanGet(num As Integer) As Boolean
      Debug.Assert(_lineBufferOffset + num >= 0)
      Debug.Assert(num >= -MaxCharsLookBehind)

      Return _lineBufferOffset + num < _bufferLen
    End Function

    Private Function GetText(length As Integer) As String
      Debug.Assert(length > 0)
      Debug.Assert(CanGet(length - 1))

      If length = 1 Then Return GetNextChar()
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

    Private Function LengthOfLineBreak(StartCharacter As Char, Optional offset As Integer = 0) As Integer
      Dim c As Char
      Dim res = TryPeek(offset, c)
      Debug.Assert(res)
       Debug.Assert(IsNewLine(StartCharacter))
      Debug.Assert(StartCharacter = c)
      If StartCharacter = CARRIAGE_RETURN AndAlso  NextIs(offset + 1,LINE_FEED) Then Return 2
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
      If _lineBufferOffset < _endOfTerminatorTrivia Then Return MakeColonToken(precedingTrivia, charIsFullWidth)
      Return MakeEmptyToken(precedingTrivia)
    End Function

    ''' <summary>
    ''' Accept a CR/LF pair or either in isolation as a newline.
    ''' Make it a whitespace
    ''' </summary>
    Private Function ScanNewlineAsTrivia(StartCharacter As Char) As SyntaxTrivia
      If LengthOfLineBreak(StartCharacter) = 2 Then Return MakeEndOfLineTriviaCRLF()
      Return MakeEndOfLineTrivia(GetNextChar)
    End Function

    Private Function ScanLineContinuation(tList As SyntaxListBuilder) As Boolean
      Dim ch As Char
      If Not TryPeek(ch) OrElse Not IsAfterWhitespace() OrElse Not IsUnderscore(ch) Then Return False

      Dim offset = 1
      While TryPeek(offset, ch) AndAlso IsWhitespace(ch)
        offset += 1
      End While

      ' Line continuation is valid at the end of the
      ' line or at the end of file only.
      Dim atNewLine = IsNewLine(ch)
      If Not atNewLine AndAlso CanGet(offset) Then Return False

      tList.Add(MakeLineContinuationTrivia(GetText(1)))
      If offset > 1 Then tList.Add(MakeWhiteSpaceTrivia(GetText(offset - 1)))

      If atNewLine Then
        Dim newLine = SkipLineBreak(ch, 0)
        offset = GetWhitespaceLength(newLine)
        Dim spaces = offset - newLine
        Dim startComment = PeekStartComment(offset)

        ' If the line following the line continuation is blank, or blank with a comment,
        ' do not include the new line character since that would confuse code handling
        ' implicit line continuations. (See Scanner::EatLineContinuation.) Otherwise,
        ' include the new line and any additional spaces as trivia.
        If startComment = 0 AndAlso TryPeek(offset, ch) AndAlso Not IsNewLine(ch) Then

          tList.Add(MakeEndOfLineTrivia(GetText(newLine)))
          If spaces > 0 Then tList.Add(MakeWhiteSpaceTrivia(GetText(spaces)))
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
      Dim ch As Char
      If Not TryPeek(ch) Then Return Nothing
      ' optimization for a common case
      ' the ASCII range between ': and ~ , with exception of except "'", "_" and R cannot start trivia
      If ch.IsBetween(":"c, "~"c) AndAlso ch.IsNoneOf("'"c, "_"c, "R"c, "r"c) Then Return Nothing
      Dim triviaList = triviaListPool.Allocate()
      While TryScanSinglePieceOfMultilineTrivia(triviaList)
      End While
      Dim result = MakeTriviaArray(triviaList)
      triviaListPool.Free(triviaList)
      Return result
    End Function

    ''' <summary>
    ''' Scans a single piece of trivia
    ''' </summary>
    Private Function TryScanSinglePieceOfMultilineTrivia(tList As SyntaxListBuilder) As Boolean
      Dim ch As Char
      If TryPeek(ch) Then
        Dim atNewLine = IsAtNewLine()
        ' check for XmlDocComment and directives
        If atNewLine Then
          If StartsXmlDoc(0) Then Return TryScanXmlDocComment(tList)
          If StartsDirective(0) Then Return TryScanDirective(tList)
        End If
        Select Case True
          Case IsWhitespace(ch)
            ' eat until linebreak or nonwhitespace
            Dim wslen = GetWhitespaceLength(1)
            If atNewLine Then
              If StartsXmlDoc(wslen) Then Return TryScanXmlDocComment(tList)
              If StartsDirective(wslen) Then Return TryScanDirective(tList)
            End If
            tList.Add(MakeWhiteSpaceTrivia(GetText(wslen)))
            Return True
          Case IsNewLine(ch)
            tList.Add(ScanNewlineAsTrivia(ch))
            Return True
          Case IsUnderscore(ch)
            Return ScanLineContinuation(tList)
          Case IsColonAndNotColonEquals(ch, offset:=0)
            tList.Add(ScanColonAsTrivia())
            Return True
        End Select
        ' try get a comment
        Return ScanCommentIfAny(tList)
      End If
      Return False
    End Function

    ' check for '''(~')
    Private Function StartsXmlDoc(offset As Integer) As Boolean
      Return _options.DocumentationMode >= DocumentationMode.Parse AndAlso CanGet(offset + 3) AndAlso
             IsSingleQuote(Peek(offset)) AndAlso IsSingleQuote(Peek(offset + 1)) AndAlso IsSingleQuote(Peek(offset + 2)) AndAlso
         Not IsSingleQuote(Peek(offset + 3))
    End Function

    ' check for #
    Private Function StartsDirective(offset As Integer) As Boolean
      Dim ch As Char
      Return TryPeek(offset, ch) AndAlso IsHash(ch)
    End Function

    Private Function IsAtNewLine() As Boolean
      Return _lineBufferOffset = 0 OrElse IsNewLine(Peek(-1))
    End Function

    Private Function IsAfterWhitespace() As Boolean
      If _lineBufferOffset = 0 Then Return True
      Dim prevChar = Peek(-1)
      Return IsWhitespace(prevChar)
    End Function

    ''' <summary>
    ''' Scan trivia on one LOGICAL line
    ''' Will check for whitespace, comment, EoL, implicit line break
    ''' EoL may be consumed as whitespace only as a part of line continuation ( _ )
    ''' </summary>
    Friend Function ScanSingleLineTrivia() As SyntaxList(Of VisualBasicSyntaxNode)
      Dim tList = triviaListPool.Allocate()
      ScanSingleLineTrivia(tList)
      Dim result = MakeTriviaArray(tList)
      triviaListPool.Free(tList)
      Return result
    End Function

    Private Sub ScanSingleLineTrivia(tList As SyntaxListBuilder)
      If Me.IsScanningXmlDoc Then
        ScanSingleLineTriviaInXmlDoc(tList)
      Else
        ScanWhitespaceAndLineContinuations(tList)
        ScanCommentIfAny(tList)
        ScanTerminatorTrivia(tList)
      End If
    End Sub

    Private Sub ScanSingleLineTriviaInXmlDoc(tList As SyntaxListBuilder)
      Dim c As Char
      If TryPeek(c) Then
        Select Case (c)
          ' // Whitespace
          ' //  S    ::=    (#x20 | #x9 | #xD | #xA)+
          Case CARRIAGE_RETURN, LINE_FEED, " "c, CHARACTER_TABULATION
            Dim offsets = CreateOffsetRestorePoint()
            Dim triviaList = triviaListPool.Allocate(Of VisualBasicSyntaxNode)()
            Dim continueLine = ScanXmlTriviaInXmlDoc(c, triviaList)
            If Not continueLine Then
              triviaListPool.Free(triviaList)
              offsets.Restore()
              Return
            End If
            For i = 0 To triviaList.Count - 1
              tList.Add(triviaList(i))
            Next
            triviaListPool.Free(triviaList)
        End Select
      End If
    End Sub

    Private Function ScanLeadingTrivia() As SyntaxList(Of VisualBasicSyntaxNode)
      Dim tList = triviaListPool.Allocate()
      ScanWhitespaceAndLineContinuations(tList)
      Dim result = MakeTriviaArray(tList)
      triviaListPool.Free(tList)
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
      Dim tList = triviaListPool.Allocate()
      ScanSingleLineTrivia(tList)
      If includeFollowingBlankLines AndAlso IsBlankLine(tList) Then
        Dim more = triviaListPool.Allocate()
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
        triviaListPool.Free(more)
      End If
      Dim result = tList.ToList()
      triviaListPool.Free(tList)
      Return result
    End Function

    ''' <summary>
    ''' Return True if the builder is a (possibly empty) list of
    ''' WhitespaceTrivia followed by an EndOfLineTrivia.
    ''' </summary>
    Private Shared Function IsBlankLine(tList As SyntaxListBuilder) As Boolean
      Dim n = tList.Count
      If n = 0 OrElse tList(n - 1).Kind <> SyntaxKind.EndOfLineTrivia Then Return False
      For i = 0 To n - 2
        If tList(i).Kind <> SyntaxKind.WhitespaceTrivia Then Return False
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
      Dim ch As Char
      If TryPeek(ch) Then
        Dim startOfTerminatorTrivia = _lineBufferOffset
        If IsNewLine(ch) Then
          tList.Add(ScanNewlineAsTrivia(ch))
        ElseIf IsColonAndNotColonEquals(ch, offset:=0) Then
          tList.Add(ScanColonAsTrivia())
          ' collect { ws, colon }
          Do
            Dim len = GetWhitespaceLength(0)
            If Not TryPeek(len, ch) OrElse Not IsColonAndNotColonEquals(ch, offset:=len) Then Exit Do
            If len > 0 Then tList.Add(MakeWhiteSpaceTrivia(GetText(len)))
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
      Dim c As Char
      ' eat until linebreak or nonwhitespace
      While TryPeek(len, c) AndAlso IsWhitespace(c)
        len += 1
      End While
      Return len
    End Function

    Private Function GetXmlWhitespaceLength(len As Integer) As Integer
      Dim c As Char
      ' eat until linebreak or nonwhitespace
      While TryPeek(len, c) AndAlso IsXmlWhitespace(c)
        len += 1
      End While
      Return len
    End Function

    Private Function ScanWhitespace(Optional len As Integer = 0) As VisualBasicSyntaxNode
      len = GetWhitespaceLength(len)
      If len > 0 Then Return MakeWhiteSpaceTrivia(GetText(len))
      Return Nothing
    End Function

    Private Function ScanXmlWhitespace(Optional len As Integer = 0) As VisualBasicSyntaxNode
      len = GetXmlWhitespaceLength(len)
      If len > 0 Then Return MakeWhiteSpaceTrivia(GetText(len))
      Return Nothing
    End Function

    Private Sub EatWhitespace()
      Dim c As Char
      Dim res = TryPeek(c)
      Debug.Assert(res)
      Debug.Assert(IsWhitespace(c))
      AdvanceChar()
      ' eat until linebreak or nonwhitespace
      While TryPeek(c) AndAlso IsWhitespace(c)
        AdvanceChar()
      End While
    End Sub

    Private Function PeekStartComment(i As Integer) As Integer
      Dim ch As Char
      If TryPeek(i, ch) Then
        If IsSingleQuote(ch) Then Return 1
        If MatchOneOrAnotherOrFullwidth(ch, "R"c, "r"c) AndAlso
                TryPeek(i + 2, ch) AndAlso MatchOneOrAnotherOrFullwidth(Peek(i + 1), "E"c, "e"c) AndAlso
                MatchOneOrAnotherOrFullwidth(ch, "M"c, "m"c) Then
          If Not TryPeek(i + 3, ch) OrElse IsNewLine(ch) Then
            ' have only 'REM'
            Return 3
          ElseIf Not IsIdentifierPartCharacter(ch) Then
            ' have 'REM '
            Return 4
          End If
        End If
      End If

      Return 0
    End Function

    Private Function ScanComment() As SyntaxTrivia
      Dim ch As Char
      Dim res = TryPeek(ch)
      Debug.Assert(res)

      Dim length = PeekStartComment(0)
      If length > 0 Then
        Dim looksLikeDocComment As Boolean = StartsXmlDoc(0)

        ' eat all chars until EoL
        While TryPeek(length, ch) AndAlso Not IsNewLine(ch)
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


    ' at this point it is very likely that we are located at 
    ' the beginning of a token        
    Private Function TryScanToken(precedingTrivia As SyntaxList(Of VisualBasicSyntaxNode)) As SyntaxToken
      Dim ch As Char
      If Not TryPeek(ch) Then Return MakeEofToken(precedingTrivia)
      Return _Unified_(precedingTrivia, ch, False)
    End Function
    Private Function _Unified_(precedingTrivia As SyntaxList(Of VisualBasicSyntaxNode), ch As Char, q As Boolean) As SyntaxToken
      Dim c1 As Char
      Dim lengthWithMaybeEquals = 1
      Select Case ch
        Case CARRIAGE_RETURN, LINE_FEED, NEXT_LINE
          Return ScanNewlineAsStatementTerminator(ch, precedingTrivia)
        Case LINE_SEPARATOR, PARAGRAPH_SEPARATOR
          If q Then Exit Select
          Return ScanNewlineAsStatementTerminator(ch, precedingTrivia)
        Case " "c, CHARACTER_TABULATION, "'"c
          Debug.Assert(False, String.Format("Unexpected char: &H{0:x}", AscW(ch)))
          Return Nothing ' trivia cannot start a token

        Case "@"c : Return MakeAtToken(precedingTrivia, q)
        Case "("c : Return MakeOpenParenToken(precedingTrivia, q)
        Case ")"c : Return MakeCloseParenToken(precedingTrivia, q)
        Case "{"c : Return MakeOpenBraceToken(precedingTrivia, q)
        Case "}"c : Return MakeCloseBraceToken(precedingTrivia, q)
        Case ","c : Return MakeCommaToken(precedingTrivia, q)
        Case "#"c
          Dim dl = ScanDateLiteral(precedingTrivia)
          Return If(dl, MakeHashToken(precedingTrivia, q))
        Case "&"c
          If TryPeek(1, c1) AndAlso BeginsBaseLiteral(c1) Then Return ScanNumericLiteral(precedingTrivia)
          If TrySkipFollowingEquals(lengthWithMaybeEquals) Then Return MakeAmpersandEqualsToken(precedingTrivia, lengthWithMaybeEquals)
          Return MakeAmpersandToken(precedingTrivia, q)

        Case "="c : Return MakeEqualsToken(precedingTrivia, q)
        Case "<"c : Return ScanLeftAngleBracket(precedingTrivia, q, _scanSingleLineTriviaFunc)
        Case ">"c : Return ScanRightAngleBracket(precedingTrivia, q)
        Case ":"c
          If TrySkipFollowingEquals(lengthWithMaybeEquals) Then Return MakeColonEqualsToken(precedingTrivia, lengthWithMaybeEquals)
          Return ScanColonAsStatementTerminator(precedingTrivia, q)
        Case "+"c
          If TrySkipFollowingEquals(lengthWithMaybeEquals) Then Return MakePlusEqualsToken(precedingTrivia, lengthWithMaybeEquals)
          Return MakePlusToken(precedingTrivia, q)
        Case "-"c
          If TrySkipFollowingEquals(lengthWithMaybeEquals) Then Return MakeMinusEqualsToken(precedingTrivia, lengthWithMaybeEquals)
          Return MakeMinusToken(precedingTrivia, q)
        Case "*"c
          If TrySkipFollowingEquals(lengthWithMaybeEquals) Then Return MakeAsteriskEqualsToken(precedingTrivia, lengthWithMaybeEquals)
          Return MakeAsteriskToken(precedingTrivia, q)
        Case "/"c
          If TrySkipFollowingEquals(lengthWithMaybeEquals) Then Return MakeSlashEqualsToken(precedingTrivia, lengthWithMaybeEquals)
          Return MakeSlashToken(precedingTrivia, q)
        Case "\"c
          If TrySkipFollowingEquals(lengthWithMaybeEquals) Then Return MakeBackSlashEqualsToken(precedingTrivia, lengthWithMaybeEquals)
          Return MakeBackslashToken(precedingTrivia, q)
        Case "^"c
          If TrySkipFollowingEquals(lengthWithMaybeEquals) Then Return MakeCaretEqualsToken(precedingTrivia, lengthWithMaybeEquals)
          Return MakeCaretToken(precedingTrivia, q)
        Case "!"c
          Return MakeExclamationToken(precedingTrivia, q)
        Case "."c
          If TryPeek(1, c1) AndAlso IsDecimalDigit(c1) Then Return ScanNumericLiteral(precedingTrivia)
          Return MakeDotToken(precedingTrivia, q)
        Case "0"c, "1"c, "2"c, "3"c, "4"c, "5"c, "6"c, "7"c, "8"c, "9"c
          Return ScanNumericLiteral(precedingTrivia)
        Case """"c
          Return ScanStringLiteral(precedingTrivia)
        Case "A"c
          If Not NextAre(1, "s ") Then Return ScanIdentifierOrKeyword(precedingTrivia)
          ' TODO: do we allow widechars in keywords?
          Dim spelling = "As"
          AdvanceChar(2)
          Return MakeKeyword(SyntaxKind.AsKeyword, spelling, precedingTrivia)
        Case "E"c
          If Not NextAre(1, "nd ") Then Return ScanIdentifierOrKeyword(precedingTrivia)
          ' TODO: do we allow widechars in keywords?
          Dim spelling = "End"
          AdvanceChar(3)
          Return MakeKeyword(SyntaxKind.EndKeyword, spelling, precedingTrivia)
        Case "I"c
          If Not NextAre(1, "f ") Then Return ScanIdentifierOrKeyword(precedingTrivia)
          ' TODO: do we allow widechars in keywords?
          Dim spelling = "If"
          AdvanceChar(2)
          Return MakeKeyword(SyntaxKind.IfKeyword, spelling, precedingTrivia)
        Case "a"c, "b"c, "c"c, "d"c, "e"c, "f"c, "g"c, "h"c, "i"c, "j"c, "k"c, "l"c, "m"c,
             "n"c, "o"c, "p"c, "q"c, "r"c, "s"c, "t"c, "u"c, "v"c, "w"c, "x"c, "y"c, "z"c
          Return ScanIdentifierOrKeyword(precedingTrivia)
        Case "B"c, "C"c, "D"c, "F"c, "G"c, "H"c, "J"c, "K"c, "L"c, "M"c, "N"c, "O"c, "P"c, "Q"c,
             "R"c, "S"c, "T"c, "U"c, "V"c, "W"c, "X"c, "Y"c, "Z"c
          Return ScanIdentifierOrKeyword(precedingTrivia)
        Case "_"c
          If TryPeek(1, c1) AndAlso IsIdentifierPartCharacter(c1) Then Return ScanIdentifierOrKeyword(precedingTrivia)

          Dim err As ERRID = ERRID.ERR_ExpectedIdentifier
          Dim len = GetWhitespaceLength(1)
          If Not TryPeek(len, c1) OrElse IsNewLine(c1) OrElse PeekStartComment(len) > 0 Then
            err = ERRID.ERR_LineContWithCommentOrNoPrecSpace
          End If
          ' not a line continuation and cannot start identifier.
          Return MakeBadToken(precedingTrivia, 1, err)
        Case "["c : Return ScanBracketedIdentifier(precedingTrivia)
        Case "?"c : Return MakeQuestionToken(precedingTrivia, q)
        Case "%"c : If NextIs(1, ">"c) Then Return XmlMakeEndEmbeddedToken(precedingTrivia, _scanSingleLineTriviaFunc)
        Case "$"c, FULLWIDTH_DOLLAR_SIGN
          If q Then Exit Select
          If TryPeek(1, c1) AndAlso IsDoubleQuote(c1) Then Return MakePunctuationToken(precedingTrivia, 2, SyntaxKind.DollarSignDoubleQuoteToken)
      End Select

      If IsIdentifierStartCharacter(ch) Then Return ScanIdentifierOrKeyword(precedingTrivia)

      Debug.Assert(Not IsNewLine(ch))
      If q Then
        Debug.Assert(Not IsDoubleQuote(ch))
      Else
      If IsDoubleQuote(ch) Then Return ScanStringLiteral(precedingTrivia)

      If IsFullWidth(ch) Then
        ch = MakeHalfWidth(ch)
        Return ScanTokenFullWidth(precedingTrivia, ch)
      End If
      End If



      Return Nothing
    End Function
    'Private Function _Unified_B_(precedingTrivia As SyntaxList(Of VisualBasicSyntaxNode), ch As Char,q As Boolean) As SyntaxToken
    '  Dim c1 As char
    '  Dim lengthWithMaybeEquals = 1
    '  Select Case ch
    '    Case CARRIAGE_RETURN, LINE_FEED, NEXT_LINE, LINE_SEPARATOR, PARAGRAPH_SEPARATOR
    '      Return ScanNewlineAsStatementTerminator(ch, precedingTrivia)
    '    Case " "c, CHARACTER_TABULATION, "'"c
    '      Debug.Assert(False, String.Format("Unexpected char: &H{0:x}", AscW(ch)))
    '      Return Nothing ' trivia cannot start a token

    '    Case "@"c : Return MakeAtToken(precedingTrivia, q)
    '    Case "("c : Return MakeOpenParenToken(precedingTrivia, q)
    '    Case ")"c : Return MakeCloseParenToken(precedingTrivia, q)
    '    Case "{"c : Return MakeOpenBraceToken(precedingTrivia, q)
    '    Case "}"c : Return MakeCloseBraceToken(precedingTrivia, q)
    '    Case ","c : Return MakeCommaToken(precedingTrivia, q)
    '    Case "#"c
    '      Dim dl = ScanDateLiteral(precedingTrivia)
    '      Return If(dl, MakeHashToken(precedingTrivia, q))
    '    Case "&"c
    '      If TryPeek(1, c1) AndAlso BeginsBaseLiteral(c1) Then Return ScanNumericLiteral(precedingTrivia)
    '      If TrySkipFollowingEquals(lengthWithMaybeEquals) Then Return MakeAmpersandEqualsToken(precedingTrivia, lengthWithMaybeEquals)
    '      Return MakeAmpersandToken(precedingTrivia, q)

    '    Case "="c : Return MakeEqualsToken(precedingTrivia, q)
    '    Case "<"c : Return ScanLeftAngleBracket(precedingTrivia, q, _scanSingleLineTriviaFunc)
    '    Case ">"c : Return ScanRightAngleBracket(precedingTrivia, q)
    '    Case ":"c
    '      If TrySkipFollowingEquals(lengthWithMaybeEquals) Then Return MakeColonEqualsToken(precedingTrivia, lengthWithMaybeEquals)
    '      Return ScanColonAsStatementTerminator(precedingTrivia, q)
    '    Case "+"c
    '      If TrySkipFollowingEquals(lengthWithMaybeEquals) Then Return MakePlusEqualsToken(precedingTrivia, lengthWithMaybeEquals)
    '      Return MakePlusToken(precedingTrivia, q)
    '    Case "-"c
    '      If TrySkipFollowingEquals(lengthWithMaybeEquals) Then Return MakeMinusEqualsToken(precedingTrivia, lengthWithMaybeEquals)
    '      Return MakeMinusToken(precedingTrivia, q)
    '    Case "*"c
    '      If TrySkipFollowingEquals(lengthWithMaybeEquals) Then Return MakeAsteriskEqualsToken(precedingTrivia, lengthWithMaybeEquals)
    '      Return MakeAsteriskToken(precedingTrivia, q)
    '    Case "/"c
    '      If TrySkipFollowingEquals(lengthWithMaybeEquals) Then Return MakeSlashEqualsToken(precedingTrivia, lengthWithMaybeEquals)
    '      Return MakeSlashToken(precedingTrivia, q)
    '    Case "\"c
    '      If TrySkipFollowingEquals(lengthWithMaybeEquals) Then Return MakeBackSlashEqualsToken(precedingTrivia, lengthWithMaybeEquals)
    '      Return MakeBackslashToken(precedingTrivia, q)
    '    Case "^"c
    '      If TrySkipFollowingEquals(lengthWithMaybeEquals) Then Return MakeCaretEqualsToken(precedingTrivia, lengthWithMaybeEquals)
    '      Return MakeCaretToken(precedingTrivia, q)
    '    Case "!"c
    '      Return MakeExclamationToken(precedingTrivia, q)
    '    Case "."c
    '      If TryPeek(1, c1) AndAlso IsDecimalDigit(c1) Then Return ScanNumericLiteral(precedingTrivia)
    '      Return MakeDotToken(precedingTrivia, q)
    '    Case "0"c, "1"c, "2"c, "3"c, "4"c, "5"c, "6"c, "7"c, "8"c, "9"c
    '      Return ScanNumericLiteral(precedingTrivia)
    '    Case """"c
    '      Return ScanStringLiteral(precedingTrivia)
    '    Case "A"c
    '      If Not NextAre(1, "s ") Then Return ScanIdentifierOrKeyword(precedingTrivia)
    '      ' TODO: do we allow widechars in keywords?
    '      Dim spelling = "As"
    '      AdvanceChar(2)
    '      Return MakeKeyword(SyntaxKind.AsKeyword, spelling, precedingTrivia)
    '    Case "E"c
    '      If Not NextAre(1, "nd ") Then Return ScanIdentifierOrKeyword(precedingTrivia)
    '      ' TODO: do we allow widechars in keywords?
    '      Dim spelling = "End"
    '      AdvanceChar(3)
    '      Return MakeKeyword(SyntaxKind.EndKeyword, spelling, precedingTrivia)
    '    Case "I"c
    '      If Not NextAre(1, "f ") Then Return ScanIdentifierOrKeyword(precedingTrivia)
    '      ' TODO: do we allow widechars in keywords?
    '      Dim spelling = "If"
    '      AdvanceChar(2)
    '      Return MakeKeyword(SyntaxKind.IfKeyword, spelling, precedingTrivia)
    '    Case "a"c, "b"c, "c"c, "d"c, "e"c, "f"c, "g"c, "h"c, "i"c, "j"c, "k"c, "l"c, "m"c,
    '         "n"c, "o"c, "p"c, "q"c, "r"c, "s"c, "t"c, "u"c, "v"c, "w"c, "x"c, "y"c, "z"c
    '      Return ScanIdentifierOrKeyword(precedingTrivia)
    '    Case "B"c, "C"c, "D"c, "F"c, "G"c, "H"c, "J"c, "K"c, "L"c, "M"c, "N"c, "O"c, "P"c, "Q"c,
    '         "R"c, "S"c, "T"c, "U"c, "V"c, "W"c, "X"c, "Y"c, "Z"c
    '      Return ScanIdentifierOrKeyword(precedingTrivia)
    '    Case "_"c
    '      If TryPeek(1, c1) AndAlso IsIdentifierPartCharacter(c1) Then Return ScanIdentifierOrKeyword(precedingTrivia)

    '      Dim err As ERRID = ERRID.ERR_ExpectedIdentifier
    '      Dim len = GetWhitespaceLength(1)
    '      If Not TryPeek(len, c1) OrElse IsNewLine(c1) OrElse PeekStartComment(len) > 0 Then
    '        err = ERRID.ERR_LineContWithCommentOrNoPrecSpace
    '      End If
    '      ' not a line continuation and cannot start identifier.
    '      Return MakeBadToken(precedingTrivia, 1, err)
    '    Case "["c : Return ScanBracketedIdentifier(precedingTrivia)
    '    Case "?"c : Return MakeQuestionToken(precedingTrivia, q)
    '    Case "%"c : If NextIs(1, ">"c) Then Return XmlMakeEndEmbeddedToken(precedingTrivia, _scanSingleLineTriviaFunc)
    '    Case "$"c, FULLWIDTH_DOLLAR_SIGN
    '      If TryPeek(1, c1) AndAlso IsDoubleQuote(c1) Then Return MakePunctuationToken(precedingTrivia, 2, SyntaxKind.DollarSignDoubleQuoteToken)
    '  End Select

    '  If IsIdentifierStartCharacter(ch) Then Return ScanIdentifierOrKeyword(precedingTrivia)

    '  Debug.Assert(Not IsNewLine(ch))

    '  If IsDoubleQuote(ch) Then Return ScanStringLiteral(precedingTrivia)

    '  If IsFullWidth(ch) Then
    '    ch = MakeHalfWidth(ch)
    '    Return ScanTokenFullWidth(precedingTrivia, ch)
    '  End If

    '  Return Nothing
    'End Function

    '' at this point it is very likely that we are located at 
    '' the beginning of a token        
    'Private Function TryScanToken(precedingTrivia As SyntaxList(Of VisualBasicSyntaxNode)) As SyntaxToken
    '  Dim ch, c1 As Char
    '  If Not TryPeek(ch) Then Return MakeEofToken(precedingTrivia)
    '  Dim lengthWithMaybeEquals = 1
    '  Select Case ch
    '    Case CARRIAGE_RETURN, LINE_FEED, NEXT_LINE, LINE_SEPARATOR, PARAGRAPH_SEPARATOR
    '      Return ScanNewlineAsStatementTerminator(ch, precedingTrivia)
    '    Case " "c, CHARACTER_TABULATION, "'"c
    '      Debug.Assert(False, String.Format("Unexpected char: &H{0:x}", AscW(ch)))
    '      Return Nothing ' trivia cannot start a token

    '    Case "@"c :  Return MakeAtToken(precedingTrivia, False)
    '    Case "("c :  Return MakeOpenParenToken(precedingTrivia, False)
    '    Case ")"c :  Return MakeCloseParenToken(precedingTrivia, False)
    '    Case "{"c :  Return MakeOpenBraceToken(precedingTrivia, False)
    '    Case "}"c :  Return MakeCloseBraceToken(precedingTrivia, False)
    '    Case ","c :  Return MakeCommaToken(precedingTrivia, False)
    '    Case "#"c
    '      Dim dl = ScanDateLiteral(precedingTrivia)
    '      Return If(dl, MakeHashToken(precedingTrivia, False))
    '    Case "&"c
    '      If TryPeek(1, c1) AndAlso BeginsBaseLiteral(c1) Then Return ScanNumericLiteral(precedingTrivia)
    '      If TrySkipFollowingEquals(lengthWithMaybeEquals) Then Return MakeAmpersandEqualsToken(precedingTrivia, lengthWithMaybeEquals)
    '      Return MakeAmpersandToken(precedingTrivia, False)

    '    Case "="c :  Return MakeEqualsToken(precedingTrivia, False)
    '    Case "<"c :  Return ScanLeftAngleBracket(precedingTrivia, False, _scanSingleLineTriviaFunc)
    '    Case ">"c :  Return ScanRightAngleBracket(precedingTrivia, False)
    '    Case ":"c
    '      If TrySkipFollowingEquals(lengthWithMaybeEquals) Then Return MakeColonEqualsToken(precedingTrivia, lengthWithMaybeEquals)
    '      Return ScanColonAsStatementTerminator(precedingTrivia, False)
    '    Case "+"c
    '      If TrySkipFollowingEquals(lengthWithMaybeEquals) Then Return MakePlusEqualsToken(precedingTrivia, lengthWithMaybeEquals)
    '      Return MakePlusToken(precedingTrivia, False)
    '    Case "-"c
    '      If TrySkipFollowingEquals(lengthWithMaybeEquals) Then Return MakeMinusEqualsToken(precedingTrivia, lengthWithMaybeEquals)
    '      Return MakeMinusToken(precedingTrivia, False)
    '    Case "*"c
    '      If TrySkipFollowingEquals(lengthWithMaybeEquals) Then Return MakeAsteriskEqualsToken(precedingTrivia, lengthWithMaybeEquals)
    '      Return MakeAsteriskToken(precedingTrivia, False)
    '    Case "/"c
    '      If TrySkipFollowingEquals(lengthWithMaybeEquals) Then Return MakeSlashEqualsToken(precedingTrivia, lengthWithMaybeEquals)
    '      Return MakeSlashToken(precedingTrivia, False)
    '    Case "\"c
    '      If TrySkipFollowingEquals(lengthWithMaybeEquals) Then Return MakeBackSlashEqualsToken(precedingTrivia, lengthWithMaybeEquals)
    '      Return MakeBackslashToken(precedingTrivia, False)
    '    Case "^"c
    '      If TrySkipFollowingEquals(lengthWithMaybeEquals) Then Return MakeCaretEqualsToken(precedingTrivia, lengthWithMaybeEquals)
    '      Return MakeCaretToken(precedingTrivia, False)
    '    Case "!"c
    '      Return MakeExclamationToken(precedingTrivia, False)
    '    Case "."c
    '      If TryPeek(1, c1) AndAlso IsDecimalDigit(c1) Then Return ScanNumericLiteral(precedingTrivia)
    '      Return MakeDotToken(precedingTrivia, False)
    '    Case "0"c, "1"c, "2"c, "3"c, "4"c, "5"c, "6"c, "7"c, "8"c, "9"c
    '      Return ScanNumericLiteral(precedingTrivia)
    '    Case """"c
    '      Return ScanStringLiteral(precedingTrivia)
    '    Case "A"c
    '      If Not NextAre(1, "s ") Then Return ScanIdentifierOrKeyword(precedingTrivia)
    '      ' TODO: do we allow widechars in keywords?
    '      Dim spelling = "As"
    '      AdvanceChar(2)
    '      Return MakeKeyword(SyntaxKind.AsKeyword, spelling, precedingTrivia)
    '    Case "E"c
    '      If Not NextAre(1, "nd ") Then Return ScanIdentifierOrKeyword(precedingTrivia)
    '      ' TODO: do we allow widechars in keywords?
    '      Dim spelling = "End"
    '      AdvanceChar(3)
    '      Return MakeKeyword(SyntaxKind.EndKeyword, spelling, precedingTrivia)
    '    Case "I"c
    '      If Not NextAre(1, "f ") Then Return ScanIdentifierOrKeyword(precedingTrivia)
    '      ' TODO: do we allow widechars in keywords?
    '      Dim spelling = "If"
    '      AdvanceChar(2)
    '      Return MakeKeyword(SyntaxKind.IfKeyword, spelling, precedingTrivia)
    '    Case "a"c, "b"c, "c"c, "d"c, "e"c, "f"c, "g"c, "h"c, "i"c, "j"c, "k"c, "l"c, "m"c,
    '         "n"c, "o"c, "p"c, "q"c, "r"c, "s"c, "t"c, "u"c, "v"c, "w"c, "x"c, "y"c, "z"c
    '      Return ScanIdentifierOrKeyword(precedingTrivia)
    '    Case "B"c, "C"c, "D"c, "F"c, "G"c, "H"c, "J"c, "K"c, "L"c, "M"c, "N"c, "O"c, "P"c, "Q"c,
    '         "R"c, "S"c, "T"c, "U"c, "V"c, "W"c, "X"c, "Y"c, "Z"c
    '      Return ScanIdentifierOrKeyword(precedingTrivia)
    '    Case "_"c
    '      If TryPeek(1, c1) AndAlso IsIdentifierPartCharacter(c1) Then Return ScanIdentifierOrKeyword(precedingTrivia)

    '      Dim err As ERRID = ERRID.ERR_ExpectedIdentifier
    '      Dim len = GetWhitespaceLength(1)
    '      If Not TryPeek(len, c1) OrElse IsNewLine(c1) OrElse PeekStartComment(len) > 0 Then
    '        err = ERRID.ERR_LineContWithCommentOrNoPrecSpace
    '      End If
    '      ' not a line continuation and cannot start identifier.
    '      Return MakeBadToken(precedingTrivia, 1, err)
    '    Case "["c :  Return ScanBracketedIdentifier(precedingTrivia)
    '    Case "?"c :  Return MakeQuestionToken(precedingTrivia, False)
    '    Case "%"c :  If NextIs(1,">"c) Then Return XmlMakeEndEmbeddedToken(precedingTrivia, _scanSingleLineTriviaFunc)
    '    Case "$"c, FULLWIDTH_DOLLAR_SIGN
    '      If TryPeek(1, c1) AndAlso IsDoubleQuote(c1) Then Return MakePunctuationToken(precedingTrivia, 2, SyntaxKind.DollarSignDoubleQuoteToken)
    '  End Select

    '  If IsIdentifierStartCharacter(ch) Then Return ScanIdentifierOrKeyword(precedingTrivia)

    '  Debug.Assert(Not IsNewLine(ch))

    '  If IsDoubleQuote(ch) Then Return ScanStringLiteral(precedingTrivia)

    '  If IsFullWidth(ch) Then
    '    ch = MakeHalfWidth(ch)
    '    Return ScanTokenFullWidth(precedingTrivia, ch)
    '  End If

    '  Return Nothing
    'End Function

    ' REVIEW: Is there a better way to reuse this logic? 
    Private Function ScanTokenFullWidth(precedingTrivia As SyntaxList(Of VisualBasicSyntaxNode), ch As Char) As SyntaxToken
      Return _Unified_(precedingTrivia, ch, True)
    End Function

    'Private Function _UnifiedA_(precedingTrivia As SyntaxList(Of VisualBasicSyntaxNode), ch As Char, q As Boolean) As SyntaxToken
    '  Dim lengthWithMaybeEquals = 1
    '  Dim c1 As Char
    '  Select Case ch
    '    Case CARRIAGE_RETURN, LINE_FEED
    '      Return ScanNewlineAsStatementTerminator(ch, precedingTrivia)
    '    Case " "c, CHARACTER_TABULATION, "'"c
    '      Debug.Assert(False, String.Format("Unexpected char: &H{0:x}", AscW(ch)))
    '      Return Nothing ' trivia cannot start a token
    '    Case "@"c :  Return MakeAtToken( precedingTrivia, q )
    '    Case "("c :  Return MakeOpenParenToken( precedingTrivia, q )
    '    Case ")"c :  Return MakeCloseParenToken( precedingTrivia, q )
    '    Case "{"c :  Return MakeOpenBraceToken( precedingTrivia, q )
    '    Case "}"c :  Return MakeCloseBraceToken( precedingTrivia, q )
    '    Case ","c :  Return MakeCommaToken( precedingTrivia, q )
    '    Case "#"c
    '      Dim dl = ScanDateLiteral(precedingTrivia)
    '      Return If(dl, MakeHashToken( precedingTrivia, q ))
    '    Case "&"c
    '      If TryPeek(1, c1) AndAlso BeginsBaseLiteral(c1) Then Return ScanNumericLiteral(precedingTrivia)
    '      If TrySkipFollowingEquals(lengthWithMaybeEquals) Then Return MakeAmpersandEqualsToken(precedingTrivia, lengthWithMaybeEquals)
    '      Return MakeAmpersandToken(precedingTrivia, q )

    '    Case "="c :  Return MakeEqualsToken( precedingTrivia, q )
    '    Case "<"c :  Return ScanLeftAngleBracket( precedingTrivia, q , _scanSingleLineTriviaFunc)
    '    Case ">"c :  Return ScanRightAngleBracket( precedingTrivia, q )
    '    Case ":"c
    '      If TrySkipFollowingEquals(lengthWithMaybeEquals) Then Return MakeColonEqualsToken(precedingTrivia, lengthWithMaybeEquals)
    '      Return ScanColonAsStatementTerminator( precedingTrivia, q )
    '    Case "+"c
    '      If TrySkipFollowingEquals(lengthWithMaybeEquals) Then Return MakePlusEqualsToken(precedingTrivia, lengthWithMaybeEquals)
    '      Return MakePlusToken( precedingTrivia, q )
    '    Case "-"c
    '      If TrySkipFollowingEquals(lengthWithMaybeEquals) Then Return MakeMinusEqualsToken(precedingTrivia, lengthWithMaybeEquals)
    '      Return MakeMinusToken( precedingTrivia, q )
    '    Case "*"c
    '      If TrySkipFollowingEquals(lengthWithMaybeEquals) Then Return MakeAsteriskEqualsToken(precedingTrivia, lengthWithMaybeEquals)
    '      Return MakeAsteriskToken( precedingTrivia, q )
    '    Case "/"c
    '      If TrySkipFollowingEquals(lengthWithMaybeEquals) Then Return MakeSlashEqualsToken(precedingTrivia, lengthWithMaybeEquals)
    '      Return MakeSlashToken( precedingTrivia, q )
    '    Case "\"c
    '      If TrySkipFollowingEquals(lengthWithMaybeEquals) Then Return MakeBackSlashEqualsToken(precedingTrivia, lengthWithMaybeEquals)
    '      Return MakeBackslashToken( precedingTrivia, q )
    '    Case "^"c
    '      If TrySkipFollowingEquals(lengthWithMaybeEquals) Then Return MakeCaretEqualsToken(precedingTrivia, lengthWithMaybeEquals)
    '      Return MakeCaretToken( precedingTrivia, q )
    '    Case "!"c
    '      Return MakeExclamationToken( precedingTrivia, q )
    '    Case "."c
    '      If TryPeek(1, c1) AndAlso IsDecimalDigit(c1) Then Return ScanNumericLiteral(precedingTrivia)
    '      Return MakeDotToken( precedingTrivia, q )
    '    Case "0"c, "1"c, "2"c, "3"c, "4"c, "5"c, "6"c, "7"c, "8"c, "9"c
    '      Return ScanNumericLiteral( precedingTrivia )
    '    Case """"c 
    '      Return ScanStringLiteral( precedingTrivia )
    '    Case "A"c
    '      If Not NextAre(1, "s ") Then Return ScanIdentifierOrKeyword(precedingTrivia)
    '      Dim spelling = GetText(2)
    '      Return MakeKeyword(SyntaxKind.AsKeyword, spelling, precedingTrivia)
    '    Case "E"c
    '      If Not NextAre(1, "nd ") Then Return ScanIdentifierOrKeyword(precedingTrivia)
    '      Dim spelling = GetText(3)
    '      Return MakeKeyword(SyntaxKind.EndKeyword, spelling, precedingTrivia)
    '    Case "I"c
    '      If Not NextAre(1, "f ") Then Return ScanIdentifierOrKeyword(precedingTrivia)
    '      ' TODO: do we allow widechars in keywords?
    '      Dim spelling = GetText(2)
    '      Return MakeKeyword(SyntaxKind.IfKeyword, spelling, precedingTrivia)
    '    Case "a"c, "b"c, "c"c, "d"c, "e"c, "f"c, "g"c, "h"c, "i"c, "j"c, "k"c, "l"c, "m"c,
    '         "n"c, "o"c, "p"c, "q"c, "r"c, "s"c, "t"c, "u"c, "v"c, "w"c, "x"c, "y"c, "z"c
    '      Return ScanIdentifierOrKeyword(precedingTrivia)
    '    Case "B"c, "C"c, "D"c, "F"c, "G"c, "H"c, "J"c, "K"c, "L"c, "M"c, "N"c, "O"c, "P"c, "Q"c,
    '         "R"c, "S"c, "T"c, "U"c, "V"c, "W"c, "X"c, "Y"c, "Z"c
    '      Return ScanIdentifierOrKeyword(precedingTrivia)
    '    Case "_"c
    '      If TryPeek(1, c1) AndAlso IsIdentifierPartCharacter(c1) Then Return ScanIdentifierOrKeyword(precedingTrivia)
    '      Dim err As ERRID = ERRID.ERR_ExpectedIdentifier
    '      Dim len = GetWhitespaceLength(1)
    '      If Not CanGet(len) OrElse IsNewLine(Peek(len)) OrElse PeekStartComment(len) > 0 Then
    '        err = ERRID.ERR_LineContWithCommentOrNoPrecSpace
    '      End If
    '      ' not a line continuation and cannot start identifier.
    '      Return MakeBadToken(precedingTrivia, 1, err)
    '    Case "["c :  Return ScanBracketedIdentifier(precedingTrivia)
    '    Case "?"c :  Return MakeQuestionToken( precedingTrivia, q )
    '    Case "%"c :  If NextIs(1, ">"c) Then Return XmlMakeEndEmbeddedToken(precedingTrivia, _scanSingleLineTriviaFunc)
    '  End Select

    '  If IsIdentifierStartCharacter(ch) Then Return ScanIdentifierOrKeyword(precedingTrivia)

    '  Debug.Assert(Not IsNewLine(ch))
    '  Debug.Assert(Not IsDoubleQuote(ch))

    '  Return Nothing
    'End Function


    '' REVIEW: Is there a better way to reuse this logic? 
    'Private Function ScanTokenFullWidth(precedingTrivia As SyntaxList(Of VisualBasicSyntaxNode), ch As Char) As SyntaxToken
    '  Dim lengthWithMaybeEquals = 1
    '  Dim c1 As Char
    '  Select Case ch
    '    Case CARRIAGE_RETURN, LINE_FEED
    '      Return ScanNewlineAsStatementTerminator(ch, precedingTrivia)
    '    Case " "c, CHARACTER_TABULATION, "'"c
    '      Debug.Assert(False, String.Format("Unexpected char: &H{0:x}", AscW(ch)))
    '      Return Nothing ' trivia cannot start a token
    '    Case "@"c
    '      Return MakeAtToken(precedingTrivia, True)
    '    Case "("c
    '      Return MakeOpenParenToken(precedingTrivia, True)
    '    Case ")"c
    '      Return MakeCloseParenToken(precedingTrivia, True)
    '    Case "{"c
    '      Return MakeOpenBraceToken(precedingTrivia, True)
    '    Case "}"c
    '      Return MakeCloseBraceToken(precedingTrivia, True)
    '    Case ","c
    '      Return MakeCommaToken(precedingTrivia, True)
    '    Case "#"c
    '      Dim dl = ScanDateLiteral(precedingTrivia)
    '      Return If(dl, MakeHashToken(precedingTrivia, True))
    '    Case "&"c
    '      If TryPeek(1, c1) AndAlso BeginsBaseLiteral(c1) Then Return ScanNumericLiteral(precedingTrivia)
    '      If TrySkipFollowingEquals(lengthWithMaybeEquals) Then Return MakeAmpersandEqualsToken(precedingTrivia, lengthWithMaybeEquals)
    '      Return MakeAmpersandToken(precedingTrivia, True)
    '    Case "="c
    '      Return MakeEqualsToken(precedingTrivia, True)
    '    Case "<"c
    '      Return ScanLeftAngleBracket(precedingTrivia, True, _scanSingleLineTriviaFunc)
    '    Case ">"c
    '      Return ScanRightAngleBracket(precedingTrivia, True)
    '    Case ":"c
    '      If TrySkipFollowingEquals(lengthWithMaybeEquals) Then Return MakeColonEqualsToken(precedingTrivia, lengthWithMaybeEquals)
    '      Return ScanColonAsStatementTerminator(precedingTrivia, True)
    '    Case "+"c
    '      If TrySkipFollowingEquals(lengthWithMaybeEquals) Then Return MakePlusEqualsToken(precedingTrivia, lengthWithMaybeEquals)
    '      Return MakePlusToken(precedingTrivia, True)
    '    Case "-"c
    '      If TrySkipFollowingEquals(lengthWithMaybeEquals) Then Return MakeMinusEqualsToken(precedingTrivia, lengthWithMaybeEquals)
    '      Return MakeMinusToken(precedingTrivia, True)
    '    Case "*"c
    '      If TrySkipFollowingEquals(lengthWithMaybeEquals) Then Return MakeAsteriskEqualsToken(precedingTrivia, lengthWithMaybeEquals)
    '      Return MakeAsteriskToken(precedingTrivia, True)
    '    Case "/"c
    '      If TrySkipFollowingEquals(lengthWithMaybeEquals) Then Return MakeSlashEqualsToken(precedingTrivia, lengthWithMaybeEquals)
    '      Return MakeSlashToken(precedingTrivia, True)
    '    Case "\"c
    '      If TrySkipFollowingEquals(lengthWithMaybeEquals) Then Return MakeBackSlashEqualsToken(precedingTrivia, lengthWithMaybeEquals)
    '      Return MakeBackslashToken(precedingTrivia, True)
    '    Case "^"c
    '      If TrySkipFollowingEquals(lengthWithMaybeEquals) Then Return MakeCaretEqualsToken(precedingTrivia, lengthWithMaybeEquals)
    '      Return MakeCaretToken(precedingTrivia, True)
    '    Case "!"c
    '      Return MakeExclamationToken(precedingTrivia, True)
    '    Case "."c
    '      If TryPeek(1, c1) AndAlso IsDecimalDigit(c1) Then Return ScanNumericLiteral(precedingTrivia)
    '      Return MakeDotToken(precedingTrivia, True)
    '    Case "0"c, "1"c, "2"c, "3"c, "4"c, "5"c, "6"c, "7"c, "8"c, "9"c
    '      Return ScanNumericLiteral(precedingTrivia)
    '    Case """"c
    '      Return ScanStringLiteral(precedingTrivia)
    '    Case "A"c
    '      If Not NextAre(1, "s ") Then Return ScanIdentifierOrKeyword(precedingTrivia)
    '      Dim spelling = GetText(2)
    '      Return MakeKeyword(SyntaxKind.AsKeyword, spelling, precedingTrivia)
    '    Case "E"c
    '      If Not NextAre(1, "nd ") Then Return ScanIdentifierOrKeyword(precedingTrivia)
    '      Dim spelling = GetText(3)
    '      Return MakeKeyword(SyntaxKind.EndKeyword, spelling, precedingTrivia)
    '    Case "I"c
    '      If Not NextAre(1, "f ") Then Return ScanIdentifierOrKeyword(precedingTrivia)
    '      ' TODO: do we allow widechars in keywords?
    '      Dim spelling = GetText(2)
    '      Return MakeKeyword(SyntaxKind.IfKeyword, spelling, precedingTrivia)
    '    Case "a"c, "b"c, "c"c, "d"c, "e"c, "f"c, "g"c, "h"c, "i"c, "j"c, "k"c, "l"c, "m"c,
    '         "n"c, "o"c, "p"c, "q"c, "r"c, "s"c, "t"c, "u"c, "v"c, "w"c, "x"c, "y"c, "z"c
    '      Return ScanIdentifierOrKeyword(precedingTrivia)
    '    Case "B"c, "C"c, "D"c, "F"c, "G"c, "H"c, "J"c, "K"c, "L"c, "M"c, "N"c, "O"c, "P"c, "Q"c,
    '         "R"c, "S"c, "T"c, "U"c, "V"c, "W"c, "X"c, "Y"c, "Z"c
    '      Return ScanIdentifierOrKeyword(precedingTrivia)
    '    Case "_"c
    '      If TryPeek(1, c1) AndAlso IsIdentifierPartCharacter(c1) Then Return ScanIdentifierOrKeyword(precedingTrivia)
    '      Dim err As ERRID = ERRID.ERR_ExpectedIdentifier
    '      Dim len = GetWhitespaceLength(1)
    '      If Not CanGet(len) OrElse IsNewLine(Peek(len)) OrElse PeekStartComment(len) > 0 Then
    '        err = ERRID.ERR_LineContWithCommentOrNoPrecSpace
    '      End If
    '      ' not a line continuation and cannot start identifier.
    '      Return MakeBadToken(precedingTrivia, 1, err)
    '    Case "["c
    '      Return ScanBracketedIdentifier(precedingTrivia)
    '    Case "?"c
    '      Return MakeQuestionToken(precedingTrivia, True)
    '    Case "%"c
    '      If NextIs(1, ">"c) Then Return XmlMakeEndEmbeddedToken(precedingTrivia, _scanSingleLineTriviaFunc)
    '  End Select

    '  If IsIdentifierStartCharacter(ch) Then Return ScanIdentifierOrKeyword(precedingTrivia)

    '  Debug.Assert(Not IsNewLine(ch))
    '  Debug.Assert(Not IsDoubleQuote(ch))

    '  Return Nothing
    'End Function

    ' // Allow whitespace between the characters of a two-character token.
    Private Function TrySkipFollowingEquals(ByRef Index As Integer) As Boolean
      Debug.Assert(Index > 0)
      Debug.Assert(CanGet(Index - 1))

      Dim offset = Index
      Dim eq As Char

      While TryPeek(offset, eq)
        offset += 1
        If Not IsWhitespace(eq) Then
          If Not eq.IsAnyOf("="c, FULLWIDTH_EQUALS_SIGN) Then Return False
          Index = offset
          Return True
        End If
      End While
      Return False
    End Function

    Private Function ScanRightAngleBracket(precedingTrivia As SyntaxList(Of VisualBasicSyntaxNode), charIsFullWidth As Boolean) As SyntaxToken
      Dim c As Char
      Dim res = TryPeek(c)
      Debug.Assert(res)  ' > 
      Debug.Assert(c = ">"c OrElse c = FULLWIDTH_GREATER_THAN_SIGN)

      Dim length As Integer = 1

      ' // Allow whitespace between the characters of a two-character token.
      length = GetWhitespaceLength(length)

      If TryPeek(length, c) Then
        If c.IsAnyOf("="c, FULLWIDTH_EQUALS_SIGN) Then
          length += 1
          Return MakeGreaterThanEqualsToken(precedingTrivia, length)
        ElseIf c.IsAnyOf(">"c, FULLWIDTH_GREATER_THAN_SIGN) Then
          length += 1
          If TrySkipFollowingEquals(length) Then Return MakeGreaterThanGreaterThanEqualsToken(precedingTrivia, length)
          Return MakeGreaterThanGreaterThanToken(precedingTrivia, length)
        End If
      End If
      Return MakeGreaterThanToken(precedingTrivia, charIsFullWidth)
    End Function

    Private Function ScanLeftAngleBracket(precedingTrivia As SyntaxList(Of VisualBasicSyntaxNode), charIsFullWidth As Boolean, scanTrailingTrivia As ScanTriviaFunc) As SyntaxToken
      Dim c As Char
      Dim res = TryPeek(c)
      Debug.Assert(res)  ' < 
      Debug.Assert(c = "<"c OrElse c = FULLWIDTH_LESS_THAN_SIGN)

      Dim length As Integer = 1

      ' Check for XML tokens
      If Not charIsFullWidth AndAlso TryPeek(length, c) Then
        Select Case c
          Case "!"c
            If CanGet(length + 2) Then
              Select Case (Peek(length + 1))
                Case "-"c
                  If CanGet(length + 3) AndAlso Peek(length + 2) = "-"c Then
                    Return XmlMakeBeginCommentToken(precedingTrivia, scanTrailingTrivia)
                  End If
                Case "["c
                  If NextAre(length + 2, "CDATA[") Then Return XmlMakeBeginCDataToken(precedingTrivia, scanTrailingTrivia)
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

      If TryPeek(length, c) Then
        If c.IsAnyOf("="c, FULLWIDTH_EQUALS_SIGN) Then
          length += 1
          Return MakeLessThanEqualsToken(precedingTrivia, length)
        ElseIf c.IsAnyOf(">"c, FULLWIDTH_GREATER_THAN_SIGN) Then
          length += 1
          Return MakeLessThanGreaterThanToken(precedingTrivia, length)
        ElseIf c.IsAnyOf("<"c, FULLWIDTH_LESS_THAN_SIGN) Then
          length += 1

          If TryPeek(length, c) Then
            'if the second "<" is a part of "<%" - like in "<<%" , we do not want to use it.
            If c.IsNoneOf("%"c, FULLWIDTH_PERCENT_SIGN) Then
              If TrySkipFollowingEquals(length) Then Return MakeLessThanLessThanEqualsToken(precedingTrivia, length)
              Return MakeLessThanLessThanToken(precedingTrivia, length)
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
      If spellingLength = 0 Then Return False

      Dim c = spelling(0)
      If SyntaxFacts.IsIdentifierStartCharacter(c) Then
        '  SPEC: ... Visual Basic identifiers conform to the Unicode Standard Annex 15 with one 
        '  SPEC:     exception: identifiers may begin with an underscore (connector) character. 
        '  SPEC:     If an identifier begins with an underscore, it must contain at least one other 
        '  SPEC:     valid identifier character to disambiguate it from a line continuation. 
        If IsConnectorPunctuation(c) AndAlso spellingLength = 1 Then Return False

        For i = 1 To spellingLength - 1
          If Not IsIdentifierPartCharacter(spelling(i)) Then Return False
        Next
      End If

      Return True
    End Function

    Private Function ScanIdentifierOrKeyword(precedingTrivia As SyntaxList(Of VisualBasicSyntaxNode)) As SyntaxToken
      Dim ch, ch1, NextChar As Char
      Dim res = TryPeek(ch)
      Debug.Assert(res)
      Debug.Assert(IsIdentifierStartCharacter(ch))
      Debug.Assert(PeekStartComment(0) = 0) ' comment should be handled by caller

      If TryPeek(1, ch1) AndAlso IsConnectorPunctuation(ch) AndAlso Not IsIdentifierPartCharacter(ch1) Then
        Return MakeBadToken(precedingTrivia, 1, ERRID.ERR_ExpectedIdentifier)
      End If

      Dim len = 1 ' we know that the first char was good

      ' // The C++ compiler refuses to inline IsIdentifierCharacter, so the
      ' // < 128 test is inline here. (This loop gets a *lot* of traffic.)
      ' TODO: make sure we get good perf here
      While TryPeek(len, ch)
        Dim code = Convert.ToUInt16(ch)
        If code < 128 AndAlso IsNarrowIdentifierCharacter(code) OrElse
            IsWideIdentifierCharacter(ch) Then

          len += 1
        Else
          Exit While
        End If
      End While

      'Check for a type character
      Dim TypeCharacter As TypeCharacter = TypeCharacter.None
      If TryPeek(len, ch) Then
FullWidthRepeat:
        Select Case ch
          Case "!"c
            ' // If the ! is followed by an identifier it is a dictionary lookup operator, not a type character.
            If TryPeek(len + 1, NextChar) Then
              If IsIdentifierStartCharacter(NextChar) OrElse MatchOneOrAnotherOrFullwidth(NextChar, "["c, "]"c) Then
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

      Dim BaseSpelling = If(TypeCharacter = TypeCharacter.None, spelling, Intern(spelling, 0, len - 1))

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

      If tokenType <> SyntaxKind.IdentifierToken Then Return MakeKeyword(tokenType, spelling, precedingTrivia) ' KEYWORD
      ' IDENTIFIER or CONTEXTUAL
      Dim id As SyntaxToken = MakeIdentifier(spelling, contextualKind, False, BaseSpelling, TypeCharacter, precedingTrivia)
      Return id
    End Function

    Private Function TokenOfStringCached(spelling As String, Optional kind As SyntaxKind = SyntaxKind.IdentifierToken) As SyntaxKind
      If spelling.Length = 1 OrElse spelling.Length > 16 Then Return kind
      Return _KeywordsObjs.GetOrMakeValue(spelling)
    End Function

    Private Function ScanBracketedIdentifier(precedingTrivia As SyntaxList(Of VisualBasicSyntaxNode)) As SyntaxToken
      Dim ch, c1 As Char
      Dim res = TryPeek(ch)
      Debug.Assert(res)  ' [
      Debug.Assert(ch = "["c OrElse ch = FULLWIDTH_LEFT_SQUARE_BRACKET)

      Dim IdStart = 1
      Dim offset = IdStart

      Dim InvalidIdentifier = False

      If Not TryPeek(offset, ch) Then Return MakeBadToken(precedingTrivia, offset, ERRID.ERR_MissingEndBrack)
      ' check if we can start an ident.
      If Not IsIdentifierStartCharacter(ch) OrElse
                (IsConnectorPunctuation(ch) AndAlso
                Not (TryPeek(offset + 1, c1) AndAlso IsIdentifierPartCharacter(c1))) Then InvalidIdentifier = True
      Dim [Next] As Char
      ' check ident until ]
      While TryPeek(offset, [Next])
        If [Next].IsAnyOf("]"c, FULLWIDTH_RIGHT_SQUARE_BRACKET) Then
          Dim IdStringLength As Integer = offset - IdStart

          If IdStringLength > 0 AndAlso Not InvalidIdentifier Then
            Dim spelling = GetText(IdStringLength + 2)
            ' TODO: this should be provable?
            Debug.Assert(spelling.Length > IdStringLength + 1)

            ' TODO: consider interning.
            Dim baseText = spelling.Substring(1, IdStringLength)
            Dim id As SyntaxToken = MakeIdentifier(spelling, SyntaxKind.IdentifierToken, True,
                                                   baseText, TypeCharacter.None, precedingTrivia)
            Return id
          Else
            ' // The sequence "[]" does not define a valid identifier.
            Return MakeBadToken(precedingTrivia, offset + 1, ERRID.ERR_ExpectedIdentifier)
          End If
        ElseIf IsNewLine([Next]) Then
          Exit While
        ElseIf Not IsIdentifierPartCharacter([Next]) Then
          InvalidIdentifier = True
          Exit While
        End If

        offset += 1
      End While

      If offset > 1 Then
        Return MakeBadToken(precedingTrivia, offset, ERRID.ERR_MissingEndBrack)
      Else
        Return MakeBadToken(precedingTrivia, offset, ERRID.ERR_ExpectedIdentifier)
      End If
    End Function

    Private Enum NumericLiteralKind
      Integral
      Float
      [Decimal]
    End Enum

    Private Function ScanNumericLiteral(precedingTrivia As SyntaxList(Of VisualBasicSyntaxNode)) As SyntaxToken
      Dim ch, c1 As Char
      Dim r = TryPeek(ch)
      Debug.Assert(r)

      Dim offset As Integer = 0
      Dim IntegerLiteralStart As Integer

      Dim Base As LiteralBase = LiteralBase.Decimal
      Dim literalKind As NumericLiteralKind = NumericLiteralKind.Integral

      ' ####################################################
      ' // Validate literal and find where the number starts and ends.
      ' ####################################################

      ' // First read a leading base specifier, if present, followed by a sequence of zero
      ' // or more digits.
      If ch.IsAnyOf("&"c, FULLWIDTH_AMPERSAND) Then
        offset += 1
        If Not TryPeek(offset, ch) Then ch = ChrW(0)

FullWidthRepeat:
        Select Case ch
          Case "H"c, "h"c
            offset += 1
            IntegerLiteralStart = offset
            Base = LiteralBase.Hexadecimal
            While TryPeek(offset, c1) AndAlso IsHexDigit(c1)
              offset += 1
            End While
          Case "O"c, "o"c
            offset += 1
            IntegerLiteralStart = offset
            Base = LiteralBase.Octal
            While TryPeek(offset, c1) AndAlso IsOctalDigit(c1)
              offset += 1
            End While
          Case Else
            If IsFullWidth(ch) Then
              ch = MakeHalfWidth(ch)
              GoTo FullWidthRepeat
            End If

            Throw ExceptionUtilities.UnexpectedValue(ch)
        End Select
      Else
        ' no base specifier - just go through decimal digits.
        IntegerLiteralStart = offset
        While TryPeek(offset, c1) AndAlso IsDecimalDigit(c1)
          offset += 1
        End While
      End If

      ' we may have a dot, and then it is a float, but if this is an integral, then we have seen it all.
      Dim IntegerLiteralEnd As Integer = offset

      ' // Unless there was an explicit base specifier (which indicates an integer literal),
      ' // read the rest of a float literal.
      If Base = LiteralBase.Decimal AndAlso TryPeek(offset, ch) Then
        ' // First read a '.' followed by a sequence of one or more digits.
        If (ch = "."c Or ch = FULLWIDTH_FULL_STOP) AndAlso TryPeek(offset + 1, c1) AndAlso IsDecimalDigit(c1) Then
          offset += 2   ' skip dot and first digit
          ' all following decimal digits belong to the literal (fractional part)
          While TryPeek(offset, ch) AndAlso IsDecimalDigit(ch)
            offset += 1
          End While
          literalKind = NumericLiteralKind.Float
        End If

        ' // Read an exponent symbol followed by an optional sign and a sequence of
        ' // one or more digits.
        If TryPeek(offset, c1) AndAlso BeginsExponent(c1) Then
          offset += 1

          If TryPeek(offset, ch) AndAlso MatchOneOrAnotherOrFullwidth(ch, "+"c, "-"c) Then offset += 1
          If TryPeek(offset, c1) AndAlso IsDecimalDigit(c1) Then
            offset += 1
            While TryPeek(offset, ch) AndAlso IsDecimalDigit(ch)
              offset += 1
            End While
          Else
            Return MakeBadToken(precedingTrivia, offset, ERRID.ERR_InvalidLiteralExponent)
          End If

          literalKind = NumericLiteralKind.Float
        End If
      End If

      Dim literalWithoutTypeChar = offset

      ' ####################################################
      ' // Read a trailing type character.
      ' ####################################################

      Dim TypeCharacter As TypeCharacter = TypeCharacter.None
      If TryPeek(offset, ch) Then

FullWidthRepeat2:
        Select Case ch
          Case "!"c
            If Base = LiteralBase.Decimal Then
              TypeCharacter = TypeCharacter.Single
              literalKind = NumericLiteralKind.Float
              offset += 1
            End If
          Case "F"c, "f"c
            If Base = LiteralBase.Decimal Then
              TypeCharacter = TypeCharacter.SingleLiteral
              literalKind = NumericLiteralKind.Float
              offset += 1
            End If
          Case "#"c
            If Base = LiteralBase.Decimal Then
              TypeCharacter = TypeCharacter.Double
              literalKind = NumericLiteralKind.Float
              offset += 1
            End If
          Case "R"c, "r"c
            If Base = LiteralBase.Decimal Then
              TypeCharacter = TypeCharacter.DoubleLiteral
              literalKind = NumericLiteralKind.Float
              offset += 1
            End If
          Case "S"c, "s"c
            If literalKind <> NumericLiteralKind.Float Then
              TypeCharacter = TypeCharacter.ShortLiteral
              offset += 1
            End If
          Case "%"c
            If literalKind <> NumericLiteralKind.Float Then
              TypeCharacter = TypeCharacter.Integer
              offset += 1
            End If
          Case "I"c, "i"c
            If literalKind <> NumericLiteralKind.Float Then
              TypeCharacter = TypeCharacter.IntegerLiteral
              offset += 1
            End If

          Case "&"c
            If literalKind <> NumericLiteralKind.Float Then
              TypeCharacter = TypeCharacter.Long
              offset += 1
            End If
          Case "L"c, "l"c
            If literalKind <> NumericLiteralKind.Float Then
              TypeCharacter = TypeCharacter.LongLiteral
              offset += 1
            End If
          Case "@"c
            If Base = LiteralBase.Decimal Then
              TypeCharacter = TypeCharacter.Decimal
              literalKind = NumericLiteralKind.Decimal
              offset += 1
            End If
          Case "D"c, "d"c
            If Base = LiteralBase.Decimal Then
              TypeCharacter = TypeCharacter.DecimalLiteral
              literalKind = NumericLiteralKind.Decimal
              ' check if this was not attempt to use obsolete exponent
              If TryPeek(offset + 1, c1) Then
                If IsDecimalDigit(c1) OrElse MatchOneOrAnotherOrFullwidth(c1, "+"c, "-"c) Then
                  Return MakeBadToken(precedingTrivia, offset, ERRID.ERR_ObsoleteExponent)
                End If
              End If
              offset += 1
            End If
          Case "U"c, "u"c
            Dim NextChar As Char
            If literalKind <> NumericLiteralKind.Float AndAlso TryPeek(offset + 1, NextChar) Then
              'unsigned suffixes - US, UL, UI
              If MatchOneOrAnotherOrFullwidth(NextChar, "S"c, "s"c) Then
                TypeCharacter = TypeCharacter.UShortLiteral
                offset += 2
              ElseIf MatchOneOrAnotherOrFullwidth(NextChar, "I"c, "i"c) Then
                TypeCharacter = TypeCharacter.UIntegerLiteral
                offset += 2
              ElseIf MatchOneOrAnotherOrFullwidth(NextChar, "L"c, "l"c) Then
                TypeCharacter = TypeCharacter.ULongLiteral
                offset += 2
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
          Return MakeBadToken(precedingTrivia, offset, ERRID.ERR_Syntax)
        Else
          IntegralValue = IntegralLiteralCharacterValue(Peek(IntegerLiteralStart))

          If Base = LiteralBase.Decimal Then
            ' Init For loop
            For LiteralCharacter As Integer = IntegerLiteralStart + 1 To IntegerLiteralEnd - 1
              Dim NextCharacterValue As UInteger = IntegralLiteralCharacterValue(Peek(LiteralCharacter))

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
            Dim Shift As Integer = If(Base = LiteralBase.Hexadecimal, 4, 3)
            Dim OverflowMask As UInt64 = If(Base = LiteralBase.Hexadecimal, &HF000000000000000UL, &HE000000000000000UL)

            ' Init For loop
            For LiteralCharacter As Integer = IntegerLiteralStart + 1 To IntegerLiteralEnd - 1
              If (IntegralValue And OverflowMask) <> 0 Then
                Overflows = True
              End If

              IntegralValue = (IntegralValue << Shift) + IntegralLiteralCharacterValue(Peek(LiteralCharacter))
            Next
          End If
          Select Case TypeCharacter
            Case TypeCharacter.None
              ' nothing to do
            Case TypeCharacter.Integer, TypeCharacter.IntegerLiteral
              If (Base = LiteralBase.Decimal AndAlso IntegralValue > &H7FFFFFFF) OrElse (IntegralValue > &HFFFFFFFFUI) Then
                Overflows = True
              End If
            Case TypeCharacter.UIntegerLiteral
              If IntegralValue > &HFFFFFFFFUI Then Overflows = True
            Case TypeCharacter.ShortLiteral
              If (Base = LiteralBase.Decimal AndAlso IntegralValue > &H7FFF) OrElse IntegralValue > &HFFFF Then
                Overflows = True
              End If
            Case TypeCharacter.UShortLiteral
              If IntegralValue > &HFFFF Then Overflows = True
            Case Else
            Debug.Assert(TypeCharacter = TypeCharacter.Long        OrElse
                         TypeCharacter = TypeCharacter.LongLiteral OrElse
                         TypeCharacter = TypeCharacter.ULongLiteral, "Integral literal value computation is lost.")
          End Select
        End If

      Else
        ' // Copy the text of the literal to deal with fullwidth 
        Dim scratch = GetScratch()
        For i = 0 To literalWithoutTypeChar - 1
          Dim curCh = Peek(i)
          scratch.Append(If(IsFullWidth(curCh), MakeHalfWidth(curCh), curCh))
        Next
        Dim LiteralSpelling = GetScratchTextInterned(scratch)

        If literalKind = NumericLiteralKind.Decimal Then
          ' Attempt to convert to Decimal.
          Overflows = Not GetDecimalValue(LiteralSpelling, DecimalValue)
        Else
          If TypeCharacter = TypeCharacter.Single OrElse TypeCharacter = TypeCharacter.SingleLiteral Then
            ' // Attempt to convert to single
            Dim SingleValue As Single
            If Not Single.TryParse(LiteralSpelling, NumberStyles.Float, CultureInfo.InvariantCulture, SingleValue) Then
              Overflows = True
            Else
              FloatingValue = SingleValue
            End If
          Else
            ' // Attempt to convert to double.
            If Not Double.TryParse(LiteralSpelling, NumberStyles.Float, CultureInfo.InvariantCulture, FloatingValue) Then
              Overflows = True
            End If
          End If
        End If
      End If

      Dim result As SyntaxToken
      Select Case literalKind
        Case NumericLiteralKind.Integral
          result = MakeIntegerLiteralToken(precedingTrivia, Base, TypeCharacter, If(Overflows, 0UL, IntegralValue), offset)
        Case NumericLiteralKind.Float
          result = MakeFloatingLiteralToken(precedingTrivia, TypeCharacter, If(Overflows, 0.0F, FloatingValue), offset)
        Case NumericLiteralKind.Decimal
          result = MakeDecimalLiteralToken(precedingTrivia, TypeCharacter, If(Overflows, 0D, DecimalValue), offset)
        Case Else
          Throw ExceptionUtilities.UnexpectedValue(literalKind)
      End Select

      If Overflows Then result = DirectCast(result.AddError(ErrorFactory.ErrorInfo(ERRID.ERR_Overflow)), SyntaxToken)
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

    Private Function ScanIntLiteral(ByRef ReturnValue As Integer, ByRef offset As Integer) As Boolean
      Debug.Assert(offset >= 0)
      Dim ch As Char
      If Not TryPeek(offset, ch) OrElse Not IsDecimalDigit(ch) Then Return False

      Dim IntegralValue As Integer = IntegralLiteralCharacterValue(ch)
      offset += 1

      While TryPeek(offset, ch)
        If Not IsDecimalDigit(ch) Then Exit While

        Dim nextDigit = IntegralLiteralCharacterValue(ch)
        If IntegralValue < 214748364 OrElse (IntegralValue = 214748364 AndAlso nextDigit < 8) Then
          IntegralValue = IntegralValue * 10 + nextDigit
          offset += 1
        Else
          Return False
        End If
      End While

      ReturnValue = IntegralValue
      Return True
    End Function

    Private Function ScanDateLiteral(precedingTrivia As SyntaxList(Of VisualBasicSyntaxNode)) As SyntaxToken
      Dim c As Char
      Dim r = TryPeek(c)
      Debug.Assert(r)
      Debug.Assert(IsHash(c))

      Dim offset As Integer = 1 'skip #
      Dim FirstValue, YearValue, MonthValue, DayValue, HourValue, MinuteValue, SecondValue As Integer
      Dim HaveDateValue, HaveYearValue, HaveTimeValue, HaveMinuteValue, HaveSecondValue As Boolean
      Dim HaveAM, HavePM, DateIsInvalid, YearIsTwoDigits As Boolean
      Dim DaysToMonth As Integer() = Nothing

      ' // Unfortunately, we can't fall back on OLE Automation's date parsing because
      ' // they don't have the same range as the URT's DateTime class

      ' // First, eat any whitespace
      offset = GetWhitespaceLength(offset)

      Dim FirstValueStart As Integer = offset

      ' // The first thing has to be an integer, although it's not clear what it is yet
      If Not ScanIntLiteral(FirstValue, offset) Then Return Nothing

      ' // If we see a /, then it's a date

      If TryPeek(offset, c) AndAlso IsDateSeparatorCharacter(c) Then
        Dim FirstDateSeparator As Integer = offset

        ' // We've got a date
        HaveDateValue = True
        offset += 1

        ' Is the first value a year? 
        ' It is a year if it consists of exactly 4 digits.
        ' Condition below uses 5 because we already skipped the separator.
        If offset - FirstValueStart = 5 Then
          HaveYearValue = True
          YearValue = FirstValue

          ' // We have to have a month value
          If Not ScanIntLiteral(MonthValue, offset) Then GoTo baddate

          ' Do we have a day value?
          If TryPeek(offset, c) AndAlso IsDateSeparatorCharacter(c) Then
            ' // Check to see they used a consistent separator

            If c <> Peek(FirstDateSeparator) Then GoTo baddate

            ' // Yes.
            offset += 1

            If Not ScanIntLiteral(DayValue, offset) Then GoTo baddate
          End If
        Else
          ' First value is month
          MonthValue = FirstValue

          ' // We have to have a day value

          If Not ScanIntLiteral(DayValue, offset) Then GoTo baddate

          ' // Do we have a year value?

          If TryPeek(offset, c) AndAlso IsDateSeparatorCharacter(c) Then
            ' // Check to see they used a consistent separator

            If c <> Peek(FirstDateSeparator) Then GoTo baddate

            ' // Yes.
            HaveYearValue = True
            offset += 1

            Dim YearStart As Integer = offset

            If Not ScanIntLiteral(YearValue, offset) Then GoTo baddate

            If (offset - YearStart) = 2 Then YearIsTwoDigits = True
          End If
        End If

        offset = GetWhitespaceLength(offset)
      End If

      ' // If we haven't seen a date, assume it's a time value

      If Not HaveDateValue Then
        HaveTimeValue = True
        HourValue = FirstValue
      Else
        ' // We did see a date. See if we see a time value...

        If ScanIntLiteral(HourValue, offset) Then
          ' // Yup.
          HaveTimeValue = True
        End If
      End If

      If HaveTimeValue Then
        ' // Do we see a :?

        If TryPeek(offset, c) AndAlso IsColon(c) Then
          offset += 1

          ' // Now let's get the minute value

          If Not ScanIntLiteral(MinuteValue, offset) Then GoTo baddate

          HaveMinuteValue = True

          ' // Do we have a second value?

          If TryPeek(offset, c) AndAlso IsColon(c) Then
            ' // Yes.
            HaveSecondValue = True
            offset += 1

            If Not ScanIntLiteral(SecondValue, offset) Then GoTo baddate
          End If
        End If

        offset = GetWhitespaceLength(offset)

        ' // Check AM/PM

        If TryPeek(offset, c) Then
          If c.IsAnyOf("A"c, FULLWIDTH_LATIN_CAPITAL_LETTER_A, "a"c, FULLWIDTH_LATIN_SMALL_LETTER_A) Then

            HaveAM = True
            offset += 1

          ElseIf c.IsAnyOf("P"c, FULLWIDTH_LATIN_CAPITAL_LETTER_P, "p"c, FULLWIDTH_LATIN_SMALL_LETTER_P) Then

            HavePM = True
            offset += 1

          End If

          If TryPeek(offset, c) AndAlso (HaveAM OrElse HavePM) Then
            If c.IsAnyOf("M"c, FULLWIDTH_LATIN_CAPITAL_LETTER_M, "m"c, FULLWIDTH_LATIN_SMALL_LETTER_M) Then
              offset = GetWhitespaceLength(offset + 1)
            Else
              GoTo baddate
            End If
          End If
        End If

        ' // If there's no minute/second value and no AM/PM, it's invalid

        If Not HaveMinuteValue AndAlso Not HaveAM AndAlso Not HavePM Then GoTo baddate
      End If

      If Not CanGet(offset) OrElse Not IsHash(Peek(offset)) Then GoTo baddate

      offset += 1

      ' // OK, now we've got all the values, let's see if we've got a valid date
      If HaveDateValue Then
        If MonthValue < 1 OrElse MonthValue > 12 Then DateIsInvalid = True

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

        If YearIsTwoDigits Then DateIsInvalid = True

        If YearValue < 1 OrElse YearValue > 9999 Then DateIsInvalid = True

      Else
        MonthValue = 1
        DayValue = 1
        YearValue = 1
        DaysToMonth = DaysToMonth365
      End If

      If HaveTimeValue Then
        If HaveAM OrElse HavePM Then
          ' // 12-hour value

          If HourValue < 1 OrElse HourValue > 12 Then DateIsInvalid = True

          If HaveAM Then
            HourValue = HourValue Mod 12
          ElseIf HavePM Then
            HourValue = HourValue + 12

            If HourValue = 24 Then HourValue = 12
          End If

        Else
          If HourValue < 0 OrElse HourValue > 23 Then DateIsInvalid = True
        End If

        If HaveMinuteValue Then
          If MinuteValue < 0 OrElse MinuteValue > 59 Then DateIsInvalid = True
        Else
          MinuteValue = 0
        End If

        If HaveSecondValue Then
          If SecondValue < 0 OrElse SecondValue > 59 Then DateIsInvalid = True
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
        Return MakeDateLiteralToken(precedingTrivia, DateTimeValue, offset)
      Else
        Return MakeBadToken(precedingTrivia, offset, ERRID.ERR_InvalidDate)
      End If

baddate:
      ' // If we can find a closing #, then assume it's a malformed date,
      ' // otherwise, it's not a date
      Dim ch As Char
      While TryPeek(offset, ch) AndAlso Not (IsHash(ch) OrElse IsNewLine(ch))
        offset += 1
      End While

      If Not TryPeek(offset, ch) OrElse IsNewLine(ch) Then
        ' // No closing #
        Return Nothing
      Else
        Debug.Assert(IsHash(Peek(offset)))
        offset += 1  ' consume trailing #
        Return MakeBadToken(precedingTrivia, offset, ERRID.ERR_InvalidDate)
      End If
    End Function

    Private Function ScanStringLiteral(precedingTrivia As SyntaxList(Of VisualBasicSyntaxNode)) As SyntaxToken
      Dim ch, c1, c2 As Char
      Dim r = TryPeek(ch)
      Dim r1 As Boolean
      Debug.Assert(r)
      Debug.Assert(IsDoubleQuote(ch))

      Dim length As Integer = 1
      Dim followingTrivia As SyntaxList(Of VisualBasicSyntaxNode)

      ' // Check for a Char literal, which can be of the form:
      ' // """"c or "<anycharacter-except-">"c
      r = TryPeek(2, c2)
      r1 = TryPeek(1, c1)
      If TryPeek(3, ch) AndAlso IsDoubleQuote(c2) Then
        If IsDoubleQuote(c1) Then
          If IsDoubleQuote(ch) AndAlso TryPeek(4, ch) AndAlso IsLetterC(ch) Then
            ' // Double-quote Char literal: """"c
            Return MakeCharacterLiteralToken(precedingTrivia, """"c, 5)
          End If

        ElseIf IsLetterC(ch) Then
          ' // Char literal.  "x"c
          Return MakeCharacterLiteralToken(precedingTrivia, c1, 4)
        End If
      End If

      If r AndAlso IsDoubleQuote(c1) AndAlso IsLetterC(c2) Then
        ' // Error. ""c is not a legal char constant
        Return MakeBadToken(precedingTrivia, 3, ERRID.ERR_IllegalCharConstant)
      End If

      Dim scratch = GetScratch()
      While TryPeek(length, ch)
        If IsDoubleQuote(ch) Then
          If TryPeek(length + 1, ch) Then
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
          Return SyntaxFactory.StringLiteralToken(spelling, GetScratchText(scratch), precedingTrivia.Node, followingTrivia.Node)

        ElseIf Me.IsScanningDirective AndAlso IsNewLine(ch) Then
          Exit While
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

      If id.PossibleKeywordKind = SyntaxKind.IdentifierToken Then Return False
      k = id.PossibleKeywordKind
      Return True
    End Function

    ''' <summary>
    ''' Try to convert an Identifier to a Keyword.  Called by the parser when it wants to force
    ''' an identifer to be a keyword.
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
      If t Is Nothing Then Return False
      If t.Kind = SyntaxKind.IdentifierToken Then Return TryIdentifierAsContextualKeyword(DirectCast(t, IdentifierTokenSyntax), k)
      Return False
    End Function

    Friend Shared Function TryTokenAsKeyword(t As SyntaxToken, ByRef kind As SyntaxKind) As Boolean
      If t Is Nothing Then Return False
      If t.IsKeyword Then
        kind = t.Kind
        Return True
      End If

      If t.Kind = SyntaxKind.IdentifierToken Then Return TryIdentifierAsContextualKeyword(DirectCast(t, IdentifierTokenSyntax), kind)

      Return False
    End Function

    Friend Shared Function IsContextualKeyword(t As SyntaxToken, ParamArray kinds As SyntaxKind()) As Boolean
      Dim kind As SyntaxKind = Nothing
      If TryTokenAsKeyword(t, kind) Then Return Array.IndexOf(kinds, kind) >= 0
      Return False
    End Function

    Private Function IsIdentifierStartCharacter(c As Char) As Boolean
      Return (_isScanningForExpressionCompiler AndAlso c = "$"c) OrElse SyntaxFacts.IsIdentifierStartCharacter(c)
    End Function
  End Class




End Namespace
