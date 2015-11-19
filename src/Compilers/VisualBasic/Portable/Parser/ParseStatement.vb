' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

'-----------------------------------------------------------------------------
' Contains the definition of the Parser
'-----------------------------------------------------------------------------
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports InternalSyntaxFactory = Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax.SyntaxFactory

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax

    Friend Partial Class Parser

        '
        '============ Methods for parsing specific executable statements
        '

        ' [in] Token starting the statement
        ' File: Parser.cpp
        ' Lines: 5870 - 5870
        ' Statement* .Parser::ParseContinueStatement( [ _In_ Token* StmtStart ] [ _Inout_ bool& ErrorInConstruct ] )

        Private Function ParseContinueStatement() As ContinueStatementSyntax
            Debug.Assert(CurrentToken.Kind = SyntaxKind.ContinueKeyword, "ParseContinueStatement called on wrong token")

            Dim continueKeyword As KeywordSyntax = DirectCast(CurrentToken, KeywordSyntax)
            GetNextToken()

            Dim kind As SyntaxKind
            Dim blockKeyword As KeywordSyntax = Nothing

            Select Case (CurrentToken.Kind)

                Case SyntaxKind.DoKeyword
                    kind = SyntaxKind.ContinueDoStatement
                    blockKeyword = DirectCast(CurrentToken, KeywordSyntax)
                    GetNextToken()

                Case SyntaxKind.ForKeyword
                    kind = SyntaxKind.ContinueForStatement
                    blockKeyword = DirectCast(CurrentToken, KeywordSyntax)
                    GetNextToken()

                Case SyntaxKind.WhileKeyword
                    kind = SyntaxKind.ContinueWhileStatement
                    blockKeyword = DirectCast(CurrentToken, KeywordSyntax)
                    GetNextToken()

                Case Else

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

                            Case SyntaxKind.ForBlock, SyntaxKind.ForEachBlock
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
            End Select

            Dim statement = SyntaxFactory.ContinueStatement(kind, continueKeyword, blockKeyword)

            Return statement
        End Function

        Private Function ParseExitStatement() As StatementSyntax
            Debug.Assert(CurrentToken.Kind = SyntaxKind.ExitKeyword, "ParseExitStatement called on wrong token")

            Dim exitKeyword As KeywordSyntax = DirectCast(CurrentToken, KeywordSyntax)
            GetNextToken()

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
            Debug.Assert(CurrentToken.Kind = SyntaxKind.CaseKeyword, "ParseCaseStatement called on wrong token.")

            Dim caseKeyword As KeywordSyntax = DirectCast(CurrentToken, KeywordSyntax)
            GetNextToken()

            Dim caseClauses = _pool.AllocateSeparated(Of CaseClauseSyntax)()
            Dim elseKeyword As KeywordSyntax = Nothing

            If CurrentToken.Kind = SyntaxKind.ElseKeyword Then
                elseKeyword = DirectCast(CurrentToken, KeywordSyntax)
                GetNextToken() '// get off ELSE

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

                            Dim relationalOperator = DirectCast(CurrentToken, PunctuationSyntax)
                            GetNextToken() ' get off relational operator
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

                    Dim comma As PunctuationSyntax = Nothing
                    If Not TryGetTokenAndEatNewLine(SyntaxKind.CommaToken, comma) Then
                        Exit Do
                    End If

                    caseClauses.AddSeparator(comma)
                Loop

            End If

            Dim separatedCaseClauses = caseClauses.ToList()
            _pool.Free(caseClauses)

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
            Debug.Assert(CurrentToken.Kind = SyntaxKind.SelectKeyword, "ParseSelectStatement called on wrong token.")

            Dim selectKeyword As KeywordSyntax = DirectCast(CurrentToken, KeywordSyntax)

            GetNextToken() ' get off SELECT

            ' Allow the expected CASE token to be present or not.

            Dim optionalCaseKeyword As KeywordSyntax = Nothing
            TryGetToken(SyntaxKind.CaseKeyword, optionalCaseKeyword)

            Dim value As ExpressionSyntax = ParseExpressionCore()

            If value.ContainsDiagnostics Then
                value = ResyncAt(value)
            End If

            Dim statement = SyntaxFactory.SelectStatement(selectKeyword, optionalCaseKeyword, value)

            Return statement
        End Function

        ' ParseIfConstruct handles the parsing of block and line if statements and
        ' block and line elseif statements, setting *IsLineIf as appropriate.
        ' For a line if/elseif, parsing consumes through the first statement (if any)
        ' in the then clause. The returned tree is the block created for the if or else if.

        ' Parse the expression (and following text) in an If or ElseIf statement.

        ' File: Parser.cpp
        ' Lines: 11249 - 11249
        ' ExpressionBlockStatement* .Parser::ParseIfConstruct( [ ParseTree::IfStatement* IfContainingElseIf ] [ _Out_ bool* IsLineIf ] [ _Inout_ bool& ErrorInConstruct ] )

        'davidsch - Renamed ParseIfStatement from ParseIfConstruct
        Private Function ParseIfStatement() As IfStatementSyntax

            Debug.Assert(CurrentToken.Kind = SyntaxKind.IfKeyword, "ParseIfConstruct called on wrong token.")

            Dim ifKeyword As KeywordSyntax = DirectCast(CurrentToken, KeywordSyntax)
            GetNextToken()

            Dim condition = ParseExpressionCore(OperatorPrecedence.PrecedenceNone)

            If condition.ContainsDiagnostics Then
                condition = ResyncAt(condition, SyntaxKind.ThenKeyword)
            End If

            Dim thenKeyword As KeywordSyntax = Nothing
            TryGetToken(SyntaxKind.ThenKeyword, thenKeyword)

            Dim statement = SyntaxFactory.IfStatement(ifKeyword, condition, thenKeyword)

            Return statement

        End Function

        Private Function ParseElseStatement() As ElseStatementSyntax

            Debug.Assert(CurrentToken.Kind = SyntaxKind.ElseKeyword, "ParseIfConstruct called on wrong token.")

            Dim elseKeyword = DirectCast(CurrentToken, KeywordSyntax)
            GetNextToken()

            Dim statement = SyntaxFactory.ElseStatement(elseKeyword)

            Return statement
        End Function

        Private Function ParseElseIfStatement() As StatementSyntax

            Debug.Assert(CurrentToken.Kind = SyntaxKind.ElseIfKeyword OrElse (CurrentToken.Kind = SyntaxKind.ElseKeyword AndAlso PeekToken(1).Kind = SyntaxKind.IfKeyword),
                         "ParseIfConstruct called on wrong token.")

            Dim elseIfKeyword As KeywordSyntax = Nothing

            If CurrentToken.Kind = SyntaxKind.ElseIfKeyword Then
                elseIfKeyword = DirectCast(CurrentToken, KeywordSyntax)

                GetNextToken()

            ElseIf CurrentToken.Kind = SyntaxKind.ElseKeyword Then
                ' When written as 'Else If' we need to merged the two keywords together.

                Dim elseKeyword = DirectCast(CurrentToken, KeywordSyntax)
                GetNextToken()

                If Context.IsSingleLine Then
                    ' But inside of a single-line If this isn't allowed. We parse as an Else statement
                    ' so that the SingleLineIfBlockContext can parse the rest as a separate If statement.

                    Return SyntaxFactory.ElseStatement(elseKeyword)
                End If

                Dim ifKeyword = DirectCast(CurrentToken, KeywordSyntax)
                GetNextToken()

                elseIfKeyword = New KeywordSyntax(SyntaxKind.ElseIfKeyword, MergeTokenText(elseKeyword, ifKeyword), elseKeyword.GetLeadingTrivia(), ifKeyword.GetTrailingTrivia())
            End If

            Dim condition = ParseExpressionCore(OperatorPrecedence.PrecedenceNone)

            If condition.ContainsDiagnostics Then
                condition = ResyncAt(condition, SyntaxKind.ThenKeyword)
            End If

            Dim thenKeyword As KeywordSyntax = Nothing
            TryGetToken(SyntaxKind.ThenKeyword, thenKeyword)

            Dim statement = SyntaxFactory.ElseIfStatement(elseIfKeyword, condition, thenKeyword)

            Return statement
        End Function

        Private Function ParseAnachronisticStatement() As StatementSyntax
            ' Assume CurrentToken is on ENDIF, WEND
            Debug.Assert(CurrentToken.Kind = SyntaxKind.EndIfKeyword OrElse
                             CurrentToken.Kind = SyntaxKind.GosubKeyword OrElse
                             CurrentToken.Kind = SyntaxKind.WendKeyword, "ParseAnachronisticEndIfStatement called on wrong token")

            Dim keyword As KeywordSyntax = DirectCast(CurrentToken, KeywordSyntax)

            ' Put the 'ENDIF'/'WEND'/'GOSUB' token in the unexpected.
            Dim unexpected As SyntaxList(Of SyntaxToken) = ResyncAt()

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

            ' Assume CurrentToken is on Do
            Debug.Assert(CurrentToken.Kind = SyntaxKind.DoKeyword, "ParseDoStatement called on wrong token")

            Dim doKeyword = DirectCast(CurrentToken, KeywordSyntax)

            ' Consume the Do.
            GetNextToken()

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

            Dim statement As DoStatementSyntax = SyntaxFactory.DoStatement(kind, doKeyword, optionalWhileOrUntilClause)

            Return statement
        End Function

        Private Function ParseLoopStatement() As LoopStatementSyntax
            ' Assume CurrentToken is on Loop
            Debug.Assert(CurrentToken.Kind = SyntaxKind.LoopKeyword, "ParseDoStatement called on wrong token")

            Dim loopKeyword = DirectCast(CurrentToken, KeywordSyntax)
            GetNextToken()

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

            Dim statement As LoopStatementSyntax = SyntaxFactory.LoopStatement(kind, loopKeyword, optionalWhileOrUntilClause)

            Return statement
        End Function

        Private Function ParseForStatement() As StatementSyntax
            ' Assume CurrentToken is on For
            Debug.Assert(CurrentToken.Kind = SyntaxKind.ForKeyword, "ParseForStatement called on wrong token")

            Dim forKeyword As KeywordSyntax = DirectCast(CurrentToken, KeywordSyntax)

            ' Consume the FOR.
            GetNextToken()

            Dim eachKeyword As KeywordSyntax = Nothing
            If TryGetToken(SyntaxKind.EachKeyword, eachKeyword) Then

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

            Dim statement = SyntaxFactory.ForEachStatement(forKeyword, eachKeyword, controlVariable, inKeyword, expression)

            Return statement
        End Function

        Private Function ParseForStatement(forKeyword As KeywordSyntax) As ForStatementSyntax

            Dim controlVariable = ParseForLoopControlVariable()

            Dim fromValue As ExpressionSyntax = Nothing
            Dim toValue As ExpressionSyntax = Nothing

            If controlVariable.ContainsDiagnostics Then
                controlVariable = ResyncAt(controlVariable, SyntaxKind.EqualsToken, SyntaxKind.ToKeyword)
            End If

            ' TODO - Handle case where controlVariable is OK but current token is not equals.  Need to resync in that case too
            ' e.g.
            ' For i,j = 1 to 10
            ' See bug 8590.

            Dim equalsToken As PunctuationSyntax = Nothing

            If TryGetTokenAndEatNewLine(SyntaxKind.EqualsToken, equalsToken) Then

                ' Dev10_545918 - Allow implicit line continuation after '=' 

                fromValue = ParseExpressionCore()

                If fromValue.ContainsDiagnostics Then
                    fromValue = ResyncAt(fromValue, SyntaxKind.ToKeyword)
                End If

            Else
                'Dev 10 only reported syntax error.  Code now reports expected '=' and syntax error.
                ' TODO - consider removing redundant syntax error message.
                equalsToken = DirectCast(HandleUnexpectedToken(SyntaxKind.EqualsToken), PunctuationSyntax)

                fromValue = InternalSyntaxFactory.MissingExpression.AddTrailingSyntax(ResyncAt({SyntaxKind.ToKeyword}), ERRID.ERR_Syntax)

            End If

            Dim toKeyword As KeywordSyntax = Nothing
            If TryGetToken(SyntaxKind.ToKeyword, toKeyword) Then

                'TODO - davidsch - Why is newline allowed after '=' but not after 'to'?
                toValue = ParseExpressionCore()

                If toValue.ContainsDiagnostics Then
                    toValue = ResyncAt(toValue, SyntaxKind.StepKeyword)
                End If

            Else
                'No error for expected 'To' keyword.  HandleUnexpectedToken returns ERRID_Syntax
                toKeyword = DirectCast(HandleUnexpectedToken(SyntaxKind.ToKeyword), KeywordSyntax)
                'TODO - ERRID_ExpectedExpression here?
                toValue = InternalSyntaxFactory.MissingExpression.AddTrailingSyntax(ResyncAt({SyntaxKind.ToKeyword}))

            End If

            Dim optionalStepClause As ForStepClauseSyntax = Nothing
            Dim stepKeyword As KeywordSyntax = Nothing
            Dim stepValue As ExpressionSyntax = Nothing

            If TryGetToken(SyntaxKind.StepKeyword, stepKeyword) Then

                stepValue = ParseExpressionCore()

                If stepValue.ContainsDiagnostics Then
                    stepValue = ResyncAt(stepValue)
                End If

                optionalStepClause = SyntaxFactory.ForStepClause(stepKeyword, stepValue)

            End If

            Dim statement = SyntaxFactory.ForStatement(forKeyword, controlVariable, equalsToken, fromValue, toKeyword, toValue, optionalStepClause)

            Return statement
        End Function

        Private Function ParseNextStatement() As NextStatementSyntax
            ' Assume CurrentToken is on Next
            Debug.Assert(CurrentToken.Kind = SyntaxKind.NextKeyword, "ParseNextStatement called on wrong token")

            Dim nextKeyword As KeywordSyntax = DirectCast(CurrentToken, KeywordSyntax)

            GetNextToken()

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

                    Dim comma As PunctuationSyntax = Nothing
                    If Not TryGetTokenAndEatNewLine(SyntaxKind.CommaToken, comma) Then
                        Exit Do
                    End If

                    variables.AddSeparator(comma)

                    If enclosing IsNot Nothing Then
                        enclosing = enclosing.PrevBlock
                        Debug.Assert(enclosing IsNot Nothing)
                    End If
                Loop

                Dim statement = SyntaxFactory.NextStatement(nextKeyword, variables.ToList)

                _pool.Free(variables)

                Return statement
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
            Dim Type As TypeSyntax = Nothing
            Dim optionalAsClause As AsClauseSyntax = Nothing

            If CurrentToken.Kind = SyntaxKind.AsKeyword Then
                [As] = DirectCast(CurrentToken, KeywordSyntax)

                ' Parse the type
                GetNextToken()
                Type = ParseGeneralType()
                optionalAsClause = SyntaxFactory.SimpleAsClause([As], Nothing, Type)
            End If ' Else if "As" is not present, the error falls out as a "Syntax error" IN the caller

            Dim names = _pool.AllocateSeparated(Of ModifiedIdentifierSyntax)()
            names.Add(Declarator)

            Dim result = SyntaxFactory.VariableDeclarator(names.ToList, optionalAsClause, Nothing)

            _pool.Free(names)

            Return result
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

            Debug.Assert(CurrentToken.Kind = SyntaxKind.GoToKeyword, "Alleged GOTO isn't.")

            Dim gotoKeyword As KeywordSyntax = DirectCast(CurrentToken, KeywordSyntax)

            GetNextToken()

            Dim labelName = ParseLabelReference()

            'TODO - Not calling ResyncAt may cause different errors from Dev10.

            'Dev10 calls ResyncAt  here if the label has an error.  However, this is
            ' unnecessary now because GetStatementTerminator will do the resync.

            Dim gotoStmt = SyntaxFactory.GoToStatement(gotoKeyword, GetLabelSyntaxForIdentifierOrLineNumber(labelName))

            Return gotoStmt
        End Function

        Private Function GetLabelSyntaxForIdentifierOrLineNumber(ByVal labelName As SyntaxToken) As LabelSyntax
            Debug.Assert(labelName.Kind = SyntaxKind.IntegerLiteralToken OrElse labelName.Kind = SyntaxKind.IdentifierToken)
            Return If(labelName.Kind = SyntaxKind.IntegerLiteralToken, SyntaxFactory.NumericLabel(labelName), SyntaxFactory.IdentifierLabel(labelName))
        End Function

        ' File: Parser.cpp
        ' Lines: 11440 - 11440
        ' .Parser::ParseOnErrorStatement( [ _Inout_ bool& ErrorInConstruct ] )

        Private Function ParseOnErrorStatement() As StatementSyntax

            Debug.Assert(CurrentToken.Kind = SyntaxKind.OnKeyword, "ON statement must start with ON.")

            Dim onKeyword As KeywordSyntax = DirectCast(CurrentToken, KeywordSyntax)

            GetNextToken()
            Dim errorKeyword As KeywordSyntax

            If CurrentToken.Kind = SyntaxKind.ErrorKeyword Then
                errorKeyword = DirectCast(CurrentToken, KeywordSyntax)
                GetNextToken()

            Else
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

            Debug.Assert(CurrentToken.Kind = SyntaxKind.ResumeKeyword, "ParseOnErrorResumeNext called on wrong token.")

            Dim resumeKeyword As KeywordSyntax = DirectCast(CurrentToken, KeywordSyntax)
            Dim nextKeyword As KeywordSyntax = Nothing

            GetNextToken()

            VerifyExpectedToken(SyntaxKind.NextKeyword, nextKeyword)

            Dim statement = SyntaxFactory.OnErrorResumeNextStatement(onKeyword, errorKeyword, resumeKeyword, nextKeyword)

            Return statement
        End Function

        Private Function ParseOnErrorGoto(onKeyword As KeywordSyntax, errorKeyword As KeywordSyntax) As OnErrorGoToStatementSyntax
            Debug.Assert(CurrentToken.Kind = SyntaxKind.GoToKeyword, "ParseOnErrorGoto called on wrong token.")

            Dim gotoKeyword = DirectCast(CurrentToken, KeywordSyntax)

            Dim optionalMinusToken As PunctuationSyntax = Nothing
            Dim label As LabelSyntax
            Dim kind As SyntaxKind

            Dim nextToken As SyntaxToken = PeekToken(1)

            If nextToken.Kind = SyntaxKind.IntegerLiteralToken AndAlso
                nextToken.ValueText = "0" Then

                kind = SyntaxKind.OnErrorGoToZeroStatement
                label = SyntaxFactory.NumericLabel(nextToken)

                GetNextToken()
                GetNextToken()

            ElseIf nextToken.Kind = SyntaxKind.MinusToken AndAlso
                PeekToken(2).Kind = SyntaxKind.IntegerLiteralToken AndAlso
                PeekToken(2).ValueText = "1" Then

                kind = SyntaxKind.OnErrorGoToMinusOneStatement
                optionalMinusToken = DirectCast(nextToken, PunctuationSyntax)
                label = SyntaxFactory.NumericLabel(PeekToken(2))

                GetNextToken()
                GetNextToken()
                GetNextToken()

            Else
                GetNextToken()
                kind = SyntaxKind.OnErrorGoToLabelStatement
                label = GetLabelSyntaxForIdentifierOrLineNumber(ParseLabelReference())
            End If

            Dim statement = SyntaxFactory.OnErrorGoToStatement(kind, onKeyword, errorKeyword, gotoKeyword, optionalMinusToken, label)

            Return statement
        End Function

        ' File: Parser.cpp
        ' Lines: 11551 - 11551
        ' .Parser::ParseResumeStatement( [ _Inout_ bool& ErrorInConstruct ] )

        Private Function ParseResumeStatement() As ResumeStatementSyntax
            Debug.Assert(CurrentToken.Kind = SyntaxKind.ResumeKeyword, "ParseResumeStatement called on wrong token.")

            Dim resumeKeyword As KeywordSyntax = DirectCast(CurrentToken, KeywordSyntax)
            Dim optionalLabel As SyntaxToken = Nothing
            Dim statement As ResumeStatementSyntax

            GetNextToken()

            If Not IsValidStatementTerminator(CurrentToken) Then

                If CurrentToken.Kind = SyntaxKind.NextKeyword Then

                    Dim nextKeyword = DirectCast(CurrentToken, KeywordSyntax)
                    GetNextToken()

                    statement = SyntaxFactory.ResumeNextStatement(resumeKeyword, SyntaxFactory.NextLabel(nextKeyword))

                Else
                    optionalLabel = ParseLabelReference()

                    statement = SyntaxFactory.ResumeLabelStatement(resumeKeyword, GetLabelSyntaxForIdentifierOrLineNumber(optionalLabel))
                End If

            Else
                statement = SyntaxFactory.ResumeStatement(resumeKeyword, Nothing)
            End If

            Return statement

        End Function

        Private Function ParseAssignmentOrInvocationStatement() As StatementSyntax
            Dim target As ExpressionSyntax = ParseTerm()

            If target.ContainsDiagnostics Then
                target = ResyncAt(target, SyntaxKind.EqualsToken)
            End If

            ' Could be a function call or it could be an assignment

            If SyntaxFacts.IsAssignmentStatementOperatorToken(CurrentToken.Kind) Then
                ' Consume the assignment operator
                Dim operatorToken As PunctuationSyntax = DirectCast(CurrentToken, PunctuationSyntax)
                GetNextToken()

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

                    Dim unexpected As VisualBasicSyntaxNode = Nothing
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

        Private Function MakeAssignmentStatement(left As ExpressionSyntax, operatorToken As PunctuationSyntax, right As ExpressionSyntax) As AssignmentStatementSyntax
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
            Debug.Assert(CurrentToken.Kind = SyntaxKind.CallKeyword, "ParseCallStatement called on wrong token.")

            Dim callKeyword As KeywordSyntax = DirectCast(CurrentToken, KeywordSyntax)
            GetNextToken()

            Dim invocation As ExpressionSyntax = MakeCallStatementExpression(ParseVariable())

            If invocation.ContainsDiagnostics Then
                invocation = ResyncAt(invocation)
            End If

            Dim statement = SyntaxFactory.CallStatement(callKeyword, invocation)

            Return statement

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
            Debug.Assert(CurrentToken.Kind = SyntaxKind.RaiseEventKeyword, "RaiseEvent statement must start with RaiseEvent.")

            Dim raiseEventKeyword As KeywordSyntax = DirectCast(CurrentToken, KeywordSyntax)

            GetNextToken()

            Dim ident = ParseIdentifierNameAllowingKeyword()

            If ident.ContainsDiagnostics Then
                ident = ident.AddTrailingSyntax(ResyncAt())
            End If

            Dim optionalArgumentList As ArgumentListSyntax = Nothing
            If CurrentToken.Kind = SyntaxKind.OpenParenToken Then
                optionalArgumentList = ParseParenthesizedArguments()
            End If

            Dim statement = SyntaxFactory.RaiseEventStatement(raiseEventKeyword, ident, optionalArgumentList)

            Return statement
        End Function

        ' File: Parser.cpp
        ' Lines: 11690 - 11690
        ' .Parser::ParseRedimStatement( [ _Inout_ bool& ErrorInConstruct ] )

        Private Function ParseRedimStatement() As StatementSyntax '[ReDim]

            Debug.Assert(CurrentToken.Kind = SyntaxKind.ReDimKeyword, "ParseRedimStatement must start with Redim.")
            Dim reDimKeyword As KeywordSyntax = DirectCast(CurrentToken, KeywordSyntax)
            GetNextToken()

            Dim possibleKeyword As KeywordSyntax = Nothing
            Dim optionalPreserveKeyword As KeywordSyntax = Nothing

            If CurrentToken.Kind = SyntaxKind.IdentifierToken AndAlso
                TryIdentifierAsContextualKeyword(CurrentToken, possibleKeyword) AndAlso possibleKeyword.Kind = SyntaxKind.PreserveKeyword Then
                optionalPreserveKeyword = possibleKeyword
                GetNextToken()
            End If

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
                    clause = SyntaxFactory.RedimClause(possibleInvocation, SyntaxFactory.ArgumentList(InternalSyntaxFactory.MissingPunctuation(SyntaxKind.OpenParenToken),
                                                                           Nothing,
                                                                           InternalSyntaxFactory.MissingPunctuation(SyntaxKind.CloseParenToken)))
                End If

                clauses.Add(clause)

                Dim comma As PunctuationSyntax = Nothing
                If Not TryGetTokenAndEatNewLine(SyntaxKind.CommaToken, comma) Then
                    Exit Do
                End If

                clauses.AddSeparator(comma)

            Loop

            Dim statement = If(optionalPreserveKeyword Is Nothing,
                               SyntaxFactory.ReDimStatement(reDimKeyword, optionalPreserveKeyword, clauses.ToList),
                               SyntaxFactory.ReDimPreserveStatement(reDimKeyword, optionalPreserveKeyword, clauses.ToList)
                            )

            _pool.Free(clauses)

            If CurrentToken.Kind = SyntaxKind.AsKeyword Then
                statement = statement.AddTrailingSyntax(CurrentToken, ERRID.ERR_ObsoleteRedimAs)
                GetNextToken()
            End If

            Return statement
        End Function

        ' File: Parser.cpp
        ' Lines: 11755 - 11755
        ' .Parser::ParseHandlerStatement( [ _Inout_ bool& ErrorInConstruct ] )

        Private Function ParseHandlerStatement() As AddRemoveHandlerStatementSyntax

            Debug.Assert(CurrentToken.Kind = SyntaxKind.AddHandlerKeyword OrElse CurrentToken.Kind = SyntaxKind.RemoveHandlerKeyword, "Handler statement parsing confused.")

            Dim keyword As KeywordSyntax = DirectCast(CurrentToken, KeywordSyntax)
            Dim kind = If(keyword.Kind = SyntaxKind.AddHandlerKeyword, SyntaxKind.AddHandlerStatement, SyntaxKind.RemoveHandlerStatement)
            GetNextToken()

            Dim eventExpression = ParseExpressionCore()

            If eventExpression.ContainsDiagnostics Then
                eventExpression = ResyncAt(eventExpression, SyntaxKind.CommaToken)
            End If

            Dim commaToken As PunctuationSyntax = Nothing
            TryGetTokenAndEatNewLine(SyntaxKind.CommaToken, commaToken, createIfMissing:=True)

            Dim DelegateExpression = ParseExpressionCore()

            If DelegateExpression.ContainsDiagnostics Then
                DelegateExpression = ResyncAt(DelegateExpression)
            End If

            Dim statement = SyntaxFactory.AddRemoveHandlerStatement(kind, keyword, eventExpression, commaToken, DelegateExpression)

            Return statement
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
            Debug.Assert(CurrentToken.Kind = SyntaxKind.WhileKeyword OrElse
                             CurrentToken.Kind = SyntaxKind.WithKeyword OrElse
                             CurrentToken.Kind = SyntaxKind.SyncLockKeyword, "ParseExpressionBlockStatement called on wrong token.")

            Dim keyword As KeywordSyntax = DirectCast(CurrentToken, KeywordSyntax)

            GetNextToken()

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
            Debug.Assert(CurrentToken.Kind = SyntaxKind.LetKeyword OrElse CurrentToken.Kind = SyntaxKind.SetKeyword, "Assignment statement parsing is lost.")
            ' Let and set are now illegal

            If CurrentToken.Kind = SyntaxKind.SetKeyword AndAlso
                (IsValidStatementTerminator(PeekToken(1)) OrElse PeekToken(1).Kind = SyntaxKind.OpenParenToken) AndAlso
                Context.IsWithin(SyntaxKind.SetAccessorBlock, SyntaxKind.GetAccessorBlock) Then
                ' If this is a set parse it as a property accessor and then mark it with an error
                ' so that the Set will terminate a Get context.
                Return ParsePropertyOrEventAccessor(SyntaxKind.SetAccessorStatement, Nothing, Nothing)
            Else

                Dim keyword As SyntaxToken = CurrentToken
                GetNextToken()

                ' Only consume the let.  Leave the rest to be processed as an assignment in case the user wrote let x = ...
                Return InternalSyntaxFactory.EmptyStatement.AddTrailingSyntax(keyword, ERRID.ERR_ObsoleteLetSetNotNeeded)
            End If

        End Function

        ' File: Parser.cpp
        ' Lines: 11909 - 11909
        ' .Parser::ParseTry( )
        Private Function ParseTry() As TryStatementSyntax
            Debug.Assert(CurrentToken.Kind = SyntaxKind.TryKeyword, "ParseTry called on wrong token")

            Dim tryKeyword As KeywordSyntax = DirectCast(CurrentToken, KeywordSyntax)
            GetNextToken()

            Dim statement = SyntaxFactory.TryStatement(tryKeyword)

            Return statement
        End Function

        ' File: Parser.cpp
        ' Lines: 11933 - 11933
        ' .Parser::ParseCatch( [ _Inout_ bool& ErrorInConstruct ] )

        Private Function ParseCatch() As CatchStatementSyntax
            Debug.Assert(CurrentToken.Kind = SyntaxKind.CatchKeyword, "ParseCatch called on wrong token.")

            Dim catchKeyword As KeywordSyntax = DirectCast(CurrentToken, KeywordSyntax)

            GetNextToken()

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
            If TryGetToken(SyntaxKind.WhenKeyword, whenKeyword) Then
                filter = ParseExpressionCore()

                optionalWhenClause = SyntaxFactory.CatchFilterClause(whenKeyword, filter)
            End If

            Dim statement = SyntaxFactory.CatchStatement(catchKeyword, optionalName, optionalAsClause, optionalWhenClause)

            Return statement
        End Function

        ' File: Parser.cpp
        ' Lines: 12022 - 12022
        ' .Parser::ParseFinally( [ _Inout_ bool& ErrorInConstruct ] )

        Private Function ParseFinally() As FinallyStatementSyntax
            Debug.Assert(CurrentToken.Kind = SyntaxKind.FinallyKeyword, "ParseFinally called on wrong token.")

            Dim finallyKeyword = DirectCast(CurrentToken, KeywordSyntax)
            GetNextToken()

            Dim statement = SyntaxFactory.FinallyStatement(finallyKeyword)

            Return statement
        End Function

        Private Function ParseThrowStatement() As ThrowStatementSyntax
            Debug.Assert(CurrentToken.Kind = SyntaxKind.ThrowKeyword, "ParseThrowStatement called on wrong token.")

            Dim throwKeyword = DirectCast(CurrentToken, KeywordSyntax)
            GetNextToken()

            Dim value = ParseExpressionCore(bailIfFirstTokenRejected:=True)
            If value IsNot Nothing Then
                If value.ContainsDiagnostics Then
                    value = ResyncAt(value)
                End If
            End If

            Dim statement = SyntaxFactory.ThrowStatement(throwKeyword, value)

            Return statement
        End Function

        Private Function ParseError() As ErrorStatementSyntax
            Debug.Assert(CurrentToken.Kind = SyntaxKind.ErrorKeyword)

            Dim errorKeyword = DirectCast(CurrentToken, KeywordSyntax)
            GetNextToken()

            Dim value = ParseExpressionCore()

            If value.ContainsDiagnostics Then
                value = ResyncAt(value)
            End If

            Dim statement = SyntaxFactory.ErrorStatement(errorKeyword, value)

            Return statement
        End Function

        ' Parse an Erase statement.

        ' File: Parser.cpp
        ' Lines: 12077 - 12077
        ' .Parser::ParseErase( [ _Inout_ bool& ErrorInConstruct ] )

        Private Function ParseErase() As EraseStatementSyntax
            Debug.Assert(CurrentToken.Kind = SyntaxKind.EraseKeyword, "Erase statement parsing lost.")

            Dim eraseKeyword As KeywordSyntax = DirectCast(CurrentToken, KeywordSyntax)

            GetNextToken()

            Dim variables = ParseVariableList()

            Dim statement = SyntaxFactory.EraseStatement(eraseKeyword, variables)

            Return statement

        End Function

        Private Function ShouldParseAsLabel() As Boolean
            Return IsFirstStatementOnLine(CurrentToken) AndAlso PeekToken(1).Kind = SyntaxKind.ColonToken
        End Function

        Private Function ParseLabel() As LabelStatementSyntax
            Debug.Assert(CurrentToken.Kind = SyntaxKind.IdentifierToken OrElse CurrentToken.Kind = SyntaxKind.IntegerLiteralToken)

            Dim labelName = ParseLabelReference()

            If labelName.Kind = SyntaxKind.IntegerLiteralToken AndAlso CurrentToken.Kind <> SyntaxKind.ColonToken Then
                Return ReportSyntaxError(SyntaxFactory.LabelStatement(labelName, InternalSyntaxFactory.MissingPunctuation(SyntaxKind.ColonToken)), ERRID.ERR_ObsoleteLineNumbersAreLabels)
            End If

            Dim trivia = New SyntaxList(Of VisualBasicSyntaxNode)(labelName.GetTrailingTrivia())
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
            Dim statement = SyntaxFactory.LabelStatement(labelName, colonToken)
            Return statement

        End Function

        ' Parse a Mid statement.

        ' File: Parser.cpp
        ' Lines: 12105 - 12105
        ' .Parser::ParseMid( [ _Inout_ bool& ErrorInConstruct ] )

        Private Function ParseMid() As AssignmentStatementSyntax

            Debug.Assert(CurrentToken.Kind = SyntaxKind.IdentifierToken AndAlso DirectCast(CurrentToken, IdentifierTokenSyntax).PossibleKeywordKind = SyntaxKind.MidKeyword)

            ' Mid[$]  OpenParenthesis  Expression  Comma  Expression  [  Comma  Expression  ]  CloseParenthesis  Equals  Expression  StatementTerminator

            'TODO - Does the Mid need to be modeled as a contextual keyword?  Right now it is kept as an identifier.
            Dim mid = DirectCast(CurrentToken, IdentifierTokenSyntax)
            GetNextToken()

            Debug.Assert(CurrentToken.Kind = SyntaxKind.OpenParenToken)
            Dim openParen As PunctuationSyntax = Nothing
            TryGetTokenAndEatNewLine(SyntaxKind.OpenParenToken, openParen)

            Dim argumentsBuilder As SeparatedSyntaxListBuilder(Of ArgumentSyntax) = _pool.AllocateSeparated(Of ArgumentSyntax)()
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

            Dim arguments As SeparatedSyntaxList(Of ArgumentSyntax) = argumentsBuilder.ToList
            _pool.Free(argumentsBuilder)

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
            Debug.Assert(CurrentToken.Kind = SyntaxKind.ReturnKeyword, "ParseReturnStatement called on wrong token.")

            Dim returnKeyword As KeywordSyntax = DirectCast(CurrentToken, KeywordSyntax)
            GetNextToken()

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
                ' be in a situation like "foo(Sub() Return, 15)" where next token was a valid thing
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

            Dim statement = SyntaxFactory.ReturnStatement(returnKeyword, operand)

            Return statement

        End Function

        'TODO - See if all of the simple statements that take only one keyword can be merged into one parsing function
        ' StopOrEnd
        ' TryStatement
        ' FinallyStatement
        Private Function ParseStopOrEndStatement() As StopOrEndStatementSyntax
            Debug.Assert(CurrentToken.Kind = SyntaxKind.StopKeyword OrElse CurrentToken.Kind = SyntaxKind.EndKeyword, "ParseStopOrEndStatement called on wrong token.")

            Dim stopOrEndKeyword As KeywordSyntax = DirectCast(CurrentToken, KeywordSyntax)
            GetNextToken()

            Dim stmtKind As SyntaxKind = If(stopOrEndKeyword.Kind = SyntaxKind.StopKeyword, SyntaxKind.StopStatement, SyntaxKind.EndStatement)

            Dim statement = SyntaxFactory.StopOrEndStatement(stmtKind, stopOrEndKeyword)

            Return statement
        End Function

        Private Function ParseUsingStatement() As UsingStatementSyntax
            Debug.Assert(CurrentToken.Kind = SyntaxKind.UsingKeyword, "ParseUsingStatement called on wrong token")

            Dim usingKeyword As KeywordSyntax = DirectCast(CurrentToken, KeywordSyntax)
            GetNextToken()

            Dim optionalExpression As ExpressionSyntax = Nothing
            Dim variables As SeparatedSyntaxList(Of VariableDeclaratorSyntax) = Nothing

            Dim nextToken As SyntaxToken = PeekToken(1)

            ' change from Dev10: allowing as new with multiple variable names, e.g. "Using a, b As New C1()"
            If nextToken.Kind = SyntaxKind.AsKeyword OrElse
               nextToken.Kind = SyntaxKind.EqualsToken OrElse
               nextToken.Kind = SyntaxKind.CommaToken OrElse
               nextToken.Kind = SyntaxKind.QuestionToken Then

                variables = ParseVariableDeclaration(allowAsNewWith:=True)
            Else
                optionalExpression = ParseExpressionCore()
            End If

            'TODO - not resyncing here may cause errors to differ from Dev10.

            'No need to resync on error.  This will be handled by GetStatementTerminator

            Dim statement = SyntaxFactory.UsingStatement(usingKeyword, optionalExpression, variables)

            Return statement
        End Function


        Private Function ParseAwaitStatement() As ExpressionStatementSyntax

            Debug.Assert(CurrentToken.Kind = SyntaxKind.IdentifierToken AndAlso
                         DirectCast(CurrentToken, IdentifierTokenSyntax).ContextualKind = SyntaxKind.AwaitKeyword,
                         "ParseAwaitStatement called on wrong token.")

            Dim expression = ParseAwaitExpression()

            Debug.Assert(expression.Kind = SyntaxKind.AwaitExpression)

            If expression.ContainsDiagnostics Then
                expression = ResyncAt(expression)
            End If

            Dim statement = SyntaxFactory.ExpressionStatement(expression)

            Return statement

        End Function

        Private Function ParseYieldStatement() As YieldStatementSyntax
            Debug.Assert(DirectCast(CurrentToken, IdentifierTokenSyntax).ContextualKind = SyntaxKind.YieldKeyword)

            Dim yieldKeyword As KeywordSyntax = Nothing

            TryIdentifierAsContextualKeyword(CurrentToken, yieldKeyword)

            Debug.Assert(yieldKeyword IsNot Nothing AndAlso yieldKeyword.Kind = SyntaxKind.YieldKeyword)

            yieldKeyword = CheckFeatureAvailability(Feature.Iterators, yieldKeyword)
            GetNextToken()

            Dim expression As ExpressionSyntax = ParseExpressionCore()
            Dim result = SyntaxFactory.YieldStatement(yieldKeyword, expression)

            Return result

        End Function

        Private Function ParsePrintStatement() As PrintStatementSyntax
            Dim questionToken = DirectCast(CurrentToken, PunctuationSyntax)
            GetNextToken()

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
