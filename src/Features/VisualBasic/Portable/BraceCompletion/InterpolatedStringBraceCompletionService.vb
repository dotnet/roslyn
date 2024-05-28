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
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery

Namespace Microsoft.CodeAnalysis.VisualBasic.BraceCompletion
    <ExportBraceCompletionService(LanguageNames.VisualBasic), [Shared]>
    Friend Class InterpolatedStringBraceCompletionService
        Inherits AbstractVisualBasicBraceCompletionService

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
            MyBase.New()
        End Sub

        Protected Overrides ReadOnly Property OpeningBrace As Char = DoubleQuote.OpenCharacter
        Protected Overrides ReadOnly Property ClosingBrace As Char = DoubleQuote.CloseCharacter

        Protected Overrides Function IsValidOpenBraceTokenAtPosition(text As SourceText, token As SyntaxToken, position As Integer) As Boolean
            Return IsValidOpeningBraceToken(token) AndAlso token.Span.End - 1 = position
        End Function

        Public Overrides Function AllowOverType(context As BraceCompletionContext, cancellationToken As CancellationToken) As Boolean
            Return AllowOverTypeWithValidClosingToken(context)
        End Function

        Public Overrides Function CanProvideBraceCompletion(brace As Char, openingPosition As Integer, document As ParsedDocument, cancellationToken As CancellationToken) As Boolean
            Return OpeningBrace = brace And IsPositionInInterpolatedStringContext(document, openingPosition)
        End Function

        Protected Overrides Function IsValidOpeningBraceToken(token As SyntaxToken) As Boolean
            Return token.IsKind(SyntaxKind.DollarSignDoubleQuoteToken)
        End Function

        Protected Overrides Function IsValidClosingBraceToken(token As SyntaxToken) As Boolean
            Return token.IsKind(SyntaxKind.DoubleQuoteToken)
        End Function

        Public Shared Function IsPositionInInterpolatedStringContext(document As ParsedDocument, position As Integer) As Boolean
            If position = 0 Then
                Return False
            End If

            ' Position can be in an interpolated string if the preceding character is a $
            Return document.Text(position - 1) = "$"c
        End Function
    End Class
End Namespace
