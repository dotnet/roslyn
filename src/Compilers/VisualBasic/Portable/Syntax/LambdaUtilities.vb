' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    Friend NotInheritable Class LambdaUtilities

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
        ''' <remarks>
        ''' We need to handle case when an old node that represents a lambda body with multiple nodes 
        ''' of the same kind is mapped to a new node that belongs to the lambda body but is 
        ''' different from the one that represents the new body.
        ''' 
        ''' In that case <paramref name="newLambdaOrPeer"/> isn't lambda representing node (the first range variable of a clause)
        ''' but its equivalent peer (another range variable of the same clause).
        ''' </remarks>
        Public Shared Function GetCorrespondingLambdaBody(oldBody As SyntaxNode, newLambdaOrPeer As SyntaxNode) As SyntaxNode
            Dim oldLambda = GetLambda(oldBody)

            Dim newLambdaBody As SyntaxNode = Nothing
            If TryGetSimpleLambdaBody(newLambdaOrPeer, newLambdaBody) Then
                Return newLambdaBody
            End If

            Select Case oldLambda.Kind
                Case SyntaxKind.ExpressionRangeVariable
                    Return GetExpressionRangeVariableLambdaBody(DirectCast(newLambdaOrPeer, ExpressionRangeVariableSyntax))

                ' TODO: handle peers
                Case SyntaxKind.CollectionRangeVariable
                    ' From, Aggregate (other than the first in the query)
                    Return DirectCast(newLambdaOrPeer, CollectionRangeVariableSyntax).Expression

                Case SyntaxKind.JoinCondition
                    ' Left sides of all join conditions are merged into one body,
                    ' Right sides of all join conditions are merged into another body.
                    ' THe lambda is the first JoinCondition and the bodies are its Left and Right expressions
                    Dim oldJoinCondition = DirectCast(oldLambda, JoinConditionSyntax)
                    Dim newJoinCondition = DirectCast(newLambdaOrPeer, JoinConditionSyntax)
                    Dim newJoinClause = DirectCast(newJoinCondition.Parent, JoinClauseSyntax)
                    Return If(oldJoinCondition.Left Is oldBody, GetJoinLeftLambdaBody(newJoinClause), GetJoinRightLambdaBody(newJoinClause))

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(oldLambda.Kind)
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
        ''' Returns true if the specified <paramref name="node"/> is part of a lambda body and its parent is not.
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
                Return True
            End If

            Select Case parent.Kind
                Case SyntaxKind.ExpressionRangeVariable
                    Dim erv = DirectCast(parent, ExpressionRangeVariableSyntax)

                    ' Let, Select, GroupBy: the lambda body nodes are ERV.Expressions
                    ' The ERV.Identifiers and ERV.AsClauses are considered part of the containing clause, not the lambda body.
                    If node IsNot erv.Expression Then
                        Exit Select
                    End If

                    lambdaBody = GetExpressionRangeVariableLambdaBody(erv)
                    Return True

                Case SyntaxKind.CollectionRangeVariable
                    Dim crv = DirectCast(parent, CollectionRangeVariableSyntax)

                    ' FromClause, Aggregate: the lambda body nodes are CRV.Expressions
                    ' The CRV.Identifiers and CRV.AsClauses are considered part of the containing clause, not the lambda body.
                    If node IsNot crv.Expression Then
                        Exit Select
                    End If

                    Dim clause = parent.Parent

                    ' In the following #N denotes the N-th clause in a query expression, or the N-th variable in a clause.
                    '
                    ' FromClause#1  -> CRV#1 -> expression
                    '                  CRV#>1 (lambda) -> expression (lambda body -- representative)
                    '
                    ' FromClause#>1 -> CRV (lambda) -> expression    (lambda body -- representative)
                    '
                    '
                    ' Aggregate#1  -> CRV#1 -> expression
                    '
                    ' Aggregate#>1 -> CRV#1  (aggregate lambda) -> expression (aggregate lambda body node -- representative)
                    '                 CRV#>1 (lambda)           -> expression (lambda body node -- representative)
                    '
                    ' Aggregate#>1 -> JoinClause  -> CRV -> expression                             (aggregate lambda body node)
                    '                             -> JoinClause -> CRV -> expression               (aggregate lambda body node)
                    '                                           -> JoinClause -> CRV -> expression (aggregate lambda body node)
                    '                             ...

                    ' If the CRV is a lambda on its own then the node is its body.
                    ' (includes the first CRV of non-starting aggregate clause since it represents the aggregate clause lambda)
                    If IsLambdaCollectionRangeVariable(parent) Then
                        lambdaBody = node
                        Return True
                    End If

                    ' If the CRV is not a lambda, it may be part of containing aggregate clause lambda body
                    If IsJoinClause(clause) Then
                        Dim parentClause = clause.Parent
                        Do
                            clause = clause.Parent
                        Loop While IsJoinClause(clause)

                        If clause.IsKind(SyntaxKind.AggregateClause) AndAlso Not IsQueryStartingClause(clause) Then
                            lambdaBody = GetAggregateLambdaBody(DirectCast(clause, AggregateClauseSyntax))
                            Return True
                        End If
                    End If

                Case SyntaxKind.TakeClause,
                     SyntaxKind.SkipClause
                    ' Aggregate -> TakeClause -> expression (aggregate lambda body node)
                    '           -> SkipClause -> expression (aggregate lambda body node)
                    Dim parentClause = parent.Parent
                    If parentClause.IsKind(SyntaxKind.AggregateClause) AndAlso Not IsQueryStartingClause(parentClause) Then
                        lambdaBody = GetAggregateLambdaBody(DirectCast(parentClause, AggregateClauseSyntax))
                        Return True
                    End If

                Case SyntaxKind.JoinCondition
                    ' JoinClause -> Condition#1 (lambda) -> left expression  (lambda left body node #1 -- representative)
                    '                                    -> right expression (lambda right body node #1)
                    '               Condition#2          -> left expression  (lambda left body node #2)
                    '                                    -> right expression (lambda right body node #2)
                    '               Condition#3          -> left expression  (lambda left body node #3)
                    '                                    -> right expression (lambda right body node #3)
                    '               ...
                    Dim joinCondition = DirectCast(parent, JoinConditionSyntax)
                    Dim joinClause = DirectCast(parent.Parent, JoinClauseSyntax)
                    If node Is joinCondition.Left Then
                        lambdaBody = GetJoinLeftLambdaBody(joinClause)
                    Else
                        lambdaBody = GetJoinRightLambdaBody(joinClause)
                    End If

                    Return True
            End Select

            lambdaBody = Nothing
            Return False
        End Function

        Private Shared Function IsJoinClause(node As SyntaxNode) As Boolean
            Return node.IsKind(SyntaxKind.GroupJoinClause) OrElse node.IsKind(SyntaxKind.SimpleJoinClause)
        End Function

        Friend Shared Function GetLambdaExpressionLambdaBody(lambda As LambdaExpressionSyntax) As VisualBasicSyntaxNode
            Return lambda.SubOrFunctionHeader
        End Function

        Friend Shared Function GetFromOrAggregateVariableLambdaBody(rangeVariable As CollectionRangeVariableSyntax) As VisualBasicSyntaxNode
            Return rangeVariable.Expression
        End Function

        Friend Shared Function GetOrderingLambdaBody(ordering As OrderingSyntax) As VisualBasicSyntaxNode
            Return ordering.Expression
        End Function

        Friend Shared Function GetAggregationLambdaBody(aggregation As FunctionAggregationSyntax) As VisualBasicSyntaxNode
            Return aggregation.Argument
        End Function

        Friend Shared Function GetLetVariableLambdaBody(rangeVariable As ExpressionRangeVariableSyntax) As VisualBasicSyntaxNode
            Return rangeVariable.Expression
        End Function

        Friend Shared Function GetSelectLambdaBody(selectClause As SelectClauseSyntax) As VisualBasicSyntaxNode
            Return selectClause.Variables.First.Expression
        End Function

        Friend Shared Function GetAggregateLambdaBody(aggregateClause As AggregateClauseSyntax) As VisualBasicSyntaxNode
            Return aggregateClause.Variables.First.Expression
        End Function

        Friend Shared Function GetGroupByItemsLambdaBody(groupByClause As GroupByClauseSyntax) As VisualBasicSyntaxNode
            Return groupByClause.Items.First.Expression
        End Function

        Friend Shared Function GetGroupByKeysLambdaBody(groupByClause As GroupByClauseSyntax) As VisualBasicSyntaxNode
            Return groupByClause.Keys.First.Expression
        End Function

        Friend Shared Function GetJoinLeftLambdaBody(joinClause As JoinClauseSyntax) As VisualBasicSyntaxNode
            Return joinClause.JoinConditions.First.Left
        End Function

        Friend Shared Function GetJoinRightLambdaBody(joinClause As JoinClauseSyntax) As VisualBasicSyntaxNode
            Return joinClause.JoinConditions.First.Right
        End Function

        Private Shared Function GetExpressionRangeVariableLambdaBody(rangeVariable As ExpressionRangeVariableSyntax) As SyntaxNode
            Dim clause = rangeVariable.Parent

            Select Case clause.Kind
                ' LetClause -> any ERV (lambda) -> expression (lambda body -- representative)
                Case SyntaxKind.LetClause
                    Return GetLetVariableLambdaBody(rangeVariable)

                ' SelectClause -> ERV#1 (lambda) -> expression (lambda body node #1 -- representative)
                '              -> ERV#2          -> expression (lambda body node #2)
                '              ...
                Case SyntaxKind.SelectClause
                    Return GetSelectLambdaBody(DirectCast(clause, SelectClauseSyntax))

                ' GroupByClause -> Item ERV#1 (item lambda) -> expression (item lambda body node #1 -- representative)
                '               -> Item ERV#2               -> expression (item lambda body node #2)
                '               ...
                '               -> Key ERV#1 (key lambda)   -> expression (key lambda body node #1 -- representative)
                '               -> Key ERV#2                -> expression (key lambda body node #2)
                '               ...
                Case SyntaxKind.GroupByClause
                    Dim groupByClause = DirectCast(clause, GroupByClauseSyntax)
                    If rangeVariable.SpanStart < groupByClause.ByKeyword.SpanStart OrElse
                       (rangeVariable.SpanStart = groupByClause.ByKeyword.SpanStart AndAlso rangeVariable Is groupByClause.Items.Last) Then
                        Return GetGroupByItemsLambdaBody(groupByClause)
                    Else
                        Return GetGroupByKeysLambdaBody(groupByClause)
                    End If

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(clause.Kind)
            End Select
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
                    ' From or Aggregate (other than the first in the query)
                    If Not IsLambdaCollectionRangeVariable(node) Then
                        Return False
                    End If

                    lambdaBody1 = DirectCast(node, CollectionRangeVariableSyntax).Expression

                Case SyntaxKind.ExpressionRangeVariable
                    ' Let, Select, GroupBy
                    If Not IsLambdaExpressionRangeVariable(node) Then
                        Return False
                    End If

                    lambdaBody1 = DirectCast(node, ExpressionRangeVariableSyntax).Expression

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
        ''' Enumerates all nodes that belong to the given <paramref name="lambdaBody"/> and their parents do not
        ''' (they are the top-most expressions and statements of the body).
        ''' </summary>
        Friend Shared Function GetLambdaBodyExpressionsAndStatements(lambdaBody As SyntaxNode) As IEnumerable(Of SyntaxNode)
            Dim lambda = GetLambda(lambdaBody)

            Select Case lambda.Kind
                Case SyntaxKind.SingleLineFunctionLambdaExpression,
                     SyntaxKind.SingleLineSubLambdaExpression
                    Return SpecializedCollections.SingletonEnumerable(DirectCast(lambda, SingleLineLambdaExpressionSyntax).Body)

                Case SyntaxKind.MultiLineFunctionLambdaExpression,
                     SyntaxKind.MultiLineSubLambdaExpression
                    Return DirectCast(lambda, MultiLineLambdaExpressionSyntax).Statements

                Case SyntaxKind.WhereClause,
                     SyntaxKind.TakeWhileClause,
                     SyntaxKind.SkipWhileClause,
                     SyntaxKind.AscendingOrdering,
                     SyntaxKind.DescendingOrdering,
                     SyntaxKind.FunctionAggregation

                    Debug.Assert(TypeOf lambdaBody Is ExpressionSyntax)
                    Return SpecializedCollections.SingletonEnumerable(lambdaBody)

                Case SyntaxKind.ExpressionRangeVariable
                    Dim clause = lambda.Parent
                    Select Case clause.Kind
                        Case SyntaxKind.LetClause
                            Return SpecializedCollections.SingletonEnumerable(lambdaBody)

                        Case SyntaxKind.SelectClause
                            Return EnumerateExpressions(DirectCast(clause, SelectClauseSyntax).Variables)

                        Case SyntaxKind.GroupByClause
                            Dim groupByClause = DirectCast(clause, GroupByClauseSyntax)
                            If lambdaBody.SpanStart < groupByClause.ByKeyword.SpanStart Then
                                Return EnumerateExpressions(groupByClause.Items)
                            Else
                                Return EnumerateExpressions(groupByClause.Keys)
                            End If

                        Case Else
                            Throw ExceptionUtilities.UnexpectedValue(clause.Kind)
                    End Select

                Case SyntaxKind.CollectionRangeVariable
                    Dim clause = lambda.Parent
                    Select Case clause.Kind
                        Case SyntaxKind.FromClause
                            Return SpecializedCollections.SingletonEnumerable(lambdaBody)

                        Case SyntaxKind.AggregateClause
                            Dim aggregateClause = DirectCast(clause, AggregateClauseSyntax)
                            If lambda Is aggregateClause.Variables.First Then
                                ' first CRV of Aggregate clause represents the entire aggregate lambda
                                Return GetAggregateLambdaBodyExpressions(aggregateClause)
                            Else
                                ' the rest CRVs are translated to their own lambdas
                                Return SpecializedCollections.SingletonEnumerable(lambdaBody)
                            End If

                        Case Else
                            Throw ExceptionUtilities.UnexpectedValue(clause.Kind)
                    End Select

                Case SyntaxKind.JoinCondition
                    Dim joinClause = DirectCast(lambda.Parent, JoinClauseSyntax)
                    Dim joinCondition = DirectCast(lambda, JoinConditionSyntax)

                    If lambdaBody Is joinCondition.Left Then
                        Return EnumerateJoinClauseLeftExpressions(joinClause)
                    Else
                        Return EnumerateJoinClauseRightExpressions(joinClause)
                    End If

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(lambda.Kind)
            End Select
        End Function

        Private Shared Function GetAggregateLambdaBodyExpressions(clause As AggregateClauseSyntax) As IEnumerable(Of SyntaxNode)
            Dim result = ArrayBuilder(Of SyntaxNode).GetInstance()

            result.Add(clause.Variables.First.Expression)

            For Each innerClause In clause.AdditionalQueryOperators
                Select Case innerClause.Kind
                    Case SyntaxKind.TakeClause,
                         SyntaxKind.SkipClause
                        result.Add(DirectCast(innerClause, PartitionClauseSyntax).Count)

                    Case SyntaxKind.GroupJoinClause,
                         SyntaxKind.SimpleJoinClause
                        AddFirstJoinVariableRecursive(result, DirectCast(innerClause, JoinClauseSyntax))

                End Select
            Next

            Return result.ToImmutableAndFree()
        End Function

        Private Shared Sub AddFirstJoinVariableRecursive(result As ArrayBuilder(Of SyntaxNode), joinClause As JoinClauseSyntax)
            result.Add(joinClause.JoinedVariables.First.Expression)

            For Each additionalJoin In joinClause.AdditionalJoins
                AddFirstJoinVariableRecursive(result, additionalJoin)
            Next
        End Sub

        Private Shared Iterator Function EnumerateExpressions(variables As SeparatedSyntaxList(Of ExpressionRangeVariableSyntax)) As IEnumerable(Of SyntaxNode)
            For Each variable In variables
                Yield variable.Expression
            Next
        End Function

        Private Shared Iterator Function EnumerateJoinClauseLeftExpressions(clause As JoinClauseSyntax) As IEnumerable(Of SyntaxNode)
            For Each condition As JoinConditionSyntax In clause.JoinConditions
                Yield condition.Left
            Next
        End Function

        Private Shared Iterator Function EnumerateJoinClauseRightExpressions(clause As JoinClauseSyntax) As IEnumerable(Of SyntaxNode)
            For Each condition As JoinConditionSyntax In clause.JoinConditions
                Yield condition.Right
            Next
        End Function

        ''' <summary>
        ''' If the specified node represents a "simple" lambda returns a node (or nodes) that represent its body (bodies).
        ''' Lambda is "simple" if all its body nodes are also its child nodes and vice versa.
        ''' </summary>
        Private Shared Function TryGetSimpleLambdaBody(node As SyntaxNode, <Out> ByRef lambdaBody As SyntaxNode) As Boolean
            Select Case node.Kind
                Case SyntaxKind.MultiLineFunctionLambdaExpression,
                     SyntaxKind.SingleLineFunctionLambdaExpression,
                     SyntaxKind.MultiLineSubLambdaExpression,
                     SyntaxKind.SingleLineSubLambdaExpression
                    ' The header of the lambda represents its body.
                    lambdaBody = GetLambdaExpressionLambdaBody(DirectCast(node, LambdaExpressionSyntax))

                Case SyntaxKind.WhereClause
                    lambdaBody = DirectCast(node, WhereClauseSyntax).Condition

                Case SyntaxKind.TakeWhileClause,
                     SyntaxKind.SkipWhileClause
                    lambdaBody = DirectCast(node, PartitionWhileClauseSyntax).Condition

                Case SyntaxKind.AscendingOrdering,
                     SyntaxKind.DescendingOrdering
                    lambdaBody = GetOrderingLambdaBody(DirectCast(node, OrderingSyntax))

                Case SyntaxKind.FunctionAggregation
                    ' function call in Group By, Group Join, Aggregate: the argument 
                    lambdaBody = GetAggregationLambdaBody(DirectCast(node, FunctionAggregationSyntax))
                    If lambdaBody Is Nothing Then
                        Return False
                    End If

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
            '   Each ERV in Let clause is translated to a separate lambda.
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

            Dim clause = collectionRangeVariable.Parent

            ' Join clause has a single CRV that is not a lambda on its own
            If IsJoinClause(clause) Then
                Return False
            End If

            Debug.Assert(clause.IsKind(SyntaxKind.FromClause) OrElse clause.IsKind(SyntaxKind.AggregateClause))

            If IsQueryStartingClause(clause) Then
                ' Only the first collection range variable of the starting From/Aggregate clause is not a lambda
                Return collectionRangeVariable IsNot GetCollectionRangeVariables(clause).First
            End If

            ' All variables of any non-starting From/Aggregate clause are lambdas.
            ' The first variable of each Aggregate clause represents the lambda containing the query nested into the aggregate clause.
            Return True
        End Function

        Private Shared Function IsQueryStartingClause(clause As SyntaxNode) As Boolean
            ' Clauses directly contained in a query expression
            Return clause.Parent.IsKind(SyntaxKind.QueryExpression) AndAlso
                   clause Is DirectCast(clause.Parent, QueryExpressionSyntax).Clauses.First
        End Function

        Private Shared Function GetCollectionRangeVariables(clause As SyntaxNode) As SeparatedSyntaxList(Of CollectionRangeVariableSyntax)
            Select Case clause.Kind
                Case SyntaxKind.FromClause
                    Return DirectCast(clause, FromClauseSyntax).Variables

                Case SyntaxKind.AggregateClause
                    Return DirectCast(clause, AggregateClauseSyntax).Variables

                Case SyntaxKind.GroupJoinClause,
                     SyntaxKind.SimpleJoinClause
                    Return DirectCast(clause, JoinClauseSyntax).JoinedVariables

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(clause.Kind)
            End Select
        End Function

        Friend Shared Function IsLambdaJoinCondition(joinCondition As SyntaxNode) As Boolean
            Debug.Assert(joinCondition.IsKind(SyntaxKind.JoinCondition))
            Return joinCondition Is DirectCast(joinCondition.Parent, JoinClauseSyntax).JoinConditions.First
        End Function

        ''' <summary>
        ''' Compares content of two nodes ignoring lambda bodies and trivia.
        ''' </summary>
        Public Shared Function AreEquivalentIgnoringLambdaBodies(oldNode As SyntaxNode, newNode As SyntaxNode) As Boolean
            Return DescendantTokensIgnoringLambdaBodies(oldNode).SequenceEqual(DescendantTokensIgnoringLambdaBodies(newNode), AddressOf SyntaxFactory.AreEquivalent)
        End Function

        ''' <summary>
        ''' Returns all tokens of <paramref name="node"/> that are not part of lambda bodies.
        ''' </summary>
        Public Shared Function DescendantTokensIgnoringLambdaBodies(node As SyntaxNode) As IEnumerable(Of SyntaxToken)
            Return node.DescendantTokens(Function(child) child Is node OrElse Not IsLambdaBodyStatementOrExpression(child))
        End Function

        ''' <summary>
        ''' Non-user code lambdas are synthesized lambdas that create an instance of an anonymous type representing a pair of values,
        ''' or otherwise transform sequences/anonymous types from one form to another without calling user code.
        ''' TODO: Could we avoid generating proper lambdas for these?
        ''' </summary>
        Friend Shared Function IsNonUserCodeQueryLambda(syntax As SyntaxNode) As Boolean
            Return syntax.IsKind(SyntaxKind.GroupJoinClause) OrElse
                   syntax.IsKind(SyntaxKind.SimpleJoinClause) OrElse
                   syntax.IsKind(SyntaxKind.AggregateClause) OrElse
                   syntax.IsKind(SyntaxKind.FromClause) OrElse
                   syntax.IsKind(SyntaxKind.GroupByClause) OrElse
                   syntax.IsKind(SyntaxKind.SimpleAsClause)
        End Function

        ''' <summary>
        ''' Returns true if the specified node can represent a closure scope -- that is a scope of a captured variable.
        ''' Doesn't validate whether or not the node actually declares any captured variable.
        ''' </summary>
        Friend Shared Function IsClosureScope(node As SyntaxNode) As Boolean
            Select Case node.Kind()
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

                Case SyntaxKind.AggregateClause,
                     SyntaxKind.SimpleJoinClause,
                     SyntaxKind.GroupJoinClause
                    ' synthesized closure
                    Return True

                Case SyntaxKind.SingleLineFunctionLambdaExpression,
                     SyntaxKind.SingleLineSubLambdaExpression,
                     SyntaxKind.MultiLineFunctionLambdaExpression,
                     SyntaxKind.MultiLineSubLambdaExpression
                    ' lambda expression body closure
                    Return True

                Case SyntaxKind.ClassBlock, SyntaxKind.StructureBlock, SyntaxKind.ModuleBlock
                    ' With dynamic analysis instrumentation, a type declaration can be the syntax associated
                    ' with the analysis payload local of a synthesized constructor.
                    ' If the synthesized constructor includes an initializer with a lambda,
                    ' that lambda needs a closure that captures the analysis payload of the constructor.
                    Return True

                Case Else
                    If IsLambdaBody(node) Then
                        Return True
                    End If

                    ' TODO: EE expression
                    If node.Parent?.Parent IsNot Nothing AndAlso
                       node.Parent.Parent.Parent Is Nothing Then
                        Return True
                    End If

                    Return False
            End Select
        End Function
    End Class
End Namespace
