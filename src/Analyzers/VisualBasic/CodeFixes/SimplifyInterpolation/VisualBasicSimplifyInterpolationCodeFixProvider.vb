' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports System.Diagnostics.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.SimplifyInterpolation
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.SimplifyInterpolation
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.SimplifyInterpolation), [Shared]>
    Friend Class VisualBasicSimplifyInterpolationCodeFixProvider
        Inherits AbstractSimplifyInterpolationCodeFixProvider(Of
            InterpolationSyntax, ExpressionSyntax, InterpolationAlignmentClauseSyntax,
            InterpolationFormatClauseSyntax, InterpolatedStringExpressionSyntax)

        <ImportingConstructor>
        <SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification:="Used in test code: https://github.com/dotnet/roslyn/issues/42814")>
        Public Sub New()
        End Sub

        Protected Overrides Function GetHelpers() As AbstractSimplifyInterpolationHelpers
            Return VisualBasicSimplifyInterpolationHelpers.Instance
        End Function

        Protected Overrides Function WithExpression(interpolation As InterpolationSyntax, expression As ExpressionSyntax) As InterpolationSyntax
            Return interpolation.WithExpression(expression)
        End Function

        Protected Overrides Function WithAlignmentClause(interpolation As InterpolationSyntax, alignmentClause As InterpolationAlignmentClauseSyntax) As InterpolationSyntax
            Return interpolation.WithAlignmentClause(alignmentClause)
        End Function

        Protected Overrides Function WithFormatClause(interpolation As InterpolationSyntax, formatClause As InterpolationFormatClauseSyntax) As InterpolationSyntax
            Return interpolation.WithFormatClause(formatClause)
        End Function

        Protected Overrides Function Escape(interpolatedString As InterpolatedStringExpressionSyntax, formatString As String) As String
            Return formatString.Replace("""", """""")
        End Function
    End Class
End Namespace
