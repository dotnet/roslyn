' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Syntax.InternalSyntax
Imports InternalSyntaxFactory = Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax.SyntaxFactory

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax

    Partial Friend Class Parser
        ' File: Parser.cpp
        ' Lines: 14199 - 14199
        ' Initializer* .Parser::ParseSelectListInitializer( [ _Inout_ bool& ErrorInConstruct ] )

        Private Function ParseSelectListInitializer() As ExpressionRangeVariableSyntax

            Dim nameEqualsOpt As VariableNameEqualsSyntax = Nothing

            If ((CurrentToken.Kind = SyntaxKind.IdentifierToken OrElse CurrentToken.IsKeyword()) AndAlso
                PeekToken(1).Kind = SyntaxKind.EqualsToken OrElse
                (PeekToken(1).Kind = SyntaxKind.QuestionToken AndAlso PeekToken(2).Kind = SyntaxKind.EqualsToken)) Then

                Dim varName As ModifiedIdentifierSyntax = Nothing
                Dim Equals As PunctuationSyntax = Nothing

                ' // Parse form: <IdentifierOrKeyword> '=' <Expression>
                varName = ParseSimpleIdentifierAsModifiedIdentifier()

                ' NOTE: do not need to resync here. we should land on "="
                Debug.Assert(CurrentToken.Kind = SyntaxKind.EqualsToken)

                Equals = DirectCast(CurrentToken, PunctuationSyntax)
                GetNextToken() '// move off the '='
                TryEatNewLine() ' // line continuation allowed after  '=' 

                nameEqualsOpt = SyntaxFactory.VariableNameEquals(varName, Nothing, Equals)
            End If

            Dim expr As ExpressionSyntax = ParseExpressionCore()

            Return SyntaxFactory.ExpressionRangeVariable(
                nameEqualsOpt,
                expr)
        End Function

        ' File: Parser.cpp
        ' Lines: 14290 - 14290
        ' InitializerList* .Parser::ParseSelectList( [ _Inout_ bool& ErrorInConstruct ] )

        Private Function ParseSelectList() As CodeAnalysis.Syntax.InternalSyntax.SeparatedSyntaxList(Of ExpressionRangeVariableSyntax)

            Dim RangeVariables = Me._pool.AllocateSeparated(Of ExpressionRangeVariableSyntax)()

            Do
                Dim rangeVar = ParseSelectListInitializer()

                If rangeVar.ContainsDiagnostics Then
                    rangeVar = ResyncAt(rangeVar, SyntaxKind.CommaToken, SyntaxKind.WhereKeyword, SyntaxKind.GroupKeyword,
                                                 SyntaxKind.SelectKeyword, SyntaxKind.OrderKeyword, SyntaxKind.JoinKeyword,
                                                 SyntaxKind.FromKeyword, SyntaxKind.DistinctKeyword, SyntaxKind.AggregateKeyword,
                                                 SyntaxKind.IntoKeyword, SyntaxKind.SkipKeyword, SyntaxKind.TakeKeyword, SyntaxKind.LetKeyword)
                End If

                RangeVariables.Add(rangeVar)

                If CurrentToken.Kind = SyntaxKind.CommaToken Then
                    Dim comma = DirectCast(CurrentToken, PunctuationSyntax)
                    GetNextToken()
                    TryEatNewLine()
                    RangeVariables.AddSeparator(comma)
                Else
                    Exit Do
                End If
            Loop

            Dim result = RangeVariables.ToList
            Me._pool.Free(RangeVariables)

            Return result
        End Function

        ' File: Parser.cpp
        ' Lines: 14333 - 14333
        ' Expression* .Parser::ParseAggregationExpression( [ _Inout_ bool& ErrorInConstruct ] )

        Private Function ParseAggregationExpression() As AggregationSyntax

            If CurrentToken.Kind = SyntaxKind.IdentifierToken Then
                Dim curIdent = DirectCast(CurrentToken, IdentifierTokenSyntax)
                If curIdent.PossibleKeywordKind = SyntaxKind.GroupKeyword Then
                    Debug.Assert(PeekToken(1).Kind = SyntaxKind.OpenParenToken)

                    curIdent = ReportSyntaxError(curIdent, ERRID.ERR_InvalidUseOfKeyword)
                    GetNextToken()
                    Return SyntaxFactory.FunctionAggregation(curIdent, Nothing, Nothing, Nothing)
                End If
            End If

            Dim aggName = ParseIdentifier()

            Dim aggregateFunc As FunctionAggregationSyntax = Nothing

            If Not aggName.ContainsDiagnostics AndAlso CurrentToken.Kind = SyntaxKind.OpenParenToken Then
                Dim lParen = DirectCast(CurrentToken, PunctuationSyntax)
                GetNextToken()  ' get off lparen

                TryEatNewLine()

                Dim arg As ExpressionSyntax = Nothing
                If CurrentToken.Kind <> SyntaxKind.CloseParenToken Then
                    arg = ParseExpressionCore()
                End If

                Dim rParen As PunctuationSyntax = Nothing
                If TryEatNewLineAndGetToken(SyntaxKind.CloseParenToken, rParen, createIfMissing:=True) Then
                    ' // check that expression doesn't continue
                    If arg IsNot Nothing Then
                        CheckForEndOfExpression(arg)
                    End If
                End If

                aggregateFunc = SyntaxFactory.FunctionAggregation(aggName, lParen, arg, rParen)
            Else
                aggregateFunc = SyntaxFactory.FunctionAggregation(aggName, Nothing, Nothing, Nothing)
                ' // check that expression doesn't continue
                CheckForEndOfExpression(aggregateFunc)
            End If

            Return aggregateFunc
        End Function

        ' // check that expression doesn't continue
        ' File: Parser.cpp
        ' Lines: 14420 - 14420

        ' bool .Parser::CheckForEndOfExpression( [ Token* Start ] [ bool& ErrorInConstruct ] )
        Private Function CheckForEndOfExpression(Of T As VisualBasicSyntaxNode)(ByRef syntax As T) As Boolean
            Debug.Assert(syntax IsNot Nothing)

            'TODO: this is very different from original implementation.
            ' originally we would try to parse the whole thing as an expression and see if we
            ' will not consume more than "syntax". It seems that "syntax" is always a term
            ' so the only way ParseExpressionCore could consume more is if there is a binary operator.
            ' Hopefully checking for binary operator is enough...
            If Not CurrentToken.IsBinaryOperator AndAlso
                CurrentToken.Kind <> SyntaxKind.DotToken Then
                Return True
            End If

            syntax = ReportSyntaxError(syntax, ERRID.ERR_ExpectedEndOfExpression)
            Return False
        End Function

        ' Several places the syntax trees allow a modified identifier, but we actually don't want to 
        ' allow a trailing ?. This function encapsulates that.
        Private Function ParseSimpleIdentifierAsModifiedIdentifier() As ModifiedIdentifierSyntax
            Dim varName As IdentifierTokenSyntax = ParseIdentifier()

            If CurrentToken.Kind = SyntaxKind.QuestionToken Then
                Dim unexpectedNullable As SyntaxToken = CurrentToken
                varName = varName.AddTrailingSyntax(ReportSyntaxError(unexpectedNullable, ERRID.ERR_NullableTypeInferenceNotSupported))
                GetNextToken()   ' get off the "?"
            End If

            Return SyntaxFactory.ModifiedIdentifier(varName, Nothing, Nothing, Nothing)
        End Function

        ' File: Parser.cpp
        ' Lines: 14447 - 14447
        ' Initializer* .Parser::ParseAggregateListInitializer( [ bool AllowGroupName ] [ _Inout_ bool& ErrorInConstruct ] )

        Private Function ParseAggregateListInitializer(AllowGroupName As Boolean) As AggregationRangeVariableSyntax

            Dim varName As ModifiedIdentifierSyntax = Nothing
            Dim Equals As PunctuationSyntax = Nothing
            If ((CurrentToken.Kind = SyntaxKind.IdentifierToken OrElse CurrentToken.IsKeyword()) AndAlso
                PeekToken(1).Kind = SyntaxKind.EqualsToken _
                 OrElse
                (PeekToken(1).Kind = SyntaxKind.QuestionToken AndAlso PeekToken(2).Kind = SyntaxKind.EqualsToken)) Then

                ' // Parse form: <IdentifierOrKeyword> '=' <Expression>
                varName = ParseSimpleIdentifierAsModifiedIdentifier()

                ' NOTE: do not need to resync here. we should land on "="
                Debug.Assert(CurrentToken.Kind = SyntaxKind.EqualsToken)

                Equals = DirectCast(CurrentToken, PunctuationSyntax)
                GetNextToken() '// move off the '='
                TryEatNewLine() ' // line continuation allowed after  '=' 
            End If

            Dim expr As AggregationSyntax = Nothing

            If CurrentToken.Kind = SyntaxKind.IdentifierToken OrElse CurrentToken.IsKeyword() Then
                Dim groupKw As KeywordSyntax = Nothing
                If TryTokenAsContextualKeyword(CurrentToken, SyntaxKind.GroupKeyword, groupKw) AndAlso
                                        PeekToken(1).Kind <> SyntaxKind.OpenParenToken Then

                    If Not AllowGroupName Then
                        groupKw = ReportSyntaxError(groupKw, ERRID.ERR_UnexpectedGroup)
                    End If

                    expr = SyntaxFactory.GroupAggregation(groupKw)

                    ' // check that expression doesn't continue
                    CheckForEndOfExpression(expr)
                    GetNextToken() ' // Move off 'Group
                Else
                    expr = ParseAggregationExpression() ' // this must be an Aggregation
                End If

            Else
                Dim missingIdent = InternalSyntaxFactory.MissingIdentifier()
                If AllowGroupName Then
                    missingIdent = ReportSyntaxError(missingIdent, ERRID.ERR_ExpectedIdentifierOrGroup)
                Else
                    missingIdent = ReportSyntaxError(missingIdent, ERRID.ERR_ExpectedIdentifier)
                End If
                expr = SyntaxFactory.FunctionAggregation(missingIdent, Nothing, Nothing, Nothing)
            End If

            Debug.Assert((varName Is Nothing) = (Equals Is Nothing))
            Dim variableNameEquals As VariableNameEqualsSyntax = Nothing
            If varName IsNot Nothing AndAlso Equals IsNot Nothing Then
                variableNameEquals = SyntaxFactory.VariableNameEquals(varName, Nothing, Equals)
            End If

            Return SyntaxFactory.AggregationRangeVariable(
                variableNameEquals,
                expr)
        End Function

        ' File: Parser.cpp
        ' Lines: 14615 - 14615
        ' InitializerList* .Parser::ParseAggregateList( [ bool AllowGroupName ] [ bool IsGroupJoinProjection ] [ _Inout_ bool& ErrorInConstruct ] )
        Private Function ParseAggregateList(
            AllowGroupName As Boolean,
            IsGroupJoinProjection As Boolean) As CodeAnalysis.Syntax.InternalSyntax.SeparatedSyntaxList(Of AggregationRangeVariableSyntax)

            Dim RangeVariables = Me._pool.AllocateSeparated(Of AggregationRangeVariableSyntax)()

            Do
                Dim rangeVar = ParseAggregateListInitializer(AllowGroupName)

                If rangeVar.ContainsDiagnostics Then
                    If IsGroupJoinProjection Then
                        rangeVar = ResyncAt(rangeVar, SyntaxKind.CommaToken, SyntaxKind.WhereKeyword, SyntaxKind.GroupKeyword,
                                 SyntaxKind.SelectKeyword, SyntaxKind.OrderKeyword, SyntaxKind.JoinKeyword,
                                 SyntaxKind.FromKeyword, SyntaxKind.DistinctKeyword, SyntaxKind.AggregateKeyword,
                                 SyntaxKind.IntoKeyword, SyntaxKind.OnKeyword, SyntaxKind.SkipKeyword,
                                 SyntaxKind.TakeKeyword, SyntaxKind.LetKeyword)
                    Else
                        rangeVar = ResyncAt(rangeVar, SyntaxKind.CommaToken, SyntaxKind.WhereKeyword, SyntaxKind.GroupKeyword,
                                 SyntaxKind.SelectKeyword, SyntaxKind.OrderKeyword, SyntaxKind.JoinKeyword,
                                 SyntaxKind.FromKeyword, SyntaxKind.DistinctKeyword, SyntaxKind.AggregateKeyword,
                                 SyntaxKind.IntoKeyword, SyntaxKind.SkipKeyword, SyntaxKind.TakeKeyword,
                                 SyntaxKind.LetKeyword)
                    End If
                End If

                RangeVariables.Add(rangeVar)

                If CurrentToken.Kind = SyntaxKind.CommaToken Then
                    Dim comma = DirectCast(CurrentToken, PunctuationSyntax)
                    GetNextToken()
                    TryEatNewLine()
                    RangeVariables.AddSeparator(comma)
                Else
                    Exit Do
                End If
            Loop

            Dim result = RangeVariables.ToList
            Me._pool.Free(RangeVariables)

            Return result
        End Function

        ' File: Parser.cpp
        ' Lines: 14666 - 14666
        ' FromList* .Parser::ParseFromList( [ bool AssignmentList ] [ _Inout_ bool& ErrorInConstruct ] )

        ' TODO: Merge with ParseFromControlVars. The two methods are almost identical.
        Private Function ParseLetList() As CodeAnalysis.Syntax.InternalSyntax.SeparatedSyntaxList(Of ExpressionRangeVariableSyntax)

            Dim RangeVariables = Me._pool.AllocateSeparated(Of ExpressionRangeVariableSyntax)()

            Do
                Dim varName As ModifiedIdentifierSyntax = ParseNullableModifiedIdentifier()

                If varName.ContainsDiagnostics Then
                    ' // If we see As or In before other query operators, then assume that
                    ' // we are still on the Control Variable Declaration.
                    ' // Otherwise, don't resync and allow the caller to
                    ' // decide how to recover.

                    Dim peek = PeekAheadFor(SyntaxKind.AsKeyword, SyntaxKind.InKeyword, SyntaxKind.CommaToken,
                        SyntaxKind.FromKeyword, SyntaxKind.WhereKeyword, SyntaxKind.GroupKeyword,
                        SyntaxKind.SelectKeyword, SyntaxKind.OrderKeyword, SyntaxKind.JoinKeyword,
                        SyntaxKind.DistinctKeyword, SyntaxKind.AggregateKeyword, SyntaxKind.IntoKeyword,
                        SyntaxKind.SkipKeyword, SyntaxKind.TakeKeyword, SyntaxKind.LetKeyword)

                    Select Case (peek)
                        Case SyntaxKind.AsKeyword,
                            SyntaxKind.InKeyword,
                            SyntaxKind.CommaToken

                            varName = varName.AddTrailingSyntax(ResyncAt({peek}))
                    End Select
                End If

                If CurrentToken.Kind = SyntaxKind.QuestionToken AndAlso
                    (PeekToken(1).Kind = SyntaxKind.InKeyword OrElse
                      PeekToken(1).Kind = SyntaxKind.EqualsToken) Then

                    Dim unexpectedNullable As SyntaxToken = CurrentToken
                    varName = varName.AddTrailingSyntax(ReportSyntaxError(unexpectedNullable, ERRID.ERR_NullableTypeInferenceNotSupported))
                    GetNextToken()   ' get off the "?"
                End If

                Dim AsClause As SimpleAsClauseSyntax = Nothing
                Dim TokenFollowingAsWasIn As Boolean = False

                ' // Parse the type if specified

                If CurrentToken.Kind = SyntaxKind.AsKeyword Then
                    Dim AsKw As KeywordSyntax = DirectCast(CurrentToken, KeywordSyntax)
                    Debug.Assert(AsKw IsNot Nothing)

                    ' // Parse the type
                    GetNextToken() ' // get off AS

                    If CurrentToken.Kind = SyntaxKind.InKeyword Then
                        TokenFollowingAsWasIn = True
                    End If

                    Dim Type As TypeSyntax = ParseGeneralType()
                    AsClause = SyntaxFactory.SimpleAsClause(AsKw, Nothing, Type)

                    ' // try to recover
                    If Type.ContainsDiagnostics Then
                        Dim peek = PeekAheadFor(SyntaxKind.InKeyword, SyntaxKind.CommaToken, SyntaxKind.EqualsToken,
                                                SyntaxKind.FromKeyword, SyntaxKind.WhereKeyword, SyntaxKind.GroupKeyword,
                                                SyntaxKind.SelectKeyword, SyntaxKind.OrderKeyword, SyntaxKind.JoinKeyword,
                                                SyntaxKind.DistinctKeyword, SyntaxKind.AggregateKeyword, SyntaxKind.IntoKeyword,
                                                SyntaxKind.SkipKeyword, SyntaxKind.TakeKeyword, SyntaxKind.LetKeyword)
                        Select Case (peek)
                            Case SyntaxKind.AsKeyword,
                                SyntaxKind.InKeyword,
                                SyntaxKind.CommaToken

                                AsClause = AsClause.AddTrailingSyntax(ResyncAt({peek}))
                        End Select
                    End If
                End If

                Dim Equals As PunctuationSyntax = Nothing
                Dim source As ExpressionSyntax = Nothing
                If Not TryGetToken(SyntaxKind.EqualsToken, Equals) Then
                    Equals = InternalSyntaxFactory.MissingPunctuation(SyntaxKind.EqualsToken)
                    Equals = ReportSyntaxError(Equals, ERRID.ERR_ExpectedAssignmentOperator)
                Else
                    If Not TokenFollowingAsWasIn Then
                        TryEatNewLine() ' // enable implicit LC after 'In' or '=' But not if somebody did from x as IN 
                    End If
                    source = ParseExpressionCore()
                End If

                ' // try to recover
                If source Is Nothing OrElse source.ContainsDiagnostics Then
                    ' Fix up source
                    source = If(source, InternalSyntaxFactory.MissingExpression)

                    Dim peek = PeekAheadFor(SyntaxKind.CommaToken,
                                            SyntaxKind.FromKeyword, SyntaxKind.WhereKeyword, SyntaxKind.GroupKeyword,
                                            SyntaxKind.SelectKeyword, SyntaxKind.OrderKeyword, SyntaxKind.JoinKeyword,
                                            SyntaxKind.DistinctKeyword, SyntaxKind.AggregateKeyword, SyntaxKind.IntoKeyword,
                                            SyntaxKind.SkipKeyword, SyntaxKind.TakeKeyword, SyntaxKind.LetKeyword)

                    If peek = SyntaxKind.CommaToken Then
                        source = source.AddTrailingSyntax(ResyncAt({peek}))
                    End If
                End If

                Dim rangeVar = SyntaxFactory.ExpressionRangeVariable(
                    SyntaxFactory.VariableNameEquals(varName, AsClause, Equals),
                    source)

                RangeVariables.Add(rangeVar)

                ' // check for list continuation
                Dim comma As PunctuationSyntax = Nothing
                If TryGetTokenAndEatNewLine(SyntaxKind.CommaToken, comma) Then
                    RangeVariables.AddSeparator(comma)
                Else
                    Exit Do
                End If
            Loop

            Dim result = RangeVariables.ToList
            Me._pool.Free(RangeVariables)

            Return result
        End Function

        Private Function ParseFromControlVars() As CodeAnalysis.Syntax.InternalSyntax.SeparatedSyntaxList(Of CollectionRangeVariableSyntax)
            Dim RangeVariables = Me._pool.AllocateSeparated(Of CollectionRangeVariableSyntax)()

            Do
                Dim varName As ModifiedIdentifierSyntax = ParseNullableModifiedIdentifier()

                If varName.ContainsDiagnostics Then
                    ' // If we see As or In before other query operators, then assume that
                    ' // we are still on the Control Variable Declaration.
                    ' // Otherwise, don't resync and allow the caller to
                    ' // decide how to recover.

                    Dim peek = PeekAheadFor(SyntaxKind.AsKeyword, SyntaxKind.InKeyword, SyntaxKind.CommaToken,
                                            SyntaxKind.FromKeyword, SyntaxKind.WhereKeyword, SyntaxKind.GroupKeyword,
                                            SyntaxKind.SelectKeyword, SyntaxKind.OrderKeyword, SyntaxKind.JoinKeyword,
                                            SyntaxKind.DistinctKeyword, SyntaxKind.AggregateKeyword, SyntaxKind.IntoKeyword,
                                            SyntaxKind.SkipKeyword, SyntaxKind.TakeKeyword, SyntaxKind.LetKeyword)

                    Select Case (peek)
                        Case SyntaxKind.AsKeyword,
                            SyntaxKind.InKeyword,
                            SyntaxKind.CommaToken

                            varName = varName.AddTrailingSyntax(ResyncAt({peek}))
                    End Select
                End If

                If CurrentToken.Kind = SyntaxKind.QuestionToken AndAlso
                    (PeekToken(1).Kind = SyntaxKind.InKeyword OrElse
                      PeekToken(1).Kind = SyntaxKind.EqualsToken) Then

                    Dim unexpectedNullable As SyntaxToken = CurrentToken
                    varName = varName.AddTrailingSyntax(ReportSyntaxError(unexpectedNullable, ERRID.ERR_NullableTypeInferenceNotSupported))
                    GetNextToken()   ' get off the "?"
                End If

                Dim AsClause As SimpleAsClauseSyntax = Nothing
                Dim TokenFollowingAsWasIn As Boolean = False

                ' // Parse the type if specified

                If CurrentToken.Kind = SyntaxKind.AsKeyword Then
                    Dim AsKw As KeywordSyntax = DirectCast(CurrentToken, KeywordSyntax)
                    Debug.Assert(AsKw IsNot Nothing)

                    ' // Parse the type
                    GetNextToken() ' // get off AS

                    If CurrentToken.Kind = SyntaxKind.InKeyword Then
                        TokenFollowingAsWasIn = True
                    End If

                    Dim Type As TypeSyntax = ParseGeneralType()
                    AsClause = SyntaxFactory.SimpleAsClause(AsKw, Nothing, Type)

                    ' // try to recover
                    If Type.ContainsDiagnostics Then
                        Dim peek = PeekAheadFor(SyntaxKind.InKeyword, SyntaxKind.CommaToken, SyntaxKind.EqualsToken,
                                                SyntaxKind.FromKeyword, SyntaxKind.WhereKeyword, SyntaxKind.GroupKeyword,
                                                SyntaxKind.SelectKeyword, SyntaxKind.OrderKeyword, SyntaxKind.JoinKeyword,
                                                SyntaxKind.DistinctKeyword, SyntaxKind.AggregateKeyword, SyntaxKind.IntoKeyword,
                                                SyntaxKind.SkipKeyword, SyntaxKind.TakeKeyword, SyntaxKind.LetKeyword)

                        Select Case (peek)
                            Case SyntaxKind.AsKeyword,
                                SyntaxKind.InKeyword,
                                SyntaxKind.CommaToken

                                AsClause = AsClause.AddTrailingSyntax(ResyncAt({peek}))
                        End Select
                    End If
                End If

                Dim [In] As KeywordSyntax = Nothing
                Dim source As ExpressionSyntax = Nothing
                If TryEatNewLineAndGetToken(SyntaxKind.InKeyword, [In], createIfMissing:=True) Then
                    If Not TokenFollowingAsWasIn Then
                        TryEatNewLine() ' // enable implicit LC after 'In' or '=' But not if somebody did from x as IN 
                    End If
                    source = ParseExpressionCore()
                End If

                ' // try to recover
                If source Is Nothing OrElse source.ContainsDiagnostics Then
                    ' Fix up source
                    source = If(source, InternalSyntaxFactory.MissingExpression)

                    ' resync
                    Dim peek = PeekAheadFor(SyntaxKind.CommaToken,
                                            SyntaxKind.FromKeyword, SyntaxKind.WhereKeyword, SyntaxKind.GroupKeyword,
                                            SyntaxKind.SelectKeyword, SyntaxKind.OrderKeyword, SyntaxKind.JoinKeyword,
                                            SyntaxKind.DistinctKeyword, SyntaxKind.AggregateKeyword, SyntaxKind.IntoKeyword,
                                            SyntaxKind.SkipKeyword, SyntaxKind.TakeKeyword, SyntaxKind.LetKeyword)

                    If peek = SyntaxKind.CommaToken Then
                        source = source.AddTrailingSyntax(ResyncAt({peek}))
                    End If
                End If

                Dim rangeVar = SyntaxFactory.CollectionRangeVariable(varName, AsClause, [In], source)
                RangeVariables.Add(rangeVar)

                ' // check for list continuation
                Dim comma As PunctuationSyntax = Nothing
                If TryGetTokenAndEatNewLine(SyntaxKind.CommaToken, comma) Then
                    RangeVariables.AddSeparator(comma)
                Else
                    Exit Do
                End If
            Loop

            Dim result = RangeVariables.ToList
            Me._pool.Free(RangeVariables)

            Return result
        End Function

        ' /*********************************************************************
        ' ;ParsePotentialQuery
        ' 
        ' The parser determined that we might be on a query expression because we were on 'From' or
        ' 'Aggregate'  We now see if we actually are on a query expression and parse it if we are.
        ' **********************************************************************/
        ' // the query expression if this is in fact a query we are on

        ' // [out] true = that we were on a query and that we handled it / false = not a query
        ' // [out] whether we encountered an error processing the query statement (assuming this actually is a query statement)

        ' Expression* .Parser::ParsePotentialQuery( [ _Inout_ ParseTree::LineContinuationState& continuationState ] [ _Out_ bool& ParsedQuery ] [ _Out_ bool& ErrorInConstruct ] )
        Private Function ParsePotentialQuery(contextualKeyword As KeywordSyntax) As ExpressionSyntax
            Debug.Assert(contextualKeyword IsNot Nothing)
            Debug.Assert(contextualKeyword.Kind = SyntaxKind.FromKeyword OrElse contextualKeyword.Kind = SyntaxKind.AggregateKeyword)

            Debug.Assert(CurrentToken.Text = contextualKeyword.Text)
            ' // Look ahead and see if it looks like a query, i.e.
            ' // {AGGREGATE | FROM } <id>[?] {In | As | = }

            Dim newLineAfterFrom As Boolean = False

            Dim curIndex = 1
            Dim current As SyntaxToken = PeekToken(curIndex)

            If current IsNot Nothing AndAlso current.IsEndOfLine() Then
                If Not NextLineStartsWithStatementTerminator(1) Then '// we don't allow two EOLs in a row here
                    curIndex += 1
                    current = PeekToken(curIndex)
                    newLineAfterFrom = True
                End If
            End If

            ' <id>
            If current Is Nothing OrElse (current.Kind <> SyntaxKind.IdentifierToken AndAlso Not current.IsKeyword) Then
                Return Nothing
            End If

            ' "FROM <id> <EOL>" on the same line is enough to consider this a query.  Only do the full check if
            ' there is a new line between the FROM and the <id>, if this is a keyword after the FROM or if the identifier is
            ' a contextual keyword.
            '
            ' These are query expressions:
            ' x = From y    (it's important for the IDE to have this classified as a query to offer correct highlighting
            '                and completion suggestions)
            '   or
            ' x = from y
            '     In ...
            ' This logic is needed to not classify the identifier "From" in the following JoinCondition as query:
            ' dim f = Join x On From Equals y
            ' 
            ' These are two assignments:
            ' x = From
            ' y =
            '   and
            ' x = from a With
            ' 
            ' See Bugs 1678 and 10020 for more context.
            '

            Dim identifierAsContextualKeyword As KeywordSyntax = Nothing
            If newLineAfterFrom OrElse
                current.IsKeyword OrElse
                (current.Kind = SyntaxKind.IdentifierToken AndAlso
                TryTokenAsContextualKeyword(current, identifierAsContextualKeyword)) Then

                ' Note: this block is used to reject queries. Everything that skips this if 
                ' will be classified as a query. 

                curIndex += 1
                current = PeekToken(curIndex)

                ' // Look ahead for 'IN' as it can start it's own line
                If current IsNot Nothing Then
                    If current.Kind = SyntaxKind.StatementTerminatorToken Then
                        current = PeekToken(curIndex + 1)

                        If current Is Nothing OrElse current.Kind <> SyntaxKind.InKeyword Then
                            Return Nothing
                        End If
                    ElseIf current.Kind = SyntaxKind.QuestionToken Then
                        ' // Skip '?'
                        curIndex += 1
                        current = PeekToken(curIndex)
                    End If
                End If

                If current Is Nothing Then
                    Return Nothing
                End If

                ' check for As, In or  = 
                ' note that = may not come after EoL
                If current.Kind <> SyntaxKind.InKeyword AndAlso
                    current.Kind <> SyntaxKind.AsKeyword AndAlso
                    (newLineAfterFrom OrElse current.Kind <> SyntaxKind.EqualsToken) Then

                    Return Nothing
                End If

            End If

            If contextualKeyword.Kind = SyntaxKind.FromKeyword Then
                GetNextToken() 'get off the From
                Return ParseFromQueryExpression(contextualKeyword)
            Else
                Debug.Assert(contextualKeyword.Kind = SyntaxKind.AggregateKeyword)
                GetNextToken() 'get off the Aggregate
                Return ParseAggregateQueryExpression(contextualKeyword)
            End If
        End Function

        Private Function ParseGroupByExpression(groupKw As KeywordSyntax) As GroupByClauseSyntax
            Debug.Assert(groupKw IsNot Nothing)

            Dim byKw As KeywordSyntax = Nothing
            Dim elements As CodeAnalysis.Syntax.InternalSyntax.SeparatedSyntaxList(Of ExpressionRangeVariableSyntax) = Nothing
            If Not TryEatNewLineAndGetContextualKeyword(SyntaxKind.ByKeyword, byKw, createIfMissing:=False) Then
                TryEatNewLine()
                ' // parse element selector
                elements = ParseSelectList()
            End If

            Dim keys As CodeAnalysis.Syntax.InternalSyntax.SeparatedSyntaxList(Of ExpressionRangeVariableSyntax) = Nothing
            If byKw IsNot Nothing OrElse TryEatNewLineAndGetContextualKeyword(SyntaxKind.ByKeyword, byKw, createIfMissing:=True) Then
                TryEatNewLine()
                ' // parse key selector
                keys = ParseSelectList()
            Else
                Dim rangeVariables = Me._pool.AllocateSeparated(Of ExpressionRangeVariableSyntax)()
                rangeVariables.Add(InternalSyntaxFactory.ExpressionRangeVariable(Nothing, InternalSyntaxFactory.MissingExpression()))
                keys = rangeVariables.ToList
                Me._pool.Free(rangeVariables)
            End If

            Dim intoKw As KeywordSyntax = Nothing
            Dim Aggregation As CodeAnalysis.Syntax.InternalSyntax.SeparatedSyntaxList(Of AggregationRangeVariableSyntax) = Nothing
            If TryEatNewLineAndGetContextualKeyword(SyntaxKind.IntoKeyword, intoKw, createIfMissing:=True) Then
                TryEatNewLine()
                ' // parse result selector
                Aggregation = ParseAggregateList(True, False)
            Else
                Aggregation = Me.MissingAggregationRangeVariables()
            End If

            Return SyntaxFactory.GroupByClause(groupKw, elements, byKw, keys, intoKw, Aggregation)
        End Function

        Private Function MissingAggregationRangeVariables() As CodeAnalysis.Syntax.InternalSyntax.SeparatedSyntaxList(Of AggregationRangeVariableSyntax)
            Dim rangeVariables = Me._pool.AllocateSeparated(Of AggregationRangeVariableSyntax)()
            rangeVariables.Add(InternalSyntaxFactory.AggregationRangeVariable(Nothing, SyntaxFactory.FunctionAggregation(InternalSyntaxFactory.MissingIdentifier(), Nothing, Nothing, Nothing)))
            Dim result As CodeAnalysis.Syntax.InternalSyntax.SeparatedSyntaxList(Of AggregationRangeVariableSyntax) = rangeVariables.ToList
            Me._pool.Free(rangeVariables)
            Return result
        End Function

        ' File: Parser.cpp
        ' Lines: 15016 - 15016
        ' GroupJoinExpression* .Parser::ParseGroupJoinExpression( [ _In_ Token* beginSourceToken ] [ _In_opt_ ParseTree::LinqExpression* Source ] [ _Inout_ bool& ErrorInConstruct ] )

        Private Function ParseInnerJoinOrGroupJoinExpression(groupKw As KeywordSyntax,
                                                  joinKw As KeywordSyntax) As JoinClauseSyntax

            Debug.Assert(joinKw IsNot Nothing)

            ' // Make sure control var on the left is always named
            ' TODO: semantics
            ' MakeSureLeftControlVarIsNamed(source)

            TryEatNewLine()
            Dim joinVariable = ParseJoinControlVar()

            Dim moreJoinsBuilder = _pool.Allocate(Of JoinClauseSyntax)()
            Do
                Dim nextJoin = ParseOptionalJoinOperator()
                If nextJoin Is Nothing Then
                    Exit Do
                End If
                moreJoinsBuilder.Add(nextJoin)
            Loop

            Dim onKw As KeywordSyntax = Nothing
            Dim Predicate As CodeAnalysis.Syntax.InternalSyntax.SeparatedSyntaxList(Of JoinConditionSyntax) = Nothing
            If TryEatNewLineAndGetToken(SyntaxKind.OnKeyword, onKw, createIfMissing:=True) Then
                TryEatNewLine()
                Predicate = ParseJoinPredicateExpression()
            Else
                Dim missingEq = SyntaxFactory.JoinCondition(InternalSyntaxFactory.MissingExpression,
                                        InternalSyntaxFactory.MissingKeyword(SyntaxKind.EqualsKeyword),
                                        InternalSyntaxFactory.MissingExpression)
                Predicate = New CodeAnalysis.Syntax.InternalSyntax.SeparatedSyntaxList(Of JoinConditionSyntax)(missingEq)
            End If

            Dim joinVarList = New CodeAnalysis.Syntax.InternalSyntax.SeparatedSyntaxList(Of CollectionRangeVariableSyntax)(joinVariable)
            Dim moreJoins = moreJoinsBuilder.ToList()
            _pool.Free(moreJoinsBuilder)

            If groupKw Is Nothing Then
                Return SyntaxFactory.SimpleJoinClause(joinKw, joinVarList, moreJoins, onKw, Predicate)
            Else
                Dim intoKw As KeywordSyntax = Nothing
                Dim Aggregation As CodeAnalysis.Syntax.InternalSyntax.SeparatedSyntaxList(Of AggregationRangeVariableSyntax) = Nothing
                If TryEatNewLineAndGetContextualKeyword(SyntaxKind.IntoKeyword, intoKw, createIfMissing:=True) Then
                    TryEatNewLine()

                    ' // parse result selector
                    Aggregation = ParseAggregateList(True, True)
                Else
                    Aggregation = Me.MissingAggregationRangeVariables()
                End If

                Return SyntaxFactory.GroupJoinClause(groupKw, joinKw, joinVarList, moreJoins, onKw, Predicate, intoKw, Aggregation)
            End If
        End Function

        ' File: Parser.cpp
        ' Lines: 15260 - 15260
        ' LinqExpression* .Parser::ParseJoinSourceExpression( [ _Inout_ bool& ErrorInConstruct ] )

        Private Function ParseOptionalJoinOperator() As JoinClauseSyntax
            Dim joinKw As KeywordSyntax = Nothing
            If TryEatNewLineAndGetContextualKeyword(SyntaxKind.JoinKeyword, joinKw) Then
                Return ParseInnerJoinOrGroupJoinExpression(Nothing, joinKw)
            End If

            Dim groupKw As KeywordSyntax = Nothing
            If TryEatNewLineAndGetContextualKeyword(SyntaxKind.GroupKeyword, groupKw) Then
                TryGetContextualKeyword(SyntaxKind.JoinKeyword, joinKw, createIfMissing:=True)
                Return ParseInnerJoinOrGroupJoinExpression(groupKw, joinKw)
            End If
            Return Nothing
        End Function

        ' File: Parser.cpp
        ' Lines: 15107 - 15107
        ' LinqSourceExpression* .Parser::ParseJoinControlVarExpression( [ _Inout_ bool& ErrorInConstruct ] )

        Private Function ParseJoinControlVar() As CollectionRangeVariableSyntax

            Dim varName As ModifiedIdentifierSyntax = ParseNullableModifiedIdentifier()
            If varName.ContainsDiagnostics Then
                ' // If we see As or In before other query operators, then assume that
                ' // we are still on the Control Variable Declaration.
                ' // Otherwise, don't resync and allow the caller to
                ' // decide how to recover.

                Dim peek = PeekAheadFor(SyntaxKind.AsKeyword, SyntaxKind.InKeyword, SyntaxKind.OnKeyword,
                                        SyntaxKind.FromKeyword, SyntaxKind.WhereKeyword, SyntaxKind.GroupKeyword,
                                        SyntaxKind.SelectKeyword, SyntaxKind.OrderKeyword, SyntaxKind.JoinKeyword,
                                        SyntaxKind.DistinctKeyword, SyntaxKind.AggregateKeyword, SyntaxKind.IntoKeyword,
                                        SyntaxKind.SkipKeyword, SyntaxKind.TakeKeyword, SyntaxKind.LetKeyword)

                Select Case (peek)
                    Case SyntaxKind.AsKeyword,
                        SyntaxKind.InKeyword,
                        SyntaxKind.GroupKeyword,
                        SyntaxKind.JoinKeyword,
                        SyntaxKind.OnKeyword

                        varName = varName.AddTrailingSyntax(ResyncAt({peek}))
                End Select
            End If

            If CurrentToken.Kind = SyntaxKind.QuestionToken AndAlso
                PeekToken(1).Kind = SyntaxKind.InKeyword Then

                Dim unexpectedNullable As SyntaxToken = CurrentToken
                varName = varName.AddTrailingSyntax(ReportSyntaxError(unexpectedNullable, ERRID.ERR_NullableTypeInferenceNotSupported))
                GetNextToken()   ' get off the "?"
            End If

            Dim AsClause As SimpleAsClauseSyntax = Nothing

            ' // Parse the type if specified
            If CurrentToken.Kind = SyntaxKind.AsKeyword Then
                Dim AsKw As KeywordSyntax = DirectCast(CurrentToken, KeywordSyntax)
                Debug.Assert(AsKw IsNot Nothing)

                ' // Parse the type
                GetNextToken() ' // get off AS

                Dim Type As TypeSyntax = ParseGeneralType()
                AsClause = SyntaxFactory.SimpleAsClause(AsKw, Nothing, Type)

                ' // try to recover
                If Type.ContainsDiagnostics Then
                    Dim peek = PeekAheadFor(SyntaxKind.InKeyword, SyntaxKind.OnKeyword, SyntaxKind.EqualsToken,
                                            SyntaxKind.FromKeyword, SyntaxKind.WhereKeyword, SyntaxKind.GroupKeyword,
                                            SyntaxKind.SelectKeyword, SyntaxKind.OrderKeyword, SyntaxKind.JoinKeyword,
                                            SyntaxKind.DistinctKeyword, SyntaxKind.AggregateKeyword, SyntaxKind.IntoKeyword,
                                            SyntaxKind.SkipKeyword, SyntaxKind.TakeKeyword, SyntaxKind.LetKeyword)

                    Select Case (peek)
                        Case SyntaxKind.EqualsToken,
                            SyntaxKind.InKeyword,
                            SyntaxKind.GroupKeyword,
                            SyntaxKind.JoinKeyword,
                            SyntaxKind.OnKeyword

                            AsClause = AsClause.AddTrailingSyntax(ResyncAt({peek}))
                    End Select
                End If
            End If

            Dim [In] As KeywordSyntax = Nothing
            Dim source As ExpressionSyntax = Nothing
            If TryEatNewLineAndGetToken(SyntaxKind.InKeyword, [In], createIfMissing:=True) Then
                TryEatNewLine() ' // dev10_500708 allow line continuation after 'IN' 
                source = ParseExpressionCore()
            End If

            ' // try to recover
            If source Is Nothing OrElse source.ContainsDiagnostics Then
                ' Fix up source
                source = If(source, InternalSyntaxFactory.MissingExpression)

                ' resync
                Dim peek = PeekAheadFor(SyntaxKind.CommaToken,
                                        SyntaxKind.FromKeyword, SyntaxKind.WhereKeyword, SyntaxKind.GroupKeyword,
                                        SyntaxKind.SelectKeyword, SyntaxKind.OrderKeyword, SyntaxKind.JoinKeyword,
                                        SyntaxKind.DistinctKeyword, SyntaxKind.AggregateKeyword, SyntaxKind.IntoKeyword,
                                        SyntaxKind.SkipKeyword, SyntaxKind.TakeKeyword, SyntaxKind.LetKeyword)

                If peek = SyntaxKind.CommaToken Then
                    source = source.AddTrailingSyntax(ResyncAt({peek}))
                End If
            End If

            Return SyntaxFactory.CollectionRangeVariable(varName, AsClause, [In], source)
        End Function

        ' File: Parser.cpp
        ' Lines: 15293 - 15293
        ' Expression* .Parser::ParseJoinPredicateExpression( [ _Inout_ bool& ErrorInConstruct ] )

        Private Function ParseJoinPredicateExpression() As CodeAnalysis.Syntax.InternalSyntax.SeparatedSyntaxList(Of JoinConditionSyntax)

            Dim Exprs = Me._pool.AllocateSeparated(Of JoinConditionSyntax)()
            Dim AndTk As KeywordSyntax = Nothing

            Do
                Dim element As JoinConditionSyntax = Nothing

                If CurrentToken.Kind <> SyntaxKind.StatementTerminatorToken Then
                    Dim Left = ParseExpressionCore(OperatorPrecedence.PrecedenceRelational)

                    ' // try to recover
                    If Left.ContainsDiagnostics Then
                        Left = ResyncAt(Left, SyntaxKind.EqualsToken, SyntaxKind.FromKeyword, SyntaxKind.WhereKeyword,
                                        SyntaxKind.GroupKeyword, SyntaxKind.SelectKeyword, SyntaxKind.OrderKeyword,
                                        SyntaxKind.JoinKeyword, SyntaxKind.DistinctKeyword, SyntaxKind.AggregateKeyword,
                                        SyntaxKind.IntoKeyword, SyntaxKind.OnKeyword, SyntaxKind.AndKeyword, SyntaxKind.AndAlsoKeyword,
                                        SyntaxKind.OrKeyword, SyntaxKind.OrElseKeyword, SyntaxKind.SkipKeyword, SyntaxKind.SkipKeyword,
                                        SyntaxKind.LetKeyword)
                    End If

                    Dim eqKw As KeywordSyntax = Nothing
                    Dim Right As ExpressionSyntax = Nothing
                    If TryGetContextualKeywordAndEatNewLine(SyntaxKind.EqualsKeyword, eqKw, createIfMissing:=True) Then
                        Right = ParseExpressionCore(OperatorPrecedence.PrecedenceRelational)
                    Else
                        Right = InternalSyntaxFactory.MissingExpression
                    End If

                    element = SyntaxFactory.JoinCondition(Left, eqKw, Right)
                Else
                    element = SyntaxFactory.JoinCondition(InternalSyntaxFactory.MissingExpression,
                                                             InternalSyntaxFactory.MissingKeyword(SyntaxKind.EqualsKeyword),
                                                             InternalSyntaxFactory.MissingExpression)

                    element = ReportSyntaxError(element, ERRID.ERR_ExpectedExpression)
                End If

                ' // try to recover
                If element.ContainsDiagnostics Then
                    element = ResyncAt(element, SyntaxKind.AndKeyword, SyntaxKind.FromKeyword, SyntaxKind.WhereKeyword,
                                       SyntaxKind.GroupKeyword, SyntaxKind.SelectKeyword, SyntaxKind.OrderKeyword, SyntaxKind.JoinKeyword,
                                       SyntaxKind.DistinctKeyword, SyntaxKind.AggregateKeyword, SyntaxKind.IntoKeyword, SyntaxKind.OnKeyword,
                                       SyntaxKind.AndAlsoKeyword, SyntaxKind.OrKeyword, SyntaxKind.OrElseKeyword, SyntaxKind.SkipKeyword,
                                       SyntaxKind.TakeKeyword, SyntaxKind.LetKeyword)
                End If

                If Exprs.Count > 0 Then
                    Exprs.AddSeparator(AndTk)
                End If
                Exprs.Add(element)

                If TryGetTokenAndEatNewLine(SyntaxKind.AndKeyword, AndTk) Then
                    Continue Do

                ElseIf CurrentToken.Kind = SyntaxKind.AndAlsoKeyword OrElse
                       CurrentToken.Kind = SyntaxKind.OrKeyword OrElse
                       CurrentToken.Kind = SyntaxKind.OrElseKeyword Then

                    Exprs(Exprs.Count - 1) = Exprs(Exprs.Count - 1).AddTrailingSyntax(CurrentToken, ERRID.ERR_ExpectedAnd)
                    GetNextToken() ' consume bad token
                End If
                Exit Do
            Loop

            Dim result = Exprs.ToList
            Me._pool.Free(Exprs)

            ' // try to recover
            If result.Node.ContainsDiagnostics Then
                Dim elements = result.Node
                elements = ResyncAt(elements, SyntaxKind.FromKeyword, SyntaxKind.WhereKeyword, SyntaxKind.GroupKeyword,
                                   SyntaxKind.SelectKeyword, SyntaxKind.OrderKeyword, SyntaxKind.JoinKeyword, SyntaxKind.DistinctKeyword,
                                   SyntaxKind.AggregateKeyword, SyntaxKind.IntoKeyword, SyntaxKind.OnKeyword, SyntaxKind.SkipKeyword,
                                   SyntaxKind.TakeKeyword, SyntaxKind.LetKeyword)

                result = New CodeAnalysis.Syntax.InternalSyntax.SeparatedSyntaxList(Of JoinConditionSyntax)(CType(New CodeAnalysis.Syntax.InternalSyntax.SyntaxList(Of JoinConditionSyntax)(CType(elements, GreenNode)), CodeAnalysis.Syntax.InternalSyntax.SyntaxList(Of GreenNode)))
            End If

            Return result
        End Function

        ' File: Parser.cpp
        ' Lines: 14861 - 14861
        ' LinqExpression* .Parser::ParseFromExpression( [ bool StartingQuery ] [ bool ImplicitFrom ] [ _Inout_ bool& ErrorInConstruct ] )

        Private Function ParseFromOperator(FromKw As KeywordSyntax) As FromClauseSyntax
            Debug.Assert(FromKw IsNot Nothing)

            TryEatNewLine()
            Return SyntaxFactory.FromClause(FromKw, ParseFromControlVars())
        End Function

        Private Function ParseLetOperator(LetKw As KeywordSyntax) As LetClauseSyntax
            Debug.Assert(LetKw IsNot Nothing)

            TryEatNewLine()
            Return SyntaxFactory.LetClause(LetKw, ParseLetList())
        End Function

        ' File: Parser.cpp
        ' Lines: 15389 - 15389
        'ParseTree::OrderByList *
        'Parser::ParseOrderByList
        '(
        '    _Inout_ bool &ErrorInConstruct
        '    _Inout_ ParseTree::LineContinuationState &OrderExprContinuationState, // [out] the continuation situation for the Order statement as relates to the Ascending/Descending keywords
        '    _Inout_ bool &ErrorInConstruct // [out] whether we ran into an error processing the OrderBy list.
        ')

        Private Function ParseOrderByList() As CodeAnalysis.Syntax.InternalSyntax.SeparatedSyntaxList(Of OrderingSyntax)

            Dim exprs = Me._pool.AllocateSeparated(Of OrderingSyntax)()

            Do
                Dim OrderExpression = ParseExpressionCore()

                ' // try to recover
                If OrderExpression.ContainsDiagnostics Then
                    OrderExpression = ResyncAt(OrderExpression, SyntaxKind.CommaToken, SyntaxKind.AscendingKeyword,
                                               SyntaxKind.DescendingKeyword, SyntaxKind.WhereKeyword, SyntaxKind.GroupKeyword,
                                               SyntaxKind.SelectKeyword, SyntaxKind.OrderKeyword, SyntaxKind.JoinKeyword,
                                               SyntaxKind.FromKeyword, SyntaxKind.DistinctKeyword, SyntaxKind.AggregateKeyword,
                                               SyntaxKind.IntoKeyword, SyntaxKind.SkipKeyword, SyntaxKind.TakeKeyword, SyntaxKind.LetKeyword)
                End If

                Dim element As OrderingSyntax = Nothing
                Dim directionKw As KeywordSyntax = Nothing
                If TryEatNewLineAndGetContextualKeyword(SyntaxKind.DescendingKeyword, directionKw) Then
                    element = SyntaxFactory.DescendingOrdering(OrderExpression, directionKw)
                Else
                    TryEatNewLineAndGetContextualKeyword(SyntaxKind.AscendingKeyword, directionKw)
                    element = SyntaxFactory.AscendingOrdering(OrderExpression, directionKw)
                End If

                exprs.Add(element)

                Dim comma As PunctuationSyntax = Nothing
                If TryEatNewLineAndGetToken(SyntaxKind.CommaToken, comma, createIfMissing:=False) Then
                    TryEatNewLine()
                    exprs.AddSeparator(comma)
                Else
                    Exit Do
                End If
            Loop

            Dim result = exprs.ToList
            Me._pool.Free(exprs)

            Return result
        End Function

        ' File: Parser.cpp
        ' Lines: 15508 - 15508
        ' LinqExpression* .Parser::ParseFromQueryExpression( [ bool ImplicitFrom ] [ _Inout_ bool& ErrorInConstruct ] )
        Private Sub ParseMoreQueryOperators(ByRef operators As SyntaxListBuilder(Of QueryClauseSyntax))
            Do
                ' // try to recover
                If operators.Count > 0 AndAlso operators(operators.Count - 1).ContainsDiagnostics Then

                    operators(operators.Count - 1) = ResyncAt(operators(operators.Count - 1),
                        SyntaxKind.FromKeyword,
                        SyntaxKind.WhereKeyword,
                        SyntaxKind.GroupKeyword,
                        SyntaxKind.SelectKeyword,
                        SyntaxKind.OrderKeyword,
                        SyntaxKind.JoinKeyword,
                        SyntaxKind.DistinctKeyword,
                        SyntaxKind.AggregateKeyword,
                        SyntaxKind.IntoKeyword,
                        SyntaxKind.SkipKeyword,
                        SyntaxKind.TakeKeyword,
                        SyntaxKind.LetKeyword)
                End If

                Dim clause = ParseNextQueryOperator()
                If clause Is Nothing Then
                    Return
                End If

                operators.Add(clause)
            Loop
        End Sub

        Private Function ParseNextQueryOperator() As QueryClauseSyntax
            Dim Start = CurrentToken

            ' //We allow implicit line continuations before query keywords when in a query context.
            If Start.Kind = SyntaxKind.StatementTerminatorToken Then

                If NextLineStartsWithStatementTerminator() OrElse
                   Not IsContinuableQueryOperator(PeekToken(1)) Then

                    Return Nothing
                End If

                ' we are going to use this EoL and next token since we know it is a valid operator.
                ' so it is ok to move to the next token and grab EoL as a trivia.
                TryEatNewLine()
                Start = CurrentToken
            End If

            ' // it must be Id or Keyword from here

            Select Case Start.Kind
                Case SyntaxKind.SelectKeyword
                    GetNextToken() ' get off Select
                    TryEatNewLine()

                    Dim Projection = ParseSelectList()
                    Return InternalSyntaxFactory.SelectClause(DirectCast(Start, KeywordSyntax), Projection)

                Case SyntaxKind.LetKeyword
                    GetNextToken() ' get off Let
                    Return ParseLetOperator(DirectCast(Start, KeywordSyntax))

                Case SyntaxKind.IdentifierToken
                    Dim kw As KeywordSyntax = Nothing
                    If Not TryTokenAsContextualKeyword(Start, kw) Then
                        Return Nothing
                    End If

                    Select Case kw.Kind

                        Case SyntaxKind.WhereKeyword
                            GetNextToken() ' get off where
                            TryEatNewLine()
                            Return InternalSyntaxFactory.WhereClause(kw, ParseExpressionCore())

                        Case SyntaxKind.SkipKeyword
                            GetNextToken() ' get off Skip

                            If CurrentToken.Kind = SyntaxKind.WhileKeyword Then
                                ' // Note: no line continuation if Skip is followed by While, i.e. Skip While is treated as a unit
                                Dim whileKw = DirectCast(CurrentToken, KeywordSyntax)
                                GetNextToken() ' get off While
                                TryEatNewLine()
                                Return InternalSyntaxFactory.SkipWhileClause(kw, whileKw, ParseExpressionCore())
                            Else
                                TryEatNewLineIfNotFollowedBy(SyntaxKind.WhileKeyword) ' // when Skip ends the line, allow a implicit line continuation
                                Return InternalSyntaxFactory.SkipClause(kw, ParseExpressionCore())
                            End If

                        Case SyntaxKind.TakeKeyword
                            GetNextToken() ' get off Take

                            If CurrentToken.Kind = SyntaxKind.WhileKeyword Then
                                ' // Note: no line continuation if Skip is followed by While, i.e. Skip While is treated as a unit
                                Dim whileKw = DirectCast(CurrentToken, KeywordSyntax)
                                GetNextToken() ' get off While
                                TryEatNewLine()
                                Return InternalSyntaxFactory.TakeWhileClause(kw, whileKw, ParseExpressionCore())
                            Else
                                TryEatNewLineIfNotFollowedBy(SyntaxKind.WhileKeyword) ' // when Skip ends the line, allow a implicit line continuation
                                Return InternalSyntaxFactory.TakeClause(kw, ParseExpressionCore())
                            End If

                        Case SyntaxKind.GroupKeyword
                            GetNextToken() 'get off Group

                            ' // See if this is a 'Group Join'
                            Dim joinKw As KeywordSyntax = Nothing
                            If TryGetContextualKeyword(SyntaxKind.JoinKeyword, joinKw) Then
                                Return ParseInnerJoinOrGroupJoinExpression(kw, joinKw)
                            Else
                                Return ParseGroupByExpression(kw)
                            End If

                        Case SyntaxKind.AggregateKeyword
                            GetNextToken() ' get off Aggregate
                            Return ParseAggregateClause(kw)

                        Case SyntaxKind.OrderKeyword
                            GetNextToken() ' get off Order

                            Dim byKw As KeywordSyntax = Nothing
                            TryGetContextualKeywordAndEatNewLine(SyntaxKind.ByKeyword, byKw, createIfMissing:=True)

                            TryEatNewLine()
                            Dim OrderByItems = ParseOrderByList()

                            Return InternalSyntaxFactory.OrderByClause(kw, byKw, OrderByItems)

                        Case SyntaxKind.DistinctKeyword
                            GetNextToken()   ' get off Distinct

                            If CurrentToken.Kind = SyntaxKind.StatementTerminatorToken Then
                                ' Eat the new line only if the distinct is followed by a token that can continue a term or an expression
                                Dim tokenAfterEOL = PeekToken(1)
                                Select Case tokenAfterEOL.Kind
                                    Case SyntaxKind.DotToken, SyntaxKind.ExclamationToken, SyntaxKind.QuestionToken, SyntaxKind.OpenParenToken
                                        TryEatNewLine()
                                    Case Else
                                        If tokenAfterEOL.IsBinaryOperator Then
                                            TryEatNewLine()
                                        End If
                                End Select
                            End If

                            Return InternalSyntaxFactory.DistinctClause(kw)

                        Case SyntaxKind.JoinKeyword
                            GetNextToken()   ' get off Join
                            Return ParseInnerJoinOrGroupJoinExpression(Nothing, kw)

                        Case SyntaxKind.FromKeyword
                            GetNextToken() ' get off From
                            Return ParseFromOperator(kw)

                    End Select
            End Select

            Return Nothing
        End Function

        Private Function ParseFromQueryExpression(fromKw As KeywordSyntax) As QueryExpressionSyntax
            Debug.Assert(fromKw IsNot Nothing)

            Dim operators = Me._pool.Allocate(Of QueryClauseSyntax)()

            operators.Add(ParseFromOperator(fromKw))
            ParseMoreQueryOperators(operators)

            Dim result = operators.ToList
            Me._pool.Free(operators)

            Return SyntaxFactory.QueryExpression(result)
        End Function

        Private Function ParseAggregateQueryExpression(AggregateKw As KeywordSyntax) As QueryExpressionSyntax
            Debug.Assert(AggregateKw IsNot Nothing)

            Dim operators = Me._pool.Allocate(Of QueryClauseSyntax)()

            operators.Add(ParseAggregateClause(AggregateKw))

            Dim result = operators.ToList
            Me._pool.Free(operators)

            Return SyntaxFactory.QueryExpression(result)
        End Function

        Private Function ParseAggregateClause(AggregateKw As KeywordSyntax) As AggregateClauseSyntax
            Debug.Assert(AggregateKw IsNot Nothing)

            TryEatNewLine()

            Dim controlVariables = ParseFromControlVars()

            Dim moreOperators = Me._pool.Allocate(Of QueryClauseSyntax)()
            ParseMoreQueryOperators(moreOperators)
            Dim operatorList = moreOperators.ToList
            Me._pool.Free(moreOperators)

            Dim intoKw As KeywordSyntax = Nothing
            Dim variables As CodeAnalysis.Syntax.InternalSyntax.SeparatedSyntaxList(Of AggregationRangeVariableSyntax) = Nothing
            If TryEatNewLineAndGetContextualKeyword(SyntaxKind.IntoKeyword, intoKw, createIfMissing:=True) Then
                ' //ILC:  I took the liberty of adding implicit line continuations after query keywords in addition to before them...
                TryEatNewLine()

                ' // parse result selector
                variables = ParseAggregateList(False, False)
            Else
                variables = Me.MissingAggregationRangeVariables()
            End If

            Return SyntaxFactory.AggregateClause(AggregateKw, controlVariables, operatorList, intoKw, variables)

        End Function

        ' bool .Parser::IsContinuableQueryOperator( [ Token* pToken ] )
        Private Function IsContinuableQueryOperator(pToken As SyntaxToken) As Boolean
            Debug.Assert(pToken IsNot Nothing)

            Debug.Assert(pToken.Text Is PeekToken(1).Text)

            Dim kind As SyntaxKind = Nothing
            If Not TryTokenAsKeyword(pToken, kind) Then
                Return False
            End If

            Dim isQueryKwd As Boolean = KeywordTable.IsQueryClause(kind)

            If isQueryKwd AndAlso kind = SyntaxKind.SelectKeyword Then
                ' //We do not want to allow an implicit line continuation before a "select" keyword if it is immediately
                ' //followed by the "case" keyword. This allows code like the following to parse correctly:
                ' //    dim a = from x in xs
                ' //    select case b
                ' //    end select

                Dim nextToken = PeekToken(2)
                If nextToken.Kind = SyntaxKind.CaseKeyword Then
                    isQueryKwd = False
                End If
            End If

            Return isQueryKwd
        End Function

        ' File: Parser.cpp
        ' Lines: 14918 - 14918
        ' .::MakeSureLeftControlVarIsNamed( [ _In_opt_ ParseTree::LinqExpression* Source ] ) 

        ' TODO: this should be done in semantics.

        'Private Shared Sub MakeSureLeftControlVarIsNamed( _
        '    ByVal Source As ParseTree.LinqExpression _
        ')
        '    While Source IsNot Nothing
        '        Select Case (Source.Opcode)

        '            Case ParseTree.Expression.Opcodes.InnerJoin, _
        '                  ParseTree.Expression.Opcodes.GroupJoin, _
        '                  ParseTree.Expression.Opcodes.CrossJoin, _
        '                  ParseTree.Expression.Opcodes.From, _
        '                  ParseTree.Expression.Opcodes.Let, _
        '                  ParseTree.Expression.Opcodes.GroupBy, _
        '                  ParseTree.Expression.Opcodes.Aggregate, _
        '                  ParseTree.Expression.Opcodes.LinqSource
        '                Return ' // these operators do not produce unnamed vars

        '            Case ParseTree.Expression.Opcodes.Where, _
        '                       ParseTree.Expression.Opcodes.SkipWhile, _
        '                       ParseTree.Expression.Opcodes.TakeWhile, _
        '                       ParseTree.Expression.Opcodes.Take, _
        '                       ParseTree.Expression.Opcodes.Skip, _
        '                       ParseTree.Expression.Opcodes.Distinct, _
        '                       ParseTree.Expression.Opcodes.OrderBy
        '                Source = Source.AsLinqOperator().Source ' // these operators do not declare control vars

        '            Case ParseTree.Expression.Opcodes.Select
        '                AssertIfTrue(Source.AsSelect().ForceNameInferenceForSingleElement)
        '                Source.AsSelect().ForceNameInferenceForSingleElement = True
        '                Return ' // done

        '            Case Else
        '                AssertIfFalse(False) ' // unknown Opcode
        '                Return
        '        End Select
        '    End While
        'End Sub

    End Class
End Namespace

