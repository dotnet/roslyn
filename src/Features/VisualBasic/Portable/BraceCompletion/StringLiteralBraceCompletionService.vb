﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.BraceCompletion
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.VisualBasic

<Export(LanguageNames.VisualBasic, GetType(IBraceCompletionService)), [Shared]>
Friend Class StringLiteralBraceCompletionService
    Inherits AbstractBraceCompletionService

    <ImportingConstructor>
    <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
    Public Sub New()
        MyBase.New()
    End Sub

    Protected Overrides ReadOnly Property OpeningBrace As Char = DoubleQuote.OpenCharacter

    Protected Overrides ReadOnly Property ClosingBrace As Char = DoubleQuote.CloseCharacter

    Public Overrides Function AllowOverTypeAsync(context As BraceCompletionContext, cancellationToken As CancellationToken) As Task(Of Boolean)
        Return AllowOverTypeWithValidClosingTokenAsync(context, cancellationToken)
    End Function

    Public Overrides Async Function CanProvideBraceCompletionAsync(brace As Char, openingPosition As Integer, document As Document, cancellationToken As CancellationToken) As Task(Of Boolean)
        If (OpeningBrace = brace And Await InterpolatedStringBraceCompletionService.IsPositionInInterpolatedStringContextAsync(document, openingPosition, cancellationToken).ConfigureAwait(False)) Then
            Return False
        End If

        Return Await MyBase.CanProvideBraceCompletionAsync(brace, openingPosition, document, cancellationToken).ConfigureAwait(False)
    End Function

    Protected Overrides Function IsValidOpeningBraceToken(token As SyntaxToken) As Boolean
        Return token.IsKind(SyntaxKind.StringLiteralToken)
    End Function

    Protected Overrides Function IsValidClosingBraceToken(token As SyntaxToken) As Boolean
        Return token.IsKind(SyntaxKind.StringLiteralToken)
    End Function
End Class
