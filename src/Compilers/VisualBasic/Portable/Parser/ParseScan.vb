' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports System
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.Syntax.InternalSyntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax
    ' //
    ' //============ Methods to encapsulate scanning ========================
    ' //

    Partial Friend Class Parser

        ' File: Scanner.h
        ' Lines: 301 - 301
        ' Class: Token
        ' bool .::IsContinuableEOL( )
        Private Function IsContinuableEOL(Optional i As Integer = 0) As Boolean
            'TODO - This applies ot both IsContinuableEOL and PeekPastStatementTerminator and any code that peeks through an EOL.
            ' The scanner scans the fist token of a statement differently with regard to trivia.  It we peek past the EOL and then get the token.
            ' The token may not have the correct trivia attached to it when we get it.  The solution is to attach the state to the token so we
            ' know if we peeked the token in the first token of new statement state or the next token of a statement state.

            If PeekToken(i).Kind = SyntaxKind.StatementTerminatorToken Then
                If PeekToken(i + 1).Kind <> SyntaxKind.EmptyToken Then
                    Return True
                End If
            End If
            Return False
        End Function

        'TODO - This is really peekToken skipping optional statementterminator
        Private Function PeekPastStatementTerminator() As SyntaxToken
            Dim t1 = PeekToken(1)
            If t1.Kind = SyntaxKind.StatementTerminatorToken Then
                Dim t2 = PeekToken(2)
                If t2.Kind <> SyntaxKind.EmptyToken Then
                    Return t2
                End If
            End If
            Return t1
        End Function

        ' Is this token a valid end of statement?

        '// Parser::IsValidStatementTerminator: Is this token a valid "StatementTerminator"?
        '// NOTE: a compound statement e.g. "If b then : S1 : end if" has StatementTerminators inside it
        '// This function is used e.g. in parsing "If b Then <token>" to determine whether it's a line-if or a block-if.
        '// The statement terminators are newline, colon, comment. (Also due to a quirk in the way things flow together,
        '// "else" is considered a statement terminator if we're in a line-if.)

        ' File: Parser.cpp
        ' Lines: 18421 - 18421
        ' bool .Parser::IsValidStatementTerminator( [ _In_ Token* T ] )

        Friend Function IsValidStatementTerminator(t As SyntaxToken) As Boolean
            Debug.Assert(t IsNot Nothing)

            ' // The parser usually skips StatementTerminator tokens (if implicit line continuation is
            ' // enabled), and it isn't possible to look back past the first token of a
            ' // line, so test if this is the first token of the last read line.

            ' REM and XmlDocComment are now trivia so they have been removed from the test
            Return SyntaxFacts.IsTerminator(t.Kind)
        End Function

        ' // Parser::CanFollowStatement -- Can this token follow a complete statement?
        ' // NOTE: e.g. in "Dim x = Sub() S, y=3", if we're within a statement lambda, then the token tkCOMMA
        ' // can follow a complete statement
        ' // This function is used e.g. in parsing "End <token>" or "Call f() <token>" to determine whether
        ' // the token is part of the construct, or may be part of the following construct, or is just an error.

        ' bool .Parser::CanFollowStatement( [ _In_ Token* T ] )
        Private Function CanFollowStatement(T As SyntaxToken) As Boolean
            ' // Dev10#670492: in a single-line sub, e.g. "Dim x = (Sub() Stmt)", things like RParen are happy to end the statement
            Return If(Context.IsWithinSingleLineLambda, CanFollowExpression(T), IsValidStatementTerminator(T)) OrElse
                T.Kind = SyntaxKind.ElseKeyword
        End Function

        Friend Function CanFollowStatementButIsNotSelectFollowingExpression(nextToken As SyntaxToken) As Boolean
            ' Dev10#708061: Treat Select in "From x in Sub() End Select" as part of End statement.
            Return If(Context.IsWithinSingleLineLambda,
                      CanFollowExpression(nextToken) AndAlso Not nextToken.Kind = SyntaxKind.SelectKeyword,
                      IsValidStatementTerminator(nextToken)) OrElse (Context.IsLineIf AndAlso nextToken.Kind = SyntaxKind.ElseKeyword)
        End Function

        ' Is this token a valid end of executable statement?

        '// Parser::CanEndExecutableStatement -- Can this token follow a complete "executable" statement?
        '// WARNING: This function is *only* used when parsing a Call statement, to judge whether the tokens that follow
        '// it should produce an "obsolete: arguments must be enclosed in parentheses" message. It shouldn't be
        '// used by anything else.
        '// It's only difference from "CanFollowStatement" is that it returns true when given tkELSE: for all other
        '// inputs it's identical.

        ' File: Parser.cpp
        ' Lines: 18441 - 18441
        ' bool .Parser::CanEndExecutableStatement( [ _In_ Token* T ] )

        Private Function CanEndExecutableStatement(t As SyntaxToken) As Boolean
            Return CanFollowStatement(t) OrElse t.Kind = SyntaxKind.ElseKeyword
        End Function

        ' // Parser::CanFollowExpression -- Can this token follow a complete expression??
        ' // NOTE: a statement can end with an expression. Therefore, the set denoted by CanFollowExpression
        ' // is not smaller than that denoted by CanFollowStatement. (actually, the two sets are equal
        ' // if we happen to be parsing the statement body of a single-line sub lambda).

        ' bool .Parser::CanFollowExpression( [ _In_ Token* T ] )
        Private Function CanFollowExpression(t As SyntaxToken) As Boolean
            ' // e.g. "Aggregate" can end an expression
            Dim kind As SyntaxKind = Nothing

            If t.Kind = SyntaxKind.IdentifierToken AndAlso TryIdentifierAsContextualKeyword(t, kind) Then
                Return KeywordTable.CanFollowExpression(kind)
            End If
            Return KeywordTable.CanFollowExpression(t.Kind) OrElse IsValidStatementTerminator(t)
        End Function

        ' File: Parser.cpp
        ' Lines: 18561 - 18561

        'Token* // Returns the token following tkOF, or NULL if we aren't looking at generic type syntax 
        'Parser::BeginsGeneric // A generic is signified by '(' [tkStatementTerminator] tkOF

        Private Function BeginsGeneric(Optional nonArrayName As Boolean = False, Optional allowGenericsWithoutOf As Boolean = False) As Boolean

            If CurrentToken.Kind = SyntaxKind.OpenParenToken Then

                If nonArrayName Then
                    Return True
                End If

                Dim t = PeekPastStatementTerminator()

                If t.Kind = SyntaxKind.OfKeyword Then
                    Return True
                ElseIf allowGenericsWithoutOf Then
                    ' // To enable a better user experience in some common generics'
                    ' // error scenarios, we special case goo(Integer) and
                    ' // goo(Integer, garbage).
                    ' //
                    ' // "(Integer" indicates possibly type parameters with missing "of",
                    ' // but not "(Integer." and "Integer!" because they could possibly
                    ' // imply qualified names or expressions. Also note that "Integer :="
                    ' // could imply named arguments. Here "Integer" is just an example,
                    ' // it could be any intrinsic type.
                    ' //
                    If SyntaxFacts.IsPredefinedTypeOrVariant(t.Kind) Then
                        Select Case PeekToken(2).Kind
                            Case SyntaxKind.CloseParenToken, SyntaxKind.CommaToken
                                Return True
                        End Select
                    End If
                End If

            End If

            Return False
        End Function

        ' Does this token force the end of a statement?

        ' File: Parser.cpp
        ' Lines: 18635 - 18635
        ' bool .Parser::MustEndStatement( [ _In_ Token* T ] )

        Private Function MustEndStatement(t As SyntaxToken) As Boolean
            Debug.Assert(t IsNot Nothing)
            Return IsValidStatementTerminator(t)
        End Function

        ' /*********************************************************************
        ' *
        ' * Function:
        ' *     Parser::PeekAheadFor
        ' *
        ' * Purpose:
        ' *     Peek ahead the statement for the requested tokens. Return the
        ' *     token that was found. If the requested tokens are not encountered,
        ' *     return tkNone.
        ' *
        ' *     This routine does not consume the tokens. The current token is not
        ' *     advanced. To consume tokens, use ResyncAt().
        ' *
        ' **********************************************************************/

        ' // [in] count of tokens to look for
        ' // [in] var_list of tokens (tk[]) to look for

        ' File: Parser.cpp
        ' Lines: 18847 - 18847
        ' tokens __cdecl .Parser::PeekAheadFor( [ unsigned Count ] [ ...  ] )

        Private Function PeekAheadFor(ParamArray kinds As SyntaxKind()) As SyntaxKind
            Dim token As SyntaxToken = Nothing
            PeekAheadFor(predicate:=s_isTokenOrKeywordFunc, arg:=kinds, token:=token)
            Return If(token Is Nothing, Nothing, token.Kind)
        End Function

        Private Function PeekAheadFor(Of TArg)(predicate As Func(Of SyntaxToken, TArg, Boolean), arg As TArg, <Out()> ByRef token As SyntaxToken) As Integer
            Dim nextToken = CurrentToken

            Dim i As Integer = 0
            While Not IsValidStatementTerminator(nextToken)
                If predicate(nextToken, arg) Then
                    token = nextToken
                    Return i
                End If

                i += 1
                nextToken = PeekToken(i)
            End While

            token = Nothing
            Return 0
        End Function

        ' /*********************************************************************
        ' *
        ' * Function:
        ' *     Parser::ResyncAt
        ' *
        ' * Purpose:
        ' *     Consume tokens on the line until we encounter a token in the
        ' *     given list or EOS.
        ' *
        ' **********************************************************************/

        ' // [in] count of tokens to look for
        ' // [in] var_list of tokens to look for

        ' File: Parser.cpp
        ' Lines: 18939 - 18939
        ' tokens __cdecl .Parser::ResyncAt( [ unsigned Count ] [ ...  ] )

        ' TODO: note that queries sometimes use very large lists of resync tokens.
        ' linear search may not make much sense. Perhaps could use lazy-inited hashtables?
        Private Sub ResyncAt(skippedTokens As SyntaxListBuilder(Of SyntaxToken), state As ScannerState, resyncTokens As SyntaxKind())
            Debug.Assert(resyncTokens IsNot Nothing)

            While CurrentToken.Kind <> SyntaxKind.EndOfFileToken

                If state.IsVBState Then
                    If IsValidStatementTerminator(CurrentToken) OrElse CurrentToken.Kind = SyntaxKind.EmptyToken Then
                        Exit While
                    End If
                ElseIf CurrentToken.Kind = SyntaxKind.EndOfXmlToken OrElse CurrentToken.Kind = SyntaxKind.EndOfInterpolatedStringToken Then
                    Exit While
                End If

                If IsTokenOrKeyword(CurrentToken, resyncTokens) Then
                    Exit While
                End If

                skippedTokens.Add(CurrentToken)
                GetNextToken(state)
            End While
        End Sub

        Private Function ResyncAt(state As ScannerState, resyncTokens As SyntaxKind()) As CodeAnalysis.Syntax.InternalSyntax.SyntaxList(Of SyntaxToken)
            Dim skippedTokens = Me._pool.Allocate(Of SyntaxToken)()

            ResyncAt(skippedTokens, state, resyncTokens)

            Dim result = skippedTokens.ToList()
            Me._pool.Free(skippedTokens)

            Return result
        End Function

        ''' <summary>
        ''' Resyncs to next statement terminator. Used in Preprocessor
        ''' </summary>
        Private Function ResyncAndConsumeStatementTerminator() As CodeAnalysis.Syntax.InternalSyntax.SyntaxList(Of SyntaxToken)
            Dim skippedTokens = Me._pool.Allocate(Of SyntaxToken)()

            While CurrentToken.Kind <> SyntaxKind.EndOfFileToken AndAlso
                    CurrentToken.Kind <> SyntaxKind.StatementTerminatorToken

                skippedTokens.Add(CurrentToken)
                GetNextToken(ScannerState.VB)
            End While

            If CurrentToken.Kind = SyntaxKind.StatementTerminatorToken Then
                If CurrentToken.HasLeadingTrivia Then
                    skippedTokens.Add(CurrentToken)
                End If

                GetNextToken(ScannerState.VB)
            End If

            Dim result = skippedTokens.ToList()
            Me._pool.Free(skippedTokens)

            Return result
        End Function

        Friend Function ResyncAt() As CodeAnalysis.Syntax.InternalSyntax.SyntaxList(Of SyntaxToken)
            Return ResyncAt(ScannerState.VB, Array.Empty(Of SyntaxKind))
        End Function

        Friend Function ResyncAt(resyncTokens As SyntaxKind()) As CodeAnalysis.Syntax.InternalSyntax.SyntaxList(Of SyntaxToken)
            Debug.Assert(resyncTokens IsNot Nothing)
            Return ResyncAt(ScannerState.VB, resyncTokens)
        End Function

        Private Function ResyncAt(Of T As VisualBasicSyntaxNode)(syntax As T) As T
            Return syntax.AddTrailingSyntax(ResyncAt())
        End Function

        Private Function ResyncAt(Of T As GreenNode)(syntax As T, ParamArray resyncTokens As SyntaxKind()) As T
            Debug.Assert(resyncTokens IsNot Nothing)

            Return syntax.AddTrailingSyntax(ResyncAt(resyncTokens).Node)
        End Function

        ''' <summary>
        ''' If the current token is a newline statement terminator, then consume the token.
        ''' </summary>
        ''' <returns>True if the statement terminator was consumed</returns>
        Private Function TryEatNewLine(Optional state As ScannerState = ScannerState.VB) As Boolean

            '// Don't eat EOL's while evaluating conditional compilation constants. 

            If CurrentToken.Kind = SyntaxKind.StatementTerminatorToken AndAlso
                PeekEndStatement(1) = SyntaxKind.None AndAlso
                Not _evaluatingConditionCompilationExpression Then

                If Not NextLineStartsWithStatementTerminator() Then
                    _hadImplicitLineContinuation = True

                    If PrevToken.GetTrailingTrivia().ContainsCommentTrivia() Then
                        _hadLineContinuationComment = True
                    End If

                    GetNextToken(state)
                    Return True
                End If

            End If

            Return False
        End Function

        ' /*****************************************************************************************
        '  ;EatNewLineIfFollowedBy
        ' 
        '  If the current token is an EOL token, eat the EOL on the condition that the next line
        '  starts with the specified token
        ' ******************************************************************************************/
        ' the token we want to check for on start of the next line
        ' [in] whether the tokenType represents a query operator token
        ' .Parser::EatNewLineIfFollowedBy( [ tokens tokenType ] [ ParseTree::LineContinuationState& continuationState ] [ bool isQueryOp ] )

        Private Function TryEatNewLineIfFollowedBy(kind As SyntaxKind) As Boolean
            Debug.Assert(CanUseInTryGetToken(kind))

            If NextLineStartsWith(kind) Then
                'Add trivia to the token that has been peeked on next line
                Return TryEatNewLine()
            End If
            Return False
        End Function

        ' /*****************************************************************************************
        '  ;EatNewLineIfNotFollowedBy
        ' 
        '  If the current token is an EOL token, eat the EOL on the condition that the next line
        '  does not start with the specified token.
        ' ******************************************************************************************/
        ' the token we want to check for on start of the next line
        ' [in] whether the tokenType represents a query operator token
        ' .Parser::EatNewLineIfNotFollowedBy( [ tokens tokenType ] [ ParseTree::LineContinuationState& continuationState ] [ bool isQueryOp ] )
        Private Function TryEatNewLineIfNotFollowedBy(kind As SyntaxKind) As Boolean
            Debug.Assert(CanUseInTryGetToken(kind))

            If Not NextLineStartsWith(kind) Then
                Return TryEatNewLine()
            End If
            Return False
        End Function

        ' /*****************************************************************************************
        '  ;NextLineStartsWith
        ' 
        '  Determine if the next line starts with the specified token.
        ' ******************************************************************************************/
        ' the token we want to check for on start of the next line
        ' [out] we will update the HasExtraNewLine flag
        ' [in] whether the tokenType represents a query operator token
        ' bool .Parser::NextLineStartsWith( [ tokens tokenType ] [ ParseTree::LineContinuationState& continuationState ] [ bool isQueryOp ] )
        Private Function NextLineStartsWith(kind As SyntaxKind) As Boolean
            Debug.Assert(CanUseInTryGetToken(kind))

            If CurrentToken.IsEndOfLine Then
                Dim nextToken = PeekToken(1)

                If nextToken.Kind = kind Then
                    Return True

                ElseIf nextToken.Kind = SyntaxKind.IdentifierToken Then
                    Dim contextualKind As SyntaxKind = Nothing
                    If TryIdentifierAsContextualKeyword(nextToken, contextualKind) AndAlso contextualKind = kind Then
                        Return True
                    End If
                End If
            End If

            Return False
        End Function

        Private Function NextLineStartsWithStatementTerminator(Optional offset As Integer = 0) As Boolean
            Debug.Assert(If(offset = 0, CurrentToken, PeekToken(offset)).IsEndOfLine)

            Dim kind = PeekToken(offset + 1).Kind
            Return kind = SyntaxKind.EmptyToken OrElse kind = SyntaxKind.EndOfFileToken
        End Function

        Private Shared Function CanUseInTryGetToken(kind As SyntaxKind) As Boolean
            Return Not SyntaxFacts.IsTerminator(kind) AndAlso kind <> SyntaxKind.EmptyToken
        End Function

    End Class

End Namespace
