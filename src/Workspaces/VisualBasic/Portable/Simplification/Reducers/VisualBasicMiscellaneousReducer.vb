' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Simplification
    Partial Friend Class VisualBasicMiscellaneousReducer
        Inherits AbstractVisualBasicReducer

        Private Shared ReadOnly s_pool As ObjectPool(Of IReductionRewriter) =
            New ObjectPool(Of IReductionRewriter)(Function() New Rewriter(s_pool))

        Public Sub New()
            MyBase.New(s_pool)
        End Sub

        Private Shared ReadOnly s_simplifyParameter As Func(Of ParameterSyntax, SemanticModel, OptionSet, CancellationToken, ParameterSyntax) = AddressOf SimplifyParameter

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

        Private Shared ReadOnly s_simplifyInvocationExpression As Func(Of InvocationExpressionSyntax, SemanticModel, OptionSet, CancellationToken, SyntaxNode) = AddressOf SimplifyInvocationExpression

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

        Private Shared ReadOnly s_simplifyObjectCreationExpression As Func(Of ObjectCreationExpressionSyntax, SemanticModel, OptionSet, CancellationToken, SyntaxNode) = AddressOf SimplifyObjectCreationExpression

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
