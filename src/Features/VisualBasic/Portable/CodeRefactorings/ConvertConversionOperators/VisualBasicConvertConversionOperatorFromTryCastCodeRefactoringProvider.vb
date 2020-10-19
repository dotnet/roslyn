' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Diagnostics.CodeAnalysis
Imports System.Threading
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.CodeRefactorings.ConvertConversionOperators
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeRefactorings.ConvertConversionOperators
    <ExportCodeRefactoringProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeRefactoringProviderNames.ConvertConversionOperatorsFromTryCastToThrowingCast), [Shared]>
    Friend Class VisualBasicConvertConversionOperatorFromTryCastCodeRefactoringProvider
        Inherits AbstractConvertConversionOperatorsRefactoringProvider(Of TryCastExpressionSyntax)

        <ImportingConstructor>
        <SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification:="Used in test code: https://github.com/dotnet/roslyn/issues/42814")>
        Public Sub New()
        End Sub

        Protected Overrides Function GetTitle() As String
            Return VBFeaturesResources.Change_to_DirectCast
        End Function

        Protected Overrides Function FilterFromExpressionCandidatesAsync(
                fromExpressions As ImmutableArray(Of TryCastExpressionSyntax),
                document As CodeAnalysis.Document,
                cancellationToken As CancellationToken) As Task(Of ImmutableArray(Of TryCastExpressionSyntax))
            Return Task.FromResult(fromExpressions)
        End Function

        Protected Overrides Function ConvertExpression(fromExpression As TryCastExpressionSyntax) As CodeAnalysis.SyntaxNode
            Return SyntaxFactory.DirectCastExpression(fromExpression.Expression, fromExpression.Type)
        End Function
    End Class
End Namespace
