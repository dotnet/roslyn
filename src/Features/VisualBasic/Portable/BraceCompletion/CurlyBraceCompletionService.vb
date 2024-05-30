' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.BraceCompletion
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.VisualBasic

Namespace Microsoft.CodeAnalysis.VisualBasic.BraceCompletion
    <ExportBraceCompletionService(LanguageNames.VisualBasic), [Shared]>
    Friend Class CurlyBraceCompletionService
        Inherits AbstractVisualBasicBraceCompletionService

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
            MyBase.New()
        End Sub

        Protected Overrides ReadOnly Property OpeningBrace As Char = CurlyBrace.OpenCharacter
        Protected Overrides ReadOnly Property ClosingBrace As Char = CurlyBrace.CloseCharacter

        Public Overrides Function AllowOverType(context As BraceCompletionContext, cancellationToken As CancellationToken) As Boolean
            Return AllowOverTypeInUserCodeWithValidClosingToken(context, cancellationToken)
        End Function

        Public Overrides Function CanProvideBraceCompletion(brace As Char, openingPosition As Integer, document As ParsedDocument, cancellationToken As CancellationToken) As Boolean
            If OpeningBrace = brace And InterpolationBraceCompletionService.IsPositionInInterpolationContext(document, openingPosition) Then
                Return False
            End If

            Return MyBase.CanProvideBraceCompletion(brace, openingPosition, document, cancellationToken)
        End Function

        Protected Overrides Function IsValidOpeningBraceToken(token As SyntaxToken) As Boolean
            Return token.IsKind(SyntaxKind.OpenBraceToken)
        End Function

        Protected Overrides Function IsValidClosingBraceToken(token As SyntaxToken) As Boolean
            Return token.IsKind(SyntaxKind.CloseBraceToken)
        End Function
    End Class
End Namespace
