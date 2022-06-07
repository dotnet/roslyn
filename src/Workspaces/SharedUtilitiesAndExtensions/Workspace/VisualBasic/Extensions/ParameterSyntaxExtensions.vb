' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.Utilities
Imports System.Threading

Namespace Microsoft.CodeAnalysis.VisualBasic.Extensions
    Friend Module ParameterSyntaxExtensions
        <Extension()>
        Public Function CanRemoveAsClause(parameter As ParameterSyntax, semanticModel As SemanticModel, cancellationToken As CancellationToken) As Boolean
            If parameter.AsClause IsNot Nothing AndAlso
                parameter.IsParentKind(SyntaxKind.ParameterList) AndAlso
                parameter.Parent.Parent.IsKind(SyntaxKind.FunctionLambdaHeader, SyntaxKind.SubLambdaHeader) Then

                Dim annotation = New SyntaxAnnotation()
                Dim newParameterSyntax = parameter.WithAsClause(Nothing).WithAdditionalAnnotations(annotation)
                Dim oldLambda = parameter.FirstAncestorOrSelf(Of LambdaExpressionSyntax)()
                Dim newLambda = oldLambda.ReplaceNode(parameter, newParameterSyntax)
                Dim speculationAnalyzer = New SpeculationAnalyzer(oldLambda, newLambda, semanticModel, cancellationToken)
                newParameterSyntax = speculationAnalyzer.ReplacedExpression.GetAnnotatedNodes(Of ParameterSyntax)(annotation).First()

                Dim oldSymbol = semanticModel.GetDeclaredSymbol(parameter, cancellationToken)
                Dim newSymbol = speculationAnalyzer.SpeculativeSemanticModel.GetDeclaredSymbol(newParameterSyntax, cancellationToken)
                If oldSymbol IsNot Nothing AndAlso
                    newSymbol IsNot Nothing AndAlso
                    Equals(oldSymbol.Type, newSymbol.Type) Then

                    Return Not speculationAnalyzer.ReplacementChangesSemantics()
                End If
            End If

            Return False
        End Function
    End Module
End Namespace
