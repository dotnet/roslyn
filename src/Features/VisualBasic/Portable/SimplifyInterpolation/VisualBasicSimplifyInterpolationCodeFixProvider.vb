' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.SimplifyInterpolation

Namespace Microsoft.CodeAnalysis.VisualBasic.SimplifyInterpolation
    <ExportCodeFixProvider(LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicSimplifyInterpolationCodeFixProvider
        Inherits AbstractSimplifyInterpolationCodeFixProvider(Of
            InterpolationSyntax, ExpressionSyntax)

        Protected Overrides Function Update(interpolation As InterpolationSyntax, unwrapped As ExpressionSyntax, alignment As ExpressionSyntax, formatString As String) As InterpolationSyntax
            Dim result = interpolation.WithExpression(unwrapped)
            If alignment IsNot Nothing Then
                result = result.WithAlignmentClause(
                    SyntaxFactory.InterpolationAlignmentClause(SyntaxFactory.Token(SyntaxKind.CommaToken), alignment))
            End If

            If formatString IsNot Nothing Then
                If formatString = "" Then
                    result = result.WithFormatClause(Nothing)
                Else
                    result = result.WithFormatClause(SyntaxFactory.InterpolationFormatClause(
                        SyntaxFactory.Token(SyntaxKind.ColonToken),
                        SyntaxFactory.InterpolatedStringTextToken(formatString, formatString)))
                End If
            End If

            Return result
        End Function
    End Class
End Namespace
