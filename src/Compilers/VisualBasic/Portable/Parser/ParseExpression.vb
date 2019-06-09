' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

'
'============ Methods for parsing portions of executable statements ==
'
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.Syntax.InternalSyntax
Imports InternalSyntaxFactory = Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax.SyntaxFactory

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax

    Friend Partial Class Parser

        Friend Function ParseExpression(
            Optional pendingPrecedence As OperatorPrecedence = OperatorPrecedence.PrecedenceNone,
            Optional bailIfFirstTokenRejected As Boolean = False
        ) As ExpressionSyntax

            Return ParseWithStackGuard(Of ExpressionSyntax)(
                Function() ParseExpressionCore(pendingPrecedence, bailIfFirstTokenRejected),
                Function() InternalSyntaxFactory.MissingExpression())

        End Function

        ' /*********************************************************************
        ' *
        ' * Function:
        ' *     Parser::ParseExpression
        ' *
        ' * Purpose:
        ' *
        ' *     Parses an expression.
        ' *
        ' **********************************************************************/

        ' [in] precedence of previous oper

        ' File: Parser.cpp
        ' Lines: 12312 - 12312
        ' Expression* .Parser::ParseExpression( [ OperatorPrecedence PendingPrecedence ] [ _Inout_ bool& ErrorInConstruct ] [ bool AllowArrayInitExpression ] )
        '        //EatLeadingNewLine: Indicates that the parser should
        '        //dig through a leading EOL token when it tries
        '        //to parse the expression.
        '
        '    bool EatLeadingNewLine,         - we no longer support it in ParseExpressionCore, please eat the new line yourself before calling
        '    bool BailIfFirstTokenRejected                 // bail (return NULL) if the first token isn't a valid expression-starter, rather than reporting an error or setting ErrorInConstruct
        Private Function ParseExpressionCore(
            Optional pendingPrecedence As OperatorPrecedence = OperatorPrecedence.PrecedenceNone,
            Optional bailIfFirstTokenRejected As Boolean = False
        ) As ExpressionSyntax

            Try
                _recursionDepth += 1
                StackGuard.EnsureSufficientExecutionStack(_recursionDepth)

                '// Note: this function will only ever return NULL if the flag "BailIfFirstTokenIsRejected" is set,
                '// and if the first token isn't a valid way to start an expression. In all other error scenarios
                '// it returns a "bad expression".

                Dim expression As ExpressionSyntax = Nothing
                Dim startToken As SyntaxToken = CurrentToken

                If _evaluatingConditionCompilationExpression AndAlso
               Not StartsValidConditionalCompilationExpr(startToken) Then

                    If bailIfFirstTokenRejected Then
                        Return Nothing
                    End If

                    expression = InternalSyntaxFactory.MissingExpression().AddTrailingSyntax(startToken, ERRID.ERR_BadCCExpression)

                    GetNextToken()

                    Return expression
                End If

                ' Check for leading unary operators
                Select Case (startToken.Kind)

                    Case SyntaxKind.MinusToken

                        ' "-" unary minus
                        GetNextToken()

                        Dim Operand As ExpressionSyntax = ParseExpressionCore(OperatorPrecedence.PrecedenceNegate)
                        expression = SyntaxFactory.UnaryMinusExpression(startToken, Operand)

                    Case SyntaxKind.NotKeyword
                        ' NOT expr
                        GetNextToken()
                        Dim Operand = ParseExpressionCore(OperatorPrecedence.PrecedenceNot)
                        expression = SyntaxFactory.NotExpression(startToken, Operand)

                    Case SyntaxKind.PlusToken
                        ' "+" unary plus
                        GetNextToken()

                        ' unary "+" has the same precedence as unary "-"

                        Dim Operand = ParseExpressionCore(OperatorPrecedence.PrecedenceNegate)
                        expression = SyntaxFactory.UnaryPlusExpression(startToken, Operand)

                    Case SyntaxKind.AddressOfKeyword
                        GetNextToken()

                        Dim Operand = ParseExpressionCore(OperatorPrecedence.PrecedenceNegate)
                        expression = SyntaxFactory.AddressOfExpression(startToken, Operand)

                    Case Else
                        expression = ParseTerm(bailIfFirstTokenRejected)

                        If expression Is Nothing Then
                            Return Nothing
                        End If
                End Select

                ' SHIQIC: I removed Expr->Opcode == ParseTree.Expression.Lambda, since we don't need to cast it to Lambda before test whether it is a statement lambda.
                '         Expr.AsLambda().IsStatementLambda is changed to (Expr.Kind = NodeKind.MultiLineFunctionLambda OrElse Expr.Kind = NodeKind.MultiLineSubLambda)

                If SyntaxKind.CollectionInitializer <> expression.Kind Then 'AndAlso

#If UNDONE Then
                Not ( (Expr.Kind = NodeKind.MultiLineFunctionLambda OrElse Expr.Kind = NodeKind.MultiLineSubLambda) AndAlso
                      Not DirectCast(Expr, Lambda).GetStatementLambdaBody().HasProperTermination ) Then ' Array initializer expressions NYI and we do not want to enter here if the expression is a multiline lambda without an end construct.
