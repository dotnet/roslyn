' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Simplification
    Partial Friend Class VisualBasicCastReducer
        Inherits AbstractVisualBasicReducer

        Public Overrides Function CreateExpressionRewriter(optionSet As OptionSet, cancellationToken As CancellationToken) As IExpressionRewriter
            Return New Rewriter(optionSet, cancellationToken)
        End Function

        Private Overloads Shared Function SimplifyCast(
            castNode As ExpressionSyntax,
            innerNode As ExpressionSyntax,
            optionSet As OptionSet,
            cancellationToken As CancellationToken
        ) As ExpressionSyntax

            Dim resultNode = innerNode _
                .WithLeadingTrivia(castNode.GetLeadingTrivia()) _
                .WithTrailingTrivia(castNode.GetTrailingTrivia())

            resultNode = SimplificationHelpers.CopyAnnotations(castNode, resultNode)

            Return resultNode
        End Function

        Private Overloads Shared Function SimplifyCast(
            node As CastExpressionSyntax,
            semanticModel As SemanticModel,
            optionSet As OptionSet,
            cancellationToken As CancellationToken
        ) As ExpressionSyntax

            If Not node.IsUnnecessaryCast(semanticModel, cancellationToken) Then
                Return node
            End If

            Return SimplifyCast(node, node.Expression, optionSet, cancellationToken)
        End Function

        Private Overloads Shared Function SimplifyCast(
            node As PredefinedCastExpressionSyntax,
            semanticModel As SemanticModel,
            optionSet As OptionSet,
            cancellationToken As CancellationToken
        ) As ExpressionSyntax

            If Not node.IsUnnecessaryCast(semanticModel, cancellationToken) Then
                Return node
            End If

            Return SimplifyCast(node, node.Expression, optionSet, cancellationToken)
        End Function
    End Class
End Namespace
