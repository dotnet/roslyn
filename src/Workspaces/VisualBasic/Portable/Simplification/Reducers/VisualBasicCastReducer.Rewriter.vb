' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Simplification
    Partial Friend Class VisualBasicCastReducer
        Private Class Rewriter
            Inherits AbstractReductionRewriter

            Public Sub New(pool As ObjectPool(Of IReductionRewriter))
                MyBase.New(pool)
            End Sub

            Public Overrides Function VisitCTypeExpression(node As CTypeExpressionSyntax) As SyntaxNode
                Return SimplifyExpression(
                    node,
                    newNode:=MyBase.VisitCTypeExpression(node),
                    simplifier:=s_simplifyCast)
            End Function

            Public Overrides Function VisitDirectCastExpression(node As DirectCastExpressionSyntax) As SyntaxNode
                Return SimplifyExpression(
                    node,
                    newNode:=MyBase.VisitDirectCastExpression(node),
                    simplifier:=s_simplifyCast)
            End Function

            Public Overrides Function VisitTryCastExpression(node As TryCastExpressionSyntax) As SyntaxNode
                Return SimplifyExpression(
                    node,
                    newNode:=MyBase.VisitTryCastExpression(node),
                    simplifier:=s_simplifyCast)
            End Function

            Public Overrides Function VisitPredefinedCastExpression(node As PredefinedCastExpressionSyntax) As SyntaxNode
                Return SimplifyExpression(
                    node,
                    newNode:=MyBase.VisitPredefinedCastExpression(node),
                    simplifier:=s_simplifyPredefinedCast)
            End Function
        End Class
    End Class
End Namespace
