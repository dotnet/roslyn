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

        ''' <summary>
        ''' Returns true if the specified node can represent a closure scope -- that is a scope of a captured variable.
        ''' Doesn't validate whether or not the node actually declares any captured variable.
        ''' </summary>
        Friend Shared Function IsClosureScope(node As VisualBasicSyntaxNode) As Boolean
            Select Case node.Kind()
                Case SyntaxKind.MultiLineSubLambdaExpression,
                     SyntaxKind.MultiLineFunctionLambdaExpression,
                     SyntaxKind.SingleLineSubLambdaExpression,
                     SyntaxKind.SingleLineFunctionLambdaExpression
                    ' lambda parameters, variables defined in lambda body
                    Return True

                Case SyntaxKind.SubBlock,
                     SyntaxKind.FunctionBlock,
                     SyntaxKind.ConstructorBlock,
                     SyntaxKind.OperatorBlock,
                     SyntaxKind.GetAccessorBlock,
                     SyntaxKind.SetAccessorBlock,
                     SyntaxKind.AddHandlerAccessorBlock,
                     SyntaxKind.RemoveHandlerAccessorBlock,
                     SyntaxKind.RaiseEventAccessorBlock
                    ' parameters, variables defined in method body
                    ' Note: property parameters, accessor parameters and variables defined in an accessor have all the same scope (the accessor scope).
                    Return True

                Case SyntaxKind.WhileBlock,
                     SyntaxKind.ForBlock,
                     SyntaxKind.ForEachBlock,
                     SyntaxKind.SimpleDoLoopBlock,
                     SyntaxKind.DoWhileLoopBlock,
                     SyntaxKind.DoUntilLoopBlock,
                     SyntaxKind.DoLoopWhileBlock,
                     SyntaxKind.DoLoopUntilBlock,
                     SyntaxKind.UsingBlock,
                     SyntaxKind.SyncLockBlock,
                     SyntaxKind.WithBlock,
                     SyntaxKind.CaseBlock,
                     SyntaxKind.CaseElseBlock,
                     SyntaxKind.SingleLineIfStatement,
                     SyntaxKind.SingleLineElseClause,
                     SyntaxKind.MultiLineIfBlock,
                     SyntaxKind.ElseIfBlock,
                     SyntaxKind.ElseBlock,
                     SyntaxKind.TryBlock,
                     SyntaxKind.CatchBlock,
                     SyntaxKind.FinallyBlock
                    ' variable declared in a statement block
                    Return True

                Case SyntaxKind.SelectClause,
                     SyntaxKind.SimpleJoinClause,
                     SyntaxKind.GroupJoinClause,
                     SyntaxKind.GroupByClause,
                     SyntaxKind.AggregateClause
                    ' range variable captured by the clause
                    Return True

                Case Else
                    Dim parent = node.Parent

                    If TypeOf node IsNot ExpressionSyntax OrElse parent Is Nothing Then
                        Return False
                    End If

                    Select Case parent.Kind()
                        Case SyntaxKind.WhereClause,
                             SyntaxKind.TakeWhileClause,
                             SyntaxKind.SkipWhileClause,
                             SyntaxKind.AscendingOrdering,
                             SyntaxKind.DescendingOrdering
                            ' captured range variable by the clause
                            Return True

                        Case SyntaxKind.FunctionAggregation,
                             SyntaxKind.GroupAggregation
                            ' range variable captured by IntoClause
                            Return True

                        Case SyntaxKind.ExpressionRangeVariable
                            ' range variable captured by Let clause
                            Return parent.Parent IsNot Nothing AndAlso parent.Parent.IsKind(SyntaxKind.LetClause)

                    End Select

                    ' TODO: EE expression
                    If parent.Parent IsNot Nothing AndAlso
                       parent.Parent.Parent Is Nothing Then
                        Return True
                    End If

                    Return False
            End Select
        End Function
    End Class
End Namespace