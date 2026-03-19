' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

'-----------------------------------------------------------------------------
' Contains the definition of the Scanner, which produces tokens from text 
'-----------------------------------------------------------------------------
Option Compare Binary
Option Strict On

Imports Microsoft.CodeAnalysis.Syntax.InternalSyntax
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax
    Friend NotInheritable Class Blender
        Inherits Scanner

        ''' <summary>
        ''' Candidate nodes that may be reused.
        ''' </summary>
        Private ReadOnly _nodeStack As New Stack(Of GreenNode)

        ''' <summary>
        ''' The text changes combined into a single region.
        ''' </summary>
        Private ReadOnly _change As TextChangeRange

        ''' <summary>
        ''' The range from which we cannot reuse nodes.
        ''' </summary>
        Private ReadOnly _affectedRange As TextChangeRange

        ''' <summary>
        ''' Current node. Not necessarily reusable or even a NonTerminal.
        ''' Can be null if we are out of nodes.
        ''' </summary>
        Private _currentNode As VisualBasicSyntaxNode
        Private _curNodeStart As Integer
        Private _curNodeLength As Integer

        Private ReadOnly _baseTreeRoot As VisualBasic.VisualBasicSyntaxNode

        ''' <summary>
        ''' preprocessor state before _currentNode
        ''' </summary>
        Private _currentPreprocessorState As PreprocessorState

        ''' <summary>
        ''' preprocessor state getter after _currentNode
        ''' </summary>
        Private _nextPreprocessorStateGetter As NextPreprocessorStateGetter

        Private Shared Sub PushReverseNonterminal(stack As Stack(Of GreenNode), nonterminal As GreenNode)
            Dim cnt = nonterminal.SlotCount
            For i As Integer = 1 To cnt
                Dim child = nonterminal.GetSlot(cnt - i)
                PushChildReverse(stack, child)
            Next
        End Sub

        Private Shared Sub PushReverseTerminal(stack As Stack(Of GreenNode), tk As SyntaxToken)
            Dim trivia = tk.GetTrailingTrivia

            If trivia IsNot Nothing Then
                PushChildReverse(stack, trivia)
            End If

            PushChildReverse(stack, DirectCast(tk.WithLeadingTrivia(Nothing).WithTrailingTrivia(Nothing), SyntaxToken))

            trivia = tk.GetLeadingTrivia

            If trivia IsNot Nothing Then
                PushChildReverse(stack, trivia)
            End If
        End Sub

        Private Shared Sub PushChildReverse(stack As Stack(Of GreenNode), child As GreenNode)
            If child IsNot Nothing Then
                If child.IsList Then
                    PushReverseNonterminal(stack, child)
                Else
                    stack.Push(child)
                End If
            End If
        End Sub

        ''' <summary>
        ''' Expand the span in the tree to encompass the
        ''' nearest statements that the span overlaps.
        ''' </summary>
        Private Shared Function ExpandToNearestStatements(root As VisualBasic.VisualBasicSyntaxNode, span As TextSpan) As TextSpan
            Dim fullSpan = New TextSpan(0, root.FullWidth)
            Dim start = NearestStatementThatContainsPosition(root, span.Start, fullSpan)
            Debug.Assert(start.Start <= span.Start)
            If span.Length = 0 Then
                Return start
            Else
                Dim [end] = NearestStatementThatContainsPosition(root, span.End - 1, fullSpan)
                Debug.Assert([end].End >= span.End)
                Return TextSpan.FromBounds(start.Start, [end].End)
            End If
        End Function

        ''' <remarks>
        ''' Not guaranteed to return the span of a StatementSyntax.
        ''' </remarks>
        Private Shared Function NearestStatementThatContainsPosition(
            node As SyntaxNode,
            position As Integer,
            rootFullSpan As TextSpan) As TextSpan

            If Not node.FullSpan.Contains(position) Then
                Debug.Assert(node.FullSpan.End = position)
                Return New TextSpan(position, 0)
            End If

            If node.Kind = SyntaxKind.CompilationUnit OrElse IsStatementLike(node) Then
                Do
                    Dim child = node.ChildThatContainsPosition(position).AsNode()
                    If child Is Nothing OrElse Not IsStatementLike(child) Then
                        Return node.FullSpan
                    End If
                    node = child
                Loop
            End If

            Return rootFullSpan
        End Function

        Private Shared Function IsStatementLike(node As SyntaxNode) As Boolean
            Select Case node.Kind
                Case SyntaxKind.ElseIfBlock,
                     SyntaxKind.ElseBlock,
                     SyntaxKind.CatchBlock,
                     SyntaxKind.FinallyBlock

                    Return node.GetTrailingTrivia().Any(SyntaxKind.EndOfLineTrivia)
                Case SyntaxKind.SingleLineIfStatement,
                     SyntaxKind.SingleLineElseClause
                    ' Steer clear of single-line if's because they have custom handling of statement 
                    ' terminators that may make it difficult to reuse sub-statements.
                    Return False
                Case Else
                    Return TypeOf node Is Syntax.StatementSyntax
            End Select
        End Function

        ''' <summary>
        ''' Expand the span in the tree by the maximum number
        ''' of tokens required for look ahead and the maximum
        ''' number of characters for look behind.
        ''' </summary>
        Private Shared Function ExpandByLookAheadAndBehind(root As VisualBasic.VisualBasicSyntaxNode, span As TextSpan) As TextSpan
            Dim fullWidth = root.FullWidth
            Dim start = Math.Min(span.Start, Math.Max(0, fullWidth - 1))
            Dim [end] = span.End

            If start > 0 Then
                ' Move to the left by the look ahead required by the Scanner.
                For i As Integer = 0 To Scanner.MaxTokensLookAheadBeyondEOL
                    Dim node = root.FindTokenInternal(start)
                    If node.Kind = SyntaxKind.None Then
                        Exit For
                    Else
                        start = node.Position
                        If start = 0 Then
                            Exit For
                        Else
                            start -= 1
                        End If
                    End If
                Next
            End If

            ' Allow for look behind of some number of characters.
            If [end] < fullWidth Then
                [end] += Scanner.MaxCharsLookBehind
            End If

            Return TextSpan.FromBounds(start, [end])
        End Function

        Friend Sub New(newText As SourceText,
                       changes As TextChangeRange(),
                       baseTreeRoot As SyntaxTree,
                       options As VisualBasicParseOptions)

            MyBase.New(newText, options)

            ' initially blend state and scanner state are same
            _currentPreprocessorState = _scannerPreprocessorState
            _nextPreprocessorStateGetter = Nothing

            _baseTreeRoot = baseTreeRoot.GetVisualBasicRoot()
            _currentNode = _baseTreeRoot.VbGreen
            _curNodeStart = 0
            _curNodeLength = 0

            TryCrumbleOnce()

            If _currentNode Is Nothing Then
                Return  ' tree seems to be empty
            End If

            _change = TextChangeRange.Collapse(changes)

#If DEBUG Then
            Dim start = _change.Span.Start
            Dim [end] = _change.Span.End
            Debug.Assert(start >= 0)
            Debug.Assert(start <= [end])
            Debug.Assert([end] <= _baseTreeRoot.FullWidth)
#End If

            ' Parser requires look ahead of some number of tokens
            ' beyond EOL and some number of characters back.
            ' Expand the change range to accommodate look ahead/behind.
            Dim span = ExpandToNearestStatements(
                _baseTreeRoot,
                ExpandByLookAheadAndBehind(_baseTreeRoot, _change.Span))
            _affectedRange = New TextChangeRange(span, span.Length - _change.Span.Length + _change.NewLength)
        End Sub

        Private Function MapNewPositionToOldTree(position As Integer) As Integer
            If position < _change.Span.Start Then
                Return position
            End If

            If position >= _change.Span.Start + _change.NewLength Then
                Return position - _change.NewLength + _change.Span.Length
            End If

            Return -1
        End Function

        ''' <summary>
        ''' Moving to the next node on the stack.
        ''' returns false if we are out of nodes.
        ''' </summary>
        Private Function TryPopNode() As Boolean
            If _nodeStack.Count > 0 Then
                Dim node = _nodeStack.Pop
                _currentNode = DirectCast(node, VisualBasicSyntaxNode)
                _curNodeStart = _curNodeStart + _curNodeLength
                _curNodeLength = node.FullWidth

                ' move blender preprocessor state forward if possible
                If _nextPreprocessorStateGetter.Valid Then
                    _currentPreprocessorState = _nextPreprocessorStateGetter.State()
                End If

                _nextPreprocessorStateGetter = New NextPreprocessorStateGetter(_currentPreprocessorState, DirectCast(node, VisualBasicSyntaxNode))

                Return True
            Else
                _currentNode = Nothing
                Return False
            End If
        End Function

        ''' <summary>
        ''' Crumbles current node onto the stack and pops one node into current.
        ''' Returns false if current node cannot be crumbled.
        ''' </summary>
        Friend Overrides Function TryCrumbleOnce() As Boolean
            If _currentNode Is Nothing Then
                Return False
            End If

            If _currentNode.SlotCount = 0 Then
                If Not _currentNode.ContainsStructuredTrivia Then
                    ' terminal with no structured trivia is not interesting
                    Return False
                End If

                ' try reusing structured trivia (in particular XML)
                PushReverseTerminal(_nodeStack, DirectCast(_currentNode, SyntaxToken))
            Else
                If Not ShouldCrumble(_currentNode) Then
                    Return False
                End If

                PushReverseNonterminal(_nodeStack, _currentNode)
            End If

            ' crumbling does not affect start, but length is set to 0 until we see a node
            _curNodeLength = 0

            ' crumbling doesn't move things forward. discard next preprocessor state we calculated before
            _nextPreprocessorStateGetter = Nothing

            Return TryPopNode()
        End Function

        ''' <summary>
        ''' Certain syntax node kinds should not be crumbled since
        ''' re-using individual child nodes may complicate parsing.
        ''' </summary>
        Private Shared Function ShouldCrumble(node As VisualBasicSyntaxNode) As Boolean
            If TypeOf node Is StructuredTriviaSyntax Then
                ' Do not crumble into structured trivia content.
                ' we will not use any of the parts anyways and 
                ' evaluation of directives may go out of sync.
                Return False
            End If

            Select Case node.Kind
                Case SyntaxKind.SingleLineIfStatement,
                    SyntaxKind.SingleLineElseClause
                    ' Parsing of single line If is particularly complicated
                    ' since the statement may contain colon separated or
                    ' multi-line statements. Avoid re-using child nodes.
                    Return False

                Case SyntaxKind.EnumBlock
                    ' Interpretation of other nodes within Enum block
                    ' may depend on the kind of this node.
                    Return False

                Case Else
                    Return True

            End Select
        End Function

        ''' <summary>
        ''' Advances to given position if needed (note: no way back)
        ''' Gets a nonterminal that can be used for incremental.
        ''' May return Nothing if such node is not available.
        ''' Typically it is _currentNode.
        ''' </summary>
        Private Function GetCurrentNode(position As Integer) As VisualBasicSyntaxNode
            Debug.Assert(_currentNode IsNot Nothing)

            Dim mappedPosition = MapNewPositionToOldTree(position)

            If mappedPosition = -1 Then
                Return Nothing
            End If

            Do
                ' too far ahead
                If _curNodeStart > mappedPosition Then
                    Return Nothing
                End If

                ' node ends before or on the mappedPosition
                ' whole node is unusable, move to the next node
                If (_curNodeStart + _curNodeLength) <= mappedPosition Then
                    If TryPopNode() Then
                        Continue Do
                    Else
                        Return Nothing
                    End If
                End If

                If _curNodeStart = mappedPosition AndAlso CanReuseNode(_currentNode) Then
                    ' have some node
                    Exit Do
                End If

                ' current node spans the position or node is not usable
                ' try crumbling and look through children
                If Not TryCrumbleOnce() Then
                    Return Nothing
                End If
            Loop

            ' zero-length nodes are ambiguous when given a particular position
            ' also the vast majority of such nodes are synthesized
            Debug.Assert(_currentNode.FullWidth > 0, "reusing zero-length nodes?")
            Return _currentNode
        End Function

        ''' <summary>
        ''' Returns current candidate for reuse if there is one.
        ''' </summary>
        Friend Overrides Function GetCurrentSyntaxNode() As VisualBasicSyntaxNode
            ' not going to get any nodes if there is no current node.
            If _currentNode Is Nothing Then
                Return Nothing
            End If

            ' node must start where the current token starts.
            Dim start = _currentToken.Position

            ' position is in affected range - no point trying.
            Dim range = New TextSpan(_affectedRange.Span.Start, _affectedRange.NewLength)
            If range.Contains(start) Then
                Return Nothing
            End If

            Dim nonterminal = GetCurrentNode(start)
            Return nonterminal
        End Function

        ''' <summary>
        ''' Checks if node is reusable.
        ''' The reasons for it not be usable are typically that it intersects affected range.
        ''' </summary>
        Private Function CanReuseNode(node As VisualBasicSyntaxNode) As Boolean
            If node Is Nothing Then
                Return False
            End If

            If node.SlotCount = 0 Then
                Return False
            End If

            ' TODO: This is a temporary measure to get around contextual errors.
            ' The problem is that some errors are contextual, but get attached to inner nodes.
            ' as a result in a case when an edit changes the context the error may be invalidated
            ' but since the node with actual error did not change the error will stay.
            If node.ContainsDiagnostics Then
                Return False
            End If

            ' As of 2013/03/14, the compiler never attempts to incrementally parse a tree containing
            ' annotations.  Our goal in instituting this restriction is to prevent API clients from
            ' taking a dependency on the survival of annotations.
            If node.ContainsAnnotations Then
                Return False
            End If

            ' If the node is an If statement, we need to determine whether it is a
            ' single-line or multi-line If. That requires the scanner to be positioned
            ' correctly relative to the end of line terminator if any, and currently we
            ' do not guarantee that. (See bug #16557.) For now, for simplicity, we
            ' do not reuse If statements.
            If node.Kind = SyntaxKind.IfStatement Then
                Return False
            End If

            Dim _curNodeSpan = New TextSpan(_curNodeStart, _curNodeLength)
            ' TextSpan.OverlapsWith does not handle empty spans so
            ' empty spans need to be handled explicitly.
            Debug.Assert(_curNodeSpan.Length > 0)
            If _affectedRange.Span.Length = 0 Then
                If _curNodeSpan.Contains(_affectedRange.Span.Start) Then
                    Return False
                End If
            Else
                If _curNodeSpan.OverlapsWith(_affectedRange.Span) Then
                    Return False
                End If
            End If

            ' we cannot use nodes that contain directives since we need to process
            ' directives individually.
            ' We however can use individual directives.
            If node.ContainsDirectives AndAlso Not TypeOf node Is DirectiveTriviaSyntax Then
                Return _scannerPreprocessorState.IsEquivalentTo(_currentPreprocessorState)
            End If

            ' sometimes nodes contain linebreaks in leading trivia
            ' if we are in VBAllowLeadingMultilineTrivia state (common case), it is ok.
            ' otherwise nodes with leading trivia containing linebreaks should be rejected.
            If Not Me._currentToken.State = ScannerState.VBAllowLeadingMultilineTrivia AndAlso
                ContainsLeadingLineBreaks(node) Then

                Return False
            End If

            If _currentNode.IsMissing Then
                Return Nothing
            End If

            Return True
        End Function

        Private Function ContainsLeadingLineBreaks(node As VisualBasicSyntaxNode) As Boolean
            Dim lt = node.GetLeadingTrivia
            If lt IsNot Nothing Then
                If lt.RawKind = SyntaxKind.EndOfLineTrivia Then
                    Return True
                End If

                Dim asList = TryCast(lt, CodeAnalysis.Syntax.InternalSyntax.SyntaxList)
                If asList IsNot Nothing Then
                    For i As Integer = 0 To asList.SlotCount - 1
                        If lt.GetSlot(i).RawKind = SyntaxKind.EndOfLineTrivia Then
                            Return True
                        End If
                    Next
                End If
            End If

            Return False
        End Function

        Friend Overrides Sub MoveToNextSyntaxNode()
            If _currentNode Is Nothing Then
                Return
            End If

            Debug.Assert(CanReuseNode(_currentNode), "this node could not have been used.")
            Debug.Assert(_nextPreprocessorStateGetter.Valid, "we should have _nextPreprocessorState")

            Dim nextPreprocessorState = _nextPreprocessorStateGetter.State()
            Debug.Assert(nextPreprocessorState.ConditionalStack.Count = 0 OrElse
                         nextPreprocessorState.ConditionalStack.Peek.BranchTaken = ConditionalState.BranchTakenState.Taken,
                        "how could a parser in taken PP state?")

            ' update buffer offset relative to current token
            _lineBufferOffset = _currentToken.Position + _curNodeLength

            ' sync current token's preprocessor state to blender preprocessor state
            ' it is safe to do so here since if we are here, it means we can reuse information in old tree up to this point
            ' including preprocessor state
            ' *NOTE* we are using _nextPreprocessorState instead of _currentPreprocessorState because
            ' we are actually moving things forward here. _nextPreprocessorState will become _currentPreprocessorState
            ' at the "TryPopNode" below.
            If _currentNode.ContainsDirectives Then
                _currentToken = _currentToken.With(nextPreprocessorState)
            End If

            ' this will discard any prefetched tokens, including current. 
            ' We do not need them since we moved to completely new node.
            MyBase.MoveToNextSyntaxNode()

            TryPopNode()

            ' at this point, all three pointers (position in old tree, position in text, position in parser)
            ' and all preprocessor state (_currentPreprocessorState, _scannerPreprocessorState, _currentToken.PreprocessorState) 
            ' should point to same position (in sync)
        End Sub

        Friend Overrides Sub MoveToNextSyntaxNodeInTrivia()
            If _currentNode Is Nothing Then
                Return
            End If

            Debug.Assert(CanReuseNode(_currentNode), "this node could not have been used.")

            ' just move forward
            _lineBufferOffset = _lineBufferOffset + _curNodeLength

            ' this will just verify that we do not have any prefetched tokens, including current. 
            ' otherwise advancing line buffer offset could go out of sync with token stream.
            MyBase.MoveToNextSyntaxNodeInTrivia()

            TryPopNode()
        End Sub

        Private Structure NextPreprocessorStateGetter
            Private ReadOnly _state As PreprocessorState
            Private ReadOnly _node As VisualBasicSyntaxNode

            Private _nextState As PreprocessorState

            Public Sub New(state As PreprocessorState, node As VisualBasicSyntaxNode)
                Me._state = state
                Me._node = node
                Me._nextState = Nothing
            End Sub

            Public ReadOnly Property Valid As Boolean
                Get
                    Return _node IsNot Nothing
                End Get
            End Property

            Public Function State() As PreprocessorState
                If _nextState Is Nothing Then
                    _nextState = ApplyDirectives(Me._state, Me._node)
                End If

                Return _nextState
            End Function
        End Structure
    End Class
End Namespace
