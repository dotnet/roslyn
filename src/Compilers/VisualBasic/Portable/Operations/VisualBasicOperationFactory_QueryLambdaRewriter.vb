' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.Semantics
    Partial Friend NotInheritable Class VisualBasicOperationFactory
        Private Shared Function RewriteQueryLambda(node As BoundQueryLambda) As BoundNode
            ' We rewrite query lambda into regular lambda with 2 passes.
            ' Pass 1 uses helper methods from LocalRewriter to do the lowering. This introduces large number of DAGs.
            ' Pass 2 walks over the lowered tree and replaces duplicate bound nodes in the tree with their clones - this is a requirement for the Operation tree.
            Dim pass1Rewriter As New QueryLambdaRewriterPass1
            Dim rewrittenLambda As BoundLambda = DirectCast(pass1Rewriter.VisitQueryLambda(node), BoundLambda)

            Dim pass2Rewriter As New QueryLambdaRewriterPass2
            Return pass2Rewriter.VisitLambda(rewrittenLambda)
        End Function

        Private NotInheritable Class QueryLambdaRewriterPass1
            Inherits BoundTreeRewriterWithStackGuard
            Private _rangeVariableMap As Dictionary(Of RangeVariableSymbol, BoundExpression)

            Public Sub New()
                _rangeVariableMap = Nothing
            End Sub

            Public Overrides Function VisitQueryLambda(node As BoundQueryLambda) As BoundNode
                LocalRewriter.PopulateRangeVariableMapForQueryLambdaRewrite(node, _rangeVariableMap, inExpressionLambda:=False)
                Dim rewrittenBody As BoundStatement = LocalRewriter.RewriteQueryLambdaBody(AddressOf VisitExpressionWithStackGuard, node, _rangeVariableMap)
                Return LocalRewriter.RewriteQueryLambda(rewrittenBody, node)
            End Function

            Public Overrides Function VisitRangeVariable(node As BoundRangeVariable) As BoundNode
                Dim expression As BoundExpression = Nothing
                If Not _rangeVariableMap.TryGetValue(node.RangeVariable, expression) Then
                    ' _rangeVariableMap should contain an entry for the range variable, except for error cases. 
                    Debug.Assert(node.HasErrors OrElse node.RangeVariable.Type.IsErrorType())
                    Return node
                End If

#If DEBUG Then
                ' Range variable reference should be rewritten to a parameter reference, or a call or a property access.
                ' We clone these bound nodes in QueryLambdaRewriterPass2 to avoid dag in the generated bound tree.
                ' If the LocalRewriter is changed to generate more kind of bound nodes for range variables, we should handle these in QueryLambdaRewriterPass2.
                ' Below assert helps us to stay in sync with the LocalRewriter.
                Select Case expression.Kind
                    Case BoundKind.Parameter
                    Case BoundKind.Call
                    Case BoundKind.PropertyAccess
                        Exit Select
                    Case Else
                        Debug.Fail($"Unexpected bound kind '{expression.Kind}' generated for range variable rewrite by method '{NameOf(LocalRewriter.PopulateRangeVariableMapForQueryLambdaRewrite)}'")
                End Select
#End If

                Return expression
            End Function
        End Class

        Private NotInheritable Class QueryLambdaRewriterPass2
            Inherits BoundTreeRewriterWithStackGuard
            Private ReadOnly _uniqueNodes As HashSet(Of BoundExpression)

            Public Sub New()
                _uniqueNodes = New HashSet(Of BoundExpression)
            End Sub

            Private Function HandleNode(Of T As BoundExpression)(node As T) As T
                If Not _uniqueNodes.Add(node) Then
                    node = node.MemberwiseClone(Of T)
                    _uniqueNodes.Add(node)
                End If

                Return node
            End Function

            Public Overrides Function VisitParameter(node As BoundParameter) As BoundNode
                node = DirectCast(MyBase.VisitParameter(node), BoundParameter)
                Return HandleNode(node)
            End Function

            Public Overrides Function VisitCall(node As BoundCall) As BoundNode
                node = DirectCast(MyBase.VisitCall(node), BoundCall)
                Return HandleNode(node)
            End Function

            Public Overrides Function VisitPropertyAccess(node As BoundPropertyAccess) As BoundNode
                node = DirectCast(MyBase.VisitPropertyAccess(node), BoundPropertyAccess)
                Return HandleNode(node)
            End Function
        End Class
    End Class
End Namespace
