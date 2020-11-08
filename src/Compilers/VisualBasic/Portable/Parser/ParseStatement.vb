' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

'-----------------------------------------------------------------------------
' Contains the definition of the Parser
'-----------------------------------------------------------------------------
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.Syntax.InternalSyntax
Imports InternalSyntaxFactory = Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax.SyntaxFactory
Imports System.Runtime.CompilerServices

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax

    Partial Friend Class Parser

#Region "Helper Methods"

        Private Function ParsePunctuation() As PunctuationSyntax
            ParsePunctuation = DirectCast(CurrentToken, PunctuationSyntax)
            GetNextToken()
        End Function

        Private Sub Contract_ExpectingContextualKeyword( ofKind As SyntaxKind,
                                             <Out> ByRef keyword As KeywordSyntax,
                             <CallerMemberName>Optional caller As String = nothing)
            Dim ok = TryIdentifierAsContextualKeyword(CurrentToken,keyword) AndAlso  keyword.Kind=ofKind
            If Not ok Then keyword = Nothing
            Debug.Assert(ok, caller.CalledOnWrongToken)   
        End Sub

        Private Sub Contract_ExpectingKeyword( kind As SyntaxKind,
                                   <Out> ByRef keyword As KeywordSyntax,
                   <CallerMemberName> Optional caller As String = Nothing)
            Dim currentKind = CurrentToken.Kind
            Debug.Assert(currentKind = kind, caller.CalledOnWrongToken)
            TryParseKeyword(kind, keyword)
        End Sub

        Private Function TryParseKeyword( kind As SyntaxKind,
                              <Out> ByRef keyword As KeywordSyntax,
                                 Optional consume As Boolean = True
                                        ) As Boolean
            Dim ok = CurrentToken.Kind = kind
            If ok Then
                keyword = DirectCast(CurrentToken, KeywordSyntax)
                If consume Then GetNextToken()
            End If
            Return ok
        End Function

        Private Function TryParseComma(ByRef comma As PunctuationSyntax) As Boolean
            Return TryGetTokenAndEatNewLine(SyntaxKind.CommaToken, comma)
        End Function

        Private Function TryParseCommaInto(Of T As GreenNode)(ByRef list As SeparatedSyntaxListBuilder(Of T)) As Boolean
            Dim comma As PunctuationSyntax = Nothing
            Dim ok = TryParseComma(comma)
            If ok Then list.AddSeparator(comma)
            Return ok
        End Function