#End If
                    ' Parse operators that follow the term according to precedence.

                    Do

                        Dim precedence As OperatorPrecedence

                        If Not CurrentToken.IsBinaryOperator Then
                            Exit Do
                        End If

                        If _evaluatingConditionCompilationExpression AndAlso
                       Not IsValidOperatorForConditionalCompilationExpr(CurrentToken) Then

                            ' Should current token be consumed here?
                            expression = ReportSyntaxError(expression, ERRID.ERR_BadCCExpression)
                            Exit Do
                        End If

                        precedence = KeywordTable.TokenOpPrec(CurrentToken.Kind)

                        Debug.Assert(precedence <> OperatorPrecedence.PrecedenceNone, "should have a non-zero precedence for operators.")

                        ' Only continue parsing if precedence is high enough

                        If precedence <= pendingPrecedence Then
                            Exit Do
                        End If

                        Dim [operator] As SyntaxToken = ParseBinaryOperator()

                        'Dim Binary As ParseTree.BinaryExpression = New ParseTree.BinaryExpression
                        'Binary.Opcode = Opcode

                        'Binary.Left = Expr
                        Dim rightOperand As ExpressionSyntax = ParseExpressionCore(precedence)

                        expression = SyntaxFactory.BinaryExpression(GetBinaryOperatorHelper([operator]), expression, [operator], rightOperand)

                    Loop
                End If

                Return expression
            Finally
                _recursionDepth -= 1
            End Try
        End Function

        Private Function ParseTerm(
            Optional BailIfFirstTokenRejected As Boolean = False,
            Optional RedimOrNewParent As Boolean = False
        ) As ExpressionSyntax

            '// Note: this function will only ever return NULL if the flag "BailIfFirstTokenIsRejected" is set,
            '// and if the first token isn't a valid way to start an expression. In all other error scenarios
            '// it returns a "bad expression".
            Debug.Assert(Not _evaluatingConditionCompilationExpression OrElse
                StartsValidConditionalCompilationExpr(CurrentToken), "Conditional compilation expression parsing confused!!!")

            Dim term As ExpressionSyntax = Nothing
            Dim start As SyntaxToken = CurrentToken

            ' Resync points are delimiters.

            Select Case start.Kind
                Case SyntaxKind.IdentifierToken

                    Dim keyword As KeywordSyntax = Nothing

                    ' See if this is a beginning of a query
                    If TryIdentifierAsContextualKeyword(start, keyword) Then
                        If keyword.Kind = SyntaxKind.FromKeyword OrElse keyword.Kind = SyntaxKind.AggregateKeyword Then

                            term = ParsePotentialQuery(keyword)
                            If term IsNot Nothing Then
                                Exit Select
                            End If

                        ElseIf keyword.Kind = SyntaxKind.AsyncKeyword OrElse keyword.Kind = SyntaxKind.IteratorKeyword Then

                            Dim nextToken = PeekToken(1)

                            If nextToken.Kind = SyntaxKind.IdentifierToken Then
                                Dim possibleKeyword As KeywordSyntax = Nothing
                                If TryTokenAsContextualKeyword(nextToken, possibleKeyword) AndAlso
                                   possibleKeyword.Kind <> keyword.Kind AndAlso
                                   (possibleKeyword.Kind = SyntaxKind.AsyncKeyword OrElse possibleKeyword.Kind = SyntaxKind.IteratorKeyword) Then
                                    nextToken = PeekToken(2)
                                End If
                            End If

                            If nextToken.Kind = SyntaxKind.SubKeyword OrElse nextToken.Kind = SyntaxKind.FunctionKeyword Then
                                term = ParseLambda(parseModifiers:=True)

                                Exit Select
                            End If

                        ElseIf Context.IsWithinAsyncMethodOrLambda AndAlso keyword.Kind = SyntaxKind.AwaitKeyword Then
                            term = ParseAwaitExpression(keyword)

                            Exit Select
                        End If
                    End If

                    term = ParseSimpleNameExpressionAllowingKeywordAndTypeArguments()

                Case SyntaxKind.ExclamationToken
                    term = ParseQualifiedExpr(Nothing)

                Case SyntaxKind.DotToken
                    term = ParseQualifiedExpr(Nothing)

                Case SyntaxKind.GlobalKeyword
                    ' NB. GetNextToken has the side-effect of advancing CurrentToken.
                    term = SyntaxFactory.GlobalName(DirectCast(start, KeywordSyntax))
                    GetNextToken()
                    If CurrentToken.Kind <> SyntaxKind.DotToken Then
                        ' Dev10#519742: MyClass/MyBase/Global on their own are bad parse-tree nodes.
                        ' If we don't mark them bad now, then InterpretExpression will try to bind them later on
                        ' (which would be incorrect).
                        term = ReportSyntaxError(term, ERRID.ERR_ExpectedDotAfterGlobalNameSpace)
                    End If

                Case SyntaxKind.MyBaseKeyword
                    term = SyntaxFactory.MyBaseExpression(DirectCast(start, KeywordSyntax))
                    GetNextToken()
                    If CurrentToken.Kind <> SyntaxKind.DotToken Then
                        ' Dev10#519742: MyClass/MyBase/Global on their own are bad parse-tree nodes.
                        ' If we don't mark them bad now, then InterpretExpression will try to bind them later on
                        ' (which would be incorrect).
                        term = ReportSyntaxError(term, ERRID.ERR_ExpectedDotAfterMyBase)
                    End If

                Case SyntaxKind.MyClassKeyword
                    term = SyntaxFactory.MyClassExpression(DirectCast(start, KeywordSyntax))
                    GetNextToken()
                    If CurrentToken.Kind <> SyntaxKind.DotToken Then
                        ' Dev10#519742: MyClass/MyBase/Global on their own are bad parse-tree nodes.
                        ' If we don't mark them bad now, then InterpretExpression will try to bind them later on
                        ' (which would be incorrect).
                        term = ReportSyntaxError(term, ERRID.ERR_ExpectedDotAfterMyClass)
                    End If

                Case SyntaxKind.MeKeyword
                    term = SyntaxFactory.MeExpression(DirectCast(start, KeywordSyntax))
                    GetNextToken()

                Case SyntaxKind.OpenParenToken
                    term = ParseParenthesizedExpressionOrTupleLiteral()

                'XML
                Case SyntaxKind.LessThanToken,
                     SyntaxKind.LessThanQuestionToken,
                     SyntaxKind.BeginCDataToken,
                     SyntaxKind.LessThanExclamationMinusMinusToken,
                     SyntaxKind.LessThanSlashToken,
                     SyntaxKind.LessThanGreaterThanToken
                    ' Xml Literals
                    ' 1. single element "<" element ">"
                    ' 2. xml document  "<?xml ...?>
                    ' 3. xml pi
                    ' 4. xml cdata <![CDATA[
                    ' 5. xml comment <!--
                    ' 6. end element without begin </
                    ' 7. error case <>, element missing name

                    ' /* Dev10_427764 : Allow an implicit line continuation for XML after '(', e.g. goo(

                    Dim tokenHasFullWidthChars As Boolean = TokenContainsFullWidthChars(start)

                    If tokenHasFullWidthChars Then
                        If BailIfFirstTokenRejected Then
                            Return Nothing
                        End If

                        term = InternalSyntaxFactory.MissingExpression()
                        term = term.AddTrailingSyntax(CurrentToken, ERRID.ERR_FullWidthAsXmlDelimiter)
                        GetNextToken()

                        Return term
                    End If

                    term = ParseXmlExpression()

                Case SyntaxKind.IntegerLiteralToken
                    term = ParseIntLiteral()

                Case SyntaxKind.CharacterLiteralToken
                    term = ParseCharLiteral()

                Case SyntaxKind.DecimalLiteralToken
                    term = ParseDecLiteral()

                Case SyntaxKind.FloatingLiteralToken
                    term = ParseFltLiteral()

                Case SyntaxKind.DateLiteralToken
                    term = ParseDateLiteral()

                Case SyntaxKind.StringLiteralToken
                    term = ParseStringLiteral()

                Case SyntaxKind.TrueKeyword
                    term = SyntaxFactory.TrueLiteralExpression(CurrentToken)
                    GetNextToken()

                Case SyntaxKind.FalseKeyword
                    term = SyntaxFactory.FalseLiteralExpression(CurrentToken)
                    GetNextToken()

                Case SyntaxKind.NothingKeyword
                    term = SyntaxFactory.NothingLiteralExpression(CurrentToken)
                    GetNextToken()

                Case SyntaxKind.TypeOfKeyword
                    term = ParseTypeOf()

                Case SyntaxKind.GetTypeKeyword
                    term = ParseGetType()

                Case SyntaxKind.NameOfKeyword
                    term = ParseNameOf()

                Case SyntaxKind.GetXmlNamespaceKeyword
                    term = ParseGetXmlNamespace()

                Case SyntaxKind.NewKeyword
                    term = ParseNewExpression()

                Case SyntaxKind.CBoolKeyword,
                        SyntaxKind.CDateKeyword,
                        SyntaxKind.CDblKeyword,
                        SyntaxKind.CSByteKeyword,
                        SyntaxKind.CByteKeyword,
                        SyntaxKind.CCharKeyword,
                        SyntaxKind.CShortKeyword,
                        SyntaxKind.CUShortKeyword,
                        SyntaxKind.CIntKeyword,
                        SyntaxKind.CUIntKeyword,
                        SyntaxKind.CLngKeyword,
                        SyntaxKind.CULngKeyword,
                        SyntaxKind.CSngKeyword,
                        SyntaxKind.CStrKeyword,
                        SyntaxKind.CDecKeyword,
                        SyntaxKind.CObjKeyword
                    term = ParseCastExpression()

                Case SyntaxKind.CTypeKeyword, SyntaxKind.DirectCastKeyword, SyntaxKind.TryCastKeyword
                    term = ParseCast()

                Case SyntaxKind.IfKeyword
                    term = ParseIfExpression()

                Case SyntaxKind.ShortKeyword,
                        SyntaxKind.UShortKeyword,
                        SyntaxKind.IntegerKeyword,
                        SyntaxKind.UIntegerKeyword,
                        SyntaxKind.LongKeyword,
                        SyntaxKind.ULongKeyword,
                        SyntaxKind.DecimalKeyword,
                        SyntaxKind.SingleKeyword,
                        SyntaxKind.DoubleKeyword,
                        SyntaxKind.SByteKeyword,
                        SyntaxKind.ByteKeyword,
                        SyntaxKind.BooleanKeyword,
                        SyntaxKind.CharKeyword,
                        SyntaxKind.DateKeyword,
                        SyntaxKind.StringKeyword,
                        SyntaxKind.VariantKeyword,
                        SyntaxKind.ObjectKeyword

                    term = ParseTypeName()

                Case SyntaxKind.OpenBraceToken
                    ' A CollectionInitializer is an AnonymousArrayCreationExpression. AnonymousArrayCreationExpression type
                    ' is no longer part of the object model.
                    term = ParseCollectionInitializer()

                Case SyntaxKind.SubKeyword,
                     SyntaxKind.FunctionKeyword
                    term = ParseLambda(parseModifiers:=False)

                Case SyntaxKind.DollarSignDoubleQuoteToken

                    term = ParseInterpolatedStringExpression()

                Case Else

                    If start.Kind = SyntaxKind.QuestionToken AndAlso CanStartConsequenceExpression(Me.PeekToken(1).Kind, qualified:=False) Then
                        ' This looks like ?. or ?! 

                        Dim qToken = DirectCast(start, PunctuationSyntax)
                        qToken = CheckFeatureAvailability(Feature.NullPropagatingOperator, qToken)

                        GetNextToken()
                        term = SyntaxFactory.ConditionalAccessExpression(term, qToken, ParsePostFixExpression(RedimOrNewParent, term:=Nothing))
                    Else
                        If BailIfFirstTokenRejected Then
                            Return Nothing
                        End If

                        term = InternalSyntaxFactory.MissingExpression()
                        term = ReportSyntaxError(term, ERRID.ERR_ExpectedExpression)
                    End If
            End Select

            Debug.Assert(term IsNot Nothing)

            ' Complex expressions such as "." or "!" qualified, etc are not allowed cond comp expressions.
            '
            If Not _evaluatingConditionCompilationExpression Then
                ' Valid suffixes are ".", "!", and "(". Everything else is considered
                ' to end the term.

                term = ParsePostFixExpression(RedimOrNewParent, term)
            End If

            If CurrentToken IsNot Nothing AndAlso CurrentToken.Kind = SyntaxKind.QuestionToken Then
                term = term.AddTrailingSyntax(CurrentToken, ERRID.ERR_NullableCharNotSupported)
                GetNextToken()
            End If

            Return term
        End Function

        Private Function ParsePostFixExpression(RedimOrNewParent As Boolean, term As ExpressionSyntax) As ExpressionSyntax
            Do
                Dim [Next] As SyntaxToken = CurrentToken

                ' Dev10#670442 - you can't apply invocation parentheses directly to a single-line sub lambda,
                ' nor DotQualified/BangDictionaryLookup
                Dim isAfterSingleLineSub As Boolean = term IsNot Nothing AndAlso term.Kind = SyntaxKind.SingleLineSubLambdaExpression

                If [Next].Kind = SyntaxKind.DotToken Then
                    If isAfterSingleLineSub Then
                        term = ReportSyntaxError(term, ERRID.ERR_SubRequiresParenthesesDot)
                    End If

                    term = ParseQualifiedExpr(term)

                ElseIf [Next].Kind = SyntaxKind.ExclamationToken Then
                    If isAfterSingleLineSub Then
                        term = ReportSyntaxError(term, ERRID.ERR_SubRequiresParenthesesBang)
                    End If

                    term = ParseQualifiedExpr(term)

                ElseIf [Next].Kind = SyntaxKind.OpenParenToken Then
                    If isAfterSingleLineSub Then
                        term = ReportSyntaxError(term, ERRID.ERR_SubRequiresParenthesesLParen)
                    End If

                    term = ParseParenthesizedQualifier(term, RedimOrNewParent)

                ElseIf [Next].Kind = SyntaxKind.QuestionToken AndAlso CanStartConsequenceExpression(Me.PeekToken(1).Kind, qualified:=True) Then
                    ' This looks like ?. ?! or ?(

                    Dim qToken = DirectCast([Next], PunctuationSyntax)
                    qToken = CheckFeatureAvailability(Feature.NullPropagatingOperator, qToken)

                    GetNextToken()

                    If isAfterSingleLineSub Then
                        Select Case CurrentToken.Kind
                            Case SyntaxKind.DotToken
                                term = ReportSyntaxError(term, ERRID.ERR_SubRequiresParenthesesDot)
                            Case SyntaxKind.ExclamationToken
                                term = ReportSyntaxError(term, ERRID.ERR_SubRequiresParenthesesBang)
                            Case SyntaxKind.OpenParenToken
                                term = ReportSyntaxError(term, ERRID.ERR_SubRequiresParenthesesLParen)
                            Case Else
                                Throw ExceptionUtilities.Unreachable
                        End Select
                    End If

                    term = SyntaxFactory.ConditionalAccessExpression(term, qToken, ParsePostFixExpression(RedimOrNewParent, term:=Nothing))
                Else
                    ' We're done with the term.
                    Exit Do
                End If
            Loop

            Return term
        End Function

        Private Function CanStartConsequenceExpression(kind As SyntaxKind, qualified As Boolean) As Boolean
            Return kind = SyntaxKind.DotToken OrElse kind = SyntaxKind.ExclamationToken OrElse (qualified AndAlso kind = SyntaxKind.OpenParenToken)
        End Function

        Private Shared Function TokenContainsFullWidthChars(tk As SyntaxToken) As Boolean
            Dim spelling = tk.Text
            For Each ch In spelling
                If SyntaxFacts.IsFullWidth(ch) Then
                    Return True
                End If
            Next

            Return False
        End Function

        Private Shared Function GetArgumentAsExpression(arg As ArgumentSyntax) As ExpressionSyntax
            Select Case arg.Kind
                Case SyntaxKind.SimpleArgument

                    Dim simpleArgument = DirectCast(arg, SimpleArgumentSyntax)
                    Dim expression = simpleArgument.Expression

                    If simpleArgument.NameColonEquals IsNot Nothing Then
                        expression = expression.AddLeadingSyntax(SyntaxList.List(simpleArgument.NameColonEquals.Name, simpleArgument.NameColonEquals.ColonEqualsToken), ERRID.ERR_IllegalOperandInIIFName)
                    End If

                    Return expression

                Case Else
                    ' argument for IF expression cannot be omitted
                    Dim expr = InternalSyntaxFactory.MissingExpression
                    expr = expr.AddLeadingSyntax(arg, ERRID.ERR_ExpectedExpression)
                    Return expr
            End Select
        End Function

        Private Function ParseIfExpression() As ExpressionSyntax
            Debug.Assert(CurrentToken.Kind = SyntaxKind.IfKeyword)
            Dim IfStart = DirectCast(CurrentToken, KeywordSyntax)

            GetNextToken()  ' get off If

            If CurrentToken.Kind = SyntaxKind.OpenParenToken Then

                Dim Arguments = ParseParenthesizedArguments()
                Dim Args = Arguments.Arguments

                Select Case Args.Count
                    Case 0
                        Return SyntaxFactory.BinaryConditionalExpression(
                            IfStart,
                            Arguments.OpenParenToken,
                            InternalSyntaxFactory.MissingExpression.WithDiagnostics(ErrorFactory.ErrorInfo(ERRID.ERR_IllegalOperandInIIFCount)),
                            DirectCast(InternalSyntaxFactory.MissingToken(SyntaxKind.CommaToken), PunctuationSyntax),
                            InternalSyntaxFactory.MissingExpression,
                            Arguments.CloseParenToken)
                    Case 1
                        Return SyntaxFactory.BinaryConditionalExpression(
                            IfStart,
                            Arguments.OpenParenToken,
                            GetArgumentAsExpression(Args(0)),
                            DirectCast(InternalSyntaxFactory.MissingToken(SyntaxKind.CommaToken), PunctuationSyntax),
                            InternalSyntaxFactory.MissingExpression.WithDiagnostics(ErrorFactory.ErrorInfo(ERRID.ERR_IllegalOperandInIIFCount)),
                            Arguments.CloseParenToken)
                    Case 2
                        Return SyntaxFactory.BinaryConditionalExpression(
                            IfStart,
                            Arguments.OpenParenToken,
                            GetArgumentAsExpression(Args(0)),
                            DirectCast(Args.GetWithSeparators(1), PunctuationSyntax),
                            GetArgumentAsExpression(Args(1)),
                            Arguments.CloseParenToken)
                    Case 3
                        Return SyntaxFactory.TernaryConditionalExpression(
                            IfStart,
                            Arguments.OpenParenToken,
                            GetArgumentAsExpression(Args(0)),
                            DirectCast(Args.GetWithSeparators(1), PunctuationSyntax),
                            GetArgumentAsExpression(Args(1)),
                            DirectCast(Args.GetWithSeparators(3), PunctuationSyntax),
                            GetArgumentAsExpression(Args(2)),
                            Arguments.CloseParenToken)
                    Case Else
                        ' Wrong arg count
                        Debug.Assert(Args.Count > 3)

                        Dim withSeparators As CodeAnalysis.Syntax.InternalSyntax.SyntaxList(Of VisualBasicSyntaxNode) = Args.GetWithSeparators()
                        Const firstNotUsedIndex As Integer = 5

                        Debug.Assert(withSeparators.Count > firstNotUsedIndex)

                        Dim leading(withSeparators.Count - firstNotUsedIndex - 1) As VisualBasicSyntaxNode

                        For i As Integer = firstNotUsedIndex To withSeparators.Count - 1
                            leading(i - firstNotUsedIndex) = withSeparators(i)
                        Next

                        Return SyntaxFactory.TernaryConditionalExpression(
                            IfStart,
                            Arguments.OpenParenToken,
                            GetArgumentAsExpression(Args(0)),
                            DirectCast(withSeparators(1), PunctuationSyntax),
                            GetArgumentAsExpression(Args(1)),
                            DirectCast(withSeparators(3), PunctuationSyntax),
                            GetArgumentAsExpression(Args(2)),
                            Arguments.CloseParenToken.AddLeadingSyntax(SyntaxList.List(ArrayElement(Of GreenNode).MakeElementArray(leading)), ERRID.ERR_IllegalOperandInIIFCount))
                End Select

            Else
                ' ( was not there
                Return SyntaxFactory.BinaryConditionalExpression(
                    IfStart,
                    DirectCast(HandleUnexpectedToken(SyntaxKind.OpenParenToken), PunctuationSyntax),
                    InternalSyntaxFactory.MissingExpression,
                    DirectCast(InternalSyntaxFactory.MissingToken(SyntaxKind.CommaToken), PunctuationSyntax),
                    InternalSyntaxFactory.MissingExpression,
                    DirectCast(InternalSyntaxFactory.MissingToken(SyntaxKind.CloseParenToken), PunctuationSyntax))
            End If
        End Function

        ''' <summary>
        ''' Parse GetType, 
        ''' GetTypeExpression -> GetType OpenParenthesis GetTypeTypeName CloseParenthesis 
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function ParseGetType() As GetTypeExpressionSyntax
            Debug.Assert(CurrentToken.Kind = SyntaxKind.GetTypeKeyword, "should be at GetType.")

            Dim [getType] As KeywordSyntax = DirectCast(CurrentToken, KeywordSyntax)
            GetNextToken()

            Dim openParen As PunctuationSyntax = Nothing

            TryGetTokenAndEatNewLine(SyntaxKind.OpenParenToken, openParen, createIfMissing:=True)

            '// Dev10 #526220 Note, we are passing 'true' here to allow empty type arguments, 
            '// this may be incorrect for a Nullable type when '?' is used at the end of the type name. 
            '// In this case, we may generate some spurious errors and fail to report an expected error. 
            '// In order to deal with this issue, Parser::ParseTypeName() will walk the type tree and 
            '// report expected errors, removing false errors at the same time.

            Dim getTypeTypeName = ParseGeneralType(True) ' /* Allow type arguments to be empty i.e. allow C1(Of , ,)*/

            Dim closeParen As PunctuationSyntax = Nothing

            TryEatNewLineAndGetToken(SyntaxKind.CloseParenToken, closeParen, createIfMissing:=True)

            Return SyntaxFactory.GetTypeExpression([getType], openParen, getTypeTypeName, closeParen)
        End Function

        ''' <summary>
        ''' Parse NameOf, 
        ''' NameOfExpression -> NameOf OpenParenthesis Name CloseParenthesis 
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function ParseNameOf() As NameOfExpressionSyntax
            Debug.Assert(CurrentToken.Kind = SyntaxKind.NameOfKeyword, "should be at NameOf.")

            Dim [nameOf] As KeywordSyntax = DirectCast(CurrentToken, KeywordSyntax)
            [nameOf] = CheckFeatureAvailability(Feature.NameOfExpressions, [nameOf])

            GetNextToken()

            Dim openParen As PunctuationSyntax = Nothing
            TryGetTokenAndEatNewLine(SyntaxKind.OpenParenToken, openParen, createIfMissing:=True)

            Dim nameOfName = ValidateNameOfArgument(ParseExpressionCore(), isTopLevel:=True)

            Dim closeParen As PunctuationSyntax = Nothing
            TryEatNewLineAndGetToken(SyntaxKind.CloseParenToken, closeParen, createIfMissing:=True)

            Return SyntaxFactory.NameOfExpression([nameOf], openParen, nameOfName, closeParen)
        End Function

        Private Function ValidateNameOfArgument(argument As ExpressionSyntax, isTopLevel As Boolean) As ExpressionSyntax

            Select Case argument.Kind
                Case SyntaxKind.IdentifierName,
                     SyntaxKind.GenericName
                    Return argument

                Case SyntaxKind.MeExpression,
                     SyntaxKind.MyClassExpression,
                     SyntaxKind.MyBaseExpression,
                     SyntaxKind.PredefinedType,
                     SyntaxKind.NullableType,
                     SyntaxKind.GlobalName

                    If isTopLevel Then
                        Return ReportSyntaxError(argument, ERRID.ERR_ExpressionDoesntHaveName)
                    End If

                    Return argument

                Case SyntaxKind.SimpleMemberAccessExpression
                    Dim access = DirectCast(argument, MemberAccessExpressionSyntax)

                    If access.Expression IsNot Nothing Then
                        Dim expression = ValidateNameOfArgument(access.Expression, isTopLevel:=False)

                        If expression IsNot access.Expression Then
                            access = SyntaxFactory.SimpleMemberAccessExpression(expression, access.OperatorToken, access.Name)
                        End If
                    End If

                    Return access

                Case Else
                    Return ReportSyntaxError(argument, If(isTopLevel, ERRID.ERR_ExpressionDoesntHaveName, ERRID.ERR_InvalidNameOfSubExpression))
            End Select
        End Function

        ' File: Parser.cpp
        ' Lines: 15850 - 15850
        ' Expression* .Parser::ParseGetXmlNamespace( [ _Inout_ bool& ErrorInConstruct ] )

        Private Function ParseGetXmlNamespace() As ExpressionSyntax
            Debug.Assert(CurrentToken.Kind = SyntaxKind.GetXmlNamespaceKeyword, "should be at GetXmlNamespace.")

            Dim getXmlNamespaceKeyword = DirectCast(CurrentToken, KeywordSyntax)
            GetNextToken(ScannerState.VB)

            If CurrentToken.Kind = SyntaxKind.OpenParenToken Then
                ' Use Element state so the Rem style comments are disabled to support GetXmlNamespace(Rem)
                ResetCurrentToken(ScannerState.Element)
                Dim openParen As PunctuationSyntax = Nothing
                VerifyExpectedToken(SyntaxKind.OpenParenToken, openParen, ScannerState.Element)

                Dim name As XmlPrefixNameSyntax = Nothing
                If CurrentToken.Kind = SyntaxKind.XmlNameToken Then
                    name = SyntaxFactory.XmlPrefixName(DirectCast(CurrentToken, XmlNameTokenSyntax))
                    GetNextToken(ScannerState.Element)
                End If

                Dim closeParen As PunctuationSyntax = Nothing
                VerifyExpectedToken(SyntaxKind.CloseParenToken, closeParen)

                Dim result = SyntaxFactory.GetXmlNamespaceExpression(getXmlNamespaceKeyword, openParen, name, closeParen)
                result = AdjustTriviaForMissingTokens(result)
                result = TransitionFromXmlToVB(result)
                Return result

            Else
                Dim openParen = DirectCast(HandleUnexpectedToken(SyntaxKind.OpenParenToken), PunctuationSyntax)
                Dim closeParen = InternalSyntaxFactory.MissingPunctuation(SyntaxKind.CloseParenToken)
                Return SyntaxFactory.GetXmlNamespaceExpression(getXmlNamespaceKeyword, openParen, Nothing, closeParen)

            End If

        End Function

        ' File: Parser.cpp
        ' Lines: 15892 - 15892
        ' Expression* .Parser::ParseCastExpression( [ _Inout_ bool& ErrorInConstruct ] )

        Private Function ParseCastExpression() As ExpressionSyntax
            Debug.Assert(SyntaxFacts.IsPredefinedCastExpressionKeyword(CurrentToken.Kind), "ParseCastExpression called with the wrong token.")

            Dim Start = DirectCast(CurrentToken, KeywordSyntax)
            GetNextToken()

            Dim openParen As PunctuationSyntax = Nothing
            TryGetTokenAndEatNewLine(SyntaxKind.OpenParenToken, openParen, createIfMissing:=True)

            Dim Operand = ParseExpressionCore()

            Dim closeParen As PunctuationSyntax = Nothing
            TryEatNewLineAndGetToken(SyntaxKind.CloseParenToken, closeParen, createIfMissing:=True)

            Return SyntaxFactory.PredefinedCastExpression(Start, openParen, Operand, closeParen)
        End Function

        ' File: Parser.cpp
        ' Lines: 15971 - 15971
        ' Expression* .Parser::ParseNewExpression( [ _Inout_ bool& ErrorInConstruct ] )

        Private Function ParseNewExpression() As ExpressionSyntax
            Debug.Assert(CurrentToken.Kind = SyntaxKind.NewKeyword, "must be at a New expression.")

            Dim NewKeyword = DirectCast(CurrentToken, KeywordSyntax)
            GetNextToken() ' get off 'new'

            If CurrentToken.Kind = SyntaxKind.WithKeyword Then
                ' Anonymous type initializer
                ' Parsing from "With" in "New With {Initializer list}"
                Dim objInitializer = ParseObjectInitializerList(True) ' AnonymousTypeInitializer

                Return SyntaxFactory.AnonymousObjectCreationExpression(NewKeyword, Nothing, objInitializer)
            End If

            Dim Type = ParseTypeName()

            If Type.ContainsDiagnostics Then
                Type = ResyncAt(Type, SyntaxKind.OpenParenToken)
            End If

            Dim Arguments As ArgumentListSyntax = Nothing

            If CurrentToken.Kind = SyntaxKind.OpenParenToken Then
                ' This is an ambiguity in the grammar between
                '
                ' New <Type> ( <Arguments> )
                ' New <Type> <ArrayDeclaratorList> <AggregateInitializer>
                '
                ' Try it as the first form, and if this fails, try the second.
                ' (All valid instances of the second form have a beginning that is a valid
                ' instance of the first form, so no spurious errors should result.)

                ' Try parsing the arguments to determine whether they are constructor
                ' arguments or array declarators.

                Arguments = ParseParenthesizedArguments(True)

                ' TODO: having "0 To 10" style arguments would also point to array init
                ' for Dev10 compat we will let it slip through.

                Dim IsArrayCreationExpression = False
                Dim arrayModifiers As CodeAnalysis.Syntax.InternalSyntax.SyntaxList(Of ArrayRankSpecifierSyntax) = Nothing
                If Me.CurrentToken.Kind = Global.Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.OpenParenToken Then
                    ' Parse array modifiers

                    arrayModifiers = ParseArrayRankSpecifiers(ERRID.ERR_NoConstituentArraySizes)
                    IsArrayCreationExpression = True

                ElseIf CurrentToken.Kind = SyntaxKind.OpenBraceToken Then
                    ' Check whether arguments should be reinterpreted as modifiers

                    If TryReinterpretAsArraySpecifier(Arguments, arrayModifiers) Then
                        Arguments = Nothing
                    End If
                    IsArrayCreationExpression = True
                End If

                If IsArrayCreationExpression Then
                    ' Treat this as the form of New expression that allocates an array.

                    ' TODO: this should be optional
                    Dim initializer = ParseCollectionInitializer()

                    Return SyntaxFactory.ArrayCreationExpression(
                        NewKeyword,
                        Nothing,
                        Type,
                        Arguments,
                        arrayModifiers,
                        initializer)

                End If
            End If

            ' Here we are after parsing " New Blah(a1,a2)"

            Dim FromToken As KeywordSyntax = Nothing
            If TryTokenAsContextualKeyword(CurrentToken, SyntaxKind.FromKeyword, FromToken) Then

                ' From is considered as an initializer only if followed by "{" or EOL
                ' For example, the following cases are initializers
                '   dim x1 = new C() from {
                '   dim x2 = new () from
                '                   {
                '
                ' But this is not.
                '   From i In {} Select p = New Object From j In {} Select j
                '
                ' The second "From" starts a from query and is not an initializer.
                If PeekToken(1).Kind = SyntaxKind.OpenBraceToken OrElse PeekToken(1).Kind = SyntaxKind.StatementTerminatorToken Then

                    GetNextToken()

                    Dim objInit = ParseObjectCollectionInitializer(FromToken)

                    If (CurrentToken.Kind = SyntaxKind.WithKeyword) Then
                        ' TODO: this should actually go on the next token ( "With" )
                        ' perhaps we should capture With as an unexpected syntax?
                        objInit = ReportSyntaxError(objInit, ERRID.ERR_CantCombineInitializers)
                    End If

                    Return SyntaxFactory.ObjectCreationExpression(NewKeyword, Nothing, Type, Arguments, objInit)
                End If
            End If

            If CurrentToken.Kind = SyntaxKind.WithKeyword Then
                ' Parsing from "With" in "New Type(ParameterList) with {Initializer list}"
                Dim objInit = ParseObjectInitializerList()

                If (CurrentToken.Kind = SyntaxKind.WithKeyword) Then
                    ' TODO: this should actually go on the next token ( "With" )
                    ' perhaps we should capture With as an unexpected syntax?
                    objInit = ReportSyntaxError(objInit, ERRID.ERR_CantCombineInitializers)
                End If

                If TryTokenAsContextualKeyword(CurrentToken, SyntaxKind.FromKeyword, FromToken) AndAlso
                    PeekToken(1).Kind = SyntaxKind.OpenBraceToken Then

                    objInit = ReportSyntaxError(objInit, ERRID.ERR_CantCombineInitializers)
                End If

                Return SyntaxFactory.ObjectCreationExpression(NewKeyword, Nothing, Type, Arguments, objInit)
            End If

            Return SyntaxFactory.ObjectCreationExpression(NewKeyword, Nothing, Type, Arguments, Nothing)
        End Function

        ''' <summary>
        ''' Parse TypeOf ... Is ... or TypeOf ... IsNot ...
        ''' TypeOfExpression -> "TypeOf" Expression "Is|IsNot" LineTerminator? TypeName
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function ParseTypeOf() As TypeOfExpressionSyntax
            Debug.Assert(CurrentToken.Kind = SyntaxKind.TypeOfKeyword, "must be at TypeOf.")
            Dim [typeOf] As KeywordSyntax = DirectCast(CurrentToken, KeywordSyntax)

            ' Consume 'TypeOf'.
            GetNextToken()

            Dim exp As ExpressionSyntax = ParseExpressionCore(OperatorPrecedence.PrecedenceRelational) 'Dev10 uses ParseVariable

            If exp.ContainsDiagnostics Then
                exp = ResyncAt(exp, SyntaxKind.IsKeyword, SyntaxKind.IsNotKeyword)
            End If

            Dim operatorToken As KeywordSyntax = Nothing

            Dim current As SyntaxToken = CurrentToken

            If current.Kind = SyntaxKind.IsKeyword OrElse
               current.Kind = SyntaxKind.IsNotKeyword Then

                operatorToken = DirectCast(current, KeywordSyntax)

                If operatorToken.Kind = SyntaxKind.IsNotKeyword Then
                    operatorToken = CheckFeatureAvailability(Feature.TypeOfIsNot, operatorToken)
                End If

                GetNextToken()

                TryEatNewLine(ScannerState.VB)
            Else
                operatorToken = DirectCast(HandleUnexpectedToken(SyntaxKind.IsKeyword), KeywordSyntax)
            End If

            Dim typeName = ParseGeneralType()

            Dim kind As SyntaxKind = If(operatorToken.Kind = SyntaxKind.IsNotKeyword,
                                        SyntaxKind.TypeOfIsNotExpression,
                                        SyntaxKind.TypeOfIsExpression)

            Return SyntaxFactory.TypeOfExpression(kind, [typeOf], exp, operatorToken, typeName)
        End Function

        ' /*********************************************************************
        ' *
        ' * Function:
        ' *     Parser::ParseVariable
        ' *
        ' * Purpose:
        ' *
        ' **********************************************************************/
        ' File: Parser.cpp
        ' Lines: 16191 - 16191
        ' Expression* .Parser::ParseVariable( [ _Inout_ bool& ErrorInConstruct ] )
        Private Function ParseVariable() As ExpressionSyntax
            Return ParseExpressionCore(OperatorPrecedence.PrecedenceRelational)
        End Function

        ' /*********************************************************************
        ' *
        ' * Function:
        ' *     Parser::ParseQualifiedExpr
        ' *
        ' * Purpose:
        ' *     Parses a dot or bang reference, starting at the dot or bang.
        ' *
        ' **********************************************************************/
        ' [in] token starting term
        ' [in] stuff before "." or "!"

        ' File: Parser.cpp
        ' Lines: 16211 - 16211
        ' Expression* .Parser::ParseQualifiedExpr( [ _In_ Token* Start ] [ _In_opt_ ParseTree::Expression* Term ] [ _Inout_ bool& ErrorInConstruct ] )
        Private Function ParseQualifiedExpr(
            Term As ExpressionSyntax
        ) As ExpressionSyntax
            Debug.Assert(CurrentToken.Kind = SyntaxKind.DotToken OrElse
                  CurrentToken.Kind = SyntaxKind.ExclamationToken,
                  "Must be on either a '.' or '!' when entering parseQualifiedExpr()")

            Dim DotOrBangToken As PunctuationSyntax = DirectCast(CurrentToken, PunctuationSyntax)

            Dim prevPrevToken = PrevToken
            GetNextToken()

            If DotOrBangToken.Kind = SyntaxKind.ExclamationToken Then
                Dim Name = ParseIdentifierNameAllowingKeyword()
                Return SyntaxFactory.DictionaryAccessExpression(Term, DotOrBangToken, Name)
            Else
                If (CurrentToken.IsEndOfLine() AndAlso Not CurrentToken.IsEndOfParse()) Then

                    Debug.Assert(CurrentToken.Kind = SyntaxKind.StatementTerminatorToken AndAlso
                              PrevToken.Kind = SyntaxKind.DotToken,
                              "We shouldn't get here without .<eol> tokens")

                    '/* We know we are sitting on an EOL preceded by a tkDot.  What we need to catch is the
                    '   case where a tkDot following an EOL isn't preceded by a valid token.  Bug Dev10_429652  For example:
                    '   with i <eol>
                    '     .  <-- this is bad.  This . follows an EOL and isn't preceded by a tkID.  Can't have it dangling like this
                    '     field = 42
                    '*/
                    If (prevPrevToken Is Nothing OrElse
                         prevPrevToken.Kind = SyntaxKind.StatementTerminatorToken) Then
                        ' if ( CurrentToken->m_Prev->m_Prev == NULL || // make sure we can look back far enough.  We know we can look back once, but twice we need to test
                        '     CurrentToken->m_Prev->m_Prev->m_TokenType == tkEOL ) // Make sure there is something besides air before the '.' DEV10_486908

                        Dim missingIdent = ReportSyntaxError(InternalSyntaxFactory.MissingIdentifier, ERRID.ERR_ExpectedIdentifier)

                        ' We are sitting on the tkEOL so let's just return this and keep parsing.  No ReSync() needed here, in other words.
                        Return SyntaxFactory.SimpleMemberAccessExpression(Term, DotOrBangToken, SyntaxFactory.IdentifierName(missingIdent))

                    ElseIf Not NextLineStartsWithStatementTerminator() Then
                        '//ILC: undone
                        '//       Right now we don't continue after a "." when the following tokens indicate XML member access
                        '//       We should probably enable this.
                        TryEatNewLineIfNotFollowedBy(SyntaxKind.DotToken)
                    End If
                End If

                ' Decide whether we're parsing:
                ' 1. Element axis i.e. ".<ident>"
                ' 2. Attribute axis i.e. ".@ident" or ".@<ident>
                ' 3. Descendant axis i.e. "...<ident>"
                ' 4. Regular CLR member axis i.e. ".ident"
                Select Case (CurrentToken.Kind)

                    Case SyntaxKind.AtToken
                        Dim atToken = DirectCast(CurrentToken, PunctuationSyntax)
                        Dim name As XmlNodeSyntax

                        ' Do not accept space or anything else between @ and name or <
                        If atToken.HasTrailingTrivia Then
                            GetNextToken(ScannerState.VB)
                            atToken = ReportSyntaxError(atToken, ERRID.ERR_ExpectedXmlName)
                            atToken = atToken.AddTrailingSyntax(ResyncAt())
                            name = SyntaxFactory.XmlName(Nothing, DirectCast(InternalSyntaxFactory.MissingToken(SyntaxKind.XmlNameToken), XmlNameTokenSyntax))

                        Else
                            ' Parse the Xml attribute name (allow name with and without angle brackets)
                            If PeekNextToken(ScannerState.VB).Kind = SyntaxKind.LessThanToken Then
                                ' Consume the @ and remember that this is an element axis
                                GetNextToken(ScannerState.Element)
                                name = ParseBracketedXmlQualifiedName()
                            Else
                                ' Consume the @ and remember that this is attribute axis
                                GetNextToken(ScannerState.VB)
                                name = ParseXmlQualifiedNameVB()

                                If name.HasLeadingTrivia Then
                                    atToken = ReportSyntaxError(atToken, ERRID.ERR_ExpectedXmlName)
                                    atToken.AddTrailingSyntax(name)
                                    atToken.AddTrailingSyntax(ResyncAt())
                                    name = SyntaxFactory.XmlName(Nothing, DirectCast(InternalSyntaxFactory.MissingToken(SyntaxKind.XmlNameToken), XmlNameTokenSyntax))
                                End If
                            End If
                        End If

                        Return SyntaxFactory.XmlMemberAccessExpression(SyntaxKind.XmlAttributeAccessExpression, Term, DotOrBangToken, atToken, Nothing, name)

                    Case SyntaxKind.LessThanToken
                        ' Remember that this is element axis

                        ' Parse the Xml element name
                        Dim name = ParseBracketedXmlQualifiedName()

                        Return SyntaxFactory.XmlMemberAccessExpression(SyntaxKind.XmlElementAccessExpression, Term, DotOrBangToken, Nothing, Nothing, name)

                    Case SyntaxKind.DotToken
                        If PeekToken(1).Kind = SyntaxKind.DotToken Then
                            ' Consume the 2nd and 3rd dots and remember that this is descendant axis
                            Dim secondDotToken = DirectCast(CurrentToken, PunctuationSyntax)
                            GetNextToken()
                            Dim thirdDotToken As PunctuationSyntax = Nothing
                            TryGetToken(SyntaxKind.DotToken, thirdDotToken)

                            ' Parse the Xml element name
                            TryEatNewLineIfFollowedBy(SyntaxKind.LessThanToken)
                            Dim name As XmlBracketedNameSyntax
                            If CurrentToken.Kind = SyntaxKind.LessThanToken Then
                                name = ParseBracketedXmlQualifiedName()
                            Else
                                name = ReportExpectedXmlBracketedName()
                            End If
                            Return SyntaxFactory.XmlMemberAccessExpression(SyntaxKind.XmlDescendantAccessExpression, Term, DotOrBangToken, secondDotToken, thirdDotToken, name)
                        End If

                    Case Else
                        Dim name = ParseSimpleNameExpressionAllowingKeywordAndTypeArguments()
                        Return SyntaxFactory.SimpleMemberAccessExpression(Term, DotOrBangToken, name)

                End Select
            End If

            'This is reachable with the following invalid syntax.
            '    p.  
            '      x
            ' 
            ' or 
            '   p..y
            Dim result As ExpressionSyntax
            If CurrentToken.Kind = SyntaxKind.AtToken Then
                Dim missingName = DirectCast(InternalSyntaxFactory.MissingToken(SyntaxKind.XmlNameToken), XmlNameTokenSyntax)
                result = SyntaxFactory.XmlMemberAccessExpression(SyntaxKind.XmlAttributeAccessExpression,
                                                          Term,
                                                          DotOrBangToken, Nothing, Nothing,
                                                          ReportSyntaxError(InternalSyntaxFactory.XmlName(Nothing, missingName), ERRID.ERR_ExpectedXmlName))
            Else
                result = SyntaxFactory.SimpleMemberAccessExpression(Term, DotOrBangToken, ReportSyntaxError(InternalSyntaxFactory.IdentifierName(InternalSyntaxFactory.MissingIdentifier), ERRID.ERR_ExpectedIdentifier))
            End If

            Return result
        End Function

        Private Sub RescanTrailingColonAsToken(ByRef prevToken As SyntaxToken, ByRef currentToken As SyntaxToken)
            _scanner.RescanTrailingColonAsToken(prevToken, currentToken)
            _currentToken = currentToken
        End Sub

        ''' <summary>
        ''' Transition from scanning XML to scanning VB. As a result,
        ''' any single line trivia is consumed and appended to the token
        ''' which is assumed to be the token at the transition point.
        ''' </summary>
        Private Function TransitionFromXmlToVB(Of T As VisualBasicSyntaxNode)(node As T) As T
            node = LastTokenReplacer.Replace(node, Function(token)
                                                       Dim trivia = New CodeAnalysis.Syntax.InternalSyntax.SyntaxList(Of VisualBasicSyntaxNode)(token.GetTrailingTrivia())
                                                       Dim toRemove As CodeAnalysis.Syntax.InternalSyntax.SyntaxList(Of VisualBasicSyntaxNode) = Nothing
                                                       Dim toAdd As CodeAnalysis.Syntax.InternalSyntax.SyntaxList(Of VisualBasicSyntaxNode) = Nothing
                                                       _scanner.TransitionFromXmlToVB(trivia, toRemove, toAdd)
                                                       trivia = trivia.GetStartOfTrivia(trivia.Count - toRemove.Count)
                                                       token = DirectCast(token.WithTrailingTrivia(trivia.Node), SyntaxToken)
                                                       token = SyntaxToken.AddTrailingTrivia(token, toAdd)
                                                       Return token
                                                   End Function)
            _currentToken = Nothing
            Return node
        End Function

        Private Function TransitionFromVBToXml(Of T As VisualBasicSyntaxNode)(state As ScannerState, node As T) As T
            node = LastTokenReplacer.Replace(node, Function(token)
                                                       Dim trivia = New CodeAnalysis.Syntax.InternalSyntax.SyntaxList(Of VisualBasicSyntaxNode)(token.GetTrailingTrivia())
                                                       Dim toRemove As CodeAnalysis.Syntax.InternalSyntax.SyntaxList(Of VisualBasicSyntaxNode) = Nothing
                                                       Dim toAdd As CodeAnalysis.Syntax.InternalSyntax.SyntaxList(Of VisualBasicSyntaxNode) = Nothing
                                                       _scanner.TransitionFromVBToXml(state, trivia, toRemove, toAdd)
                                                       trivia = trivia.GetStartOfTrivia(trivia.Count - toRemove.Count)
                                                       token = DirectCast(token.WithTrailingTrivia(trivia.Node), SyntaxToken)
                                                       token = SyntaxToken.AddTrailingTrivia(token, toAdd)
                                                       Return token
                                                   End Function)
            _currentToken = Nothing
            Return node
        End Function

        ' File: Parser.cpp
        ' Lines: 16286 - 16286
        ' Expression* .Parser::ParseBracketedXmlQualifiedName( [ bool IsElement ] [ _Inout_ bool& ErrorInConstruct ] )

        Private Function ParseBracketedXmlQualifiedName() As XmlBracketedNameSyntax
            Dim lessToken As PunctuationSyntax = Nothing

            ' Parse '<', Xml QName, '>'
            ' Ensure the < is parsed in Xml state so that trivia on the < is correct.  For example, we don't want comments attached
            ' to the < which can happen in a vb scanning state.
            ResetCurrentToken(ScannerState.Content)

            If TryGetToken(SyntaxKind.LessThanToken, lessToken) Then
                ' Next state is element so that a bracketed qualified name gets the > to close name.
                ' If state is VB a bracketed name followed by = does not get the > because >= is returned instead.
                ResetCurrentToken(ScannerState.Element)
                Dim name = ParseXmlQualifiedName(False, False, ScannerState.Element, ScannerState.Element)
                Dim greaterToken As PunctuationSyntax = Nothing

                VerifyExpectedToken(SyntaxKind.GreaterThanToken, greaterToken)

                Dim result = SyntaxFactory.XmlBracketedName(lessToken, DirectCast(name, XmlNameSyntax), greaterToken)
                result = AdjustTriviaForMissingTokens(result)
                result = TransitionFromXmlToVB(result)

                ' Report an error if leading and trailing whitespace is found
                Dim whitespaceChecker As New XmlWhitespaceChecker()
                Return DirectCast(whitespaceChecker.Visit(result), XmlBracketedNameSyntax)

            Else
                ResetCurrentToken(ScannerState.VB)
                Return ReportExpectedXmlBracketedName()

            End If
        End Function

        Private Function ReportExpectedXmlBracketedName() As XmlBracketedNameSyntax
            Dim lessToken = DirectCast(HandleUnexpectedToken(SyntaxKind.LessThanToken), PunctuationSyntax)
            Dim name = ReportExpectedXmlName()
            Dim greaterToken = InternalSyntaxFactory.MissingPunctuation(SyntaxKind.GreaterThanToken)
            Return SyntaxFactory.XmlBracketedName(lessToken, name, greaterToken)
        End Function

        Private Function ReportExpectedXmlName() As XmlNameSyntax
            Return ReportSyntaxError(SyntaxFactory.XmlName(Nothing, SyntaxFactory.XmlNameToken("", SyntaxKind.XmlNameToken, Nothing, Nothing)), ERRID.ERR_ExpectedXmlName)
        End Function

        Private Function ParseParenthesizedExpressionOrTupleLiteral() As ExpressionSyntax
            Debug.Assert(CurrentToken.Kind = SyntaxKind.OpenParenToken)

            ' "(" expr ")"              'parenthesized
            ' "(" Name:= ....           'parse a tuple
            ' "(" expr, ....            'parse a tuple

            Dim openParen As PunctuationSyntax = Nothing
            TryGetTokenAndEatNewLine(SyntaxKind.OpenParenToken, openParen)

            If (CurrentToken.Kind = SyntaxKind.IdentifierToken AndAlso
                PeekToken(1).Kind = SyntaxKind.ColonEqualsToken) Then

                Dim argumentName = ParseIdentifierNameAllowingKeyword()
                Dim colonEquals As PunctuationSyntax = Nothing
                TryGetTokenAndEatNewLine(SyntaxKind.ColonEqualsToken, colonEquals)

                Dim nameColonEquals = SyntaxFactory.NameColonEquals(argumentName, colonEquals)
                Dim firstArgument = SyntaxFactory.SimpleArgument(nameColonEquals, ParseExpressionCore())

                Return ParseTheRestOfTupleLiteral(openParen, firstArgument)
            End If

            Dim operand = ParseExpressionCore()

            If (CurrentToken.Kind = SyntaxKind.CommaToken) Then
                Dim firstArgument = SyntaxFactory.SimpleArgument(nameColonEquals:=Nothing, expression:=operand)

                Return ParseTheRestOfTupleLiteral(openParen, firstArgument)
            End If

            Dim closeParen As PunctuationSyntax = Nothing
            TryEatNewLineAndGetToken(SyntaxKind.CloseParenToken, closeParen, createIfMissing:=True)

            Return SyntaxFactory.ParenthesizedExpression(openParen, operand, closeParen)
        End Function

        Private Function ParseTheRestOfTupleLiteral(openParen As PunctuationSyntax, firstArgument As SimpleArgumentSyntax) As TupleExpressionSyntax

            Dim argumentBuilder = _pool.AllocateSeparated(Of SimpleArgumentSyntax)()
            argumentBuilder.Add(firstArgument)

            While CurrentToken.Kind = SyntaxKind.CommaToken
                Dim commaToken As PunctuationSyntax = Nothing
                TryGetTokenAndEatNewLine(SyntaxKind.CommaToken, commaToken)

                argumentBuilder.AddSeparator(commaToken)
                Dim nameColonEquals As NameColonEqualsSyntax = Nothing

                If (CurrentToken.Kind = SyntaxKind.IdentifierToken AndAlso
                    PeekToken(1).Kind = SyntaxKind.ColonEqualsToken) Then

                    Dim argumentName = ParseIdentifierNameAllowingKeyword()
                    Dim colonEquals As PunctuationSyntax = Nothing
                    TryGetTokenAndEatNewLine(SyntaxKind.ColonEqualsToken, colonEquals)

                    nameColonEquals = SyntaxFactory.NameColonEquals(argumentName, colonEquals)
                End If

                Dim argument = SyntaxFactory.SimpleArgument(nameColonEquals, ParseExpressionCore())
                argumentBuilder.Add(argument)
            End While

            Dim closeParen As PunctuationSyntax = Nothing
            TryEatNewLineAndGetToken(SyntaxKind.CloseParenToken, closeParen, createIfMissing:=True)

            If argumentBuilder.Count < 2 Then
                argumentBuilder.AddSeparator(InternalSyntaxFactory.MissingToken(SyntaxKind.CommaToken))

                Dim missing = SyntaxFactory.IdentifierName(InternalSyntaxFactory.MissingIdentifier())
                missing = ReportSyntaxError(missing, ERRID.ERR_TupleTooFewElements)
                argumentBuilder.Add(SyntaxFactory.SimpleArgument(nameColonEquals:=Nothing, expression:=missing))
            End If

            Dim arguments = argumentBuilder.ToList
            _pool.Free(argumentBuilder)

            Dim tupleExpression = SyntaxFactory.TupleExpression(openParen, arguments, closeParen)

            tupleExpression = CheckFeatureAvailability(Feature.Tuples, tupleExpression)
            Return tupleExpression
        End Function

        ' Parse an argument list enclosed in parentheses.

        ' File: Parser.cpp
        ' Lines: 16304 - 16304
        ' ParenthesizedArgumentList .Parser::ParseParenthesizedArguments( [ _Inout_ bool& ErrorInConstruct ] )
        Friend Function ParseParenthesizedArguments(Optional RedimOrNewParent As Boolean = False, Optional attributeListParent As Boolean = False) As ArgumentListSyntax
            Debug.Assert(CurrentToken.Kind = SyntaxKind.OpenParenToken, "should be at tkLParen.")

            Dim arguments As CodeAnalysis.Syntax.InternalSyntax.SeparatedSyntaxList(Of ArgumentSyntax) = Nothing
            Dim openParen As PunctuationSyntax = Nothing
            Dim closeParen As PunctuationSyntax = Nothing

            Debug.Assert(CurrentToken.Kind = SyntaxKind.OpenParenToken)
            TryGetTokenAndEatNewLine(SyntaxKind.OpenParenToken, openParen)

            Dim unexpected As GreenNode = Nothing
            arguments = ParseArguments(unexpected, RedimOrNewParent, attributeListParent)

            If Not TryEatNewLineAndGetToken(SyntaxKind.CloseParenToken, closeParen, createIfMissing:=False) Then
                ' On error, peek for ")" with "(". If ")" seen before
                ' "(", then sync on that. Otherwise, assume missing ")"
                ' and let caller decide.

                Dim clue As SyntaxKind = PeekAheadFor(SyntaxKind.OpenParenToken, SyntaxKind.CloseParenToken)

                If clue = SyntaxKind.CloseParenToken Then
                    ' We should not hit this case since ParseArguments should have
                    ' resynced to the close paren and TryEatNewLineAndGetToken
                    ' above should have succeeded. If we do hit this, add a test case.
                    Debug.Assert(False, "Unexpected close paren")
                    Dim trash = ResyncAt({SyntaxKind.CloseParenToken})
                    closeParen = DirectCast(CurrentToken, PunctuationSyntax)
                    closeParen = closeParen.AddLeadingSyntax(trash)

                    GetNextToken()
                Else
                    closeParen = InternalSyntaxFactory.MissingPunctuation(SyntaxKind.CloseParenToken)
                    closeParen = ReportSyntaxError(closeParen, ERRID.ERR_ExpectedRparen)
                End If
            End If

            If unexpected IsNot Nothing Then
                closeParen = closeParen.AddLeadingSyntax(unexpected)
            End If

            Return SyntaxFactory.ArgumentList(openParen, arguments, closeParen)
        End Function

        ' /*********************************************************************
        ' *
        ' * Function:
        ' *     Parser::ParseParenthesizedQualifier
        ' *
        ' * Purpose:
        ' *     Parses a parenthesized qualifier:
        ' *         <qualifier>(...)
        ' *
        ' **********************************************************************/

        ' [in] token starting term
        ' [in] preceding term

        ' File: Parser.cpp
        ' Lines: 16366 - 16366
        ' Expression* .Parser::ParseParenthesizedQualifier( [ _In_ Token* Start ] [ _In_ ParseTree::Expression* Term ] [ _Inout_ bool& ErrorInConstruct ] )

        Private Function ParseParenthesizedQualifier(Term As ExpressionSyntax, Optional RedimOrNewParent As Boolean = False) As ExpressionSyntax
            ' Because parentheses are used for array indexing, parameter passing, and array
            ' declaring (via the Redim statement), there is some ambiguity about how to handle
            ' a parenthesized list that begins with an expression. The most general case is to
            ' parse it as an argument list, with special treatment if a TO occurs in a Redim
            ' context.
            Dim Arguments = ParseParenthesizedArguments(RedimOrNewParent)

            Return SyntaxFactory.InvocationExpression(Term, Arguments)
        End Function

        ' Parse a list of comma-separated arguments.

        ' Where to insert the next list element

        ' File: Parser.cpp
        ' Lines: 16425 - 16425
        ' .Parser::ParseArguments( [ _In_ ParseTree::ArgumentList** Target ] [ _Inout_ bool& ErrorInConstruct ] )

        Private Function ParseArguments(ByRef unexpected As GreenNode, Optional RedimOrNewParent As Boolean = False, Optional attributeListParent As Boolean = False) As CodeAnalysis.Syntax.InternalSyntax.SeparatedSyntaxList(Of ArgumentSyntax)
            Dim arguments = _pool.AllocateSeparated(Of ArgumentSyntax)()

            Dim allowNonTrailingNamedArguments = _scanner.Options.LanguageVersion.AllowNonTrailingNamedArguments()
            Dim seenNames As Boolean = False

            Do
                Dim isNamed As Boolean = False
                If (CurrentToken.Kind = SyntaxKind.IdentifierToken OrElse CurrentToken.IsKeyword()) AndAlso
                    PeekToken(1).Kind = SyntaxKind.ColonEqualsToken Then

                    seenNames = True
                    isNamed = True
                End If

                Dim comma As PunctuationSyntax = Nothing
                If isNamed Then

                    If attributeListParent Then
                        ParseNamedArguments(arguments)
                        Exit Do
                    End If

                    Dim argumentName As IdentifierNameSyntax = ParseIdentifierNameAllowingKeyword()
                    Dim colonEquals As PunctuationSyntax = Nothing
                    TryGetTokenAndEatNewLine(SyntaxKind.ColonEqualsToken, colonEquals)
                    Dim namedArgument = SyntaxFactory.SimpleArgument(SyntaxFactory.NameColonEquals(argumentName, colonEquals), ParseExpressionCore())
                    arguments.Add(namedArgument)

                ElseIf CurrentToken.Kind = SyntaxKind.CommaToken Then
                    TryGetTokenAndEatNewLine(SyntaxKind.CommaToken, comma)

                    Dim argument = ReportNonTrailingNamedArgumentIfNeeded(InternalSyntaxFactory.OmittedArgument(), seenNames, allowNonTrailingNamedArguments)
                    arguments.Add(argument)
                    arguments.AddSeparator(comma)
                    Continue Do

                ElseIf CurrentToken.Kind = SyntaxKind.CloseParenToken Then
                    If arguments.Count > 0 Then
                        Dim argument = ReportNonTrailingNamedArgumentIfNeeded(InternalSyntaxFactory.OmittedArgument(), seenNames, allowNonTrailingNamedArguments)
                        arguments.Add(argument)
                    End If
                    Exit Do

                Else
                    Dim argument = ParseArgument(RedimOrNewParent)
                    argument = ReportNonTrailingNamedArgumentIfNeeded(argument, seenNames, allowNonTrailingNamedArguments)
                    arguments.Add(argument)
                End If

                If TryGetTokenAndEatNewLine(SyntaxKind.CommaToken, comma) Then
                    arguments.AddSeparator(comma)
                    Continue Do

                ElseIf CurrentToken.Kind = SyntaxKind.CloseParenToken OrElse MustEndStatement(CurrentToken) Then
                    Exit Do

                Else
                    ' There is a syntax error of some kind.

                    Dim skipped = ResyncAt({SyntaxKind.CommaToken, SyntaxKind.CloseParenToken}).Node
                    If skipped IsNot Nothing Then
                        skipped = ReportSyntaxError(skipped, ERRID.ERR_ArgumentSyntax)
                    End If

                    If CurrentToken.Kind = SyntaxKind.CommaToken Then
                        comma = DirectCast(CurrentToken, PunctuationSyntax)
                        comma = comma.AddLeadingSyntax(skipped)
                        arguments.AddSeparator(comma)
                        GetNextToken()
                    Else
                        unexpected = skipped
                        Exit Do
                    End If
                End If

            Loop

            Dim result = arguments.ToList
            _pool.Free(arguments)
            Return result

        End Function

        ''' <summary>After VB15.5 it is possible to use named arguments in non-trailing position, except in attribute lists (where it remains disallowed)</summary>
        Private Shared Function ReportNonTrailingNamedArgumentIfNeeded(argument As ArgumentSyntax, seenNames As Boolean, allowNonTrailingNamedArguments As Boolean) As ArgumentSyntax
            If Not seenNames OrElse allowNonTrailingNamedArguments Then
                Return argument
            End If

            Return ReportSyntaxError(argument, ERRID.ERR_ExpectedNamedArgument,
                    New VisualBasicRequiredLanguageVersion(Feature.NonTrailingNamedArguments.GetLanguageVersion()))
        End Function

        ' Parse a list of comma-separated keyword arguments.

        ' Where to insert the next list element

        ' File: Parser.cpp
        ' Lines: 16515 - 16515
        ' .Parser::ParseNamedArguments( [ ParseTree::ArgumentList** Target ] [ _Inout_ bool& ErrorInConstruct ] )

        Private Sub ParseNamedArguments(arguments As SeparatedSyntaxListBuilder(Of ArgumentSyntax))

            Do
                Dim argumentName As IdentifierNameSyntax
                Dim colonEquals As PunctuationSyntax = Nothing
                Dim hasError As Boolean = False

                If (CurrentToken.Kind = SyntaxKind.IdentifierToken OrElse CurrentToken.IsKeyword()) AndAlso
                    PeekToken(1).Kind = SyntaxKind.ColonEqualsToken Then

                    argumentName = ParseIdentifierNameAllowingKeyword()
                    TryGetTokenAndEatNewLine(SyntaxKind.ColonEqualsToken, colonEquals)
                Else
                    argumentName = SyntaxFactory.IdentifierName(InternalSyntaxFactory.MissingIdentifier())
                    colonEquals = InternalSyntaxFactory.MissingPunctuation(SyntaxKind.ColonEqualsToken)
                    hasError = True
                End If

                If hasError Then
                    argumentName = ReportSyntaxError(argumentName, ERRID.ERR_ExpectedNamedArgumentInAttributeList)
                End If

                Dim namedArgument = SyntaxFactory.SimpleArgument(SyntaxFactory.NameColonEquals(argumentName, colonEquals), ParseExpressionCore())

                If CurrentToken.Kind <> SyntaxKind.CommaToken Then
                    If CurrentToken.Kind = SyntaxKind.CloseParenToken OrElse MustEndStatement(CurrentToken) Then
                        arguments.Add(namedArgument)
                        Exit Do
                    End If

                    ' There is a syntax error of some kind.

                    namedArgument = ReportSyntaxError(namedArgument, ERRID.ERR_ArgumentSyntax)
                    namedArgument = ResyncAt(namedArgument, SyntaxKind.CommaToken, SyntaxKind.CloseParenToken)

                    If CurrentToken.Kind <> SyntaxKind.CommaToken Then
                        arguments.Add(namedArgument)
                        Exit Do
                    End If

                End If

                Dim comma As PunctuationSyntax = Nothing
                TryGetTokenAndEatNewLine(SyntaxKind.CommaToken, comma)
                Debug.Assert(comma.Kind = SyntaxKind.CommaToken)

                arguments.Add(namedArgument)
                arguments.AddSeparator(comma)
            Loop
        End Sub

        ' File: Parser.cpp
        ' Lines: 16564 - 16564
        ' Argument* .Parser::ParseArgument( [ _Inout_ bool& ErrorInConstruct ] )

        Private Function ParseArgument(Optional RedimOrNewParent As Boolean = False) As ArgumentSyntax
            Dim argument As ArgumentSyntax

            Dim value As ExpressionSyntax = ParseExpressionCore(OperatorPrecedence.PrecedenceNone)

            If value.ContainsDiagnostics Then
                value = ResyncAt(value, SyntaxKind.CommaToken, SyntaxKind.CloseParenToken)
            End If

            If RedimOrNewParent AndAlso CurrentToken.Kind = SyntaxKind.ToKeyword Then
                Dim toKeyword As KeywordSyntax = DirectCast(CurrentToken, KeywordSyntax)
                Dim lowerBound As ExpressionSyntax = value

                GetNextToken() ' consume tkTO
                value = ParseExpressionCore(OperatorPrecedence.PrecedenceNone)

                ' Check that lower bound is equal to 0 moved to binder.

                argument = SyntaxFactory.RangeArgument(lowerBound, toKeyword, value)
            Else
                argument = SyntaxFactory.SimpleArgument(Nothing, value)
            End If

            Return argument
        End Function

        ''' <summary>
        ''' ParseCast parses CType, DirectCast, TryCast.
        ''' CCCastExpression ->   DirectCast ( CCExpression , TypeName ) 
        '''                     | TryCast ( CCExpression , TypeName ) 
        '''                     | CType ( CCExpression , TypeName ) 
        '''                     { | CastTarget ( CCExpression ) }
        ''' </summary>
        ''' <returns>Cast</returns>
        ''' <remarks>Dev10 ParseCType does not parse exact grammar in the spec, since dev10 accepts Expression whereas the grammar uses CCExpression.
        ''' This function only does not parse CastTarget ( ... ), it is parsed in ParseTerm
        ''' </remarks>
        Private Function ParseCast() As CastExpressionSyntax
            Dim keyword As KeywordSyntax = DirectCast(CurrentToken, KeywordSyntax)
            Dim keywordKind As SyntaxKind = keyword.Kind

            Debug.Assert(keywordKind = SyntaxKind.CTypeKeyword OrElse
                    keywordKind = SyntaxKind.DirectCastKeyword OrElse
                    keywordKind = SyntaxKind.TryCastKeyword,
                    "Expected CTYPE or DIRECTCAST or TRYCAST token.")

            GetNextToken()

            Dim openParen As PunctuationSyntax = Nothing

            TryGetTokenAndEatNewLine(SyntaxKind.OpenParenToken, openParen, createIfMissing:=True)

            Dim exp As ExpressionSyntax = ParseExpressionCore(OperatorPrecedence.PrecedenceNone)

            If exp.ContainsDiagnostics Then
                exp = ResyncAt(exp, SyntaxKind.CommaToken, SyntaxKind.CloseParenToken)
            End If

            Dim comma As PunctuationSyntax = Nothing

            If Not TryGetTokenAndEatNewLine(SyntaxKind.CommaToken, comma) Then

                comma = ReportSyntaxError(InternalSyntaxFactory.MissingPunctuation(SyntaxKind.CommaToken),
                                           ERRID.ERR_SyntaxInCastOp)
            End If

            Dim targetType As TypeSyntax = ParseGeneralType()

            Dim closeParen As PunctuationSyntax = Nothing

            TryEatNewLineAndGetToken(SyntaxKind.CloseParenToken, closeParen, createIfMissing:=True)

            Dim cast As CastExpressionSyntax = Nothing

            Select Case keywordKind
                Case SyntaxKind.CTypeKeyword
                    cast = SyntaxFactory.CTypeExpression(keyword, openParen, exp, comma, targetType, closeParen)
                Case SyntaxKind.DirectCastKeyword
                    cast = SyntaxFactory.DirectCastExpression(keyword, openParen, exp, comma, targetType, closeParen)
                Case SyntaxKind.TryCastKeyword
                    cast = SyntaxFactory.TryCastExpression(keyword, openParen, exp, comma, targetType, closeParen)
                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(keywordKind)
            End Select

            Return cast
        End Function

        Private Function ParseFunctionOrSubLambdaHeader(<Out> ByRef isMultiLine As Boolean, Optional parseModifiers As Boolean = False) As LambdaHeaderSyntax

            Dim modifiers As CodeAnalysis.Syntax.InternalSyntax.SyntaxList(Of KeywordSyntax)

            Dim save_isInMethodDeclarationHeader As Boolean = _isInMethodDeclarationHeader
            _isInMethodDeclarationHeader = True

            Dim save_isInAsyncMethodDeclarationHeader As Boolean = _isInAsyncMethodDeclarationHeader
            Dim save_isInIteratorMethodDeclarationHeader As Boolean = _isInIteratorMethodDeclarationHeader

            If parseModifiers Then

                Debug.Assert(CurrentToken.Kind = SyntaxKind.IdentifierToken)

                modifiers = ParseSpecifiers()

                _isInAsyncMethodDeclarationHeader = modifiers.Any(SyntaxKind.AsyncKeyword)
                _isInIteratorMethodDeclarationHeader = modifiers.Any(SyntaxKind.IteratorKeyword)
            Else
                modifiers = Nothing
                _isInAsyncMethodDeclarationHeader = False
                _isInIteratorMethodDeclarationHeader = False
            End If

            Debug.Assert(CurrentToken.Kind = SyntaxKind.FunctionKeyword OrElse
                         CurrentToken.Kind = SyntaxKind.SubKeyword,
                         "ParseFunctionLambda called on wrong token.")
            ' The current token is on the function or delegate's name

            Dim methodKeyword = DirectCast(CurrentToken, KeywordSyntax)
            GetNextToken()

            Dim genericParams As TypeParameterListSyntax = Nothing
            Dim openParen As PunctuationSyntax = Nothing
            Dim params As CodeAnalysis.Syntax.InternalSyntax.SeparatedSyntaxList(Of ParameterSyntax) = Nothing
            Dim closeParen As PunctuationSyntax = Nothing

            isMultiLine = False

            If CurrentToken.Kind = SyntaxKind.OpenParenToken Then
                TryRejectGenericParametersForMemberDecl(genericParams)
            End If

            If CurrentToken.Kind = SyntaxKind.OpenParenToken Then
                params = ParseParameters(openParen, closeParen)
            Else
                openParen = DirectCast(HandleUnexpectedToken(SyntaxKind.OpenParenToken), PunctuationSyntax)
                closeParen = DirectCast(HandleUnexpectedToken(SyntaxKind.CloseParenToken), PunctuationSyntax)
            End If

            Debug.Assert(openParen IsNot Nothing)
            Debug.Assert(closeParen IsNot Nothing)

            If genericParams IsNot Nothing Then
                openParen = openParen.AddLeadingSyntax(genericParams, ERRID.ERR_GenericParamsOnInvalidMember)
            End If

            Dim asClause As SimpleAsClauseSyntax = Nothing
            Dim returnTypeAttributes As CodeAnalysis.Syntax.InternalSyntax.SyntaxList(Of AttributeListSyntax) = Nothing

            Dim asKeyword As KeywordSyntax = Nothing

            ' Check the return type.
            ' Parse the as clause if one exists even if this is a sub. This aids in error recovery situations. Otherwise,
            ' Sub () as integer 
            ' becomes a single line sub lambda.
            If CurrentToken.Kind = SyntaxKind.AsKeyword Then
                asKeyword = DirectCast(CurrentToken, KeywordSyntax)
                GetNextToken()

                If CurrentToken.Kind = SyntaxKind.LessThanToken Then
                    returnTypeAttributes = ParseAttributeLists(False)

                    asKeyword = asKeyword.AddTrailingSyntax(returnTypeAttributes.Node, ERRID.ERR_AttributeOnLambdaReturnType)
                End If

                Dim returnType As TypeSyntax = ParseGeneralType()

                If returnType.ContainsDiagnostics Then
                    returnType = ResyncAt(returnType)
                End If

                asClause = SyntaxFactory.SimpleAsClause(asKeyword, Nothing, returnType)

                isMultiLine = True
            End If

            If methodKeyword.Kind <> SyntaxKind.FunctionKeyword AndAlso asClause IsNot Nothing Then
                closeParen = closeParen.AddTrailingSyntax(asClause, ERRID.ERR_ExpectedEOS)
                asClause = Nothing
            End If

            isMultiLine = isMultiLine OrElse CurrentToken.Kind = SyntaxKind.StatementTerminatorToken

            _isInMethodDeclarationHeader = save_isInMethodDeclarationHeader
            _isInAsyncMethodDeclarationHeader = save_isInAsyncMethodDeclarationHeader
            _isInIteratorMethodDeclarationHeader = save_isInIteratorMethodDeclarationHeader

            Return SyntaxFactory.LambdaHeader(If(methodKeyword.Kind = SyntaxKind.FunctionKeyword, SyntaxKind.FunctionLambdaHeader, SyntaxKind.SubLambdaHeader),
                                            Nothing, modifiers, methodKeyword, SyntaxFactory.ParameterList(openParen, params, closeParen), asClause)

        End Function

        'TODO - Review all errors in Dev10 ParseStatementLambda
        ' Ensure messages are reported and messages are added to error resource.
        ' The following cases are allowed by the new parser but errors in dev10
        '
        'ERRID_SubDisallowsStatement for Sub() Dim x=e1 , y=e2

        Private Function ParseLambda(parseModifiers As Boolean) As ExpressionSyntax
            'AssertLanguageFeature(ERRID.FEATUREID_StatementLambdas, CurrentToken)
            Dim isMultiLine As Boolean = False

            Dim header = ParseFunctionOrSubLambdaHeader(isMultiLine, parseModifiers)

            header = AdjustTriviaForMissingTokens(header)

            Dim value As ExpressionSyntax

            If header.Kind = SyntaxKind.FunctionLambdaHeader AndAlso Not isMultiLine Then
                ' This is a single line lambda function
                Dim lambdaContext = New SingleLineLambdaContext(header, _context)
                _context = lambdaContext

                value = SyntaxFactory.SingleLineLambdaExpression(SyntaxKind.SingleLineFunctionLambdaExpression,
                                                          header,
                                                          ParseExpressionCore())
                value = AdjustTriviaForMissingTokens(value)
                If header.Modifiers.Any(SyntaxKind.IteratorKeyword) Then
                    value = Parser.ReportSyntaxError(value, ERRID.ERR_BadIteratorExpressionLambda)
                End If

                Debug.Assert(_context Is lambdaContext)
                _context = lambdaContext.PrevBlock

            Else
                Dim statementContext = _context 'This is the statement context that contains the lambda expression

                'This is either a single line sub or multi line lambda sub or function
                Dim lambdaContext As MethodBlockContext
                If isMultiLine Then
                    lambdaContext = New LambdaContext(header, statementContext)
                Else
                    lambdaContext = New SingleLineLambdaContext(header, statementContext)
                End If

                _context = lambdaContext

                If isMultiLine OrElse CurrentToken.Kind = SyntaxKind.ColonToken Then
                    _context = _context.ResyncAndProcessStatementTerminator(header, lambdaContext)
                End If

                Dim statement As StatementSyntax = Nothing

                While _context.Level >= lambdaContext.Level

                    If CurrentToken.IsEndOfParse() Then
                        _context = _context.EndLambda()
                        Exit While
                    End If

                    statement = _context.Parse()
                    statement = AdjustTriviaForMissingTokens(statement)

                    Dim isDeclaration = IsDeclarationStatement(statement.Kind)

                    If isDeclaration Then
                        ' A declaration always closes a lambda.
                        _context.Add(ReportSyntaxError(statement, ERRID.ERR_InvInsideEndsProc))
                    Else
                        _context = _context.LinkSyntax(statement)
                        If _context.Level < lambdaContext.Level Then
                            Exit While
                        End If
                    End If

                    _context = _context.ResyncAndProcessStatementTerminator(statement, lambdaContext)
                    statement = Nothing

                    If isDeclaration Then
                        If _context.Level >= lambdaContext.Level Then
                            _context = _context.EndLambda()
                        End If
                        Exit While
                    End If
                End While

                Debug.Assert(_context Is statementContext, "Lambda terminated with the wrong context.")
                value = DirectCast(lambdaContext.CreateBlockSyntax(statement), ExpressionSyntax)

            End If

            If isMultiLine Then
                value = CheckFeatureAvailability(Feature.StatementLambdas, value)
            End If

            Return value
        End Function

        Friend Shared Function IsDeclarationStatement(kind As SyntaxKind) As Boolean
            Select Case kind
                Case _
                    SyntaxKind.SubStatement,
                    SyntaxKind.SubNewStatement,
                    SyntaxKind.FunctionStatement,
                    SyntaxKind.OperatorStatement,
                    SyntaxKind.PropertyStatement,
                    SyntaxKind.EventStatement,
                    SyntaxKind.NamespaceStatement,
                    SyntaxKind.ClassStatement,
                    SyntaxKind.StructureStatement,
                    SyntaxKind.EnumStatement,
                    SyntaxKind.ModuleStatement,
                    SyntaxKind.InterfaceStatement,
                    SyntaxKind.SetAccessorStatement,
                    SyntaxKind.GetAccessorStatement,
                    SyntaxKind.DeclareSubStatement,
                    SyntaxKind.DeclareFunctionStatement,
                    SyntaxKind.DelegateFunctionStatement,
                    SyntaxKind.DelegateSubStatement
                    Return True
                Case Else
                    Return False
            End Select
        End Function

        ' Parse a list of comma-separated Variables.

        ' File: Parser.cpp
        ' Lines: 16738 - 16738
        ' ExpressionList* .Parser::ParseVariableList( [ _Inout_ bool& ErrorInConstruct ] )

        Private Function ParseVariableList() As CodeAnalysis.Syntax.InternalSyntax.SeparatedSyntaxList(Of ExpressionSyntax)

            Dim variables As SeparatedSyntaxListBuilder(Of ExpressionSyntax) = Me._pool.AllocateSeparated(Of ExpressionSyntax)()

            Do
                variables.Add(ParseVariable())

                Dim comma As PunctuationSyntax = Nothing
                If Not TryGetTokenAndEatNewLine(SyntaxKind.CommaToken, comma) Then
                    Exit Do
                End If

                variables.AddSeparator(comma)
            Loop

            Dim result = variables.ToList
            Me._pool.Free(variables)

            Return result
        End Function

        Private Function ParseAwaitExpression(Optional awaitKeyword As KeywordSyntax = Nothing) As AwaitExpressionSyntax

            Debug.Assert(DirectCast(CurrentToken, IdentifierTokenSyntax).ContextualKind = SyntaxKind.AwaitKeyword)

            If awaitKeyword Is Nothing Then
                TryIdentifierAsContextualKeyword(CurrentToken, awaitKeyword)

                Debug.Assert(awaitKeyword IsNot Nothing AndAlso awaitKeyword.Kind = SyntaxKind.AwaitKeyword)
            End If

            GetNextToken()

            Dim expression = ParseTerm()

            Return SyntaxFactory.AwaitExpression(awaitKeyword, expression)

        End Function

    End Class

End Namespace
