﻿' Licensed to the .NET Foundation under one or more agreements.
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
Friend Class ParenthesisBraceCompletionService
    Inherits AbstractBraceCompletionService

    <ImportingConstructor>
    <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
    Public Sub New()
        MyBase.New()
    End Sub

    Protected Overrides ReadOnly Property OpeningBrace As Char = Parenthesis.OpenCharacter

    Protected Overrides ReadOnly Property ClosingBrace As Char = Parenthesis.CloseCharacter

    Protected Overrides Function IsValidOpeningBraceToken(token As SyntaxToken) As Boolean
        Return token.IsKind(SyntaxKind.OpenParenToken)
    End Function

    Protected Overrides Function IsValidClosingBraceToken(token As SyntaxToken) As Boolean
        Return token.IsKind(SyntaxKind.CloseParenToken)
    End Function

    Protected Overrides Function IsValidOpenBraceTokenAtPositionAsync(token As SyntaxToken, position As Integer, document As Document, cancellationToken As CancellationToken) As Task(Of Boolean)
        If Not IsValidOpeningBraceToken(token) OrElse
               position <> token.SpanStart Then
            Return SpecializedTasks.False
        End If

        Dim skippedTriviaNode = TryCast(token.Parent, SkippedTokensTriviaSyntax)
        If skippedTriviaNode IsNot Nothing Then
            Dim skippedToken = skippedTriviaNode.ParentTrivia.Token
            ' These checks don't make any sense.  Leaving them in place to avoid breaking something as part of this move.
            If skippedToken.Kind <> SyntaxKind.CloseParenToken OrElse Not TypeOf skippedToken.Parent Is BinaryConditionalExpressionSyntax Then
                Return SpecializedTasks.False
            End If
        End If

        Return SpecializedTasks.True
    End Function

    Public Overrides Function AllowOverTypeAsync(context As BraceCompletionContext, cancellationToken As CancellationToken) As Task(Of Boolean)
        Return AllowOverTypeInUserCodeWithValidClosingTokenAsync(context, cancellationToken)
    End Function
End Class
