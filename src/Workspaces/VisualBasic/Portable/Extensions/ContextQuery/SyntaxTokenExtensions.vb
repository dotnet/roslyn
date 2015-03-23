' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery
    Friend Module SyntaxTokenExtensions

        <Extension>
        Friend Function HasColonBeforePosition(token As SyntaxToken, position As Integer) As Boolean
            Do
                If token.TrailingTrivia.Any(Function(t) t.IsKind(SyntaxKind.ColonTrivia) AndAlso t.Span.End <= position) Then
                    Return True
                End If

                token = token.GetNextToken(includeZeroWidth:=True)
            Loop While token.IsMissing

            Return False
        End Function

        Private Function CheckTrivia(triviaList As SyntaxTriviaList, position As Integer, ByRef checkForSecondEol As Boolean, ByRef allowsImplicitLineContinuation As Boolean) As Boolean
            For Each trivia In triviaList
                If trivia.IsKind(SyntaxKind.LineContinuationTrivia) AndAlso trivia.Span.End <= position Then
                    checkForSecondEol = True
                    allowsImplicitLineContinuation = False
                ElseIf trivia.IsKind(SyntaxKind.EndOfLineTrivia) AndAlso trivia.Span.End <= position Then
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
            If token.FollowsBadEndDirective(position) Then
                Return False
            End If

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
        Friend Function FollowsBadEndDirective(targetToken As SyntaxToken, position As Integer) As Boolean
            If targetToken.IsKind(SyntaxKind.HashToken) AndAlso targetToken.TrailingTrivia.Any(Function(t)
                                                                                                   If t.HasStructure Then
                                                                                                       Dim childTokens = t.GetStructure().ChildTokens()
                                                                                                       Return childTokens.Count() = 1 AndAlso childTokens.First().IsKind(SyntaxKind.EndKeyword)
                                                                                                   End If

                                                                                                   Return False
                                                                                               End Function) Then
                Return targetToken.Parent.IsKind(SyntaxKind.BadDirectiveTrivia)
            End If

            Return targetToken.IsKind(SyntaxKind.EndKeyword) AndAlso
               targetToken.GetPreviousToken().IsKind(SyntaxKind.HashToken) AndAlso
               targetToken.GetPreviousToken().Parent.IsKind(SyntaxKind.BadDirectiveTrivia)
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

        <Extension()>
        Friend Function IsModifier(token As SyntaxToken) As Boolean
            Select Case token.Kind
                Case SyntaxKind.AsyncKeyword,
                     SyntaxKind.ConstKeyword,
                     SyntaxKind.DefaultKeyword,
                     SyntaxKind.PublicKeyword,
                     SyntaxKind.FriendKeyword,
                     SyntaxKind.ShadowsKeyword,
                     SyntaxKind.MustOverrideKeyword,
                     SyntaxKind.MustInheritKeyword,
                     SyntaxKind.PrivateKeyword,
                     SyntaxKind.NarrowingKeyword,
                     SyntaxKind.WideningKeyword,
                     SyntaxKind.NotInheritableKeyword,
                     SyntaxKind.NotOverridableKeyword,
                     SyntaxKind.OverloadsKeyword,
                     SyntaxKind.OverridableKeyword,
                     SyntaxKind.OverridesKeyword,
                     SyntaxKind.PartialKeyword,
                     SyntaxKind.ProtectedKeyword,
                     SyntaxKind.ReadOnlyKeyword,
                     SyntaxKind.WriteOnlyKeyword,
                     SyntaxKind.SharedKeyword,
                     SyntaxKind.WithEventsKeyword,
                     SyntaxKind.CustomKeyword,
                     SyntaxKind.IteratorKeyword
                    Return True
                Case Else
                    Return False
            End Select
        End Function
    End Module
End Namespace
