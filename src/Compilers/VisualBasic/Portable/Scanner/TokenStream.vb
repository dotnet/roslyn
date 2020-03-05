' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

'-----------------------------------------------------------------------------
' Contains the definition of the Scanner, which produces tokens from text 
'-----------------------------------------------------------------------------
Option Compare Binary
Option Strict On

Imports System.Runtime.CompilerServices
Imports CoreInternalSyntax = Microsoft.CodeAnalysis.Syntax.InternalSyntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax
    Friend Module ScannerStateExtensions
        <Extension()>
        Friend Function IsVBState(state As ScannerState) As Boolean
            Return state <= ScannerState.VBAllowLeadingMultilineTrivia
        End Function
    End Module

    Friend Enum ScannerState

        ''' <summary>
        ''' Scan VB tokens
        ''' </summary>
        VB

        ''' <summary>
        ''' Scan VB tokens but consume multiline trivia before token. Done at the start of a new statement except after line if.
        ''' </summary>
        VBAllowLeadingMultilineTrivia

        ''' <summary>
        ''' Scan tokens in Xml misc state, these are tokens between document declaration and the root element
        ''' </summary>
        Misc

        ''' <summary>
        ''' Scan tokens inside of &lt;!DOCTYPE ... &gt;
        ''' </summary>
        DocType

        ''' <summary>
        ''' Scan tokens inside of &lt; ... &gt;
        ''' </summary>
        Element

        ''' <summary>
        ''' Scan tokens inside of &lt;/ ...&gt;
        ''' </summary>
        EndElement

        ''' <summary>
        ''' Scan a single quoted string
        ''' </summary>
        SingleQuotedString

        ''' <summary>
        ''' Scan a single quoted string RIGHT_SINGLE_QUOTATION_MARK
        ''' </summary>
        SmartSingleQuotedString

        ''' <summary>
        ''' Scan a quoted string
        ''' </summary>
        QuotedString

        ''' <summary>
        ''' Scan a quoted string RIGHT_DOUBLE_QUOTATION_MARK
        ''' </summary>
        SmartQuotedString

        ''' <summary>
        ''' Scan a string that is missing quotes (error recovery)
        ''' </summary>
        UnQuotedString

        ''' <summary>
        ''' Scan text between markup
        ''' </summary>
        Content

        ''' <summary>
        ''' Scan text inside of &lt;![CDATA[ ... ]]&gt;
        ''' </summary>
        CData

        ''' <summary>
        ''' Scan first text inside f &lt;? ... ?&gt;, the first text can have leading trivia
        ''' </summary>
        StartProcessingInstruction

        ''' <summary>
        ''' Scan remaining text inside of &lt;? ... ?&gt;
        ''' </summary>
        ProcessingInstruction

        ''' <summary>
        ''' Scan text inside of &lt;!-- ... --&gt;
        ''' </summary>
        Comment

        ''' <summary>
        ''' Scan punctuation in an interpolated string.
        ''' </summary>
        InterpolatedStringPunctuation

        ''' <summary>
        ''' Scan interpolated string text content.
        ''' </summary>
        InterpolatedStringContent

        ''' <summary>
        ''' Scan interpolated string format string text content (no newlines).
        ''' </summary>
        InterpolatedStringFormatString

    End Enum

    Partial Friend Class Scanner
        ' Maximum number of tokens in look ahead beyond the end of
        ' line. For resyncing, we may need to look ahead arbitrarily far
        ' up to the end of line. In other cases, we'll look ahead a fixed
        ' number of tokens some of which may be beyond the end of line.
        ' The worst case for look ahead is query expressions with implicit
        ' line continuations such as:
        ' Dim x = From
        '     c
        '     In
        '     ""
        Public Const MaxTokensLookAheadBeyondEOL As Integer = 4

        ' Maximum number of characters to look back. We look back
        ' at most one character, and only in a few scanning cases.
        Public Const MaxCharsLookBehind As Integer = 1

        Private _prevToken As ScannerToken
        Protected _currentToken As ScannerToken
        Private ReadOnly _tokens As New List(Of ScannerToken)

        ''' <summary>
        ''' Crumbles currently available node (if available) into its components.
        ''' The leftmost child becomes the current node.
        ''' If operation is not possible (node has no children, there is no node), then returns false.
        ''' </summary>
        Friend Overridable Function TryCrumbleOnce() As Boolean
            Debug.Assert(False, "regular scanner has nothing to crumble")
            Return False
        End Function

        ''' <summary>
        ''' Gets current reusable syntax node.
        ''' If node is returned its start will be aligned with the start of current token. 
        ''' NOTE: Line offset may not match start of current token because of lookahead. 
        ''' </summary>
        Friend Overridable Function GetCurrentSyntaxNode() As VisualBasicSyntaxNode
            Return Nothing
        End Function

        ''' <summary>
        ''' Indicates that previously returned node has been consumed
        ''' and scanner needs to advance by the size of the node.
        ''' 
        ''' NOTE: the advancement is done relative to the start of the current token.
        ''' Line offset may not match start of current token because of lookahead. 
        ''' 
        ''' This operation will discard lookahead tokens and reset preprocessor state 
        ''' to the state of current token. 
        ''' </summary>
        Friend Overridable Sub MoveToNextSyntaxNode()
            ' do not use prev token after consuming a nonterminal
            _prevToken = Nothing

            ' keep current PP state
            Me._scannerPreprocessorState = _currentToken.PreprocessorState
            ResetTokens()
        End Sub

        ''' <summary>
        ''' Indicates that previously returned node has been consumed
        ''' and scanner needs to advance by the size of the node.
        ''' 
        ''' NOTE: the advancement is done relative to the _lineBufferOffset.
        ''' Line offset will likely not match start of current token because this operation
        ''' is done while constructing the content of current token.
        ''' 
        ''' NOTE: This operation assumes that there is no tokens read ahead.
        ''' 
        ''' NOTE: This operation does not change preprocessor state. 
        ''' The assumption is that it is responsibility of the node consumer to update preprocessor
        ''' state if needed when using nodes that change preprocessor state.
        ''' </summary>
        Friend Overridable Sub MoveToNextSyntaxNodeInTrivia()
            ' do not use prev token after consuming a nonterminal
            _prevToken = Nothing

            Debug.Assert(_tokens.Count = 0)
            Debug.Assert(_currentToken.InnerTokenObject Is Nothing)
        End Sub

        Friend ReadOnly Property LastToken As SyntaxToken
            Get
                Dim count = _tokens.Count
                If count > 0 Then
                    Return _tokens(count - 1).InnerTokenObject
                ElseIf _currentToken.InnerTokenObject IsNot Nothing Then
                    Return _currentToken.InnerTokenObject
                Else
                    Return _prevToken.InnerTokenObject
                End If
            End Get
        End Property

        Friend ReadOnly Property PrevToken As SyntaxToken
            Get
                Return _prevToken.InnerTokenObject
            End Get
        End Property

        Friend Function GetCurrentToken() As SyntaxToken
            Dim tk = _currentToken.InnerTokenObject
            If tk Is Nothing Then
                Debug.Assert(_currentToken.PreprocessorState Is _scannerPreprocessorState)
                Debug.Assert(_currentToken.Position = _lineBufferOffset)

                Dim state = _currentToken.State
                tk = GetScannerToken(state)

                _currentToken = _currentToken.With(state, tk)
            End If
            Return tk
        End Function

        Friend Sub ResetCurrentToken(state As ScannerState)
            If state <> _currentToken.State Then

                ' this is a special case for switching from VB to Xml
                ' we need to keep preceding trivia as it was scanned 
                If _currentToken.State = ScannerState.VB AndAlso state = ScannerState.Content Then

                    Dim vbTk = GetCurrentToken()

                    AbandonAllTokens()

                    ' skip VB trivia
                    Dim afterTrivia = _currentToken.Position + vbTk.GetLeadingTriviaWidth
                    _lineBufferOffset = afterTrivia

                    Dim xmlTk = GetScannerToken(state)

                    ' we need to add and not replace the leading trivia because the vb token
                    ' could have been a StatementTerminatorToken. In that case the xml token would have
                    ' the CR/LF as leading trivia already and the statement terminator would not have any
                    ' leading trivia. Replacing the trivia would drop the LF (Roslyn Bug 7954)
                    xmlTk = SyntaxToken.AddLeadingTrivia(xmlTk, vbTk.GetLeadingTrivia())

                    _currentToken = _currentToken.With(state, xmlTk)
                Else
                    AbandonAllTokens()
                    Debug.Assert(_currentToken.Position = _lineBufferOffset)
                    Debug.Assert(_currentToken.EndOfTerminatorTrivia = _endOfTerminatorTrivia)
                    _currentToken = _currentToken.With(state, Nothing)
                End If
            End If
        End Sub

        Friend Sub RescanTrailingColonAsToken(ByRef prevToken As SyntaxToken, ByRef currentToken As SyntaxToken)
            Dim tk = _prevToken.InnerTokenObject

            Debug.Assert(tk IsNot Nothing)
            Debug.Assert(tk.Width > 0)
            Debug.Assert(tk.HasTrailingTrivia())
            Debug.Assert(tk.LastTriviaIfAny().Kind = SyntaxKind.ColonTrivia)
            Debug.Assert(_currentToken.InnerTokenObject IsNot Nothing)
            Debug.Assert(_currentToken.InnerTokenObject.Kind = SyntaxKind.ColonToken)

            AbandonAllTokens()
            RevertState(_prevToken)

            Dim state = ScannerState.VB
            tk = DirectCast(tk.WithTrailingTrivia(Nothing), SyntaxToken)
            Dim offset = tk.FullWidth
            _lineBufferOffset += offset
            _endOfTerminatorTrivia = _lineBufferOffset
            _prevToken = _prevToken.With(state, tk)
            prevToken = tk
            _currentToken = New ScannerToken(_scannerPreprocessorState, _lineBufferOffset, _endOfTerminatorTrivia, Nothing, state)

            Dim tList = _triviaListPool.Allocate()
            ScanSingleLineTrivia(tList)
            Debug.Assert(tList.Count > 0)
            Dim lastTrivia = DirectCast(tList(tList.Count - 1), SyntaxTrivia)
            tList.RemoveLast()
            Dim precedingTrivia = MakeTriviaArray(tList)
            _triviaListPool.Free(tList)

            Debug.Assert(lastTrivia.Kind = SyntaxKind.ColonTrivia)
            Debug.Assert(lastTrivia.Width = 1)
            Debug.Assert(_lineBufferOffset < _endOfTerminatorTrivia)
            _lineBufferOffset = _endOfTerminatorTrivia

            ' Include trailing trivia since this colon is a token not a terminator.
            Dim followingTrivia = ScanSingleLineTrivia()
            tk = MakePunctuationToken(SyntaxKind.ColonToken, lastTrivia.Text, precedingTrivia, followingTrivia)

            _currentToken = _currentToken.With(state, tk)
            currentToken = tk
        End Sub

        Friend Sub TransitionFromXmlToVB(toCompare As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode), ByRef toRemove As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode), ByRef toAdd As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode))
            Dim tk = _prevToken.InnerTokenObject
            Debug.Assert(tk IsNot Nothing)

            ' If _currentToken is EndOfXmlToken, then we've reached the end of an
            ' XML document, possibly followed by multiline trivia. In that case, we
            ' need to include any additional blank lines following the previous token.
            Dim includeFollowingBlankLines = _currentToken.InnerTokenObject IsNot Nothing AndAlso
                _currentToken.InnerTokenObject.Kind = SyntaxKind.EndOfXmlToken

            AbandonAllTokens()
            RevertState(_prevToken)

            Dim trivia = New CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode)(tk.GetTrailingTrivia())
            Dim index = trivia.Count - GetLengthOfCommonEnd(trivia, toCompare)
            toRemove = trivia.GetEndOfTrivia(index)

            tk = DirectCast(tk.WithTrailingTrivia(trivia.GetStartOfTrivia(index).Node), SyntaxToken)
            Dim offset = GetFullWidth(_prevToken, tk)
            _lineBufferOffset += offset
            _endOfTerminatorTrivia = _lineBufferOffset

            toAdd = ScanSingleLineTrivia(includeFollowingBlankLines)

            Dim state = ScannerState.VB
            tk = SyntaxToken.AddTrailingTrivia(tk, toAdd.Node)
            _prevToken = _prevToken.With(state, tk)
            _currentToken = New ScannerToken(_scannerPreprocessorState, _lineBufferOffset, _endOfTerminatorTrivia, Nothing, state)
        End Sub

        Friend Sub TransitionFromVBToXml(state As ScannerState, toCompare As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode), ByRef toRemove As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode), ByRef toAdd As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode))
            Dim tk = _prevToken.InnerTokenObject
            Debug.Assert(tk IsNot Nothing)

            AbandonAllTokens()
            RevertState(_prevToken)

            Dim trivia = New CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode)(tk.GetTrailingTrivia())
            Dim index = trivia.Count - GetLengthOfCommonEnd(trivia, toCompare)
            toRemove = trivia.GetEndOfTrivia(index)

            tk = DirectCast(tk.WithTrailingTrivia(trivia.GetStartOfTrivia(index).Node), SyntaxToken)
            Dim offset = GetFullWidth(_prevToken, tk)
            _lineBufferOffset += offset
            _endOfTerminatorTrivia = _lineBufferOffset

            toAdd = Nothing
            _prevToken = _prevToken.With(_prevToken.State, tk)
            _currentToken = New ScannerToken(_scannerPreprocessorState, _lineBufferOffset, _endOfTerminatorTrivia, Nothing, state)
        End Sub

        Private Shared Function GetFullWidth(token As ScannerToken, tk As SyntaxToken) As Integer
            Debug.Assert(token.InnerTokenObject IsNot Nothing)
            Debug.Assert(tk.Kind = token.InnerTokenObject.Kind)

            If tk.Width = 0 AndAlso SyntaxFacts.IsTerminator(tk.Kind) Then
                Debug.Assert(token.Position < token.EndOfTerminatorTrivia)
                Debug.Assert(tk.FullWidth = 0)
                Return token.EndOfTerminatorTrivia - token.Position
            Else
                Return tk.FullWidth
            End If
        End Function

        Friend Sub GetNextTokenInState(state As ScannerState)
            _prevToken = _currentToken

            If _tokens.Count = 0 Then
                _currentToken = New ScannerToken(_scannerPreprocessorState, _lineBufferOffset, _endOfTerminatorTrivia, Nothing, state)
            Else
                _currentToken = _tokens(0)
                _tokens.RemoveAt(0)
                ResetCurrentToken(state)
            End If
        End Sub

        Friend Function PeekNextToken(state As ScannerState) As SyntaxToken
            If _tokens.Count > 0 Then
                Dim tk = _tokens(0)
                If tk.State = state Then
                    Return tk.InnerTokenObject
                Else
                    AbandonPeekedTokens()
                End If
            End If

            ' ensure that current token has been read
            GetCurrentToken()
            Return GetTokenAndAddToQueue(state)
        End Function

        ''' <summary>
        ''' note that state is applied only to the token #1
        ''' </summary>
        Friend Function PeekToken(tokenOffset As Integer, state As ScannerState) As SyntaxToken
            Debug.Assert(tokenOffset >= 0)
