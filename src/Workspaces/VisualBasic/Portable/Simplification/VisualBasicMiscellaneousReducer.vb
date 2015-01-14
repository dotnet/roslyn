' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Simplification
    Partial Friend Class VisualBasicMiscellaneousReducer
        Inherits AbstractVisualBasicReducer

        Public Overrides Function CreateExpressionRewriter(optionSet As OptionSet, cancellationToken As CancellationToken) As IExpressionRewriter
            Return New Rewriter(optionSet, cancellationToken)
        End Function

        Private Shared Function SimplifyParameter(
            parameter As ParameterSyntax,
            semanticModel As SemanticModel,
            optionSet As OptionSet,
            cancellationToken As CancellationToken
        ) As ParameterSyntax
            If parameter.CanRemoveAsClause(semanticModel, cancellationToken) Then
                Dim newParameter = parameter.WithAsClause(Nothing).NormalizeWhitespace()
                newParameter = SimplificationHelpers.CopyAnnotations(parameter, newParameter).WithoutAnnotations(Simplifier.Annotation)
                Return newParameter
            End If

            Return parameter
        End Function

        Private Shared Function SimplifyInvocationExpression(
            invocationExpression As InvocationExpressionSyntax,
            semanticModel As SemanticModel,
            optionSet As OptionSet,
            cancellationToken As CancellationToken
        ) As InvocationExpressionSyntax

            If invocationExpression.CanRemoveEmptyArgumentList(semanticModel, cancellationToken) Then
                Dim resultNode = invocationExpression _
                    .WithArgumentList(Nothing) _
                    .WithTrailingTrivia(invocationExpression.GetTrailingTrivia())

                resultNode = SimplificationHelpers.CopyAnnotations(invocationExpression, resultNode)

                Return resultNode
            End If

            ' We don't know how to simplify this.
            Return invocationExpression
        End Function

        Private Shared Function SimplifyObjectCreationExpression(
            objectCreationExpression As ObjectCreationExpressionSyntax,
            semanticModel As SemanticModel,
            optionSet As OptionSet,
            cancellationToken As CancellationToken
        ) As ObjectCreationExpressionSyntax

            If objectCreationExpression.CanRemoveEmptyArgumentList(semanticModel) Then
                Dim resultNode = objectCreationExpression _
                    .WithArgumentList(Nothing) _
                    .WithTrailingTrivia(objectCreationExpression.GetTrailingTrivia())

                resultNode = SimplificationHelpers.CopyAnnotations(objectCreationExpression, resultNode)

                Return resultNode
            End If

            ' We don't know how to simplify this.
            Return objectCreationExpression
        End Function

    End Class
End Namespace