#End Region


        '
        '============ Methods for parsing specific executable statements
        '
        ' [in] Token starting the statement
        ' File: Parser.cpp
        ' Lines: 5870 - 5870
        ' Statement* .Parser::ParseContinueStatement( [ _In_ Token* StmtStart ] [ _Inout_ bool& ErrorInConstruct ] )

        Private Function ParseContinueStatement() As ContinueStatementSyntax
            Dim continueKeyword As KeywordSyntax = Nothing
            Contract_ExpectingKeyword(SyntaxKind.ContinueKeyword, continueKeyword)

            Dim kind As SyntaxKind
            Dim blockKeyword As KeywordSyntax = Nothing

            If TryParseKeyword(SyntaxKind.DoKeyword, blockKeyword) Then
                kind = SyntaxKind.ContinueDoStatement
            ElseIf TryParseKeyword(SyntaxKind.ForKeyword, blockKeyword) Then
                kind = SyntaxKind.ContinueForStatement
            ElseIf TryParseKeyword(SyntaxKind.WhileKeyword, blockKeyword) Then
                kind = SyntaxKind.ContinueWhileStatement
            Else
                ' The pretty lister is expected to turn Continue statements
                ' that don't specify a Do, While or For to have the correct
                ' form. That requires identifying this condition during
                ' parsing and correcting the parse trees.

                Dim loopContext = Context.FindNearest(AddressOf SyntaxFacts.SupportsContinueStatement)

                If loopContext IsNot Nothing Then

                    Select Case loopContext.BlockKind
                           Case SyntaxKind.SimpleDoLoopBlock,
                                SyntaxKind.DoWhileLoopBlock
                                kind = SyntaxKind.ContinueDoStatement
                                blockKeyword = InternalSyntaxFactory.MissingKeyword(SyntaxKind.DoKeyword)

                            Case SyntaxKind.ForBlock,
                                 SyntaxKind.ForEachBlock
                                 kind = SyntaxKind.ContinueForStatement
                                 blockKeyword = InternalSyntaxFactory.MissingKeyword(SyntaxKind.ForKeyword)

                            Case SyntaxKind.WhileBlock
                                 kind = SyntaxKind.ContinueWhileStatement
                                 blockKeyword = InternalSyntaxFactory.MissingKeyword(SyntaxKind.WhileKeyword)

                    End Select

                 End If

                If blockKeyword Is Nothing Then
                    ' No context found which can have a continue statement was found
                    ' TODO - Which keyword in this case?
                    kind = SyntaxKind.ContinueDoStatement
                    blockKeyword = InternalSyntaxFactory.MissingKeyword(SyntaxKind.DoKeyword)
                End If

                  blockKeyword = ReportSyntaxError(blockKeyword, ERRID.ERR_ExpectedContinueKind)
            End If

            Return SyntaxFactory.ContinueStatement(kind, continueKeyword, blockKeyword)

        End Function

        Private Function ParseExitStatement() As StatementSyntax
            Dim exitKeyword As KeywordSyntax = Nothing
            Contract_ExpectingKeyword(SyntaxKind.ExitKeyword, exitKeyword)

            Dim statement As StatementSyntax = Nothing
            Dim kind As SyntaxKind
            Dim blockKeyword As KeywordSyntax = Nothing

            Select Case (CurrentToken.Kind)

                Case SyntaxKind.DoKeyword
                    kind = SyntaxKind.ExitDoStatement
                    blockKeyword = DirectCast(CurrentToken, KeywordSyntax)
                    GetNextToken()

                Case SyntaxKind.ForKeyword
                    kind = SyntaxKind.ExitForStatement
                    blockKeyword = DirectCast(CurrentToken, KeywordSyntax)
                    GetNextToken()

                Case SyntaxKind.WhileKeyword
                    kind = SyntaxKind.ExitWhileStatement
                    blockKeyword = DirectCast(CurrentToken, KeywordSyntax)
                    GetNextToken()

                Case SyntaxKind.SelectKeyword
                    kind = SyntaxKind.ExitSelectStatement
                    blockKeyword = DirectCast(CurrentToken, KeywordSyntax)
                    GetNextToken()

                ' The pretty lister is expected to turn Exit statements
                ' that don
                ' statements that do. That requires identifying this
                ' condition during parsing and correcting the parse trees.

                Case SyntaxKind.SubKeyword
                    ' Error message moved to context
                    kind = SyntaxKind.ExitSubStatement
                    blockKeyword = DirectCast(CurrentToken, KeywordSyntax)
                    GetNextToken()

                Case SyntaxKind.FunctionKeyword
                    ' Error message moved to context
                    kind = SyntaxKind.ExitFunctionStatement
                    blockKeyword = DirectCast(CurrentToken, KeywordSyntax)
                    GetNextToken()

                Case SyntaxKind.PropertyKeyword
                    ' Error message moved to context
                    kind = SyntaxKind.ExitPropertyStatement
                    blockKeyword = DirectCast(CurrentToken, KeywordSyntax)
                    GetNextToken()

                Case SyntaxKind.TryKeyword
                    kind = SyntaxKind.ExitTryStatement
                    blockKeyword = DirectCast(CurrentToken, KeywordSyntax)
                    GetNextToken()

                Case Else
                    'The block keyword is missing.  Look at the contexts to determine what should be here
                    Dim loopContext = Context.FindNearest(AddressOf SyntaxFacts.SupportsExitStatement)

                    If loopContext IsNot Nothing Then

                        Select Case loopContext.BlockKind

                            Case SyntaxKind.SimpleDoLoopBlock,
                                 SyntaxKind.DoWhileLoopBlock

                                kind = SyntaxKind.ExitDoStatement
                                blockKeyword = InternalSyntaxFactory.MissingKeyword(SyntaxKind.DoKeyword)

                            Case SyntaxKind.ForBlock, SyntaxKind.ForEachBlock
                                kind = SyntaxKind.ExitForStatement
                                blockKeyword = InternalSyntaxFactory.MissingKeyword(SyntaxKind.ForKeyword)

                            Case SyntaxKind.WhileBlock
                                kind = SyntaxKind.ExitWhileStatement
                                blockKeyword = InternalSyntaxFactory.MissingKeyword(SyntaxKind.WhileKeyword)

                            Case SyntaxKind.SelectBlock
                                kind = SyntaxKind.ExitSelectStatement
                                blockKeyword = InternalSyntaxFactory.MissingKeyword(SyntaxKind.SelectKeyword)

                            Case SyntaxKind.SubBlock, SyntaxKind.ConstructorBlock
                                kind = SyntaxKind.ExitSubStatement
                                blockKeyword = InternalSyntaxFactory.MissingKeyword(SyntaxKind.SubKeyword)

                            Case SyntaxKind.FunctionBlock
                                kind = SyntaxKind.ExitFunctionStatement
                                blockKeyword = InternalSyntaxFactory.MissingKeyword(SyntaxKind.FunctionKeyword)

                            Case SyntaxKind.PropertyBlock
                                kind = SyntaxKind.ExitPropertyStatement
                                blockKeyword = InternalSyntaxFactory.MissingKeyword(SyntaxKind.PropertyKeyword)

                            Case SyntaxKind.TryBlock
                                kind = SyntaxKind.ExitTryStatement
                                blockKeyword = InternalSyntaxFactory.MissingKeyword(SyntaxKind.TryKeyword)

                        End Select
                    End If

                    ' "Exit Operator" is not a valid exit kind, but we keep track of when
                    ' a user might be expected to use it to give a smarter error.
                    ' ""Exit Operator' is not valid. Use 'Return' to exit an Operator.'
                    ' Or
                    ' ""Exit AddHandler', 'Exit RemoveHandler' and 'Exit RaiseEvent' are not valid. Use 'Return' to exit from event members.'
                    Dim stmtError As ERRID = Nothing

                    Select Case CurrentToken.Kind
                        Case SyntaxKind.OperatorKeyword
                            stmtError = ERRID.ERR_ExitOperatorNotValid

                        Case SyntaxKind.AddHandlerKeyword,
                            SyntaxKind.RemoveHandlerKeyword,
                            SyntaxKind.RaiseEventKeyword
                            stmtError = ERRID.ERR_ExitEventMemberNotInvalid

                    End Select

                    If stmtError <> ERRID.ERR_None Then
                        statement = SyntaxFactory.ReturnStatement(InternalSyntaxFactory.MissingKeyword(SyntaxKind.ReturnKeyword), Nothing)
                        statement = statement.AddLeadingSyntax(SyntaxList.List(exitKeyword, CurrentToken), stmtError)
                        GetNextToken()
                        Return statement
                    End If

                    If blockKeyword Is Nothing Then
                        ' No context found which can have an exit statement was found
                        'TODO - What kind of statement should be generated?  Bad Exit would make most sense.
                        ' For now generate an Exit Sub but this leads to spurious errors.
                        kind = SyntaxKind.ExitSubStatement
                        blockKeyword = InternalSyntaxFactory.MissingKeyword(SyntaxKind.SubKeyword)
                    End If

                    blockKeyword = ReportSyntaxError(blockKeyword, ERRID.ERR_ExpectedExitKind)
            End Select

            statement = SyntaxFactory.ExitStatement(kind, exitKeyword, blockKeyword)

            Return statement

        End Function

        Private Function ParseCaseStatement() As CaseStatementSyntax
            Dim caseKeyword As KeywordSyntax = Nothing
            Contract_ExpectingKeyword(SyntaxKind.CaseKeyword, caseKeyword)

            Dim caseClauses = _pool.AllocateSeparated(Of CaseClauseSyntax)()
            Dim elseKeyword As KeywordSyntax = Nothing

            If TryParseKeyword(SyntaxKind.ElseKeyword, elseKeyword) Then

                Dim caseClause = SyntaxFactory.ElseCaseClause(elseKeyword)
                caseClauses.Add(caseClause)

            Else

                Do
                    Dim StartCase As SyntaxKind = CurrentToken.Kind ' dev10_500588 Snap the start of the expression token AFTER we've moved off the EOL (if one is present)
                    Dim caseClause As CaseClauseSyntax

                    If StartCase = SyntaxKind.IsKeyword OrElse SyntaxFacts.IsRelationalOperator(StartCase) Then

                        ' dev10_526560 Allow implicit newline after IS
                        Dim optionalIsKeyword As KeywordSyntax = Nothing
                        TryGetTokenAndEatNewLine(SyntaxKind.IsKeyword, optionalIsKeyword)

                        If SyntaxFacts.IsRelationalOperator(CurrentToken.Kind) Then

                            Dim relationalOperator = ParsePunctuation()
                            TryEatNewLine() ' dev10_503248

                            Dim CaseExpr As ExpressionSyntax = ParseExpressionCore()

                            If CaseExpr.ContainsDiagnostics Then
                                CaseExpr = ResyncAt(CaseExpr)
                            End If

                            caseClause = SyntaxFactory.RelationalCaseClause(RelationalOperatorKindToCaseKind(relationalOperator.Kind), optionalIsKeyword, relationalOperator, CaseExpr)

                        Else
                            ' Since we saw IS, create a relational case.
                            ' This helps intellisense do a drop down of
                            ' the operators that can follow "Is".
                            Dim relationalOperator = ReportSyntaxError(InternalSyntaxFactory.MissingPunctuation(SyntaxKind.EqualsToken), ERRID.ERR_ExpectedRelational)

                            caseClause = ResyncAt(InternalSyntaxFactory.RelationalCaseClause(SyntaxKind.CaseEqualsClause, optionalIsKeyword, relationalOperator, InternalSyntaxFactory.MissingExpression))
                        End If

                    Else

                        Dim value As ExpressionSyntax = ParseExpressionCore()

                        If value.ContainsDiagnostics Then
                            value = ResyncAt(value, SyntaxKind.ToKeyword)
                        End If

                        Dim toKeyword As KeywordSyntax = Nothing
                        If TryGetToken(SyntaxKind.ToKeyword, toKeyword) Then

                            Dim upperBound As ExpressionSyntax = ParseExpressionCore()

                            If upperBound.ContainsDiagnostics Then
                                upperBound = ResyncAt(upperBound)
                            End If

                            caseClause = SyntaxFactory.RangeCaseClause(value, toKeyword, upperBound)
                        Else

                            caseClause = SyntaxFactory.SimpleCaseClause(value)
                        End If
                    End If

                    caseClauses.Add(caseClause)

                Loop While TryParseCommaInto(caseClauses)

            End If

            Dim separatedCaseClauses = caseClauses.ToListAndFree(_pool)
            Dim statement As CaseStatementSyntax

            If elseKeyword Is Nothing Then
                statement = SyntaxFactory.CaseStatement(caseKeyword, separatedCaseClauses)
            Else
                statement = SyntaxFactory.CaseElseStatement(caseKeyword, separatedCaseClauses)
            End If

            Return statement

        End Function

        Private Shared Function RelationalOperatorKindToCaseKind(kind As SyntaxKind) As SyntaxKind

            Select Case kind
                Case SyntaxKind.LessThanToken
                    Return SyntaxKind.CaseLessThanClause

                Case SyntaxKind.LessThanEqualsToken
                    Return SyntaxKind.CaseLessThanOrEqualClause

                Case SyntaxKind.EqualsToken
                    Return SyntaxKind.CaseEqualsClause

                Case SyntaxKind.LessThanGreaterThanToken
                    Return SyntaxKind.CaseNotEqualsClause

                Case SyntaxKind.GreaterThanToken
                    Return SyntaxKind.CaseGreaterThanClause

                Case SyntaxKind.GreaterThanEqualsToken
                    Return SyntaxKind.CaseGreaterThanOrEqualClause

                Case Else
                    Debug.Assert(False, "Wrong relational operator kind")
                    Return Nothing
            End Select

        End Function

        Private Function ParseSelectStatement() As SelectStatementSyntax
            Dim selectKeyword As KeywordSyntax = Nothing
            Contract_ExpectingKeyword(SyntaxKind.SelectKeyword, selectKeyword)

            ' Allow the expected CASE token to be present or not.

            Dim optionalCaseKeyword As KeywordSyntax = Nothing
            TryParseKeyword(SyntaxKind.CaseKeyword, optionalCaseKeyword)

            Dim value As ExpressionSyntax = ParseExpressionCore()

            If value.ContainsDiagnostics Then value = ResyncAt(value)

            Return SyntaxFactory.SelectStatement(selectKeyword, optionalCaseKeyword, value)
        End Function

        ' ParseIfConstruct handles the parsing of block and line if statements and
        ' block and line elseif statements, setting *IsLineIf as appropriate.
        ' For a line if/elseif, parsing consumes through the first statement (if any)
        ' in the then clause. The returned tree is the block created for the if or else if.
        '
        ' Parse the expression (and following text) in an If or ElseIf statement.
        '
        ' File: Parser.cpp
        ' Lines: 11249 - 11249
        ' ExpressionBlockStatement* .Parser::ParseIfConstruct( [ ParseTree::IfStatement* IfContainingElseIf ] [ _Out_ bool* IsLineIf ] [ _Inout_ bool& ErrorInConstruct ] )
        '
        'davidsch - Renamed ParseIfStatement from ParseIfConstruct
        Private Function ParseIfStatement() As IfStatementSyntax
            Dim ifKeyword As KeywordSyntax = Nothing
            Contract_ExpectingKeyword(SyntaxKind.IfKeyword, ifKeyword)

            Dim condition = ParseExpressionCore(OperatorPrecedence.PrecedenceNone)

            If condition.ContainsDiagnostics Then condition = ResyncAt(condition, SyntaxKind.ThenKeyword)

            Dim thenKeyword As KeywordSyntax = Nothing
            TryParseKeyword(SyntaxKind.ThenKeyword, thenKeyword)

            Return SyntaxFactory.IfStatement(ifKeyword, condition, thenKeyword)
        End Function

        Private Function ParseElseStatement() As ElseStatementSyntax
            Dim elseKeyword As KeywordSyntax = Nothing
            Contract_ExpectingKeyword(SyntaxKind.ElseKeyword, elseKeyword)

            Return SyntaxFactory.ElseStatement(elseKeyword)
        End Function

        Private Function ParseElseIfStatement() As StatementSyntax

            Debug.Assert(CurrentToken.Kind = SyntaxKind.ElseIfKeyword OrElse (CurrentToken.Kind = SyntaxKind.ElseKeyword AndAlso PeekToken(1).Kind = SyntaxKind.IfKeyword),
                         NameOf(ParseElseIfStatement).CalledOnWrongToken)

            Dim elseIfKeyword As KeywordSyntax = Nothing
            Dim elseKeyword   As KeywordSyntax = Nothing
            If TryParseKeyword(SyntaxKind.ElseIfKeyword, elseIfKeyword) Then

            ElseIf TryParseKeyword(SyntaxKind.ElseKeyword, elseKeyword) Then
                ' When written as 'Else If' we need to merged the two keywords together.

                If Context.IsSingleLine Then
                    ' But inside of a single-line If this isn't allowed. We parse as an Else statement
                    ' so that the SingleLineIfBlockContext can parse the rest as a separate If statement.

                    Return SyntaxFactory.ElseStatement(elseKeyword)
                End If

                Dim ifKeyword = ParseKeyword()

                elseIfKeyword = New KeywordSyntax(SyntaxKind.ElseIfKeyword,
                                                  MergeTokenText(elseKeyword, ifKeyword),
                                                  elseKeyword.GetLeadingTrivia(),
                                                  ifKeyword.GetTrailingTrivia())
            End If

            Dim condition = ParseExpressionCore(OperatorPrecedence.PrecedenceNone)

            If condition.ContainsDiagnostics Then
                condition = ResyncAt(condition, SyntaxKind.ThenKeyword)
            End If

            Dim toptionalTenKeyword As KeywordSyntax = Nothing
            TryGetToken(SyntaxKind.ThenKeyword, toptionalTenKeyword)

            Return SyntaxFactory.ElseIfStatement(elseIfKeyword, condition, toptionalTenKeyword)
        End Function

        Private Function ParseAnachronisticStatement() As StatementSyntax
            ' Assume CurrentToken is on ENDIF, WEND
            Debug.Assert(CurrentToken.Kind.IsIn(SyntaxKind.EndIfKeyword,
                                                SyntaxKind.GosubKeyword,
                                                SyntaxKind.WendKeyword), NameOf(ParseAnachronisticStatement).CalledOnWrongToken)

            Dim keyword As KeywordSyntax = DirectCast(CurrentToken, KeywordSyntax)

            ' Put the 'ENDIF'/'WEND'/'GOSUB' token in the unexpected.
            Dim unexpected As CodeAnalysis.Syntax.InternalSyntax.SyntaxList(Of SyntaxToken) = ResyncAt()

            Dim missingEndKeyword As KeywordSyntax = InternalSyntaxFactory.MissingKeyword(SyntaxKind.EndKeyword)
            Dim statement As StatementSyntax = Nothing
            Dim errorId As ERRID

            Select Case keyword.Kind
                Case SyntaxKind.EndIfKeyword
                    ' EndIf is anachronistic - Correct syntax is END IF
                    ' TODO - Is an END IF with missing tokens the right way to model this?
                    statement = SyntaxFactory.EndIfStatement(missingEndKeyword, InternalSyntaxFactory.MissingKeyword(SyntaxKind.IfKeyword))
                    errorId = ERRID.ERR_ObsoleteEndIf

                Case SyntaxKind.WendKeyword
                    ' While...Wend are anachronistic
                    ' TODO - Is an END IF with missing tokens the right way to model this?
                    statement = SyntaxFactory.EndWhileStatement(missingEndKeyword, InternalSyntaxFactory.MissingKeyword(SyntaxKind.WhileKeyword))
                    errorId = ERRID.ERR_ObsoleteWhileWend

                Case SyntaxKind.GosubKeyword
                    statement = InternalSyntaxFactory.EmptyStatement
                    errorId = ERRID.ERR_ObsoleteGosub

            End Select

            'The Dev10 parser does not mark this statement as bad.
            Return statement.AddTrailingSyntax(unexpected, errorId)
        End Function

        Private Function ParseDoStatement() As DoStatementSyntax
            Dim doKeyword As KeywordSyntax = Nothing
            Contract_ExpectingKeyword(SyntaxKind.DoKeyword, doKeyword)

            Dim optionalWhileOrUntilClause As WhileOrUntilClauseSyntax = Nothing

            TryParseOptionalWhileOrUntilClause(doKeyword, optionalWhileOrUntilClause)

            Dim kind As SyntaxKind
            If optionalWhileOrUntilClause Is Nothing Then
                kind = SyntaxKind.SimpleDoStatement
            ElseIf optionalWhileOrUntilClause.Kind = SyntaxKind.WhileClause Then
                kind = SyntaxKind.DoWhileStatement
            Else
                kind = SyntaxKind.DoUntilStatement
            End If

            Return SyntaxFactory.DoStatement(kind, doKeyword, optionalWhileOrUntilClause)
        End Function

        Private Function ParseLoopStatement() As LoopStatementSyntax
            Dim loopKeyword As KeywordSyntax = Nothing
            Contract_ExpectingKeyword(SyntaxKind.LoopKeyword, loopKeyword)

            ' Moved ERRID.ERR_LoopDoubleCondition to TryParseOptionalWhileOrUntilClause
            Dim optionalWhileOrUntilClause As WhileOrUntilClauseSyntax = Nothing

            TryParseOptionalWhileOrUntilClause(loopKeyword, optionalWhileOrUntilClause)

            Dim kind As SyntaxKind
            If optionalWhileOrUntilClause Is Nothing Then
                kind = SyntaxKind.SimpleLoopStatement
            ElseIf optionalWhileOrUntilClause.Kind = SyntaxKind.WhileClause Then
                kind = SyntaxKind.LoopWhileStatement
            Else
                kind = SyntaxKind.LoopUntilStatement
            End If

            Return SyntaxFactory.LoopStatement(kind, loopKeyword, optionalWhileOrUntilClause)
        End Function

        Private Function ParseForStatement() As StatementSyntax
            Dim forKeyword As KeywordSyntax = Nothing
            Contract_ExpectingKeyword(SyntaxKind.ForKeyword, forKeyword)

            Dim eachKeyword As KeywordSyntax = Nothing
            If TryParseKeyword(SyntaxKind.EachKeyword, eachKeyword) Then

                Return ParseForEachStatement(forKeyword, eachKeyword)

            Else
                Return ParseForStatement(forKeyword)

            End If
        End Function

        Private Function ParseForEachStatement(forKeyword As KeywordSyntax, eachKeyword As KeywordSyntax) As ForEachStatementSyntax

            Dim controlVariable = ParseForLoopControlVariable()

            Dim expression As ExpressionSyntax = Nothing

            If controlVariable.ContainsDiagnostics Then
                controlVariable = ResyncAt(controlVariable, SyntaxKind.InKeyword)
            End If

            TryEatNewLineIfFollowedBy(SyntaxKind.InKeyword)

            Dim inKeyword As KeywordSyntax = Nothing
            If TryGetTokenAndEatNewLine(SyntaxKind.InKeyword, inKeyword) Then

                expression = ParseExpressionCore()

                If expression.ContainsDiagnostics Then
                    expression = ResyncAt(expression)
                End If
            Else
                'TODO - In Dev 10 only syntax error was reported.  Now
                ' expected 'In" is reported.  Should only 1 message be reported?
                ' Also see parsing For with expected =.
                inKeyword = DirectCast(HandleUnexpectedToken(SyntaxKind.InKeyword), KeywordSyntax)
                expression = InternalSyntaxFactory.MissingExpression.AddTrailingSyntax(ResyncAt({SyntaxKind.ToKeyword}), ERRID.ERR_Syntax)
            End If

            Return SyntaxFactory.ForEachStatement(forKeyword, eachKeyword, controlVariable, inKeyword, expression)
        End Function

        Private Function ParseForStatement(forKeyword As KeywordSyntax) As ForStatementSyntax

            Dim controlVariable = ParseForLoopControlVariable()

            If controlVariable.ContainsDiagnostics Then
                controlVariable = ResyncAt(controlVariable, SyntaxKind.EqualsToken, SyntaxKind.ToKeyword)
            End If

            ' TODO - Handle case where controlVariable is OK but current token is not equals.  Need to resync in that case too
            ' e.g.
            ' For i,j = 1 to 10
            ' See bug 8590.

            Dim equalsToken As PunctuationSyntax = Nothing
            Dim fromValue As ExpressionSyntax = Nothing
            Dim toValue As ExpressionSyntax = Nothing

            If TryGetTokenAndEatNewLine(SyntaxKind.EqualsToken, equalsToken) Then

                ' Dev10_545918 - Allow implicit line continuation after '=' 

                fromValue = ParseExpressionCore()

                If fromValue.ContainsDiagnostics Then fromValue = ResyncAt(fromValue, SyntaxKind.ToKeyword)

            Else
                'Dev 10 only reported syntax error.  Code now reports expected '=' and syntax error.
                ' TODO - consider removing redundant syntax error message.
                equalsToken = DirectCast(HandleUnexpectedToken(SyntaxKind.EqualsToken), PunctuationSyntax)

                fromValue = InternalSyntaxFactory.MissingExpression.AddTrailingSyntax(ResyncAt({SyntaxKind.ToKeyword}), ERRID.ERR_Syntax)

            End If

            Dim toKeyword As KeywordSyntax = Nothing
            If TryParseKeyword(SyntaxKind.ToKeyword, toKeyword) Then

                'TODO - davidsch - Why is newline allowed after '=' but not after 'to'?
                toValue = ParseExpressionCore()

                If toValue.ContainsDiagnostics Then toValue = ResyncAt(toValue, SyntaxKind.StepKeyword)

            Else
                'No error for expected 'To' keyword.  HandleUnexpectedToken returns ERRID_Syntax
                toKeyword = DirectCast(HandleUnexpectedToken(SyntaxKind.ToKeyword), KeywordSyntax)
                'TODO - ERRID_ExpectedExpression here?
                toValue = InternalSyntaxFactory.MissingExpression.AddTrailingSyntax(ResyncAt({SyntaxKind.ToKeyword}))

            End If

            Dim optionalStepClause As ForStepClauseSyntax = Nothing
            Dim stepKeyword As KeywordSyntax = Nothing

            If TryParseKeyword(SyntaxKind.StepKeyword, stepKeyword) Then

                Dim stepValue = ParseExpressionCore()

                If stepValue.ContainsDiagnostics Then stepValue = ResyncAt(stepValue)

                optionalStepClause = SyntaxFactory.ForStepClause(stepKeyword, stepValue)

            End If

            Return SyntaxFactory.ForStatement(forKeyword, controlVariable, equalsToken, fromValue, toKeyword, toValue, optionalStepClause)
        End Function

        Private Function ParseNextStatement() As NextStatementSyntax
            Dim nextKeyword As KeywordSyntax = Nothing
            Contract_ExpectingKeyword(SyntaxKind.NextKeyword, nextKeyword)

            If CanFollowStatement(CurrentToken) Then

                Return SyntaxFactory.NextStatement(nextKeyword, Nothing)

            Else
                ' Collect the next variables.
                ' Context is updated when statement is processed by ForBlockContext.
                Dim variables = _pool.AllocateSeparated(Of ExpressionSyntax)()
                Dim enclosing As BlockContext = Context
                Do
                    Dim variable As ExpressionSyntax = ParseVariable()

                    If variable.ContainsDiagnostics Then
                        variable = ResyncAt(variable)
                    End If

                    ' Report ERR_ExtraNextVariable for the first additional Next variable
                    ' only. This matches the behavior of the native compiler and avoids
                    ' walking out too many contexts.
                    If enclosing IsNot Nothing AndAlso
                        enclosing.BlockKind <> SyntaxKind.ForBlock AndAlso
                        enclosing.BlockKind <> SyntaxKind.ForEachBlock Then
                        variable = ReportSyntaxError(variable, ERRID.ERR_ExtraNextVariable)
                        ' Skip further checks for extra variables.
                        enclosing = Nothing
                    End If

                    variables.Add(variable)

                    If Not TryParseCommaInto(variables) Then Exit Do


                    If enclosing IsNot Nothing Then
                        enclosing = enclosing.PrevBlock
                        Debug.Assert(enclosing IsNot Nothing)
                    End If
                Loop

                Return SyntaxFactory.NextStatement(nextKeyword, variables.ToListAndFree(_pool))

            End If
        End Function

        ' /*********************************************************************
        ' *
        ' * Function:
        ' *     Parser::ParseForLoopControlVariable
        ' *
        ' * Purpose:
        ' *     Parses: <Expression> | <ident>[ArrayList] As <type>
        ' *
        ' **********************************************************************/
        '
        ' File: Parser.cpp
        ' Lines: 5717 - 5717
        ' Expression* .Parser::ParseForLoopControlVariable( [ ParseTree::BlockStatement* ForBlock ] [ _In_ Token* ForStart ] [ _Out_ ParseTree::VariableDeclarationStatement** Decl ] [ _Inout_ bool& ErrorInConstruct ] )
        Private Function ParseForLoopControlVariable() As VisualBasicSyntaxNode

            'TODO - davidsch - I have kept this code as-is but it seems that it would be better to parse the common prefix of 
            ' ParseVariable and ParseForLoopVariableDeclaration instead of peeking for 'AS', 'IN', '='

            Select Case (CurrentToken.Kind)

                Case SyntaxKind.IdentifierToken

                    Select Case PeekToken(1).Kind

                        Case SyntaxKind.QuestionToken, SyntaxKind.AsKeyword
                            Return ParseForLoopVariableDeclaration()

                        Case SyntaxKind.OpenParenToken
                            Dim lookAhead As SyntaxToken = Nothing
                            Dim i = PeekAheadFor(s_isTokenOrKeywordFunc, {SyntaxKind.AsKeyword, SyntaxKind.InKeyword, SyntaxKind.EqualsToken}, lookAhead)
                            If lookAhead IsNot Nothing AndAlso
                                lookAhead.Kind = SyntaxKind.AsKeyword AndAlso
                                PeekToken(i - 1).Kind = SyntaxKind.CloseParenToken Then
                                Return ParseForLoopVariableDeclaration()
                            End If

                            ' Fall through to Non-Declaration, i.e. ParseVariable below
                    End Select

                    Return ParseVariable()

                Case Else
                    Return ParseVariable()
            End Select
        End Function

        ' /*********************************************************************
        ' *
        ' * Function:
        ' *     Parser::ParseForLoopVariableDeclaration
        ' *
        ' * Purpose:
        ' *     Parses: <ident>[ArrayList] As <type>
        ' *
        ' **********************************************************************/
        ' File: Parser.cpp
        ' Lines: 5761 - 5761
        ' Expression* .Parser::ParseForLoopVariableDeclaration( [ ParseTree::BlockStatement* ForBlock ] [ _In_ Token* ForStart ] [ _Out_ ParseTree::VariableDeclarationStatement** Decl ] [ _Inout_ bool& ErrorInConstruct ] )

        Private Function ParseForLoopVariableDeclaration() As VariableDeclaratorSyntax

            ' Parse the control variable declaration
            Dim Declarator = ParseModifiedIdentifier(True, False)

            If Declarator.ContainsDiagnostics Then
                ' If we see As before a In or Each, then assume that
                ' we are still on the Control Variable Declaration.
                ' Otherwise, don't resync and allow the caller to
                ' decide how to recover.

                If PeekAheadFor(SyntaxKind.AsKeyword, SyntaxKind.InKeyword, SyntaxKind.EqualsToken) = SyntaxKind.AsKeyword Then
                    Declarator = ResyncAt(Declarator, SyntaxKind.AsKeyword)
                End If
            End If

            Dim [As] As KeywordSyntax = Nothing
            Dim optionalAsClause As AsClauseSyntax = Nothing

            If TryParseKeyword(SyntaxKind.AsKeyword,[as]) Then
                Dim Type = ParseGeneralType()
                optionalAsClause = SyntaxFactory.SimpleAsClause([As], Nothing, Type)
            End If ' Else if "As" is not present, the error falls out as a "Syntax error" IN the caller

            Dim names = _pool.AllocateSeparated(Of ModifiedIdentifierSyntax)().Add(Declarator).ToListAndFree(_pool)
            Return SyntaxFactory.VariableDeclarator(names, optionalAsClause, Nothing)
        End Function

        ' Parse a reference to a label, which can be an identifier or a line number.

        ' File: Parser.cpp
        ' Lines: 11363 - 11363
        ' .Parser::ParseLabelReference( [ _Out_ ParseTree::LabelReferenceStatement* LabelReference ] [ _Inout_ bool& ErrorInConstruct ] )

        Private Function ParseLabelReference() As SyntaxToken

            Dim label As SyntaxToken = CurrentToken

            If label.Kind = SyntaxKind.IdentifierToken Then

                Dim ident As IdentifierTokenSyntax = DirectCast(label, IdentifierTokenSyntax)
                If ident.TypeCharacter <> TypeCharacter.None Then
                    label = ReportSyntaxError(label, ERRID.ERR_NoTypecharInLabel)
                    GetNextToken()
                    Return label
                End If
                Return ParseIdentifier()

            ElseIf label.Kind = SyntaxKind.IntegerLiteralToken Then

                Dim intLiteral As IntegerLiteralTokenSyntax = DirectCast(label, IntegerLiteralTokenSyntax)

                If Not intLiteral.ContainsDiagnostics Then
                    If intLiteral.TypeSuffix = TypeCharacter.None Then

                        ' Labels must be unsigned. Reinterpret label as a ulong / uint in case it was a negative number written in hex &hffffffff or octal.
                        Dim intLiteralValue As ULong = CULng(intLiteral.ObjectValue)
                        If TypeOf intLiteral Is IntegerLiteralTokenSyntax(Of Integer) Then
                            intLiteralValue = CUInt(intLiteralValue)
                        End If
                        intLiteral = New IntegerLiteralTokenSyntax(Of ULong)(SyntaxKind.IntegerLiteralToken, intLiteral.ToString, intLiteral.GetLeadingTrivia, intLiteral.GetTrailingTrivia, intLiteral.Base, TypeCharacter.None, intLiteralValue)
                    Else
                        intLiteral = ReportSyntaxError(intLiteral, ERRID.ERR_Syntax)
                    End If
                End If

                GetNextToken()

                Return intLiteral

            Else
                label = InternalSyntaxFactory.MissingIdentifier()
                label = ReportSyntaxError(label, ERRID.ERR_ExpectedIdentifier)
                Return label
            End If

        End Function

        ' File: Parser.cpp
        ' Lines: 11410 - 11410
        ' .Parser::ParseGotoStatement( [ _Inout_ bool& ErrorInConstruct ] )

        Private Function ParseGotoStatement() As StatementSyntax
            Dim gotoKeyword As KeywordSyntax = Nothing
            TryParseKeyword(SyntaxKind.GoToKeyword, gotoKeyword)

            Dim labelName = ParseLabelReference()

            'TODO - Not calling ResyncAt may cause different errors from Dev10.

            'Dev10 calls ResyncAt  here if the label has an error.  However, this is
            ' unnecessary now because GetStatementTerminator will do the resync.

            Return SyntaxFactory.GoToStatement(gotoKeyword, GetLabelSyntaxForIdentifierOrLineNumber(labelName))
        End Function

        Private Function GetLabelSyntaxForIdentifierOrLineNumber(ByVal labelName As SyntaxToken) As LabelSyntax
            Debug.Assert(labelName.Kind.IsIn(SyntaxKind.IntegerLiteralToken, SyntaxKind.IdentifierToken))
            Return If(labelName.Kind = SyntaxKind.IntegerLiteralToken,
                       SyntaxFactory.NumericLabel(labelName), SyntaxFactory.IdentifierLabel(labelName))
        End Function

        ' File: Parser.cpp
        ' Lines: 11440 - 11440
        ' .Parser::ParseOnErrorStatement( [ _Inout_ bool& ErrorInConstruct ] )

        Private Function ParseOnErrorStatement() As StatementSyntax
            Dim onKeyword As KeywordSyntax = Nothing
            TryParseKeyword(SyntaxKind.OnKeyword, onKeyword)
            Dim errorKeyword As KeywordSyntax = Nothing

            If Not TryParseKeyword(SyntaxKind.ErrorKeyword, errorKeyword) Then
                errorKeyword = ReportSyntaxError(InternalSyntaxFactory.MissingKeyword(SyntaxKind.ErrorKeyword), ERRID.ERR_ObsoleteOnGotoGosub)
                errorKeyword = ResyncAt(errorKeyword, SyntaxKind.GoToKeyword, SyntaxKind.ResumeKeyword)
            End If

            If CurrentToken.Kind = SyntaxKind.ResumeKeyword Then

                Return ParseOnErrorResumeNext(onKeyword, errorKeyword)

            ElseIf CurrentToken.Kind = SyntaxKind.GoToKeyword Then

                Return ParseOnErrorGoto(onKeyword, errorKeyword)

            Else
                Dim missingGotoKeyword = InternalSyntaxFactory.MissingKeyword(SyntaxKind.GoToKeyword)

                If Not errorKeyword.ContainsDiagnostics Then
                    missingGotoKeyword = ReportSyntaxError(missingGotoKeyword, ERRID.ERR_ExpectedResumeOrGoto)
                End If

                Dim statement = SyntaxFactory.OnErrorGoToStatement(SyntaxKind.OnErrorGoToLabelStatement,
                                                       onKeyword,
                                                       errorKeyword,
                                                       missingGotoKeyword,
                                                       Nothing,
                                                       SyntaxFactory.IdentifierLabel(InternalSyntaxFactory.MissingIdentifier()))

                Return statement
            End If

        End Function

        Private Function ParseOnErrorResumeNext(onKeyword As KeywordSyntax, errorKeyword As KeywordSyntax) As OnErrorResumeNextStatementSyntax
            Dim resumeKeyword As KeywordSyntax = Nothing
            Contract_ExpectingKeyword(SyntaxKind.ResumeKeyword, resumeKeyword)

            Dim nextKeyword As KeywordSyntax = Nothing
            VerifyExpectedToken(SyntaxKind.NextKeyword, nextKeyword)

            Return SyntaxFactory.OnErrorResumeNextStatement(onKeyword, errorKeyword, resumeKeyword, nextKeyword)
        End Function

        Private Function ParseOnErrorGoto(onKeyword As KeywordSyntax, errorKeyword As KeywordSyntax) As OnErrorGoToStatementSyntax
            Dim gotoKeyword As KeywordSyntax = Nothing
            Contract_ExpectingKeyword(SyntaxKind.GoToKeyword, gotoKeyword)

            Dim optionalMinusToken As PunctuationSyntax = Nothing
            Dim label As LabelSyntax
            Dim kind As SyntaxKind

            Dim nextToken As SyntaxToken = PeekToken(0)

            If nextToken.Kind = SyntaxKind.IntegerLiteralToken AndAlso
                nextToken.ValueText = "0" Then

                kind = SyntaxKind.OnErrorGoToZeroStatement
                label = SyntaxFactory.NumericLabel(nextToken)

                GetNextToken()

            ElseIf nextToken.Kind = SyntaxKind.MinusToken AndAlso
                PeekToken(1).Kind = SyntaxKind.IntegerLiteralToken AndAlso
                PeekToken(1).ValueText = "1" Then

                kind = SyntaxKind.OnErrorGoToMinusOneStatement
                optionalMinusToken = DirectCast(nextToken, PunctuationSyntax)
                label = SyntaxFactory.NumericLabel(PeekToken(1))

                GetNextToken()
                GetNextToken()

            Else
                kind = SyntaxKind.OnErrorGoToLabelStatement
                label = GetLabelSyntaxForIdentifierOrLineNumber(ParseLabelReference())
            End If

            Return SyntaxFactory.OnErrorGoToStatement(kind, onKeyword, errorKeyword, gotoKeyword, optionalMinusToken, label)
        End Function

        ' File: Parser.cpp
        ' Lines: 11551 - 11551
        ' .Parser::ParseResumeStatement( [ _Inout_ bool& ErrorInConstruct ] )

        Private Function ParseResumeStatement() As ResumeStatementSyntax
            Dim resumeKeyword As KeywordSyntax = Nothing
            Contract_ExpectingKeyword(SyntaxKind.ResumeKeyword, resumeKeyword)
            Dim statement As ResumeStatementSyntax

            If Not IsValidStatementTerminator(CurrentToken) Then
                Dim nextKeyword As KeywordSyntax = Nothing

                If TryParseKeyword(SyntaxKind.NextKeyword, nextKeyword) Then

                    statement = SyntaxFactory.ResumeNextStatement(resumeKeyword, SyntaxFactory.NextLabel(nextKeyword))

                Else
                      Dim optionalLabel = ParseLabelReference()

                    statement = SyntaxFactory.ResumeLabelStatement(resumeKeyword, GetLabelSyntaxForIdentifierOrLineNumber(optionalLabel))
                End If

            Else
                statement = SyntaxFactory.ResumeStatement(resumeKeyword, Nothing)
            End If

            Return statement

        End Function

        Private Function ParseAssignmentOrInvocationStatement() As StatementSyntax
            Dim target As ExpressionSyntax = ParseTerm()

            If target.ContainsDiagnostics Then target = ResyncAt(target, SyntaxKind.EqualsToken)

            ' Could be a function call or it could be an assignment

            If SyntaxFacts.IsAssignmentStatementOperatorToken(CurrentToken.Kind) Then
                ' Consume the assignment operator
                Dim operatorToken = ParsePunctuation()' As PunctuationSyntax = DirectCast(CurrentToken, PunctuationSyntax)
