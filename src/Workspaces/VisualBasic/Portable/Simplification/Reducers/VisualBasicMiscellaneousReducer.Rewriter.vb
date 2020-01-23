' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Simplification
    Partial Friend Class VisualBasicMiscellaneousReducer
        Private Class Rewriter
            Inherits AbstractReductionRewriter

            Public Sub New(pool As ObjectPool(Of IReductionRewriter))
                MyBase.New(pool)
            End Sub

            Public Overrides Function VisitInvocationExpression(node As InvocationExpressionSyntax) As SyntaxNode
                CancellationToken.ThrowIfCancellationRequested()

                Return SimplifyExpression(
                    node,
                    newNode:=MyBase.VisitInvocationExpression(node),
                    simplifier:=s_simplifyInvocationExpression)
            End Function

            Public Overrides Function VisitObjectCreationExpression(node As ObjectCreationExpressionSyntax) As SyntaxNode
                CancellationToken.ThrowIfCancellationRequested()

                Return SimplifyExpression(
                    node,
                    newNode:=MyBase.VisitObjectCreationExpression(node),
                    simplifier:=s_simplifyObjectCreationExpression)
            End Function

            Public Overrides Function VisitParameter(node As ParameterSyntax) As SyntaxNode
                CancellationToken.ThrowIfCancellationRequested()

                Return SimplifyNode(
                    node,
                    newNode:=MyBase.VisitParameter(node),
                    parentNode:=node.Parent,
                    simplifyFunc:=s_simplifyParameter)
            End Function
        End Class
    End Class
End Namespace
