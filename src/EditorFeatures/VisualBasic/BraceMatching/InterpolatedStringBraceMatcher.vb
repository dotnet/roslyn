﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.ComponentModel.Composition
Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.BraceMatching

    <ExportBraceMatcher(LanguageNames.VisualBasic)>
    Friend Class InterpolatedStringBraceMatcher
        Implements IBraceMatcher

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Public Async Function FindBraces(
            document As Document,
            position As Integer,
            Optional cancellationToken As CancellationToken = Nothing
        ) As Task(Of BraceMatchingResult?) Implements IBraceMatcher.FindBracesAsync

            Dim root = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
            Dim token = root.FindToken(position)

            If token.IsKind(SyntaxKind.DollarSignDoubleQuoteToken, SyntaxKind.DoubleQuoteToken) AndAlso
               token.Parent.IsKind(SyntaxKind.InterpolatedStringExpression) Then

                Dim interpolatedString = DirectCast(token.Parent, InterpolatedStringExpressionSyntax)

                Return New BraceMatchingResult(
                    New TextSpan(interpolatedString.DollarSignDoubleQuoteToken.SpanStart, 2),
                    New TextSpan(interpolatedString.DoubleQuoteToken.Span.End - 1, 1))
            End If

            Return Nothing
        End Function
    End Class

End Namespace
