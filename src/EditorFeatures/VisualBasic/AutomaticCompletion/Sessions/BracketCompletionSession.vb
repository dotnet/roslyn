' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.Implementation.AutomaticCompletion
Imports Microsoft.CodeAnalysis.Editor.Implementation.AutomaticCompletion.Sessions
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.VisualStudio.Text.BraceCompletion

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.AutomaticCompletion.Sessions
    Friend Class BracketCompletionSession
        Inherits AbstractTokenBraceCompletionSession

        Public Sub New(syntaxFactsService As ISyntaxFactsService)
            MyBase.New(syntaxFactsService, SyntaxKind.OpenBraceToken, SyntaxKind.CloseBraceToken)
        End Sub

        Public Overrides Function CheckOpeningPoint(session As IBraceCompletionSession, cancellationToken As CancellationToken) As Boolean
            Dim snapshot = session.SubjectBuffer.CurrentSnapshot
            Dim position = session.OpeningPoint.GetPosition(snapshot)
            Dim token = snapshot.FindToken(position, cancellationToken)

            If position = token.SpanStart AndAlso
               token.Kind = SyntaxKind.BadToken AndAlso
               token.ToString() = BraceCompletionSessionProvider.Bracket.OpenCharacter Then
                Return Not IsBracketInCData(token)
            End If

            If position < token.SpanStart Then
                Return False
            End If

            For Each trivia In token.TrailingTrivia
                Dim span = trivia.Span

                If span.End < position Then
                    Return False
                ElseIf Not span.IntersectsWith(position) OrElse
                       Not trivia.HasStructure Then
                    Continue For
                End If

                If TypeOf trivia.GetStructure() Is SkippedTokensTriviaSyntax Then
                    Return True
                End If
            Next

            Return False
        End Function

        Public Overrides Function AllowOverType(session As IBraceCompletionSession, cancellationToken As CancellationToken) As Boolean
            Return CheckCurrentPosition(session, cancellationToken)
        End Function

        Private Shared Function IsBracketInCData(token As SyntaxToken) As Boolean
            Dim skippedToken = TryCast(token.Parent, SkippedTokensTriviaSyntax)
            If skippedToken Is Nothing Then
                Return False
            End If

            Return skippedToken.ParentTrivia.Token.Kind = SyntaxKind.GreaterThanToken
        End Function
    End Class
End Namespace
