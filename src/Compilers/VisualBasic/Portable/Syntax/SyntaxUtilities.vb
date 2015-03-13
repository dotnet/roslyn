' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports System.Runtime.InteropServices

Namespace Microsoft.CodeAnalysis.VisualBasic

    Friend NotInheritable Class SyntaxUtilities

        ''' <summary>
        ''' Returns true if the specified node represents a lambda.
        ''' </summary>
        Public Shared Function IsLambda(node As SyntaxNode) As Boolean
            Select Case node.Kind
                Case SyntaxKind.MultiLineFunctionLambdaExpression,
                     SyntaxKind.SingleLineFunctionLambdaExpression,
                     SyntaxKind.MultiLineSubLambdaExpression,
                     SyntaxKind.SingleLineSubLambdaExpression,
                     SyntaxKind.WhereClause,
                     SyntaxKind.TakeWhileClause,
                     SyntaxKind.SkipWhileClause,
                     SyntaxKind.AscendingOrdering,
                     SyntaxKind.DescendingOrdering,
                     SyntaxKind.FunctionAggregation
                    Return True

                Case SyntaxKind.ExpressionRangeVariable
                    Return IsLambdaExpressionRangeVariable(node)

                Case SyntaxKind.CollectionRangeVariable
                    Return IsLambdaCollectionRangeVariable(node)

                Case SyntaxKind.JoinCondition
                    Return IsLambdaJoinCondition(node)
            End Select

            Return False
        End Function

        Public Shared Function IsNotLambda(node As SyntaxNode) As Boolean
            Return Not IsLambda(node)
        End Function

        ''' <summary>
        ''' Given a node that represents a lambda body returns a node that represents the lambda.
        ''' </summary>
        Public Shared Function GetLambda(lambdaBody As SyntaxNode) As SyntaxNode
            Dim lambda = lambdaBody.Parent
            Debug.Assert(IsLambda(lambda))
            Return lambda
        End Function

        ''' <summary>
        ''' SyntaxNode.GetCorrespondingLambdaBody(SyntaxNode)
        ''' </summary>
        Public Shared Function GetCorrespondingLambdaBody(oldBody As SyntaxNode, newLambda As SyntaxNode) As SyntaxNode
            Debug.Assert(IsLambda(newLambda))

            Dim oldLambda = GetLambda(oldBody)

            Select Case oldLambda.Kind
                Case SyntaxKind.MultiLineFunctionLambdaExpression,
                     SyntaxKind.MultiLineSubLambdaExpression,
                     SyntaxKind.SingleLineFunctionLambdaExpression,
                     SyntaxKind.SingleLineSubLambdaExpression
                    ' The header represents the lambda body
                    Return DirectCast(newLambda, LambdaExpressionSyntax).SubOrFunctionHeader

                Case SyntaxKind.WhereClause
                    Return DirectCast(newLambda, WhereClauseSyntax).Condition

                Case SyntaxKind.TakeWhileClause,
                     SyntaxKind.SkipWhileClause
                    Return DirectCast(newLambda, PartitionWhileClauseSyntax).Condition

                Case SyntaxKind.AscendingOrdering,
                     SyntaxKind.DescendingOrdering
                    Return DirectCast(newLambda, OrderingSyntax).Expression

                Case SyntaxKind.FunctionAggregation
                    ' function call in Group By, Group Join, Aggregate: the argument 
                    Return DirectCast(newLambda, FunctionAggregationSyntax).Argument

                Case SyntaxKind.ExpressionRangeVariable
                    ' Let, Select, GroupBy
                    Return DirectCast(newLambda, ExpressionRangeVariableSyntax).Expression

                Case SyntaxKind.CollectionRangeVariable
                    ' source sequence in From or Aggregate (other than the first in the query)
                    Return DirectCast(newLambda, CollectionRangeVariableSyntax).Expression

                Case SyntaxKind.JoinCondition
                    ' Left sides of all join conditions are merged into one body,
                    ' Right sides of all join conditions are merged into another body.
                    ' THe lambda is the first JoinCondition and the bodies are its Left and Right expressions
                    Dim oldJoin = DirectCast(oldLambda, JoinConditionSyntax)
                    Dim newJoin = DirectCast(newLambda, JoinConditionSyntax)
                    Debug.Assert(oldJoin.Left Is oldBody OrElse oldJoin.Right Is oldBody)
                    Return If(oldJoin.Left Is oldBody, newJoin.Left, newJoin.Right)

                Case Else
                    Throw ExceptionUtilities.Unreachable
            End Select
        End Function

        ''' <summary>
        ''' Returns true if the specified <paramref name="node"/> represents a body of a lambda.
        ''' </summary>
        Public Shared Function IsLambdaBody(node As SyntaxNode) As Boolean
            Dim body As SyntaxNode = Nothing
            Return IsLambdaBodyStatementOrExpression(node, body) AndAlso node Is body
        End Function

        ''' <summary>
        ''' Returns true if the specified <paramref name="node"/> is part of a lambda body. 
        ''' Returns the node (<paramref name="lambdaBody"/>) that represents the containing lambda body.
        ''' </summary>
        ''' <remarks>
        ''' VB lambda bodies may be non-contiguous sequences of nodes whose ancestor (parent or grandparent) is a lambda node.
        ''' Whenever we need to check whether a node is a lambda body node we should use this method.
        ''' </remarks>
        Public Shared Function IsLambdaBodyStatementOrExpression(node As SyntaxNode, <Out> Optional ByRef lambdaBody As SyntaxNode = Nothing) As Boolean
            Dim parent = node?.Parent
            If parent Is Nothing Then
                lambdaBody = Nothing
                Return False
            End If

            If TryGetSimpleLambdaBody(parent, lambdaBody) Then
                Return node Is lambdaBody
            End If

            ' SelectClause -> ERV#1 (lambda) -> expression (lambda body node #1)
            '              -> ERV#2          -> expression (lambda body node #2)
            '              ...
            If parent.IsKind(SyntaxKind.ExpressionRangeVariable) AndAlso
               parent.Parent.IsKind(SyntaxKind.SelectClause) AndAlso
               node Is DirectCast(parent, ExpressionRangeVariableSyntax).Expression Then

                lambdaBody = GetSelectClauseLambdaBody(DirectCast(parent, SelectClauseSyntax))
                Return True
            End If

            ' GroupByClause -> Item ERV#1 (item lambda) -> expression (item lambda body node #1)
            '               -> Item ERV#2               -> expression (item lambda body node #2)
            '               ...
            '               -> Key ERV#1 (key lambda)   -> expression (key lambda body node #1)
            '               -> Key ERV#2                -> expression (key lambda body node #2)
            '               ...
            If parent.IsKind(SyntaxKind.ExpressionRangeVariable) AndAlso
               parent.Parent.IsKind(SyntaxKind.GroupByClause) AndAlso
               node Is DirectCast(parent, ExpressionRangeVariableSyntax).Expression Then

                lambdaBody = GetGroupByClauseLambdaBody(DirectCast(parent.Parent, GroupByClauseSyntax), node)
                Return True
            End If

            ' JoinClause -> Condition#1 (lambda) -> left expression  (lambda left body node #1)
            '                                    -> right expression (lambda right body node #1)
            '               Condition#2          -> left expression  (lambda left body node #2)
            '                                    -> right expression (lambda right body node #2)
            '               Condition#3          -> left expression  (lambda left body node #3)
            '                                    -> right expression (lambda right body node #3)
            '               ...
            If parent.IsKind(SyntaxKind.JoinCondition) AndAlso
               (parent.Parent.IsKind(SyntaxKind.SimpleJoinClause) OrElse parent.Parent.IsKind(SyntaxKind.GroupJoinClause)) Then

                lambdaBody = GetJoinClauseLambdaBody(DirectCast(parent.Parent, JoinClauseSyntax), node)
                Return True
            End If

            lambdaBody = Nothing
            Return False
        End Function

        Private Shared Function GetSelectClauseLambdaBody(selectClause As SelectClauseSyntax) As SyntaxNode
            Return selectClause.Variables.First
        End Function

        Private Shared Function GetGroupByClauseLambdaBody(groupByClause As GroupByClauseSyntax, node As SyntaxNode) As SyntaxNode
            Return If(node.SpanStart < groupByClause.ByKeyword.SpanStart, groupByClause.Items.First, groupByClause.Keys.First).Expression
        End Function

        Private Shared Function GetJoinClauseLambdaBody(joinClause As JoinClauseSyntax, node As SyntaxNode) As SyntaxNode
            Dim joinCondition = DirectCast(node.Parent, JoinConditionSyntax)
            Debug.Assert(node Is joinCondition.Left OrElse node Is joinCondition.Right)
            Return If(node Is joinCondition.Left, joinClause.JoinConditions.First.Left, joinClause.JoinConditions.First.Right)
        End Function

        ''' <summary>
        ''' If the specified node represents a lambda returns a node (or nodes) that represent its body (bodies).
        ''' </summary>
        Public Shared Function TryGetLambdaBodies(node As SyntaxNode, <Out> ByRef lambdaBody1 As SyntaxNode, <Out> ByRef lambdaBody2 As SyntaxNode) As Boolean
            lambdaBody1 = Nothing
            lambdaBody2 = Nothing

            If TryGetSimpleLambdaBody(node, lambdaBody1) Then
                Return True
            End If

            Select Case node.Kind
                Case SyntaxKind.CollectionRangeVariable
                    ' source sequence in From or Aggregate (other than the first in the query)
                    Dim collectionRange = DirectCast(node, CollectionRangeVariableSyntax)
                    If Not IsLambdaCollectionRangeVariable(collectionRange) Then
                        Return False
                    End If

                    lambdaBody1 = collectionRange.Expression

                Case SyntaxKind.ExpressionRangeVariable
                    Dim clause = node.Parent

                    If clause.IsKind(SyntaxKind.LetClause) Then

                        ' Each ERV in Let clause is translated to a seprate lambda.
                        ' The lambda is represented by the ERV and its body by the RHS expression.
                        lambdaBody1 = DirectCast(node, ExpressionRangeVariableSyntax).Expression

                    ElseIf clause.IsKind(SyntaxKind.SelectClause)

                        ' Select clause translates to a single lambda that includes all expression range variables specified in the clause.
                        ' The lambda is represented by the first ERV and its body by the RHS expression.
                        lambdaBody1 = GetSelectClauseLambdaBody(DirectCast(clause, SelectClauseSyntax))

                        ' The node only represents a lambda if it's the first ERV 
                        If node IsNot lambdaBody1.Parent Then
                            Return False
                        End If

                    ElseIf clause.IsKind(SyntaxKind.GroupByClause)

                        ' All ERVs in Items (if any) are translated to a single lambda represented by the first ERV,
                        ' All ERVs in Keys are translated to a single lambda represented by the first ERV.
                        lambdaBody1 = GetGroupByClauseLambdaBody(DirectCast(clause, GroupByClauseSyntax), node)

                        ' The node only represents a lambda if it's the first Item or Keys ERV 
                        If node IsNot lambdaBody1.Parent Then
                            Return False
                        End If
                    End If

                Case SyntaxKind.JoinCondition
                    ' Left sides of all join conditions are merged into one body,
                    ' Right sides of all join conditions are merged into another body.

                    Dim firstCondition = DirectCast(node.Parent, JoinClauseSyntax).JoinConditions.First

                    ' The node only represents a lambda if it's the first Join Condition
                    If node IsNot firstCondition Then
                        Return False
                    End If

                    lambdaBody1 = firstCondition.Left
                    lambdaBody2 = firstCondition.Right

                Case Else
                    Return False

            End Select

            Debug.Assert(node Is GetLambda(lambdaBody1))
            Debug.Assert(lambdaBody2 Is Nothing OrElse node Is GetLambda(lambdaBody2))

            Return True
        End Function

        ''' <summary>
        ''' If the specified node represents a "simple" lambda returns a node (or nodes) that represent its body (bodies).
        ''' </summary>
        Private Shared Function TryGetSimpleLambdaBody(node As SyntaxNode, <Out> ByRef lambdaBody As SyntaxNode) As Boolean
            Select Case node.Kind
                Case SyntaxKind.MultiLineFunctionLambdaExpression,
                     SyntaxKind.SingleLineFunctionLambdaExpression,
                     SyntaxKind.MultiLineSubLambdaExpression,
                     SyntaxKind.SingleLineSubLambdaExpression
                    ' The header of the lambda represents its body.
                    lambdaBody = DirectCast(node, LambdaExpressionSyntax).SubOrFunctionHeader

                Case SyntaxKind.WhereClause
                    lambdaBody = DirectCast(node, WhereClauseSyntax).Condition

                Case SyntaxKind.TakeWhileClause,
                     SyntaxKind.SkipWhileClause
                    lambdaBody = DirectCast(node, PartitionWhileClauseSyntax).Condition

                Case SyntaxKind.AscendingOrdering,
                     SyntaxKind.DescendingOrdering
                    lambdaBody = DirectCast(node, OrderingSyntax).Expression

                Case SyntaxKind.FunctionAggregation
                    ' function call in Group By, Group Join, Aggregate: the argument 
                    lambdaBody = DirectCast(node, FunctionAggregationSyntax).Argument

                Case Else
                    lambdaBody = Nothing
                    Return False
            End Select

            Debug.Assert(node Is GetLambda(lambdaBody))
            Return True
        End Function

        Friend Shared Function IsLambdaExpressionRangeVariable(expressionRangeVariable As SyntaxNode) As Boolean
            Debug.Assert(expressionRangeVariable.IsKind(SyntaxKind.ExpressionRangeVariable))

            ' Let clause:
            '   Each ERV in Let clause is translated to a seprate lambda.
            '   The lambda is represented by the ERV and its body by the RHS expression.
            '
            ' Select clause:
            '   Translates to a single lambda that includes all expression range variables specified in the clause.
            '   The lambda is represented by the first ERV and its body by the RHS expression.
            '
            ' GroupBy clause:
            '   All ERVs in Items (if any) are translated to a single lambda represented by the first ERV,
            '   All ERVs in Keys are translated to a single lambda represented by the first ERV.
            Dim clause = expressionRangeVariable.Parent

            Select Case clause.Kind()
                Case SyntaxKind.LetClause
                    Return True

                Case SyntaxKind.SelectClause
                    Dim selectClause = DirectCast(clause, SelectClauseSyntax)
                    Return expressionRangeVariable Is selectClause.Variables.First

                Case SyntaxKind.GroupByClause
                    Dim groupByClause = DirectCast(clause, GroupByClauseSyntax)
                    Return expressionRangeVariable Is groupByClause.Keys.First OrElse
                           expressionRangeVariable Is groupByClause.Items.FirstOrDefault
            End Select

            Return False
        End Function

        Friend Shared Function IsLambdaCollectionRangeVariable(collectionRangeVariable As SyntaxNode) As Boolean
            Debug.Assert(collectionRangeVariable.IsKind(SyntaxKind.CollectionRangeVariable))

            Dim parent = collectionRangeVariable.Parent

            Dim query = DirectCast(parent.Parent, QueryExpressionSyntax)
            If query.Clauses.First() Is parent Then
                Return False
            End If

            Dim variables As SeparatedSyntaxList(Of CollectionRangeVariableSyntax)

            Select Case parent.Kind
                Case SyntaxKind.FromClause
                    variables = DirectCast(parent, FromClauseSyntax).Variables

                Case SyntaxKind.AggregateClause
                    variables = DirectCast(parent, AggregateClauseSyntax).Variables

                Case SyntaxKind.GroupJoinClause,
                     SyntaxKind.SimpleJoinClause
                    Return False

                Case Else
                    Throw ExceptionUtilities.Unreachable
            End Select

            Return collectionRangeVariable IsNot variables(0)
        End Function

        Friend Shared Function IsLambdaJoinCondition(joinCondition As SyntaxNode) As Boolean
            Debug.Assert(joinCondition.IsKind(SyntaxKind.JoinCondition))
            Return joinCondition Is DirectCast(joinCondition.Parent, JoinClauseSyntax).JoinConditions.First
        End Function

        ''' <summary>
        ''' Returns true if the specified node can represent a closure scope -- that is a scope of a captured variable.
        ''' Doesn't validate whether or not the node actually declares any captured variable.
        ''' </summary>
        Friend Shared Function IsClosureScope(node As SyntaxNode) As Boolean
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

                        Case SyntaxKind.FunctionAggregation
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