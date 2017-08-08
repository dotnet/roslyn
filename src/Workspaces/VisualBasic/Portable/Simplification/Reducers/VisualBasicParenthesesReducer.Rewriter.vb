' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Simplification
    Partial Friend Class VisualBasicParenthesesReducer
        Private Class Rewriter
            Inherits AbstractReductionRewriter

            Public Sub New(pool As ObjectPool(Of IReductionRewriter))
                MyBase.New(pool)
            End Sub

            Public Overrides Function VisitParenthesizedExpression(node As ParenthesizedExpressionSyntax) As SyntaxNode
                Return SimplifyExpression(
                    node,
                    newNode:=MyBase.VisitParenthesizedExpression(node),
                    simplifier:=s_reduceParentheses)
            End Function
        End Class
    End Class
End Namespace
