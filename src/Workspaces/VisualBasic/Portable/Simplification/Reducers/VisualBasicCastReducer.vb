' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

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

        Private Shared ReadOnly s_simplifyCast As Func(Of CastExpressionSyntax, SemanticModel, SimplifierOptions, CancellationToken, SyntaxNode) = AddressOf SimplifyCast

        Public Sub New()
            MyBase.New(s_pool)
        End Sub

        Public Overrides Function IsApplicable(options As VisualBasicSimplifierOptions) As Boolean
            Return True
        End Function

        Private Overloads Shared Function SimplifyCast(
            node As CastExpressionSyntax,
            semanticModel As SemanticModel,
            options As SimplifierOptions,
            cancellationToken As CancellationToken
        ) As ExpressionSyntax

            If Not node.IsUnnecessaryCast(semanticModel, cancellationToken) Then
                Return node
            End If

            Return node.Uncast()
        End Function

        Private Shared ReadOnly s_simplifyPredefinedCast As Func(Of PredefinedCastExpressionSyntax, SemanticModel, SimplifierOptions, CancellationToken, SyntaxNode) = AddressOf SimplifyPredefinedCast

        Private Overloads Shared Function SimplifyPredefinedCast(
            node As PredefinedCastExpressionSyntax,
            semanticModel As SemanticModel,
            options As SimplifierOptions,
            cancellationToken As CancellationToken
        ) As ExpressionSyntax

            If Not node.IsUnnecessaryCast(semanticModel, cancellationToken) Then
                Return node
            End If

            Return node.Uncast()
        End Function
    End Class
End Namespace
