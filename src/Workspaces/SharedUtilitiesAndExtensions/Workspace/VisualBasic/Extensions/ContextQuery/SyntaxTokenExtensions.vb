' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

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
        ''' and then get LeadingTrivia for the next non-missing token.
        ''' 
        ''' Note that this is even more complicated in the case that we're in structured trivia
        ''' because we might be part of the leading trivia to the next non-missing token.
        ''' </summary>
        <Extension>
        Friend Function HasNonContinuableEndOfLineBeforePosition(token As SyntaxToken, position As Integer, Optional checkForSecondEol As Boolean = False) As Boolean
            If token.FollowsBadEndDirective() Then
                Return False
            End If

            Dim allowsImplicitLineContinuation = token.Parent IsNot Nothing AndAlso
                                                 SyntaxFacts.AllowsTrailingImplicitLineContinuation(token)

            Dim originalToken = token

            Do
                If CheckTrivia(token.TrailingTrivia, position, checkForSecondEol, allowsImplicitLineContinuation) Then
                    Return True
                End If

                token = token.GetNextToken(includeZeroWidth:=True)
            Loop While token.IsMissing

            ' If our our original token was in structured trivia (such as preprocesser), it's entirely possible that the
            ' leading trivia of the next non-missing token might contain it. If that's the case, we don't want to check
            ' its leading trivia before it might have trivia that appear *before* the original token.
            '
            ' Consider the following example:
            '
            '   Class C
            '
            '     #Region $$
            '   End Class
            '
            ' In the code above, the original token is "Region", but the leading trivia to the next non-missing token ("End")
            ' includes the structured trivia containing the original token plus the trivia before it. In that case, we don't
            ' want to check the leading trivia of the "End".

            If Not token.LeadingTrivia.Span.Contains(originalToken.Span) Then
                Return CheckTrivia(token.LeadingTrivia, position, checkForSecondEol, allowsImplicitLineContinuation)
            Else
                Return False
            End If
        End Function

        <Extension>
        Friend Function FollowsBadEndDirective(targetToken As SyntaxToken) As Boolean
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

        <Extension>
        Friend Function IsMandatoryNamedParameterPosition(token As SyntaxToken) As Boolean
            If token.Kind() = SyntaxKind.CommaToken Then
                Dim argumentList = TryCast(token.Parent, ArgumentListSyntax)
                If argumentList Is Nothing Then
                    Return False
                End If

                For Each n In argumentList.Arguments.GetWithSeparators()
                    If n.IsToken AndAlso n.AsToken() = token Then
                        Return False
                    End If

                    If n.IsNode AndAlso DirectCast(n.AsNode(), ArgumentSyntax).IsNamed Then
                        Return True
                    End If
                Next
            End If

            Return False
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
