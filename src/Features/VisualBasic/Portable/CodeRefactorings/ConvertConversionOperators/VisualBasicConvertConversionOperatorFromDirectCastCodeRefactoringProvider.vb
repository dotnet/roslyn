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
    <ExportCodeRefactoringProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeRefactoringProviderNames.ConvertConversionOperatorsFromThrowingCastToTryCast), [Shared]>
    Friend Class VisualBasicConvertConversionOperatorFromDirectCastCodeRefactoringProvider
        Inherits AbstractConvertConversionOperatorsRefactoringProvider(Of DirectCastExpressionSyntax)

        <ImportingConstructor>
        <SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification:="Used in test code: https://github.com/dotnet/roslyn/issues/42814")>
        Public Sub New()
        End Sub

        Protected Overrides Function GetTitle() As String
            Return VBFeaturesResources.Change_to_TryCast
        End Function

        Protected Overrides Async Function FilterFromExpressionCandidatesAsync(
                DirectCastExpressions As ImmutableArray(Of DirectCastExpressionSyntax),
                document As Document,
                cancellationToken As CancellationToken) As Task(Of ImmutableArray(Of DirectCastExpressionSyntax))
            Return Await FilterCastExpressionsOfReferenceTypesAsync(DirectCastExpressions, document, cancellationToken).ConfigureAwait(False)
        End Function

        Protected Overrides Function ConvertExpression(fromExpression As DirectCastExpressionSyntax) As CodeAnalysis.SyntaxNode
            Return SyntaxFactory.TryCastExpression(fromExpression.Expression, fromExpression.Type)
        End Function
    End Class
End Namespace
