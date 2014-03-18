' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery
    Module SyntaxTokenExtensions

        <Extension>
        Friend Function HasColonBeforePosition(token As SyntaxToken, position As Integer) As Boolean
            Do
                If token.TrailingTrivia.Any(Function(t) t.MatchesKind(SyntaxKind.ColonTrivia) AndAlso t.Span.End <= position) Then
                    Return True
                End If

                token = token.GetNextToken(includeZeroWidth:=True)
            Loop While token.IsMissing

            Return False
        End Function

        Private Function CheckTrivia(triviaList As SyntaxTriviaList, position As Integer, ByRef checkForSecondEol As Boolean, ByRef allowsImplicitLineContinuation As Boolean) As Boolean
            For Each trivia In triviaList
                If trivia.MatchesKind(SyntaxKind.LineContinuationTrivia) AndAlso trivia.Span.End <= position Then
                    checkForSecondEol = True
                    allowsImplicitLineContinuation = False
                ElseIf trivia.MatchesKind(SyntaxKind.EndOfLineTrivia) AndAlso trivia.Span.End <= position Then
                    If Not allowsImplicitLineContinuation Then
                        If checkForSecondEol Then
                            checkForSecondEol = False
                        Else
                            Return True
                        End If
                    End If

                    allowsImplicitLineContinuation = False
                End If
            Next

            Return False
        End Function

        ''' <summary>
        ''' We need to check for EOL trivia not preceded by LineContinuation trivia.
        ''' 
        ''' This is slightly complicated since we need to get TrailingTrivia from missing tokens 
        ''' and then get LeadingTrivia for the next non-missing token
        ''' </summary>
        <Extension>
        Friend Function HasNonContinuableEndOfLineBeforePosition(token As SyntaxToken, position As Integer, Optional checkForSecondEol As Boolean = False) As Boolean
            Dim allowsImplicitLineContinuation = token.Parent IsNot Nothing AndAlso
                                                 SyntaxFacts.AllowsTrailingImplicitLineContinuation(token)

            Do
                If CheckTrivia(token.TrailingTrivia, position, checkForSecondEol, allowsImplicitLineContinuation) Then
                    Return True
                End If

                token = token.GetNextToken(includeZeroWidth:=True)
            Loop While token.IsMissing

            Return CheckTrivia(token.LeadingTrivia, position, checkForSecondEol, allowsImplicitLineContinuation)
        End Function

        <Extension>
        Friend Function FollowsEndOfStatement(token As SyntaxToken, position As Integer) As Boolean
            Return token.HasColonBeforePosition(position) OrElse
                   token.HasNonContinuableEndOfLineBeforePosition(position)
        End Function

        <Extension>
        Friend Function MustBeginNewStatement(token As SyntaxToken, position As Integer) As Boolean
            Return token.HasColonBeforePosition(position) OrElse
                   token.HasNonContinuableEndOfLineBeforePosition(position, checkForSecondEol:=True)
        End Function

    End Module
End Namespace