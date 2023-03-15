' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.BraceCompletion
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic

Namespace Microsoft.CodeAnalysis.VisualBasic.BraceCompletion
    <Export(LanguageNames.VisualBasic, GetType(IBraceCompletionService)), [Shared]>
    Friend Class InterpolationBraceCompletionService
        Inherits AbstractVisualBasicBraceCompletionService

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
            MyBase.New()
        End Sub

        Protected Overrides ReadOnly Property OpeningBrace As Char = CurlyBrace.OpenCharacter
        Protected Overrides ReadOnly Property ClosingBrace As Char = CurlyBrace.CloseCharacter

        Protected Overrides Function IsValidOpenBraceTokenAtPosition(text As SourceText, token As SyntaxToken, position As Integer) As Boolean
            Return IsValidOpeningBraceToken(token)
        End Function

        Public Overrides Function AllowOverType(context As BraceCompletionContext, cancellationToken As CancellationToken) As Boolean
            Return AllowOverTypeWithValidClosingToken(context)
        End Function

        Public Overrides Function CanProvideBraceCompletion(brace As Char, openingPosition As Integer, document As ParsedDocument, cancellationToken As CancellationToken) As Boolean
            Return OpeningBrace = brace And IsPositionInInterpolationContext(document, openingPosition)
        End Function

        Protected Overrides Function IsValidOpeningBraceToken(token As SyntaxToken) As Boolean
            Return token.IsKind(SyntaxKind.DollarSignDoubleQuoteToken, SyntaxKind.InterpolatedStringTextToken) OrElse
                   (token.IsKind(SyntaxKind.CloseBraceToken) AndAlso token.Parent.IsKind(SyntaxKind.Interpolation))
        End Function

        Protected Overrides Function IsValidClosingBraceToken(token As SyntaxToken) As Boolean
            Return token.IsKind(SyntaxKind.CloseBraceToken)
        End Function

        Public Shared Function IsPositionInInterpolationContext(document As ParsedDocument, position As Integer) As Boolean
            If position = 0 Then
                Return False
            End If

            ' First, check to see if the character to the left of the position is an open curly.
            ' If it is, we shouldn't complete because the user may be trying to escape a curly.
            ' E.g. they are trying to type $"{{"
            If CouldEscapePreviousOpenBrace("{"c, position, document.Text) Then
                Return False
            End If

            ' Next, check to see if the token we're typing is part of an existing interpolated string.
            '
            Dim token = document.Root.FindTokenOnRightOfPosition(position)

            If Not token.Span.IntersectsWith(position) Then
                Return False
            End If

            Return token.IsKind(SyntaxKind.DollarSignDoubleQuoteToken, SyntaxKind.InterpolatedStringTextToken) OrElse
               (token.IsKind(SyntaxKind.CloseBraceToken) AndAlso token.Parent.IsKind(SyntaxKind.Interpolation))
        End Function
    End Class
End Namespace
