' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports InternalSyntaxFactory = Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax.SyntaxFactory

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax

    Partial Friend Class Parser

        '
        '============ Methods for parsing syntactic terminals ===============
        '

        ' /*********************************************************************
        ' *
        ' * Function:
        ' *     Parser::ParseIdentifier
        ' *
        ' * Purpose:
        ' *     Parse an identifier. Current token must be at the expected
        ' *     identifier. Keywords are NOT allowed as identifiers.
        ' *
        ' **********************************************************************/

        ' File: Parser.cpp
        ' Lines: 16939 - 16939
        ' IdentifierDescriptor .Parser::ParseIdentifier( [ _Inout_ bool& ErrorInConstruct ] [ bool allowNullable ] )

        Private Function ParseIdentifier() As IdentifierTokenSyntax

            Dim identifier As IdentifierTokenSyntax

            If CurrentToken.Kind = SyntaxKind.IdentifierToken Then

                identifier = DirectCast(CurrentToken, IdentifierTokenSyntax)

                If (identifier.ContextualKind = SyntaxKind.AwaitKeyword AndAlso IsWithinAsyncMethodOrLambda) OrElse
                   (identifier.ContextualKind = SyntaxKind.YieldKeyword AndAlso IsWithinIteratorContext) Then

                    identifier = ReportSyntaxError(identifier, ERRID.ERR_InvalidUseOfKeyword)
                End If

                GetNextToken()
            Else
                ' If the token is a keyword, assume that the user meant it to
                ' be an identifier and consume it. Otherwise, leave current token
                ' as is and let caller decide what to do.

                If CurrentToken.IsKeyword Then
                    Dim keyword As KeywordSyntax = DirectCast(CurrentToken, KeywordSyntax)

                    identifier = _scanner.MakeIdentifier(keyword)
                    identifier = ReportSyntaxError(identifier, ERRID.ERR_InvalidUseOfKeyword)
                    GetNextToken()

                Else
                    identifier = InternalSyntaxFactory.MissingIdentifier()

                    ' a preceding "_" will be a BadToken already and there was already a 
                    ' ERR_ExpectedIdentifier diagnose message for it
                    If (CurrentToken.Kind = SyntaxKind.BadToken AndAlso CurrentToken.Text = "_") Then
                        identifier = identifier.AddLeadingSyntax(CurrentToken, ERRID.ERR_ExpectedIdentifier)
                        GetNextToken()
                    Else
                        identifier = ReportSyntaxError(identifier, ERRID.ERR_ExpectedIdentifier)
                    End If
                End If

            End If

            Return identifier
        End Function

        Private Function ParseNullableIdentifier(ByRef optionalNullable As PunctuationSyntax) As IdentifierTokenSyntax

            Dim identifier As IdentifierTokenSyntax

            identifier = ParseIdentifier()

            If SyntaxKind.QuestionToken = CurrentToken.Kind AndAlso Not identifier.ContainsDiagnostics Then
                optionalNullable = DirectCast(CurrentToken, PunctuationSyntax)

                GetNextToken()
            End If

            Return identifier
        End Function

        ' /*********************************************************************
        ' *
        ' * Function:
        ' *     Parser::ParseIdentifierAllowingKeyword
        ' *
        ' * Purpose:
        ' *     Parse an identifier. Current token must be at the expected
        ' *     identifier. Keywords are allowed as identifiers.
        ' *
        ' **********************************************************************/

        ' File: Parser.cpp
        ' Lines: 16998 - 16998
        ' IdentifierDescriptor .Parser::ParseIdentifierAllowingKeyword( [ _Inout_ bool& ErrorInConstruct ] )

        Private Function ParseIdentifierAllowingKeyword() As IdentifierTokenSyntax

            Dim identifier As IdentifierTokenSyntax

            If CurrentToken.Kind = SyntaxKind.IdentifierToken Then
                identifier = DirectCast(CurrentToken, IdentifierTokenSyntax)
                GetNextToken()

            ElseIf CurrentToken.IsKeyword() Then
                Dim keyword As KeywordSyntax = DirectCast(CurrentToken, KeywordSyntax)
                identifier = _scanner.MakeIdentifier(keyword)
                GetNextToken()

            Else
                ' Current token is not advanced. Let caller decide what to do.
                identifier = InternalSyntaxFactory.MissingIdentifier()
                identifier = ReportSyntaxError(identifier, ERRID.ERR_ExpectedIdentifier)
            End If

            Return identifier
        End Function

        ' File: Parser.cpp
        ' Lines: 17021 - 17021
        ' Expression* .Parser::ParseIdentifierExpressionAllowingKeyword( [ _Inout_ bool& ErrorInConstruct ] )

        Private Function ParseIdentifierNameAllowingKeyword() As IdentifierNameSyntax

            Dim Name = ParseIdentifierAllowingKeyword()
            Return SyntaxFactory.IdentifierName(Name)

        End Function

        Private Function ParseSimpleNameExpressionAllowingKeywordAndTypeArguments() As SimpleNameSyntax
            Dim Name = ParseIdentifierAllowingKeyword()

            If Not _evaluatingConditionCompilationExpression AndAlso
                BeginsGeneric(allowGenericsWithoutOf:=True) Then

                Dim AllowEmptyGenericArguments As Boolean = False
                Dim AllowNonEmptyGenericArguments As Boolean = True

                Dim Arguments As TypeArgumentListSyntax = ParseGenericArguments(
                                                        AllowEmptyGenericArguments,
                                                        AllowNonEmptyGenericArguments)

                Return SyntaxFactory.GenericName(Name, Arguments)
            Else
                Return SyntaxFactory.IdentifierName(Name)
            End If
        End Function

        Private Function ParseIntLiteral() As LiteralExpressionSyntax

            Debug.Assert(CurrentToken.Kind = SyntaxKind.IntegerLiteralToken, "Expected Integer literal.")

            Dim Literal As LiteralExpressionSyntax = SyntaxFactory.NumericLiteralExpression(CurrentToken)
            GetNextToken()
            Return Literal
        End Function

        Private Function ParseCharLiteral() As LiteralExpressionSyntax

            Debug.Assert(CurrentToken.Kind = SyntaxKind.CharacterLiteralToken, "Expected Char literal.")

            Dim Literal As LiteralExpressionSyntax = SyntaxFactory.CharacterLiteralExpression(CurrentToken)
            GetNextToken()

            Return Literal
        End Function

        Private Function ParseDecLiteral() As LiteralExpressionSyntax

            Debug.Assert(CurrentToken.Kind = SyntaxKind.DecimalLiteralToken, "must be at a decimal literal.")

            Dim Literal As LiteralExpressionSyntax = SyntaxFactory.NumericLiteralExpression(CurrentToken)

            GetNextToken()

            Return Literal
        End Function

        ''' <summary>
        ''' Parses StringLiteral
        ''' </summary>
        ''' <returns>LiteralNode</returns>
        ''' <remarks>If the current Token is not StringLiteral then returns LiteralNode with missing token.</remarks>  
        Private Function ParseStringLiteral() As LiteralExpressionSyntax

            Dim stringToken As SyntaxToken = Nothing
            VerifyExpectedToken(SyntaxKind.StringLiteralToken, stringToken)
            Return SyntaxFactory.StringLiteralExpression(stringToken)
        End Function

        Private Function ParseFltLiteral() As LiteralExpressionSyntax

            Debug.Assert(
            CurrentToken.Kind = SyntaxKind.FloatingLiteralToken,
            "must be at a float literal.")

            Dim Literal As LiteralExpressionSyntax = SyntaxFactory.NumericLiteralExpression(CurrentToken)
            GetNextToken()

            Return Literal
        End Function

        Private Function ParseDateLiteral() As LiteralExpressionSyntax

            Debug.Assert(
            CurrentToken.Kind = SyntaxKind.DateLiteralToken,
            "must be at a date literal.")

            Dim Literal As LiteralExpressionSyntax = SyntaxFactory.DateLiteralExpression(CurrentToken)

            GetNextToken()

            Return Literal
        End Function

    End Class

End Namespace
