' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.ConvertToInterpolatedString
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.ConvertToInterpolatedString
    <ExportCodeRefactoringProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeRefactoringProviderNames.ExtractMethod), [Shared]>
    Friend Class VisualBasicConvertConcatenationToInterpolatedStringRefactoringProvider
        Inherits AbstractConvertConcatenationToInterpolatedStringRefactoringProvider

        Protected Overrides Function CreateInterpolatedString(firstStringToken As SyntaxToken,
                                                              pieces As List(Of SyntaxNode)) As SyntaxNode

            Dim startToken = SyntaxFactory.Token(SyntaxKind.DollarSignDoubleQuoteToken).
                                           WithLeadingTrivia(pieces.First().GetLeadingTrivia())

            Dim endToken = SyntaxFactory.Token(SyntaxKind.DoubleQuoteToken).
                                         WithTrailingTrivia(pieces.Last().GetTrailingTrivia())

            Dim contents As New List(Of InterpolatedStringContentSyntax)
            For Each piece In pieces
                If piece.Kind() = SyntaxKind.StringLiteralExpression Then
                    Dim text = piece.GetFirstToken().Text
                    Dim textWithoutQuotes = text.Substring("'".Length, text.Length - "''".Length)

                    contents.Add(SyntaxFactory.InterpolatedStringText(
                                 SyntaxFactory.InterpolatedStringTextToken(textWithoutQuotes, "")))
                Else
                    contents.Add(SyntaxFactory.Interpolation(
                                 DirectCast(piece, ExpressionSyntax).WithoutTrivia()))
                End If
            Next

            Dim expression = SyntaxFactory.InterpolatedStringExpression(
                startToken, SyntaxFactory.List(contents), endToken)

            Return expression
        End Function
    End Class
End Namespace
