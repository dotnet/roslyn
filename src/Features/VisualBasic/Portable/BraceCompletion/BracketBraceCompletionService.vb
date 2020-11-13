' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.BraceCompletion
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

<Export(LanguageNames.VisualBasic, GetType(IBraceCompletionService)), [Shared]>
Friend Class BracketBraceCompletionService
    Inherits AbstractBraceCompletionService

    <ImportingConstructor>
    <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
    Public Sub New()
        MyBase.New()
    End Sub

    Protected Overrides ReadOnly Property OpeningBrace As Char
        Get
            Return Bracket.OpenCharacter
        End Get
    End Property

    Protected Overrides ReadOnly Property ClosingBrace As Char
        Get
            Return Bracket.CloseCharacter
        End Get
    End Property

    Public Overrides Function AllowOverTypeAsync(context As BraceCompletionContext, cancellationToken As CancellationToken) As Task(Of Boolean)
        Return CheckCurrentPositionAsync(context.Document, context.CaretLocation, cancellationToken)
    End Function

    Protected Overrides Function IsValidOpeningBraceToken(token As SyntaxToken) As Boolean
        Return token.IsKind(SyntaxKind.OpenBraceToken)
    End Function

    Protected Overrides Function IsValidClosingBraceToken(token As SyntaxToken) As Boolean
        Return token.IsKind(SyntaxKind.CloseBraceToken)
    End Function

    Protected Overrides Function IsValidOpenBraceTokenAtPositionAsync(token As SyntaxToken, position As Integer, document As Document, cancellationToken As CancellationToken) As Task(Of Boolean)
        If position = token.SpanStart AndAlso
               token.Kind = SyntaxKind.BadToken AndAlso
               token.ToString() = Bracket.OpenCharacter Then
            Return Task.FromResult(Not IsBracketInCData(token))
        End If

        If position < token.SpanStart Then
            Return SpecializedTasks.False
        End If

        For Each trivia In token.TrailingTrivia
            Dim span = trivia.Span

            If span.End < position Then
                Return SpecializedTasks.False
            ElseIf Not span.IntersectsWith(position) OrElse
                       Not trivia.HasStructure Then
                Continue For
            End If

            If TypeOf trivia.GetStructure() Is SkippedTokensTriviaSyntax Then
                Return SpecializedTasks.True
            End If
        Next

        Return SpecializedTasks.False
    End Function

    Private Shared Function IsBracketInCData(token As SyntaxToken) As Boolean
        Dim skippedToken = TryCast(token.Parent, SkippedTokensTriviaSyntax)
        If skippedToken Is Nothing Then
            Return False
        End If

        Return skippedToken.ParentTrivia.Token.Kind = SyntaxKind.GreaterThanToken
    End Function
End Class
