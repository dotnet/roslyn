' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Runtime.InteropServices
Imports System.Threading
Imports Microsoft.CodeAnalysis.Differencing
Imports Microsoft.CodeAnalysis.EditAndContinue
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.EditAndContinue
    Friend NotInheritable Class SyntaxUtilities
        <Conditional("DEBUG")>
        Public Shared Sub AssertIsBody(syntax As SyntaxNode, allowLambda As Boolean)
            ' lambda/query
            Dim body As SyntaxNode = Nothing
            If IsLambdaBodyStatement(syntax, body) AndAlso syntax Is body Then
                Debug.Assert(allowLambda)
                Debug.Assert(TypeOf syntax Is ExpressionSyntax OrElse TypeOf syntax Is LambdaHeaderSyntax)
                Return
            End If

            ' sub/function/ctor/operator/accessor
            If TypeOf syntax Is MethodBlockBaseSyntax Then
                Return
            End If

            ' field/property initializer
            If TypeOf syntax Is ExpressionSyntax Then
                If syntax.Parent.Kind = SyntaxKind.EqualsValue Then
                    If syntax.Parent.Parent.IsKind(SyntaxKind.PropertyStatement) Then
                        Return
                    End If

                    Debug.Assert(syntax.Parent.Parent.IsKind(SyntaxKind.VariableDeclarator))
                    Debug.Assert(syntax.Parent.Parent.Parent.IsKind(SyntaxKind.FieldDeclaration))
                    Return
                ElseIf syntax.Parent.Kind = SyntaxKind.AsNewClause Then
                    If syntax.Parent.Parent.IsKind(SyntaxKind.PropertyStatement) Then
                        Return
                    End If

                    Debug.Assert(syntax.Parent.Parent.IsKind(SyntaxKind.VariableDeclarator))
                    Debug.Assert(syntax.Parent.Parent.Parent.IsKind(SyntaxKind.FieldDeclaration))
                    Return
                End If
            End If

            ' field array initializer
            If TypeOf syntax Is ArgumentListSyntax Then
                Debug.Assert(syntax.Parent.IsKind(SyntaxKind.ModifiedIdentifier))
                Debug.Assert(syntax.Parent.Parent.IsKind(SyntaxKind.VariableDeclarator))
                Debug.Assert(syntax.Parent.Parent.Parent.IsKind(SyntaxKind.FieldDeclaration))
                Return
            End If

            Debug.Assert(False)
        End Sub

        Public Shared Function IsLambdaBodyStatement(node As SyntaxNode, <Out> ByRef body As SyntaxNode) As Boolean
            Dim parent = node.Parent
            If parent Is Nothing Then
                body = Nothing
                Return False
            End If

            body = node
            Select Case parent.Kind
                Case SyntaxKind.MultiLineFunctionLambdaExpression,
                     SyntaxKind.SingleLineFunctionLambdaExpression,
                     SyntaxKind.MultiLineSubLambdaExpression,
                     SyntaxKind.SingleLineSubLambdaExpression
                    ' The header of the lambda represents its body.
                    body = DirectCast(parent, LambdaExpressionSyntax).SubOrFunctionHeader
                    Return True

                Case SyntaxKind.WhereClause
                    Dim whereClause = DirectCast(parent, WhereClauseSyntax)
                    Return whereClause.Condition Is node

                ' source sequence in From and Aggregate (other than the first in the query)
                Case SyntaxKind.CollectionRangeVariable
                    Dim collectionRange = DirectCast(parent, CollectionRangeVariableSyntax)
                    Return collectionRange.Expression Is node AndAlso Not IsFirstInQuery(collectionRange)

                ' function call in Group By, Group Join, Aggregate: the argument 
                Case SyntaxKind.FunctionAggregation
                    Dim functionAggregation = DirectCast(parent, FunctionAggregationSyntax)
                    Return functionAggregation.Argument Is node

                ' variable in Let, Select, Group By: the RHS
                Case SyntaxKind.ExpressionRangeVariable
                    Dim expressionRange = DirectCast(parent, ExpressionRangeVariableSyntax)
                    Return expressionRange.Expression Is node

                Case SyntaxKind.TakeWhileClause,
                     SyntaxKind.SkipWhileClause
                    Dim partitionWhileClause = DirectCast(parent, PartitionWhileClauseSyntax)
                    Return partitionWhileClause.Condition Is node

                Case SyntaxKind.AscendingOrdering,
                     SyntaxKind.DescendingOrdering
                    Dim ordering = DirectCast(parent, OrderingSyntax)
                    Return ordering.Expression Is node

                Case SyntaxKind.JoinCondition
                    Dim joinCondition = DirectCast(parent, JoinConditionSyntax)
                    Return joinCondition.Left Is node OrElse joinCondition.Right Is node
            End Select

            Debug.Assert(Not SyntaxUtilities.IsLambda(parent.Kind))
            Return False
        End Function

        ' TODO(tomat): similar check is needed in breakpoint spans
        Private Shared Function IsFirstInQuery(collectionRangeVariable As CollectionRangeVariableSyntax) As Boolean
            Dim parent = collectionRangeVariable.Parent

            Dim query = DirectCast(parent.Parent, QueryExpressionSyntax)
            If query.Clauses.First() Is parent Then
                Return True
            End If

            Dim variables As SeparatedSyntaxList(Of CollectionRangeVariableSyntax)

            Select Case parent.Kind
                Case SyntaxKind.FromClause
                    variables = DirectCast(parent, FromClauseSyntax).Variables

                Case SyntaxKind.AggregateClause
                    variables = DirectCast(parent, AggregateClauseSyntax).Variables

                Case SyntaxKind.GroupJoinClause, SyntaxKind.SimpleJoinClause
                    variables = DirectCast(parent, JoinClauseSyntax).JoinedVariables

                Case Else
                    Throw ExceptionUtilities.Unreachable
            End Select

            Return variables.IndexOf(collectionRangeVariable) = 0
        End Function

        Public Shared Function GetPartnerLambdaBody(oldBody As SyntaxNode, newLambda As SyntaxNode) As SyntaxNode
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

                Case SyntaxKind.CollectionRangeVariable
                    Return DirectCast(newLambda, CollectionRangeVariableSyntax).Expression

                Case SyntaxKind.FunctionAggregation
                    Return DirectCast(newLambda, FunctionAggregationSyntax).Argument

                Case SyntaxKind.ExpressionRangeVariable
                    Return DirectCast(newLambda, ExpressionRangeVariableSyntax).Expression

                Case SyntaxKind.TakeWhileClause, SyntaxKind.SkipWhileClause
                    Return DirectCast(newLambda, PartitionWhileClauseSyntax).Condition

                Case SyntaxKind.AscendingOrdering, SyntaxKind.DescendingOrdering
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

        Public Shared Sub FindLeafNodeAndPartner(leftRoot As SyntaxNode,
                                          leftPosition As Integer,
                                          rightRoot As SyntaxNode,
                                          <Out> ByRef leftNode As SyntaxNode,
                                          <Out> ByRef rightNode As SyntaxNode)
            leftNode = leftRoot
            rightNode = rightRoot
            While True
                Debug.Assert(leftNode.RawKind = rightNode.RawKind)
                Dim childIndex As Integer = 0
                Dim leftChild = leftNode.ChildThatContainsPosition(leftPosition, childIndex)
                If leftChild.IsToken Then
                    Return
                End If

                rightNode = rightNode.ChildNodesAndTokens().ElementAt(childIndex).AsNode()
                leftNode = leftChild.AsNode()
            End While
        End Sub

        Public Shared Function IsNotLambda(node As SyntaxNode) As Boolean
            Return Not IsLambda(node.Kind())
        End Function

        Public Shared Function IsLambda(kind As SyntaxKind) As Boolean
            Select Case kind
                Case SyntaxKind.MultiLineFunctionLambdaExpression,
                     SyntaxKind.SingleLineFunctionLambdaExpression,
                     SyntaxKind.MultiLineSubLambdaExpression,
                     SyntaxKind.SingleLineSubLambdaExpression,
                     SyntaxKind.WhereClause,
                     SyntaxKind.CollectionRangeVariable,
                     SyntaxKind.FunctionAggregation,
                     SyntaxKind.ExpressionRangeVariable,
                     SyntaxKind.TakeWhileClause,
                     SyntaxKind.SkipWhileClause,
                     SyntaxKind.AscendingOrdering,
                     SyntaxKind.DescendingOrdering,
                     SyntaxKind.JoinCondition
                    Return True
            End Select

            Return False
        End Function

        Public Shared Function IsMethod(declaration As SyntaxNode) As Boolean
            Select Case declaration.Kind
                Case SyntaxKind.SubBlock,
                     SyntaxKind.FunctionBlock,
                     SyntaxKind.ConstructorBlock,
                     SyntaxKind.OperatorBlock,
                     SyntaxKind.GetAccessorBlock,
                     SyntaxKind.SetAccessorBlock,
                     SyntaxKind.AddHandlerAccessorBlock,
                     SyntaxKind.RemoveHandlerAccessorBlock,
                     SyntaxKind.RaiseEventAccessorBlock
                    Return True

                Case Else
                    Return False
            End Select
        End Function

        Public Shared Function IsParameterlessConstructor(declaration As SyntaxNode) As Boolean
            If Not declaration.IsKind(SyntaxKind.ConstructorBlock) Then
                Return False
            End If

            Dim ctor = DirectCast(declaration, ConstructorBlockSyntax)
            Return ctor.BlockStatement.ParameterList.Parameters.Count = 0
        End Function

        Public Shared Function HasBackingField(propertyDeclaration As SyntaxNode) As Boolean
            Return propertyDeclaration.IsKind(SyntaxKind.PropertyStatement) AndAlso
                   Not DirectCast(propertyDeclaration, PropertyStatementSyntax).Modifiers.Any(SyntaxKind.MustOverrideKeyword)
        End Function

        Public Shared Function IsAsyncMethodOrLambda(node As SyntaxNode) As Boolean
            ' TODO: check Lambda

            Select Case node.Kind
                Case SyntaxKind.SubBlock,
                     SyntaxKind.FunctionBlock
                    Return DirectCast(node, MethodBlockBaseSyntax).BlockStatement.Modifiers.Any(SyntaxKind.AsyncKeyword)
            End Select

            Return False
        End Function

        Public Shared Function GetAwaitExpressions(body As SyntaxNode) As ImmutableArray(Of SyntaxNode)
            ' skip lambda bodies
            Return ImmutableArray.CreateRange(Of SyntaxNode)(body.DescendantNodes(Function(n) IsNotLambda(n)).
                Where(Function(n) n.IsKind(SyntaxKind.AwaitExpression)))
        End Function

        Public Shared Function GetYieldStatements(body As SyntaxNode) As ImmutableArray(Of SyntaxNode)
            ' enumerate statements:
            Return ImmutableArray.CreateRange(Of SyntaxNode)(body.DescendantNodes(Function(n) TypeOf n IsNot ExpressionSyntax).
                Where(Function(n) n.IsKind(SyntaxKind.YieldStatement)))

        End Function
    End Class
End Namespace
