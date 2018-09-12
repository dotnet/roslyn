' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Simplification
    Partial Friend Class VisualBasicCastReducer
        Inherits AbstractVisualBasicReducer

        Private Shared ReadOnly s_pool As ObjectPool(Of IReductionRewriter) =
            New ObjectPool(Of IReductionRewriter)(Function() New Rewriter(s_pool))

        Public Sub New()
            MyBase.New(s_pool)
        End Sub

        Private Shared ReadOnly s_simplifyCast As Func(Of CastExpressionSyntax, SemanticModel, OptionSet, CancellationToken, SyntaxNode) = AddressOf SimplifyCast

        Private Overloads Shared Function SimplifyCast(
            node As CastExpressionSyntax,
            semanticModel As SemanticModel,
            optionSet As OptionSet,
            cancellationToken As CancellationToken
        ) As ExpressionSyntax

            If Not node.IsUnnecessaryCast(semanticModel, cancellationToken) Then
                Return node
            End If

            Return node.Uncast()
        End Function

        Private Shared ReadOnly s_simplifyPredefinedCast As Func(Of PredefinedCastExpressionSyntax, SemanticModel, OptionSet, CancellationToken, SyntaxNode) = AddressOf SimplifyPredefinedCast

        Private Overloads Shared Function SimplifyPredefinedCast(
            node As PredefinedCastExpressionSyntax,
            semanticModel As SemanticModel,
            optionSet As OptionSet,
            cancellationToken As CancellationToken
        ) As ExpressionSyntax

            If Not node.IsUnnecessaryCast(semanticModel, cancellationToken) Then
                Return node
            End If

            Return node.Uncast()
        End Function
    End Class
End Namespace