'                GetNextToken()

                TryEatNewLine()

                Dim source = ParseExpressionCore()

                If source.ContainsDiagnostics Then
                    ' Sync to avoid other errors
                    source = ResyncAt(source)
                End If

                Return MakeAssignmentStatement(target, operatorToken, source)
            End If

            Return SyntaxFactory.ExpressionStatement(MakeInvocationExpression(target))
        End Function

        Private Function MakeInvocationExpression(target As ExpressionSyntax) As ExpressionSyntax
            ' Dig into conditional access
            If target.Kind = SyntaxKind.ConditionalAccessExpression Then
                Dim conditionalTarget = DirectCast(target, ConditionalAccessExpressionSyntax)
                Dim invocation = MakeInvocationExpression(conditionalTarget.WhenNotNull)

                If conditionalTarget.WhenNotNull IsNot invocation Then
                    target = SyntaxFactory.ConditionalAccessExpression(conditionalTarget.Expression, conditionalTarget.QuestionMarkToken, invocation)
                End If

            ElseIf target.Kind <> SyntaxKind.InvocationExpression Then ' VS320205
                If Not CanEndExecutableStatement(CurrentToken) AndAlso
                    CurrentToken.Kind <> SyntaxKind.BadToken AndAlso
                    target.Kind <> SyntaxKind.PredefinedCastExpression Then
                    'TODO - Are there other built-in types that should not through this path?, i.e
                    'NodeKind.GetTypeKeyword,
                    'NodeKind.GetXmlNamespaceKeyword
                    ' See call to ParseAssignmentOrCallStatement
                    ' Actually why are built in casts treated differently??

                    ' absence of parentheses and act as if they were present.

                    ' A non-parenthesized argument list cannot contain
                    ' a newline.

                    Dim unexpected As GreenNode = Nothing
                    Dim arguments = ParseArguments(unexpected)
                    Dim closeParen = InternalSyntaxFactory.MissingPunctuation(SyntaxKind.CloseParenToken)
                    If unexpected IsNot Nothing Then
                        closeParen = closeParen.AddLeadingSyntax(unexpected)
                    End If
                    Dim argumentList = SyntaxFactory.ArgumentList(InternalSyntaxFactory.MissingPunctuation(SyntaxKind.OpenParenToken),
                                                                arguments,
                                                                closeParen)

                    target = SyntaxFactory.InvocationExpression(target, ReportSyntaxError(argumentList, ERRID.ERR_ObsoleteArgumentsNeedParens))
                Else
                    target = SyntaxFactory.InvocationExpression(target, Nothing)
                End If
            End If

            Return target
        End Function

        Private Function MakeAssignmentStatement(
                            left          As ExpressionSyntax,
                            operatorToken As PunctuationSyntax,
                            right         As ExpressionSyntax
                          ) As AssignmentStatementSyntax
            Select Case operatorToken.Kind

                Case SyntaxKind.EqualsToken
                    Return SyntaxFactory.SimpleAssignmentStatement(left, operatorToken, right)

                Case SyntaxKind.PlusEqualsToken
                    Return SyntaxFactory.AddAssignmentStatement(left, operatorToken, right)

                Case SyntaxKind.MinusEqualsToken
                    Return SyntaxFactory.SubtractAssignmentStatement(left, operatorToken, right)

                Case SyntaxKind.AsteriskEqualsToken
                    Return SyntaxFactory.MultiplyAssignmentStatement(left, operatorToken, right)

                Case SyntaxKind.SlashEqualsToken
                    Return SyntaxFactory.DivideAssignmentStatement(left, operatorToken, right)

                Case SyntaxKind.BackslashEqualsToken
                    Return SyntaxFactory.IntegerDivideAssignmentStatement(left, operatorToken, right)

                Case SyntaxKind.CaretEqualsToken
                    Return SyntaxFactory.ExponentiateAssignmentStatement(left, operatorToken, right)

                Case SyntaxKind.LessThanLessThanEqualsToken
                    Return SyntaxFactory.LeftShiftAssignmentStatement(left, operatorToken, right)

                Case SyntaxKind.GreaterThanGreaterThanEqualsToken
                    Return SyntaxFactory.RightShiftAssignmentStatement(left, operatorToken, right)

                Case SyntaxKind.AmpersandEqualsToken
                    Return SyntaxFactory.ConcatenateAssignmentStatement(left, operatorToken, right)

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(operatorToken.Kind)

            End Select
        End Function

        ' File: Parser.cpp
        ' Lines: 11600 - 11600
        ' .Parser::ParseCallStatement( [ _Inout_ bool& ErrorInConstruct ] )

        Private Function ParseCallStatement() As CallStatementSyntax
            Dim callKeyword As KeywordSyntax = Nothing
            Contract_ExpectingKeyword(SyntaxKind.CallKeyword, callKeyword)

            Dim invocation As ExpressionSyntax = MakeCallStatementExpression(ParseVariable())

            If invocation.ContainsDiagnostics Then invocation = ResyncAt(invocation)

            Return SyntaxFactory.CallStatement(callKeyword, invocation)
        End Function

        Private Function MakeCallStatementExpression(expr As ExpressionSyntax) As ExpressionSyntax
            If expr.Kind = SyntaxKind.ConditionalAccessExpression Then
                Dim conditionalTarget = DirectCast(expr, ConditionalAccessExpressionSyntax)
                Dim invocation = MakeCallStatementExpression(conditionalTarget.WhenNotNull)

                If conditionalTarget.WhenNotNull IsNot invocation Then
                    expr = SyntaxFactory.ConditionalAccessExpression(conditionalTarget.Expression, conditionalTarget.QuestionMarkToken, invocation)
                End If

            ElseIf expr.Kind <> SyntaxKind.InvocationExpression Then
                ' Make sure that the expression is an invocation in case user left off parentheses
                expr = SyntaxFactory.InvocationExpression(expr, Nothing)
            End If

            Return expr
        End Function

        ' File: Parser.cpp
        ' Lines: 11656 - 11656
        ' .Parser::ParseRaiseEventStatement( [ _Inout_ bool& ErrorInConstruct ] )

        Private Function ParseRaiseEventStatement() As RaiseEventStatementSyntax
            Dim raiseEventKeyword As KeywordSyntax = Nothing
            Contract_ExpectingKeyword(SyntaxKind.RaiseEventKeyword, raiseEventKeyword)

            Dim ident = ParseIdentifierNameAllowingKeyword()

            If ident.ContainsDiagnostics Then ident = ident.AddTrailingSyntax(ResyncAt())


            Dim optionalArgumentList As ArgumentListSyntax = Nothing
            If CurrentToken.Kind = SyntaxKind.OpenParenToken Then optionalArgumentList = ParseParenthesizedArguments()

            Return SyntaxFactory.RaiseEventStatement(raiseEventKeyword, ident, optionalArgumentList)
        End Function
        
        Private Function TryParseIdentiferAsContextualKeyword(thisKind As SyntaxKind, ByRef cKeyword As KeywordSyntax) As Boolean
            Dim possibleKeyword As KeywordSyntax = Nothing
            Dim isok = CurrentToken.Kind = SyntaxKind.IdentifierToken AndAlso
                TryIdentifierAsContextualKeyword(CurrentToken, possibleKeyword) AndAlso
                possibleKeyword.Kind= thisKInd
            If isok Then
                cKeyword = possibleKeyword
                GetNextToken()
            End If
            Return isok
        End Function

        ' File: Parser.cpp
        ' Lines: 11690 - 11690
        ' .Parser::ParseRedimStatement( [ _Inout_ bool& ErrorInConstruct ] )

        Private Function ParseRedimStatement() As StatementSyntax '[ReDim]
            Dim reDimKeyword As KeywordSyntax = Nothing
            Contract_ExpectingKeyword(SyntaxKind.ReDimKeyword, reDimKeyword)

            Dim optionalPreserveKeyword As KeywordSyntax = Nothing
            TryParseIdentiferAsContextualKeyword(SyntaxKind.PreserveKeyword, optionalPreserveKeyword)

            ' NOTE: Generation of ERR_RedimNoSizes error was moved to binding to match Dev10 behavior
            Dim clauses = _pool.AllocateSeparated(Of RedimClauseSyntax)()

            Do
                Dim possibleInvocation As ExpressionSyntax = ParseTerm(BailIfFirstTokenRejected:=False, RedimOrNewParent:=True)

                If possibleInvocation.ContainsDiagnostics Then
                    possibleInvocation = ResyncAt(possibleInvocation)
                End If

                Dim clause As RedimClauseSyntax

                If possibleInvocation.Kind = SyntaxKind.InvocationExpression Then
                    Dim invocation = DirectCast(possibleInvocation, InvocationExpressionSyntax)

                    clause = SyntaxFactory.RedimClause(invocation.Expression, invocation.ArgumentList)
                    Dim diagnostics() As DiagnosticInfo = invocation.GetDiagnostics()

                    If diagnostics IsNot Nothing AndAlso diagnostics.Length > 0 Then
                        clause = clause.WithDiagnostics(diagnostics)
                    End If
                Else
                    clause = SyntaxFactory.RedimClause(possibleInvocation, EmptyArgumentList())
                End If

                clauses.Add(clause)

                'Dim comma As PunctuationSyntax = Nothing
                'If Not TryGetTokenAndEatNewLine(SyntaxKind.CommaToken, comma) Then
                '    Exit Do
                'End If

                'clauses.AddSeparator(comma)

            Loop While TryParseCommaInto(clauses)

            Dim clauseslist = clauses.ToListAndFree(_pool)
            Dim statement = If(optionalPreserveKeyword Is Nothing,
                               SyntaxFactory.ReDimStatement(reDimKeyword, optionalPreserveKeyword, clausesList),
                               SyntaxFactory.ReDimPreserveStatement(reDimKeyword, optionalPreserveKeyword, clausesList)
                            )

            If CurrentToken.Kind = SyntaxKind.AsKeyword Then
                statement = statement.AddTrailingSyntax(CurrentToken, ERRID.ERR_ObsoleteRedimAs)
                GetNextToken()
            End If

            Return statement
        End Function
        
        Friend Function EmptyArgumentList() As ArgumentListSyntax
            Return SyntaxFactory.ArgumentList(InternalSyntaxFactory.MissingPunctuation(SyntaxKind.OpenParenToken),
                                              Nothing,
                                              InternalSyntaxFactory.MissingPunctuation(SyntaxKind.CloseParenToken))
        End Function

        ' File: Parser.cpp
        ' Lines: 11755 - 11755
        ' .Parser::ParseHandlerStatement( [ _Inout_ bool& ErrorInConstruct ] )

        Private Function ParseHandlerStatement() As AddRemoveHandlerStatementSyntax

            Debug.Assert(CurrentToken.Kind.IsIn(SyntaxKind.AddHandlerKeyword,
                                                SyntaxKind.RemoveHandlerKeyword), NameOf(ParseHandlerStatement).CalledOnWrongToken)

            Dim keyword = ParseKeyword()
            Dim kind = If(keyword.Kind = SyntaxKind.AddHandlerKeyword, SyntaxKind.AddHandlerStatement, SyntaxKind.RemoveHandlerStatement)

            Dim eventExpression = ParseExpressionCore()

            If eventExpression.ContainsDiagnostics Then eventExpression = ResyncAt(eventExpression, SyntaxKind.CommaToken)

            Dim commaToken As PunctuationSyntax = Nothing
            TryGetTokenAndEatNewLine(SyntaxKind.CommaToken, commaToken, createIfMissing:=True)

            Dim DelegateExpression = ParseExpressionCore()

            If DelegateExpression.ContainsDiagnostics Then
                DelegateExpression = ResyncAt(DelegateExpression)
            End If

            Return SyntaxFactory.AddRemoveHandlerStatement(kind, keyword, eventExpression, commaToken, DelegateExpression)
        End Function

        Private Function ParseKeyword(optional consume As Boolean = true) As KeywordSyntax
            Dim keyword = DirectCast(CurrentToken, KeywordSyntax)
            If consume Then GetNextToken()
            Return keyword
        End Function

        ' File: Parser.cpp
        ' Lines: 11830 - 11830
        ' .Parser::ParseExpressionBlockStatement( [ ParseTree::Statement::Opcodes Opcode ] [ _Inout_ bool& ErrorInConstruct ] )

        ' davidsch
        ' used by While/With/Synclock
        ' The return type can't be more specific because these statements don't share a more
        ' specific class.

        'TODO - Rename ParseKeywordExpression and share with other statements that follow this pattern
        ' i.e. Throw, others?
        Private Function ParseExpressionBlockStatement() As StatementSyntax
            Debug.Assert(CurrentToken.Kind.IsIn(SyntaxKind.WhileKeyword, SyntaxKind.WithKeyword, SyntaxKind.SyncLockKeyword),
                         NameOf(ParseExpressionBlockStatement).CalledOnWrongToken)

            Dim keyword = ParseKeyword()

            Dim operand = ParseExpressionCore(OperatorPrecedence.PrecedenceNone)

            If operand.ContainsDiagnostics Then
                operand = ResyncAt(operand)
            End If

            Dim statement As StatementSyntax = Nothing

            Select Case keyword.Kind
                Case SyntaxKind.WhileKeyword
                    statement = SyntaxFactory.WhileStatement(keyword, operand)
                Case SyntaxKind.WithKeyword
                    statement = SyntaxFactory.WithStatement(keyword, operand)
                Case SyntaxKind.SyncLockKeyword
                    statement = SyntaxFactory.SyncLockStatement(keyword, operand)
            End Select

            Return statement
        End Function

        ' File: Parser.cpp
        ' Lines: 11854 - 11854
        ' .Parser::ParseAssignmentStatement( [ _Inout_ bool& ErrorInConstruct ] )

        'TODO - rename ParseObsoleteAssignment
        Private Function ParseAssignmentStatement() As StatementSyntax
            Debug.Assert(CurrentToken.Kind.IsIn(SyntaxKind.LetKeyword,
                                                SyntaxKind.SetKeyword), NameOf(ParseAssignmentStatement).CalledOnWrongToken)
            ' Let and set are now illegal

            If CurrentToken.Kind = SyntaxKind.SetKeyword AndAlso
                (IsValidStatementTerminator(PeekToken(1)) OrElse PeekToken(1).Kind = SyntaxKind.OpenParenToken) AndAlso
                Context.IsWithin(SyntaxKind.SetAccessorBlock, SyntaxKind.GetAccessorBlock) Then
                ' If this is a set parse it as a property accessor and then mark it with an error
                ' so that the Set will terminate a Get context.
                Return ParsePropertyOrEventAccessor(SyntaxKind.SetAccessorStatement, Nothing, Nothing)
            Else

                Dim keyword =ParseKeyword()

                ' Only consume the let.  Leave the rest to be processed as an assignment in case the user wrote let x = ...
                Return InternalSyntaxFactory.EmptyStatement.AddTrailingSyntax(keyword, ERRID.ERR_ObsoleteLetSetNotNeeded)
            End If

        End Function

        ' File: Parser.cpp
        ' Lines: 11909 - 11909
        ' .Parser::ParseTry( )
        Private Function ParseTry() As TryStatementSyntax
            Dim tryKeyword As KeywordSyntax = Nothing
            Contract_ExpectingKeyword(SyntaxKind.TryKeyword, tryKeyword)

            Return SyntaxFactory.TryStatement(tryKeyword)
        End Function

        ' File: Parser.cpp
        ' Lines: 11933 - 11933
        ' .Parser::ParseCatch( [ _Inout_ bool& ErrorInConstruct ] )

        Private Function ParseCatch() As CatchStatementSyntax
            Dim catchKeyword As KeywordSyntax = Nothing
            Contract_ExpectingKeyword(SyntaxKind.CatchKeyword, catchKeyword)

            Dim optionalName As IdentifierNameSyntax = Nothing
            Dim optionalAsClause As SimpleAsClauseSyntax = Nothing

            If CurrentToken.Kind = SyntaxKind.IdentifierToken Then
                Dim id = ParseIdentifier()
                If id.Kind <> SyntaxKind.None Then
                    optionalName = SyntaxFactory.IdentifierName(id)
                End If

                Dim asKeyword As KeywordSyntax = Nothing
                Dim typeName As TypeSyntax = Nothing

                If TryGetToken(SyntaxKind.AsKeyword, asKeyword) Then

                    typeName = ParseTypeName()

                    If typeName.ContainsDiagnostics Then
                        typeName = ResyncAt(typeName, SyntaxKind.WhenKeyword)
                    End If

                    optionalAsClause = SyntaxFactory.SimpleAsClause(asKeyword, Nothing, typeName)
                End If
            End If

            Dim optionalWhenClause As CatchFilterClauseSyntax = Nothing
            Dim whenKeyword As KeywordSyntax = Nothing
            Dim filter As ExpressionSyntax = Nothing
            If TryParseKeyword(SyntaxKind.WhenKeyword, whenKeyword) Then
                filter = ParseExpressionCore()

                optionalWhenClause = SyntaxFactory.CatchFilterClause(whenKeyword, filter)
            End If

            Return SyntaxFactory.CatchStatement(catchKeyword, optionalName, optionalAsClause, optionalWhenClause)
        End Function

        ' File: Parser.cpp
        ' Lines: 12022 - 12022
        ' .Parser::ParseFinally( [ _Inout_ bool& ErrorInConstruct ] )

        Private Function ParseFinally() As FinallyStatementSyntax
            Dim finallyKeyword As KeywordSyntax = Nothing
            Contract_ExpectingKeyword(SyntaxKind.FinallyKeyword, finallyKeyword)

            Return SyntaxFactory.FinallyStatement(finallyKeyword)
        End Function

        Private Function ParseThrowStatement() As ThrowStatementSyntax
            Dim throwKeyword As KeywordSyntax = Nothing
            Contract_ExpectingKeyword(SyntaxKind.ThrowKeyword, throwKeyword)

            Dim value = ParseExpressionCore(bailIfFirstTokenRejected:=True)
            If value IsNot Nothing Then
                If value.ContainsDiagnostics Then
                    value = ResyncAt(value)
                End If
            End If
            Return SyntaxFactory.ThrowStatement(throwKeyword, value)
        End Function

        Private Function ParseError() As ErrorStatementSyntax
            Dim errorKeyword As KeywordSyntax = Nothing
            Contract_ExpectingKeyword(SyntaxKind.ErrorKeyword, errorKeyword)

            Dim value = ParseExpressionCore()

            If value.ContainsDiagnostics Then
                value = ResyncAt(value)
            End If

            Return SyntaxFactory.ErrorStatement(errorKeyword, value)
        End Function

        ' Parse an Erase statement.
        '
        ' File: Parser.cpp
        ' Lines: 12077 - 12077
        ' .Parser::ParseErase( [ _Inout_ bool& ErrorInConstruct ] )
        '
        Private Function ParseErase() As EraseStatementSyntax
            Dim eraseKeyword As KeywordSyntax = Nothing
            Contract_ExpectingKeyword(SyntaxKind.EraseKeyword, eraseKeyword)

            Dim variables = ParseVariableList()

            Return SyntaxFactory.EraseStatement(eraseKeyword, variables)
        End Function

        Private Function ShouldParseAsLabel() As Boolean
            Return IsFirstStatementOnLine(CurrentToken) AndAlso PeekToken(1).Kind = SyntaxKind.ColonToken
        End Function

        Private Function ParseLabel() As LabelStatementSyntax
            Debug.Assert(CurrentToken.Kind.isin(SyntaxKind.IdentifierToken, SyntaxKind.IntegerLiteralToken),
                         NameOf(ParseLabel).CalledOnWrongToken)

            Dim labelName = ParseLabelReference()

            If labelName.Kind = SyntaxKind.IntegerLiteralToken AndAlso CurrentToken.Kind <> SyntaxKind.ColonToken Then
                Return ReportSyntaxError(SyntaxFactory.LabelStatement(labelName, InternalSyntaxFactory.MissingPunctuation(SyntaxKind.ColonToken)), ERRID.ERR_ObsoleteLineNumbersAreLabels)
            End If

            Dim trivia As New CodeAnalysis.Syntax.InternalSyntax.SyntaxList(Of VisualBasicSyntaxNode)(labelName.GetTrailingTrivia())
            Debug.Assert(trivia.Count > 0)
            Dim index = -1
            For i = 0 To trivia.Count - 1
                If trivia(i).Kind = SyntaxKind.ColonTrivia Then
                    index = i
                    Exit For
                End If
            Next

            Debug.Assert(index >= 0)
            labelName = DirectCast(labelName.WithTrailingTrivia(trivia.GetStartOfTrivia(index).Node), SyntaxToken)

            Dim colonTrivia = DirectCast(trivia(index), SyntaxTrivia)
            trivia = trivia.GetEndOfTrivia(index + 1)
            Dim colonToken = New PunctuationSyntax(SyntaxKind.ColonToken, colonTrivia.Text, Nothing, trivia.Node)
            Return SyntaxFactory.LabelStatement(labelName, colonToken)
        End Function
        

        ' Parse a Mid statement.
        '
        ' File: Parser.cpp
        ' Lines: 12105 - 12105
        ' .Parser::ParseMid( [ _Inout_ bool& ErrorInConstruct ] )
        '
        Private Function ParseMid() As AssignmentStatementSyntax

            Debug.Assert(CurrentToken.Kind = SyntaxKind.IdentifierToken AndAlso DirectCast(CurrentToken, IdentifierTokenSyntax).PossibleKeywordKind = SyntaxKind.MidKeyword)

            ' Mid[$]  OpenParenthesis  Expression  Comma  Expression  [  Comma  Expression  ]  CloseParenthesis  Equals  Expression  StatementTerminator

            'TODO - Does the Mid need to be modeled as a contextual keyword?  Right now it is kept as an identifier.
            Dim mid = DirectCast(CurrentToken, IdentifierTokenSyntax)
            GetNextToken()

            Debug.Assert(CurrentToken.Kind = SyntaxKind.OpenParenToken)
            Dim openParen As PunctuationSyntax = Nothing
            TryGetTokenAndEatNewLine(SyntaxKind.OpenParenToken, openParen)

            Dim argumentsBuilder = _pool.AllocateSeparated(Of ArgumentSyntax)()
            Dim comma As PunctuationSyntax = Nothing

            ' Parse the first required argument followed by a comma
            argumentsBuilder.Add(ParseArgument(RedimOrNewParent:=False))

            If Not TryGetTokenAndEatNewLine(SyntaxKind.CommaToken, comma) Then
                VerifyExpectedToken(SyntaxKind.CommaToken, comma)
            End If

            argumentsBuilder.AddSeparator(comma)

            ' Parse the second required argument
            argumentsBuilder.Add(ParseArgument(RedimOrNewParent:=False))

            If TryGetTokenAndEatNewLine(SyntaxKind.CommaToken, comma) Then
                argumentsBuilder.AddSeparator(comma)

                ' Parse the third argument
                argumentsBuilder.Add(ParseArgument(RedimOrNewParent:=False))
            End If

            Dim arguments = argumentsBuilder.ToListAndFree(_pool)

            Dim closeParen As PunctuationSyntax = Nothing
            TryEatNewLineAndGetToken(SyntaxKind.CloseParenToken, closeParen, createIfMissing:=True)

            Dim equals As PunctuationSyntax = Nothing
            VerifyExpectedToken(SyntaxKind.EqualsToken, equals)

            Dim source As ExpressionSyntax = ParseExpressionCore()

            If source.ContainsDiagnostics() Then
                source = ResyncAt(source)
            End If

            Dim statement = SyntaxFactory.MidAssignmentStatement(SyntaxFactory.MidExpression(mid, SyntaxFactory.ArgumentList(openParen, arguments, closeParen)),
                                                                 equals, source)

            Return statement
        End Function

        ' Out
        ' File: Parser.cpp
        ' Lines: 16887 - 16887
        ' Expression* .Parser::ParseOptionalWhileOrUntilClause( [ _Out_ bool* IsWhile ] [ _Inout_ bool& ErrorInConstruct ] )
        Private Function TryParseOptionalWhileOrUntilClause(precedingKeyword As KeywordSyntax, ByRef optionalWhileOrUntilClause As WhileOrUntilClauseSyntax) As Boolean
            If Not CanFollowStatement(CurrentToken) Then

                Dim keyword As KeywordSyntax = Nothing
                If CurrentToken.Kind = SyntaxKind.WhileKeyword Then
                    keyword = DirectCast(CurrentToken, KeywordSyntax)
                Else
                    TryTokenAsContextualKeyword(CurrentToken, SyntaxKind.UntilKeyword, keyword)
                End If

                If keyword IsNot Nothing Then

                    ' Error reporting for ERRID.ERR_LoopDoubleCondition moved to the DoLoopContext

                    GetNextToken()

                    Dim condition = ParseExpressionCore()
                    If condition.ContainsDiagnostics Then
                        condition = ResyncAt(condition)
                    End If

                    Dim kind As SyntaxKind
                    If keyword.Kind = SyntaxKind.WhileKeyword Then
                        kind = SyntaxKind.WhileClause
                    Else
                        kind = SyntaxKind.UntilClause
                    End If

                    optionalWhileOrUntilClause = SyntaxFactory.WhileOrUntilClause(kind, keyword, condition)
                    Return True

                Else
                    Dim kind As SyntaxKind
                    If precedingKeyword.Kind = SyntaxKind.DoKeyword Then
                        kind = SyntaxKind.UntilClause
                        keyword = InternalSyntaxFactory.MissingKeyword(SyntaxKind.UntilKeyword)
                    Else
                        kind = SyntaxKind.WhileClause
                        keyword = InternalSyntaxFactory.MissingKeyword(SyntaxKind.WhileKeyword)
                    End If

                    Dim clause As WhileOrUntilClauseSyntax = SyntaxFactory.WhileOrUntilClause(kind, keyword, InternalSyntaxFactory.MissingExpression)

                    ' Dev10 places the error only on the current token. 
                    ' This marks the entire clause which seems better.
                    optionalWhileOrUntilClause = ReportSyntaxError(ResyncAt(clause), ERRID.ERR_Syntax)
                    Return True
                End If
            End If

            Return False
        End Function

        Private Function ParseReturnStatement() As ReturnStatementSyntax
            Dim returnKeyword As KeywordSyntax = Nothing
            Contract_ExpectingKeyword(SyntaxKind.ReturnKeyword, returnKeyword)

            Dim startToken = CurrentToken

            ' Dev10#694102 - Consider "Dim f = Sub() Return + 5". Which of the following should it mean?
            '   Dim f = (Sub() Return) + 5   ' use an overloaded addition operator
            '   Dim f = (Sub() Return (+5))  ' return the number +5
            ' The spec says that we will greedily parse the body of a statement lambda.
            ' And indeed doing so agrees with the user's intuition ("Return +5" should give an error
            ' that subs cannot return values: it should not give an error that there is no overloaded
            ' operator + between functions and integers!)

            ' We will try to parse the expression, but the final "true" argument means
            ' "Bail if the first token isn't a valid way to start an expression; in this case just return NULL"
            Dim operand = ParseExpressionCore(OperatorPrecedence.PrecedenceNone, True)

            ' Note: Orcas behavior had been to bail immediately if IsValidStatementTerminator(CurrentToken).
            ' Well, all such tokens are invalid as ways to start an expression, so the above call to ParseExpressionCore
            ' will bail correctly for them. I've put in this assert to show that it's safe to skip the check for IsValidStatementTerminator.

            Debug.Assert(operand Is Nothing OrElse Not IsValidStatementTerminator(startToken), "Unexpected: we should have bailed on the token after this return statement")

            If operand Is Nothing Then
                ' if we bailed because the first token was not a way to start an expression, we might
                ' be in a situation like "goo(Sub() Return, 15)" where next token was a valid thing
                ' to come after this return statement, in which case we proceed without trying
                ' to gobble up the return expression. Or we might be like "Return Select", where the next
                ' token cannot possibly come after the statement, so we'll report on it now:
                If Not CanFollowStatement(CurrentToken) Then
                    ' This time don't let it bail:
                    operand = ParseExpressionCore(OperatorPrecedence.PrecedenceNone, False)
                End If

            ElseIf operand.ContainsDiagnostics Then
                operand = ResyncAt(operand)
            End If

            Return SyntaxFactory.ReturnStatement(returnKeyword, operand)
        End Function

        'TODO - See if all of the simple statements that take only one keyword can be merged into one parsing function
        ' StopOrEnd
        ' TryStatement
        ' FinallyStatement
        Private Function ParseStopOrEndStatement() As StopOrEndStatementSyntax
            Debug.Assert(CurrentToken.Kind.IsIn(SyntaxKind.StopKeyword, SyntaxKind.EndKeyword),
                NameOf(ParseStopOrEndStatement).CalledOnWrongToken)

            Dim stopOrEndKeyword = ParseKeyword() 

            Dim stmtKind = If(stopOrEndKeyword.Kind = SyntaxKind.StopKeyword, SyntaxKind.StopStatement, SyntaxKind.EndStatement)

            Return SyntaxFactory.StopOrEndStatement(stmtKind, stopOrEndKeyword)
        End Function

        Private Function ParseUsingStatement() As UsingStatementSyntax
            Dim usingKeyword As KeywordSyntax = Nothing
            Contract_ExpectingKeyword(SyntaxKind.UsingKeyword, usingKeyword)

            Dim optionalExpression As ExpressionSyntax = Nothing
            Dim variables As CodeAnalysis.Syntax.InternalSyntax.SeparatedSyntaxList(Of VariableDeclaratorSyntax) = Nothing

            Dim nextToken As SyntaxToken = PeekToken(1)

            ' change from Dev10: allowing as new with multiple variable names, e.g. "Using a, b As New C1()"
            If nextToken.Kind.IsIn(SyntaxKind.AsKeyword,
                                   SyntaxKind.EqualsToken,
                                   SyntaxKind.CommaToken,
                                   SyntaxKind.QuestionToken) Then

                variables = ParseVariableDeclaration(allowAsNewWith:=True)
            Else
                optionalExpression = ParseExpressionCore()
            End If

            'TODO - not resyncing here may cause errors to differ from Dev10.

            'No need to resync on error.  This will be handled by GetStatementTerminator

            Return SyntaxFactory.UsingStatement(usingKeyword, optionalExpression, variables)
        End Function


        Private Function ParseAwaitStatement() As ExpressionStatementSyntax
            Dim awaitKeyword As KeywordSyntax = Nothing
            Contract_ExpectingContextualKeyword(SyntaxKind.AwaitKeyword, awaitKeyword)
            'Debug.Assert(CurrentToken.Kind = SyntaxKind.IdentifierToken AndAlso
            '             DirectCast(CurrentToken, IdentifierTokenSyntax).ContextualKind = SyntaxKind.AwaitKeyword,
            '             "ParseAwaitStatement called on wrong token.")

            Dim expression = ParseAwaitExpression(awaitKeyword)

            Debug.Assert(expression.Kind = SyntaxKind.AwaitExpression)

            If expression.ContainsDiagnostics Then expression = ResyncAt(expression)

            Return SyntaxFactory.ExpressionStatement(expression)
        End Function

        Private Function ParseYieldStatement() As YieldStatementSyntax
            'Dim yieldKeyword As KeywordSyntax = Nothing
            ' Contract_ExpectingContextualKeyword(SyntaxKind.YieldKeyword, yieldKeyword)

            Debug.Assert(DirectCast(CurrentToken, IdentifierTokenSyntax).ContextualKind = SyntaxKind.YieldKeyword)

            Dim yieldKeyword As KeywordSyntax = Nothing

            TryIdentifierAsContextualKeyword(CurrentToken, yieldKeyword)

            Debug.Assert(yieldKeyword IsNot Nothing AndAlso yieldKeyword.Kind = SyntaxKind.YieldKeyword)

            yieldKeyword = CheckFeatureAvailability(Feature.Iterators, yieldKeyword)
            GetNextToken()

            Dim expression As ExpressionSyntax = ParseExpressionCore()
            Return SyntaxFactory.YieldStatement(yieldKeyword, expression)
        End Function

        Private Function ParsePrintStatement() As PrintStatementSyntax

            Dim questionToken = ParsePunctuation()
            Dim expression As ExpressionSyntax = ParseExpressionCore()
            Dim result = SyntaxFactory.PrintStatement(questionToken, expression)

            ' skip possible statement terminator
            Dim lookahead = PeekToken(1)

            If lookahead.Kind <> SyntaxKind.EndOfFileToken OrElse _scanner.Options.Kind = SourceCodeKind.Regular Then
                result = result.AddError(ERRID.ERR_UnexpectedExpressionStatement)
            End If

            Return result
        End Function


    End Class

End Namespace
