' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    Friend NotInheritable Class SyntaxUtilities
        ''' <summary>
        ''' SyntaxNode.GetCorrespondingLambdaBody(SyntaxNode)
        ''' </summary>
        Friend Shared Function GetCorrespondingLambdaBody(oldBody As SyntaxNode, newLambda As SyntaxNode) As SyntaxNode

            Dim oldLambda = oldBody.Parent

            Select Case oldLambda.Kind
                Case SyntaxKind.MultiLineFunctionLambdaExpression,
                     SyntaxKind.MultiLineSubLambdaExpression,
                     SyntaxKind.SingleLineFunctionLambdaExpression,
                     SyntaxKind.SingleLineSubLambdaExpression
                    ' Any statement or header can be used to represent the lambda body.
                    ' Let's pick the header since the lambda may have no other statements.
                    Return DirectCast(newLambda, LambdaExpressionSyntax).SubOrFunctionHeader

                Case SyntaxKind.WhereClause
                    Return DirectCast(newLambda, WhereClauseSyntax).Condition

                ' source sequence in From and Aggregate (other than the first in the query)
                Case SyntaxKind.CollectionRangeVariable
                    Return DirectCast(newLambda, CollectionRangeVariableSyntax).Expression

                ' function call in Group By, Group Join, Aggregate: the argument 
                Case SyntaxKind.FunctionAggregation
                    Return DirectCast(newLambda, FunctionAggregationSyntax).Argument

                ' variable in Let, Select, Group By: the RHS
                Case SyntaxKind.ExpressionRangeVariable
                    Return DirectCast(newLambda, ExpressionRangeVariableSyntax).Expression

                Case SyntaxKind.TakeWhileClause,
                     SyntaxKind.SkipWhileClause
                    Return DirectCast(newLambda, PartitionWhileClauseSyntax).Condition

                Case SyntaxKind.AscendingOrdering,
                     SyntaxKind.DescendingOrdering
                    Return DirectCast(newLambda, OrderingSyntax).Expression

                Case SyntaxKind.JoinCondition
                    Dim oldJoin = DirectCast(oldLambda, JoinConditionSyntax)
                    Dim newJoin = DirectCast(newLambda, JoinConditionSyntax)
                    Debug.Assert(oldJoin.Left Is oldBody OrElse oldJoin.Right Is oldBody)
                    Return If(oldJoin.Left Is oldBody, newJoin.Left, newJoin.Right)

                Case Else
                    Throw ExceptionUtilities.Unreachable
            End Select
        End Function
    End Class
End Namespace