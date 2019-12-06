' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.SimplifyInterpolation

Namespace Microsoft.CodeAnalysis.VisualBasic.SimplifyInterpolation
    <ExportCodeFixProvider(LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicSimplifyInterpolationCodeFixProvider
        Inherits AbstractSimplifyInterpolationCodeFixProvider(Of
            InterpolationSyntax, ExpressionSyntax, InterpolationAlignmentClauseSyntax, InterpolationFormatClauseSyntax)

        Protected Overrides Function WithExpression(interpolation As InterpolationSyntax, expression As ExpressionSyntax) As InterpolationSyntax
            Return interpolation.WithExpression(expression)
        End Function

        Protected Overrides Function WithAlignmentClause(interpolation As InterpolationSyntax, alignmentClause As InterpolationAlignmentClauseSyntax) As InterpolationSyntax
            Return interpolation.WithAlignmentClause(alignmentClause)
        End Function

        Protected Overrides Function WithFormatClause(interpolation As InterpolationSyntax, formatClause As InterpolationFormatClauseSyntax) As InterpolationSyntax
            Return interpolation.WithFormatClause(formatClause)
        End Function
    End Class
End Namespace