#If DEBUG Then
            Dim terminatorOffset = -1
            For i = 0 To _tokens.Count - 1
                If _tokens(i).InnerTokenObject.Kind = SyntaxKind.StatementTerminatorToken Then
                    terminatorOffset = i
                    Exit For
                End If
            Next
            If terminatorOffset >= 0 Then
                Debug.Assert(tokenOffset <= terminatorOffset + MaxTokensLookAheadBeyondEOL)
            End If
#End If

            ' peeking current token is strange, but ok. Note that it ignores the state.
            If tokenOffset = 0 Then
                Debug.Assert(_currentToken.State = state)

                Return GetCurrentToken()
            End If

            ' just want the token #1
            If tokenOffset = 1 Then
                Return PeekNextToken(state)
            End If

            Dim offsetInQueue = tokenOffset - 1
            Debug.Assert(offsetInQueue <= _tokens.Count)

            ' asking for next after already read (common case)
            If offsetInQueue = _tokens.Count Then
                Return GetTokenAndAddToQueue(state)
            End If

            ' already have in right state
            If offsetInQueue < _tokens.Count AndAlso _tokens(offsetInQueue).State = state Then
                Return _tokens(offsetInQueue).InnerTokenObject
            End If

            ' we have tokens at given offset (and maybe after), but they are not in right state.
            ' need to rollback

            ' rollback to slot at index
            RevertState(_tokens(offsetInQueue))

            ' kill tokens at current slot and after
            _tokens.RemoveRange(offsetInQueue, _tokens.Count - offsetInQueue)

            Return GetTokenAndAddToQueue(state)
        End Function

        Private Function GetTokenAndAddToQueue(state As ScannerState) As SyntaxToken
            Dim lineBufferOffset = _lineBufferOffset
            Dim endOfTerminatorTrivia = _endOfTerminatorTrivia
            Dim ppState = _scannerPreprocessorState
            Dim tk = GetScannerToken(state)
            _tokens.Add(New ScannerToken(ppState, lineBufferOffset, endOfTerminatorTrivia, tk, state))
            Return tk
        End Function

        Private Sub AbandonAllTokens()
            RevertState(_currentToken)
            _tokens.Clear()
            _currentToken = _currentToken.With(ScannerState.VB, Nothing)
        End Sub

        ' resync token stream to a further position
        Private Sub ResetTokens()
            Debug.Assert(_lineBufferOffset >= _currentToken.Position)
            _tokens.Clear()
            _currentToken = New ScannerToken(_scannerPreprocessorState, _lineBufferOffset, _endOfTerminatorTrivia, Nothing, ScannerState.VB)
        End Sub

        Private Sub AbandonPeekedTokens()
            If _tokens.Count = 0 Then
                Return
            End If

            RevertState(_tokens(0))
            _tokens.Clear()
        End Sub

        Friend Structure RestorePoint
            Private ReadOnly _scanner As Scanner
            Private ReadOnly _currentToken As ScannerToken
            Private ReadOnly _prevToken As ScannerToken
            Private ReadOnly _tokens As ScannerToken()
            Private ReadOnly _lineBufferOffset As Integer
            Private ReadOnly _endOfTerminatorTrivia As Integer
            Private ReadOnly _scannerPreprocessorState As PreprocessorState

            Friend Sub New(scanner As Scanner)
                Me._scanner = scanner
                Me._currentToken = scanner._currentToken
                Me._prevToken = scanner._prevToken
                Me._tokens = scanner.SaveAndClearTokens()
                Me._lineBufferOffset = scanner._lineBufferOffset
                Me._endOfTerminatorTrivia = scanner._endOfTerminatorTrivia
                Me._scannerPreprocessorState = scanner._scannerPreprocessorState
            End Sub

            Friend Sub RestoreTokens(includeLookAhead As Boolean)
                _scanner._currentToken = Me._currentToken
                _scanner._prevToken = Me._prevToken
                _scanner.RestoreTokens(If(includeLookAhead, Me._tokens, Nothing))
            End Sub

            Friend Sub Restore()
                _scanner._currentToken = Me._currentToken
                _scanner._prevToken = Me._prevToken
                _scanner.RestoreTokens(Me._tokens)
                _scanner._lineBufferOffset = Me._lineBufferOffset
                _scanner._endOfTerminatorTrivia = Me._endOfTerminatorTrivia
                _scanner._scannerPreprocessorState = Me._scannerPreprocessorState
            End Sub
        End Structure

        Friend Function CreateRestorePoint() As RestorePoint
            Return New RestorePoint(Me)
        End Function

        Private Function SaveAndClearTokens() As ScannerToken()
            If _tokens.Count = 0 Then
                Return Nothing
            End If
            Dim tokens = _tokens.ToArray()
            _tokens.Clear()
            Return tokens
        End Function

        Private Sub RestoreTokens(tokens As ScannerToken())
            _tokens.Clear()
            If tokens IsNot Nothing Then
                _tokens.AddRange(tokens)
            End If
        End Sub

        Private Structure LineBufferAndEndOfTerminatorOffsets
            Private ReadOnly _scanner As Scanner
            Private ReadOnly _lineBufferOffset As Integer
            Private ReadOnly _endOfTerminatorTrivia As Integer

            Public Sub New(scanner As Scanner)
                _scanner = scanner
                _lineBufferOffset = scanner._lineBufferOffset
                _endOfTerminatorTrivia = scanner._endOfTerminatorTrivia
            End Sub

            Public Sub Restore()
                _scanner._lineBufferOffset = _lineBufferOffset
                _scanner._endOfTerminatorTrivia = _endOfTerminatorTrivia
            End Sub
        End Structure

        Private Function CreateOffsetRestorePoint() As LineBufferAndEndOfTerminatorOffsets
            Return New LineBufferAndEndOfTerminatorOffsets(Me)
        End Function

        Private Sub ResetLineBufferOffset()
            _lineBufferOffset = _currentToken.Position
            _endOfTerminatorTrivia = _lineBufferOffset
        End Sub

        Private Sub RevertState(revertTo As ScannerToken)
            _lineBufferOffset = revertTo.Position
            _endOfTerminatorTrivia = revertTo.EndOfTerminatorTrivia
            _scannerPreprocessorState = revertTo.PreprocessorState
        End Sub

        Private Function GetScannerToken(state As ScannerState) As SyntaxToken
            Dim token As SyntaxToken = Nothing

            Select Case state
                Case ScannerState.VB
                    token = Me.GetNextToken(allowLeadingMultilineTrivia:=False)

                Case ScannerState.VBAllowLeadingMultilineTrivia
                    token = Me.GetNextToken(allowLeadingMultilineTrivia:=Not _isScanningDirective)

                Case ScannerState.Misc
                    token = Me.ScanXmlMisc()

                Case ScannerState.Element,
                     ScannerState.EndElement,
                     ScannerState.DocType
                    token = Me.ScanXmlElement(state)

                Case ScannerState.Content
                    token = Me.ScanXmlContent()

                Case ScannerState.CData
                    token = Me.ScanXmlCData()

                Case ScannerState.StartProcessingInstruction,
                    ScannerState.ProcessingInstruction
                    token = Me.ScanXmlPIData(state)

                Case ScannerState.Comment
                    token = Me.ScanXmlComment()

                Case ScannerState.SingleQuotedString
                    token = Me.ScanXmlStringSingle()

                Case ScannerState.SmartSingleQuotedString
                    token = Me.ScanXmlStringSmartSingle()

                Case ScannerState.QuotedString
                    token = Me.ScanXmlStringDouble()

                Case ScannerState.SmartQuotedString
                    token = Me.ScanXmlStringSmartDouble()

                Case ScannerState.UnQuotedString
                    token = Me.ScanXmlStringUnQuoted()

                Case ScannerState.InterpolatedStringPunctuation
                    token = Me.ScanInterpolatedStringPunctuation()

                Case ScannerState.InterpolatedStringContent
                    token = Me.ScanInterpolatedStringContent()

                Case ScannerState.InterpolatedStringFormatString
                    token = Me.ScanInterpolatedStringFormatString()

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(state)

            End Select

            Return token
        End Function

        Protected Structure ScannerToken
            Friend Sub New(preprocessorState As PreprocessorState,
                           lineBufferOffset As Integer,
                           endOfTerminatorTrivia As Integer,
                           token As SyntaxToken,
                           state As ScannerState)
                Me.PreprocessorState = preprocessorState
                Me.Position = lineBufferOffset
                Me.EndOfTerminatorTrivia = endOfTerminatorTrivia
                Me.InnerTokenObject = token
                Me.State = state
            End Sub

            Friend Function [With](state As ScannerState, token As SyntaxToken) As ScannerToken
                Return New ScannerToken(Me.PreprocessorState, Me.Position, Me.EndOfTerminatorTrivia, token, state)
            End Function

            Friend Function [With](preprocessorState As PreprocessorState) As ScannerToken
                Return New ScannerToken(preprocessorState, Me.Position, Me.EndOfTerminatorTrivia, Me.InnerTokenObject, Me.State)
            End Function

            Public ReadOnly InnerTokenObject As SyntaxToken
            Public ReadOnly Position As Integer
            Public ReadOnly EndOfTerminatorTrivia As Integer
            Public ReadOnly State As ScannerState
            Public ReadOnly PreprocessorState As PreprocessorState
        End Structure

    End Class
End Namespace
