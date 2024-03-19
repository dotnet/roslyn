' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

'-----------------------------------------------------------------------------
' Contains scanner functionality related to Directives and Preprocessor
'-----------------------------------------------------------------------------
Option Compare Binary
Option Strict On

Imports System.Collections.Immutable
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Syntax.InternalSyntax
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.SyntaxFacts

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax

    Partial Friend Class Scanner

        Private _isScanningDirective As Boolean = False
        Protected _scannerPreprocessorState As PreprocessorState

        Private Function TryScanDirective(tList As SyntaxListBuilder) As Boolean
            Debug.Assert(IsAtNewLine())

            ' leading whitespace until we see # should be regular whitespace
            If CanGet() AndAlso IsWhitespace(Peek()) Then
                Dim ws = ScanWhitespace()
                tList.Add(ws)
            End If

            ' SAVE the lookahead state and clear current token
            Dim restorePoint = CreateRestorePoint()
            Me._isScanningDirective = True

            ' since we do not have lookahead tokens, this just 
            ' resets current token to _lineBufferOffset 
            Me.GetNextTokenInState(ScannerState.VB)

            Dim currentNonterminal = Me.GetCurrentSyntaxNode()
            Dim directiveTrivia = TryCast(currentNonterminal, DirectiveTriviaSyntax)

            ' if we are lucky to get whole directive statement, we can just reuse it.
            If directiveTrivia IsNot Nothing Then
                Me.MoveToNextSyntaxNodeInTrivia()

                ' adjust current token to just after the node
                ' we need that in case we need to skip some disabled text 
                '(yes we do tokenize disabled text for compatibility reasons)
                Me.GetNextTokenInState(ScannerState.VB)

            Else
                Dim parser As New Parser(Me)

                directiveTrivia = parser.ParseConditionalCompilationStatement()
                directiveTrivia = parser.ConsumeStatementTerminatorAfterDirective(directiveTrivia)
            End If

            Debug.Assert(directiveTrivia.FullWidth > 0, "should at least get #")
            ProcessDirective(directiveTrivia, tList)

            ResetLineBufferOffset()

            ' RESTORE lookahead state and current token if there were any
            restorePoint.RestoreTokens(includeLookAhead:=True)
            Me._isScanningDirective = False

            Return True
        End Function

        ''' <summary>
        ''' Entry point to directive processing for Scanner.
        ''' </summary>
        Private Sub ProcessDirective(directiveTrivia As DirectiveTriviaSyntax, tList As SyntaxListBuilder)

            Dim disabledCode As CodeAnalysis.Syntax.InternalSyntax.SyntaxList(Of VisualBasicSyntaxNode) = Nothing
            Dim statement As DirectiveTriviaSyntax = directiveTrivia

            Dim newState = ApplyDirective(_scannerPreprocessorState,
                                            statement)

            _scannerPreprocessorState = newState

            ' if we are in a not taken branch, skip text
            Dim conditionals = newState.ConditionalStack
            If conditionals.Count <> 0 AndAlso
                        Not conditionals.Peek.BranchTaken = ConditionalState.BranchTakenState.Taken Then

                ' we should not see #const inside disabled sections
                Debug.Assert(statement.Kind <> SyntaxKind.ConstDirectiveTrivia)

                ' skip disabled text
                disabledCode = SkipConditionalCompilationSection()
            End If

            ' Here we add the directive and disabled text that follows it 
            ' (if there is any) to the trivia list

            ' processing statement could add an error to it, 
            ' so we may need to rebuild the trivia node
            If statement IsNot directiveTrivia Then
                directiveTrivia = statement
            End If

            ' add the directive trivia to the list
            tList.Add(directiveTrivia)

            ' if had disabled code, add that too
            If disabledCode.Node IsNot Nothing Then
                tList.AddRange(disabledCode)
            End If
        End Sub

        ''' <summary>
        ''' Gets an initial preprocessor state and applies all directives from a given node.
        ''' Entry point for blender
        ''' </summary>
        Protected Shared Function ApplyDirectives(preprocessorState As PreprocessorState, node As VisualBasicSyntaxNode) As PreprocessorState
            If node.ContainsDirectives Then
                preprocessorState = ApplyDirectivesRecursive(preprocessorState, node)
            End If

            Return preprocessorState
        End Function

        Private Shared Function ApplyDirectivesRecursive(preprocessorState As PreprocessorState, node As GreenNode) As PreprocessorState
            Debug.Assert(node.ContainsDirectives, "we should not be processing nodes without Directives")

            ' node is a directive
            Dim directive = TryCast(node, DirectiveTriviaSyntax)
            If directive IsNot Nothing Then
                Dim statement = directive

                preprocessorState = ApplyDirective(preprocessorState, statement)
                Debug.Assert((statement Is directive) OrElse node.ContainsDiagnostics, "since we have no errors, we should not be changing statement")

                Return preprocessorState
            End If

            ' node is nonterminal
            Dim sCount = node.SlotCount
            If sCount > 0 Then
                For i As Integer = 0 To sCount - 1
                    Dim child = node.GetSlot(i)
                    If child IsNot Nothing AndAlso child.ContainsDirectives Then
                        preprocessorState = ApplyDirectivesRecursive(preprocessorState, child)
                    End If
                Next

                Return preprocessorState
            End If

            ' node is a token
            Dim tk = DirectCast(node, SyntaxToken)

            Dim trivia = tk.GetLeadingTrivia
            If trivia IsNot Nothing AndAlso trivia.ContainsDirectives Then
                preprocessorState = ApplyDirectivesRecursive(preprocessorState, trivia)
            End If

            trivia = tk.GetTrailingTrivia
            If trivia IsNot Nothing AndAlso trivia.ContainsDirectives Then
                preprocessorState = ApplyDirectivesRecursive(preprocessorState, trivia)
            End If

            Return preprocessorState
        End Function

        ' takes a preprocessor state and applies a directive statement to it
        Friend Shared Function ApplyDirective(preprocessorState As PreprocessorState,
                                          ByRef statement As DirectiveTriviaSyntax) As PreprocessorState

            Select Case statement.Kind
                Case SyntaxKind.ConstDirectiveTrivia
                    Dim conditionalsStack = preprocessorState.ConditionalStack
                    If conditionalsStack.Count <> 0 AndAlso
                        Not conditionalsStack.Peek.BranchTaken = ConditionalState.BranchTakenState.Taken Then

                        ' const inside disabled text - do not evaluate
                    Else
                        ' interpret the const
                        preprocessorState = preprocessorState.InterpretConstDirective(statement)
                    End If

                Case SyntaxKind.IfDirectiveTrivia
                    preprocessorState = preprocessorState.InterpretIfDirective(statement)

                Case SyntaxKind.ElseIfDirectiveTrivia
                    preprocessorState = preprocessorState.InterpretElseIfDirective(statement)

                Case SyntaxKind.ElseDirectiveTrivia
                    preprocessorState = preprocessorState.InterpretElseDirective(statement)

                Case SyntaxKind.EndIfDirectiveTrivia
                    preprocessorState = preprocessorState.InterpretEndIfDirective(statement)

                Case SyntaxKind.RegionDirectiveTrivia
                    preprocessorState = preprocessorState.InterpretRegionDirective(statement)

                Case SyntaxKind.EndRegionDirectiveTrivia
                    preprocessorState = preprocessorState.InterpretEndRegionDirective(statement)

                Case SyntaxKind.ExternalSourceDirectiveTrivia
                    preprocessorState = preprocessorState.InterpretExternalSourceDirective(statement)

                Case SyntaxKind.EndExternalSourceDirectiveTrivia
                    preprocessorState = preprocessorState.InterpretEndExternalSourceDirective(statement)

                Case SyntaxKind.ExternalChecksumDirectiveTrivia,
                    SyntaxKind.BadDirectiveTrivia,
                    SyntaxKind.EnableWarningDirectiveTrivia, 'TODO: Add support for processing #Enable and #Disable
                    SyntaxKind.DisableWarningDirectiveTrivia,
                    SyntaxKind.ReferenceDirectiveTrivia

                    ' These directives require no processing

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(statement.Kind)
            End Select

            Return preprocessorState
        End Function

        ' represents states of a single conditional frame
        ' immutable as PreprocessorState so requires
        Friend Class ConditionalState
            Public Enum BranchTakenState As Byte
                NotTaken
                Taken
                AlreadyTaken
            End Enum

            Private ReadOnly _branchTaken As BranchTakenState
            Private ReadOnly _elseSeen As Boolean
            Private ReadOnly _ifDirective As IfDirectiveTriviaSyntax

            ' branch can be taken only once.
            ' this state transitions NotTaken -> Taken -> AlreadyTaken...
            Friend ReadOnly Property BranchTaken As BranchTakenState
                Get
                    Return _branchTaken
                End Get
            End Property

            Friend ReadOnly Property ElseSeen As Boolean
                Get
                    Return _elseSeen
                End Get
            End Property

            Friend ReadOnly Property IfDirective As IfDirectiveTriviaSyntax
                Get
                    Return _ifDirective
                End Get
            End Property

            Friend Sub New(branchTaken As BranchTakenState, elseSeen As Boolean, ifDirective As IfDirectiveTriviaSyntax)
                _branchTaken = branchTaken
                _elseSeen = elseSeen
                _ifDirective = ifDirective
            End Sub
        End Class

        ' The class needs to be immutable
        ' as its instances can get associated with multiple tokens.
        Friend NotInheritable Class PreprocessorState
            Private ReadOnly _symbols As ImmutableDictionary(Of String, CConst)
            Private ReadOnly _conditionals As ImmutableStack(Of ConditionalState)
            Private ReadOnly _regionDirectives As ImmutableStack(Of RegionDirectiveTriviaSyntax)
            Private ReadOnly _haveSeenRegionDirectives As Boolean
            Private ReadOnly _externalSourceDirective As ExternalSourceDirectiveTriviaSyntax

            Friend Sub New(symbols As ImmutableDictionary(Of String, CConst))
                _symbols = symbols
                _conditionals = ImmutableStack.Create(Of ConditionalState)()
                _regionDirectives = ImmutableStack.Create(Of RegionDirectiveTriviaSyntax)()
            End Sub

            Private Sub New(symbols As ImmutableDictionary(Of String, CConst),
                    conditionals As ImmutableStack(Of ConditionalState),
                    regionDirectives As ImmutableStack(Of RegionDirectiveTriviaSyntax),
                    haveSeenRegionDirectives As Boolean,
                    externalSourceDirective As ExternalSourceDirectiveTriviaSyntax)

                Me._symbols = symbols
                Me._conditionals = conditionals
                Me._regionDirectives = regionDirectives
                Me._haveSeenRegionDirectives = haveSeenRegionDirectives
                Me._externalSourceDirective = externalSourceDirective
            End Sub

            Friend ReadOnly Property SymbolsMap As ImmutableDictionary(Of String, CConst)
                Get
                    Return _symbols
                End Get
            End Property

            Private Function SetSymbol(name As String, value As CConst) As PreprocessorState
                Dim symbols = Me._symbols
                symbols = symbols.SetItem(name, value)
                Return New PreprocessorState(symbols, Me._conditionals, Me._regionDirectives, Me._haveSeenRegionDirectives, Me._externalSourceDirective)
            End Function

            Friend ReadOnly Property ConditionalStack As ImmutableStack(Of ConditionalState)
                Get
                    Return _conditionals
                End Get
            End Property

            Private Function WithConditionals(conditionals As ImmutableStack(Of ConditionalState)) As PreprocessorState
                Return New PreprocessorState(Me._symbols, conditionals, Me._regionDirectives, Me._haveSeenRegionDirectives, Me._externalSourceDirective)
            End Function

            Friend ReadOnly Property RegionDirectiveStack As ImmutableStack(Of RegionDirectiveTriviaSyntax)
                Get
                    Return _regionDirectives
                End Get
            End Property

            Friend ReadOnly Property HaveSeenRegionDirectives As Boolean
                Get
                    Return _haveSeenRegionDirectives
                End Get
            End Property

            Private Function WithRegions(regions As ImmutableStack(Of RegionDirectiveTriviaSyntax)) As PreprocessorState
                Return New PreprocessorState(Me._symbols, Me._conditionals, regions, Me._haveSeenRegionDirectives OrElse regions.Count > 0, Me._externalSourceDirective)
            End Function

            Friend ReadOnly Property ExternalSourceDirective As ExternalSourceDirectiveTriviaSyntax
                Get
                    Return _externalSourceDirective
                End Get
            End Property

            Private Function WithExternalSource(externalSource As ExternalSourceDirectiveTriviaSyntax) As PreprocessorState
                Return New PreprocessorState(Me._symbols, Me._conditionals, Me._regionDirectives, Me._haveSeenRegionDirectives, externalSource)
            End Function

            Friend Function InterpretConstDirective(ByRef statement As DirectiveTriviaSyntax) As PreprocessorState
                Debug.Assert(statement.Kind = SyntaxKind.ConstDirectiveTrivia)

                Dim constDirective = DirectCast(statement, ConstDirectiveTriviaSyntax)
                Dim value = ExpressionEvaluator.EvaluateExpression(constDirective.Value, _symbols)

                Dim err = value.ErrorId
                If err <> 0 Then
                    statement = Parser.ReportSyntaxError(statement, err, value.ErrorArgs)
                End If

                Return SetSymbol(constDirective.Name.IdentifierText, value)
            End Function

            Friend Function InterpretExternalSourceDirective(ByRef statement As DirectiveTriviaSyntax) As PreprocessorState
                Dim externalSourceDirective = DirectCast(statement, ExternalSourceDirectiveTriviaSyntax)

                If _externalSourceDirective IsNot Nothing Then
                    statement = Parser.ReportSyntaxError(statement, ERRID.ERR_NestedExternalSource)
                    Return Me
                Else
                    Return WithExternalSource(externalSourceDirective)
                End If
            End Function

            Friend Function InterpretEndExternalSourceDirective(ByRef statement As DirectiveTriviaSyntax) As PreprocessorState
                If _externalSourceDirective Is Nothing Then
                    statement = Parser.ReportSyntaxError(statement, ERRID.ERR_EndExternalSource)
                    Return Me
                Else
                    Return WithExternalSource(Nothing)
                End If
            End Function

            Friend Function InterpretRegionDirective(ByRef statement As DirectiveTriviaSyntax) As PreprocessorState
                Dim regionDirective = DirectCast(statement, RegionDirectiveTriviaSyntax)

                Return WithRegions(_regionDirectives.Push(regionDirective))
            End Function

            Friend Function InterpretEndRegionDirective(ByRef statement As DirectiveTriviaSyntax) As PreprocessorState
                If _regionDirectives.Count = 0 Then
                    statement = Parser.ReportSyntaxError(statement, ERRID.ERR_EndRegionNoRegion)
                    Return Me
                Else
                    Return WithRegions(_regionDirectives.Pop())
                End If
            End Function

            ' // Interpret a conditional compilation #if or #elseif.

            Friend Function InterpretIfDirective(ByRef statement As DirectiveTriviaSyntax) As PreprocessorState
                Debug.Assert(statement.Kind = SyntaxKind.IfDirectiveTrivia)

                Dim ifDirective = DirectCast(statement, IfDirectiveTriviaSyntax)

                ' TODO - Is the following comment still relevant? How should the error be reported?
                ' Evaluate the expression to detect errors, whether or not
                ' its result is needed.
                Dim value = ExpressionEvaluator.EvaluateCondition(ifDirective.Condition, _symbols)

                Dim err = value.ErrorId
                If err <> 0 Then
                    statement = Parser.ReportSyntaxError(statement, err, value.ErrorArgs)
                End If

                Dim takeThisBranch = If(value.IsBad OrElse value.IsBooleanTrue,
                                        ConditionalState.BranchTakenState.Taken,
                                        ConditionalState.BranchTakenState.NotTaken)

                Return WithConditionals(_conditionals.Push(New ConditionalState(takeThisBranch, False, DirectCast(statement, IfDirectiveTriviaSyntax))))
            End Function

            Friend Function InterpretElseIfDirective(ByRef statement As DirectiveTriviaSyntax) As PreprocessorState

                Dim condition As ConditionalState
                Dim conditionals = Me._conditionals

                If conditionals.Count = 0 Then
                    statement = Parser.ReportSyntaxError(statement, ERRID.ERR_LbBadElseif)
                    condition = New ConditionalState(ConditionalState.BranchTakenState.NotTaken, False, Nothing)
                Else
                    condition = conditionals.Peek
                    conditionals = conditionals.Pop

                    If condition.ElseSeen Then
                        statement = Parser.ReportSyntaxError(statement, ERRID.ERR_LbElseifAfterElse)
                    End If
                End If

                ' TODO - Is the following comment still relevant? How should the error be reported?
                ' Evaluate the expression to detect errors, whether or not
                ' its result is needed.
                Dim ifDirective = DirectCast(statement, IfDirectiveTriviaSyntax)

                Dim value = ExpressionEvaluator.EvaluateCondition(ifDirective.Condition, _symbols)

                Dim err = value.ErrorId
                If err <> 0 Then
                    statement = Parser.ReportSyntaxError(statement, err, value.ErrorArgs)
                End If

                Dim takeThisBranch = condition.BranchTaken

                If takeThisBranch = ConditionalState.BranchTakenState.Taken Then
                    takeThisBranch = ConditionalState.BranchTakenState.AlreadyTaken

                ElseIf takeThisBranch = ConditionalState.BranchTakenState.NotTaken AndAlso Not value.IsBad AndAlso value.IsBooleanTrue Then
                    takeThisBranch = ConditionalState.BranchTakenState.Taken

                End If

                condition = New ConditionalState(takeThisBranch, condition.ElseSeen, DirectCast(statement, IfDirectiveTriviaSyntax))

                Return WithConditionals(conditionals.Push(condition))
            End Function

            Friend Function InterpretElseDirective(ByRef statement As DirectiveTriviaSyntax) As PreprocessorState
                Dim conditionals = Me._conditionals

                If conditionals.Count = 0 Then
                    ' If there has been no preceding #If, give an error and pretend that there
                    ' had been one.
                    statement = Parser.ReportSyntaxError(statement, ERRID.ERR_LbElseNoMatchingIf)
                    Return WithConditionals(conditionals.Push(New ConditionalState(ConditionalState.BranchTakenState.Taken, True, Nothing)))
                End If

                Dim condition = conditionals.Peek
                conditionals = conditionals.Pop

                If condition.ElseSeen Then
                    statement = Parser.ReportSyntaxError(statement, ERRID.ERR_LbElseNoMatchingIf)
                End If

                Dim takeThisBranch = condition.BranchTaken

                If takeThisBranch = ConditionalState.BranchTakenState.Taken Then
                    takeThisBranch = ConditionalState.BranchTakenState.AlreadyTaken

                ElseIf takeThisBranch = ConditionalState.BranchTakenState.NotTaken Then
                    takeThisBranch = ConditionalState.BranchTakenState.Taken

                End If

                condition = New ConditionalState(takeThisBranch, True, condition.IfDirective)
                Return WithConditionals(conditionals.Push(condition))
            End Function

            Friend Function InterpretEndIfDirective(ByRef statement As DirectiveTriviaSyntax) As PreprocessorState
                If _conditionals.Count = 0 Then
                    statement = Parser.ReportSyntaxError(statement, ERRID.ERR_LbNoMatchingIf)
                    Return Me
                Else
                    Return WithConditionals(_conditionals.Pop())
                End If
            End Function

            Friend Function IsEquivalentTo(other As PreprocessorState) As Boolean
                ' for now, we will only consider two are equivalents when there are only regions but no other directives
                If Me._conditionals.Count > 0 OrElse
                   Me._symbols.Count > 0 OrElse
                   Me._externalSourceDirective IsNot Nothing OrElse
                   other._conditionals.Count > 0 OrElse
                   other._symbols.Count > 0 OrElse
                   other._externalSourceDirective IsNot Nothing Then
                    Return False
                End If

                If Me._regionDirectives.Count <> other._regionDirectives.Count Then
                    Return False
                End If

                If Me._haveSeenRegionDirectives <> other._haveSeenRegionDirectives Then
                    Return False
                End If

                Return True
            End Function

        End Class

        '//-------------------------------------------------------------------------------------------------
        '//
        '// Skip all text until the end of the current conditional section.  This will also return the
        '// span of text that was skipped
        '// 
        '//-------------------------------------------------------------------------------------------------

        Private Function SkipConditionalCompilationSection() As CodeAnalysis.Syntax.InternalSyntax.SyntaxList(Of VisualBasicSyntaxNode)
            ' // If skipping encounters a nested #if, it is necessary to skip all of it through its
            ' // #end. NestedConditionalsToSkip keeps track of how many nested #if constructs
            ' // need skipping.
            Dim NestedConditionalsToSkip As Integer = 0

            ' Accumulate span of text we're skipping.
            Dim startSkipped As Integer = -1 ' Start location of skipping
            Dim lengthSkipped As Integer = 0 ' Length of skipped text.

            While True
                Dim skippedSpan = Me.SkipToNextConditionalLine()

                If startSkipped < 0 Then
                    startSkipped = skippedSpan.Start
                End If
                lengthSkipped += skippedSpan.Length

                Dim curToken = GetCurrentToken()

                Select Case curToken.Kind
                    Case SyntaxKind.HashToken
                        Dim nextKind = Me.PeekToken(1, ScannerState.VB).Kind
                        Dim nextNextToken = Me.PeekToken(2, ScannerState.VB)

                        If NestedConditionalsToSkip = 0 AndAlso
                            ((nextKind = SyntaxKind.EndKeyword AndAlso
                                Not IsContextualKeyword(nextNextToken, SyntaxKind.ExternalSourceKeyword, SyntaxKind.RegionKeyword)) OrElse
                            nextKind = SyntaxKind.EndIfKeyword OrElse
                            nextKind = SyntaxKind.ElseIfKeyword OrElse
                            nextKind = SyntaxKind.ElseKeyword) Then

                            ' // Finding one of these is sufficient to stop skipping. It is then necessary
                            ' // to process the line as a conditional compilation line. The normal
                            ' // parsing logic will do this.
                            Exit While

                        ElseIf nextKind = SyntaxKind.EndIfKeyword OrElse
                               (nextKind = SyntaxKind.EndKeyword AndAlso
                                Not IsContextualKeyword(nextNextToken, SyntaxKind.ExternalSourceKeyword, SyntaxKind.RegionKeyword)) Then

                            NestedConditionalsToSkip -= 1

                        ElseIf nextKind = SyntaxKind.IfKeyword Then
                            NestedConditionalsToSkip += 1

                        End If

                        ' if the line concluded the skip block, return from the loop
                        If NestedConditionalsToSkip < 0 Then
                            Debug.Assert(NestedConditionalsToSkip = -1, "preprocessor skip underflow")
                            Exit While
                        End If

                        ' skip over # token
                        lengthSkipped += GetCurrentToken.FullWidth
                        GetNextTokenInState(ScannerState.VB)

                        ' Skip over terminator token to avoid counting it twice because it is already trivia on current token
                        If nextKind = SyntaxKind.StatementTerminatorToken OrElse nextKind = SyntaxKind.ColonToken Then
                            GetNextTokenInState(ScannerState.VB)
                        End If

                    Case SyntaxKind.DateLiteralToken, SyntaxKind.BadToken
                        'Dev10 #777522 Do not confuse Date literal with an end of conditional block.

                        ' skip over date literal token
                        lengthSkipped += GetCurrentToken.FullWidth
                        GetNextTokenInState(ScannerState.VB)

                        Dim nextKind = GetCurrentToken().Kind

                        ' Skip over terminator token to avoid counting it twice because it is already trivia on current token
                        If nextKind = SyntaxKind.StatementTerminatorToken OrElse nextKind = SyntaxKind.ColonToken Then
                            GetNextTokenInState(ScannerState.VB)
                        End If

                    Case SyntaxKind.EndOfFileToken
                        Exit While

                    Case Else
                        Throw ExceptionUtilities.UnexpectedValue(curToken.Kind)

                End Select
            End While

            If lengthSkipped > 0 Then
                Return New CodeAnalysis.Syntax.InternalSyntax.SyntaxList(Of VisualBasicSyntaxNode)(Me.GetDisabledTextAt(New TextSpan(startSkipped, lengthSkipped)))
            Else
                Return New CodeAnalysis.Syntax.InternalSyntax.SyntaxList(Of VisualBasicSyntaxNode)(Nothing)
            End If
        End Function

        ' // If compilation ends in the middle of a non-skipped conditional section,
        ' // produce appropriate diagnostics.

        Friend Function RecoverFromMissingConditionalEnds(eof As PunctuationSyntax,
                                                          <Out> ByRef notClosedIfDirectives As ArrayBuilder(Of IfDirectiveTriviaSyntax),
                                                          <Out> ByRef notClosedRegionDirectives As ArrayBuilder(Of RegionDirectiveTriviaSyntax),
                                                          <Out> ByRef haveRegionDirectives As Boolean,
                                                          <Out> ByRef notClosedExternalSourceDirective As ExternalSourceDirectiveTriviaSyntax) As PunctuationSyntax

            notClosedIfDirectives = Nothing
            notClosedRegionDirectives = Nothing

            If Me._scannerPreprocessorState.ConditionalStack.Count > 0 Then
                For Each state In _scannerPreprocessorState.ConditionalStack
                    Dim ifDirective As IfDirectiveTriviaSyntax = state.IfDirective
                    If ifDirective IsNot Nothing Then
                        If notClosedIfDirectives Is Nothing Then
                            notClosedIfDirectives = ArrayBuilder(Of IfDirectiveTriviaSyntax).GetInstance()
                        End If
                        notClosedIfDirectives.Add(ifDirective)
                    End If
                Next

                If notClosedIfDirectives Is Nothing Then
                    ' #If directive is not found
                    eof = Parser.ReportSyntaxError(eof, ERRID.ERR_LbExpectedEndIf)
                End If
            End If

            If Me._scannerPreprocessorState.RegionDirectiveStack.Count > 0 Then
                notClosedRegionDirectives = ArrayBuilder(Of RegionDirectiveTriviaSyntax).GetInstance()
                notClosedRegionDirectives.AddRange(Me._scannerPreprocessorState.RegionDirectiveStack)
            End If

            haveRegionDirectives = Me._scannerPreprocessorState.HaveSeenRegionDirectives
            notClosedExternalSourceDirective = Me._scannerPreprocessorState.ExternalSourceDirective

            Return eof
        End Function
    End Class
End Namespace

