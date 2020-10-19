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
    <ExportCodeRefactoringProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeRefactoringProviderNames.ConvertConversionOperators), [Shared]>
    Friend Class VisualBasicConvertConversionOperatorFromCTypeCodeRefactoringProvider
        Inherits AbstractConvertConversionOperatorsRefactoringProvider(Of CTypeExpressionSyntax)

        <ImportingConstructor>
        <SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification:="Used in test code: https://github.com/dotnet/roslyn/issues/42814")>
        Public Sub New()
        End Sub

        Protected Overrides Function GetTitle() As String
            Return VBFeaturesResources.Change_to_TryCast
        End Function

        Protected Overrides Async Function FilterFromExpressionCandidatesAsync(
                cTypeExpressions As ImmutableArray(Of CTypeExpressionSyntax),
                document As Document,
                cancellationToken As CancellationToken) As Task(Of ImmutableArray(Of CTypeExpressionSyntax))
            If cTypeExpressions.IsEmpty Then
                Return cTypeExpressions
            End If

            Dim semanticModel = Await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(False)

            Return cTypeExpressions.WhereAsArray(Function(node)
                                                     Return semanticModel.GetTypeInfo(node.Type, cancellationToken).Type.IsReferenceTypeOrTypeParameter()
                                                 End Function)
        End Function

        Protected Overrides Function ConvertExpression(fromExpression As CTypeExpressionSyntax) As CodeAnalysis.SyntaxNode
            return SyntaxFactory.TryCastExpression(fromExpression.Expression, fromExpression.Type)
        End Function
    End Class
End Namespace
