' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Text
Imports System.Threading

Namespace Microsoft.CodeAnalysis.VisualBasic.EditAndContinue
    Friend Module BreakpointSpans
        Friend Function TryGetBreakpointSpan(tree As SyntaxTree, position As Integer, cancellationToken As CancellationToken, <Out> ByRef breakpointSpan As TextSpan) As Boolean
            Dim source = tree.GetText(cancellationToken)

            ' If the line is entirely whitespace, then don't set any breakpoint there.
            Dim line = source.Lines.GetLineFromPosition(position)
            If IsBlank(line) Then
                breakpointSpan = Nothing
                Return False
            End If

            ' If the user is asking for breakpoint in an inactive region, then just create a line
            ' breakpoint there.
            If tree.IsInInactiveRegion(position, cancellationToken) Then
                breakpointSpan = Nothing
                Return True
            End If

            Dim root = tree.GetRoot(cancellationToken)
            Return TryGetClosestBreakpointSpan(root, position, minLength:=0, breakpointSpan)
        End Function

        Private Function IsBlank(line As TextLine) As Boolean
            Dim text = line.ToString()

            For i = 0 To text.Length - 1
                If Not SyntaxFacts.IsWhitespace(text(i)) Then
                    Return False
                End If
            Next

            Return True
        End Function

        ''' <summary>
        ''' Given a syntax token determines a text span delimited by the closest applicable sequence points 
        ''' encompassing the token.
        ''' </summary>
        ''' <remarks>
        ''' If the span exists it Is possible To place a breakpoint at the given position.
        ''' </remarks>
        ''' <param name="minLength">
        ''' In case there are multiple breakpoint spans starting at the given <paramref name="position"/>,
        ''' <paramref name="minLength"/> can be used to disambiguate between them. 
        ''' The inner-most available span whose length is at least <paramref name="minLength"/> is returned.
        ''' </param>
        Public Function TryGetClosestBreakpointSpan(root As SyntaxNode, position As Integer, minLength As Integer, <Out> ByRef span As TextSpan) As Boolean
            Dim node = root.FindToken(position).Parent

            Dim candidate As TextSpan? = Nothing
            While node IsNot Nothing
                Dim breakpointSpan = TryCreateSpanForNode(node, position)
                If breakpointSpan.HasValue Then
                    If breakpointSpan.Value = New TextSpan() Then
                        Exit While
                    End If

                    ' the new breakpoint span doesn't alight with the previously found breakpoint span, return the previous one:
                    If candidate.HasValue AndAlso breakpointSpan.Value.Start <> candidate.Value.Start Then
                        span = candidate.Value
                        Return True
                    End If

                    ' The span length meets the requirement:
                    If breakpointSpan.Value.Length >= minLength Then
                        span = breakpointSpan.Value
                        Return True
                    End If

                    candidate = breakpointSpan
                End If

                node = node.Parent
            End While

            span = candidate.GetValueOrDefault()
            Return candidate.HasValue
        End Function

        Private Function CreateSpan(node As SyntaxNode) As TextSpan
            Return TextSpan.FromBounds(node.SpanStart, node.Span.End)
        End Function

        Private Function TryCreateSpan(Of TNode As SyntaxNode)(list As SeparatedSyntaxList(Of TNode)) As TextSpan?
            If list.Count = 0 Then
                Return Nothing
            End If

            Return TextSpan.FromBounds(list.First.SpanStart, list.Last.Span.End)
        End Function

        Private Function TryCreateSpanForNode(node As SyntaxNode, position As Integer) As TextSpan?
            Select Case node.Kind
                Case SyntaxKind.VariableDeclarator,
                     SyntaxKind.ModifiedIdentifier
                    ' Handled by parent field or local variable declaration.
                    Return Nothing

                Case SyntaxKind.FieldDeclaration
                    Dim fieldDeclaration = DirectCast(node, FieldDeclarationSyntax)
                    Return TryCreateSpanForVariableDeclaration(fieldDeclaration.Modifiers, fieldDeclaration.Declarators, position)

                Case SyntaxKind.LocalDeclarationStatement
                    Dim localDeclaration = DirectCast(node, LocalDeclarationStatementSyntax)
                    Return TryCreateSpanForVariableDeclaration(localDeclaration.Modifiers, localDeclaration.Declarators, position)

                Case SyntaxKind.PropertyStatement
                    Return TryCreateSpanForPropertyStatement(DirectCast(node, PropertyStatementSyntax))

                ' Statements that are not executable yet marked with sequence points
                Case SyntaxKind.IfStatement,
                     SyntaxKind.ElseIfStatement,
                     SyntaxKind.ElseStatement,
                     SyntaxKind.EndIfStatement,
                     SyntaxKind.UsingStatement,
                     SyntaxKind.EndUsingStatement,
                     SyntaxKind.SyncLockStatement,
                     SyntaxKind.EndSyncLockStatement,
                     SyntaxKind.WithStatement,
                     SyntaxKind.EndWithStatement,
                     SyntaxKind.SimpleDoStatement, SyntaxKind.DoWhileStatement, SyntaxKind.DoUntilStatement,
                     SyntaxKind.SimpleLoopStatement, SyntaxKind.LoopWhileStatement, SyntaxKind.LoopUntilStatement,
                     SyntaxKind.WhileStatement,
                     SyntaxKind.EndWhileStatement,
                     SyntaxKind.ForStatement,
                     SyntaxKind.ForEachStatement,
                     SyntaxKind.NextStatement,
                     SyntaxKind.SelectStatement,
                     SyntaxKind.CaseStatement,
                     SyntaxKind.CaseElseStatement,
                     SyntaxKind.EndSelectStatement,
                     SyntaxKind.TryStatement,
                     SyntaxKind.CatchStatement,
                     SyntaxKind.FinallyStatement,
                     SyntaxKind.EndTryStatement,
                     SyntaxKind.EndSubStatement,
                     SyntaxKind.EndFunctionStatement,
                     SyntaxKind.EndOperatorStatement,
                     SyntaxKind.EndGetStatement,
                     SyntaxKind.EndSetStatement,
                     SyntaxKind.EndAddHandlerStatement,
                     SyntaxKind.EndRemoveHandlerStatement,
                     SyntaxKind.EndRaiseEventStatement,
                     SyntaxKind.FunctionLambdaHeader,
                     SyntaxKind.SubLambdaHeader
                    Return CreateSpan(node)

                Case SyntaxKind.SubStatement,
                     SyntaxKind.SubNewStatement,
                     SyntaxKind.FunctionStatement,
                     SyntaxKind.OperatorStatement,
                     SyntaxKind.GetAccessorStatement,
                     SyntaxKind.SetAccessorStatement,
                     SyntaxKind.AddHandlerAccessorStatement,
                     SyntaxKind.RemoveHandlerAccessorStatement,
                     SyntaxKind.RaiseEventAccessorStatement
                    Return CreateSpanForMethodBase(DirectCast(node, MethodBaseSyntax))

                Case SyntaxKind.SingleLineIfStatement
                    Dim asSingleLine = DirectCast(node, SingleLineIfStatementSyntax)

                    If position >= asSingleLine.IfKeyword.SpanStart AndAlso position < asSingleLine.ThenKeyword.Span.End Then
                        Return TextSpan.FromBounds(asSingleLine.IfKeyword.SpanStart, asSingleLine.ThenKeyword.Span.End)
                    Else
                        Return CreateSpan(node)
                    End If

                Case SyntaxKind.SingleLineElseClause
                    Dim asSingleLineElse = DirectCast(node, SingleLineElseClauseSyntax)

                    Return asSingleLineElse.ElseKeyword.Span

                Case SyntaxKind.FunctionAggregation
                    Return TryCreateSpanForFunctionAggregation(DirectCast(node, FunctionAggregationSyntax))

                Case SyntaxKind.SingleLineFunctionLambdaExpression,
                     SyntaxKind.SingleLineSubLambdaExpression
                    Return CreateSpan(node)

                Case SyntaxKind.SelectClause
                    Return TryCreateSpanForSelectClause(DirectCast(node, SelectClauseSyntax))

                Case SyntaxKind.WhereClause
                    Return TryCreateSpanForWhereClause(DirectCast(node, WhereClauseSyntax))

                Case SyntaxKind.CollectionRangeVariable
                    Return TryCreateSpanForCollectionRangeVariable(DirectCast(node, CollectionRangeVariableSyntax))

                Case SyntaxKind.LetClause
                    Return TryCreateSpanForLetClause(DirectCast(node, LetClauseSyntax), position)

                Case SyntaxKind.GroupByClause
                    Return TryCreateSpanForGroupByClause(DirectCast(node, GroupByClauseSyntax), position)

                Case SyntaxKind.SkipWhileClause,
                     SyntaxKind.TakeWhileClause
                    Return TryCreateSpanForPartitionWhileClauseSyntax(DirectCast(node, PartitionWhileClauseSyntax))

                Case SyntaxKind.AscendingOrdering,
                     SyntaxKind.DescendingOrdering
                    Return TryCreateSpanForOrderingSyntax(DirectCast(node, OrderingSyntax))

                Case SyntaxKind.OrderByClause
                    Return TryCreateSpanForOrderByClause(DirectCast(node, OrderByClauseSyntax), position)

                Case SyntaxKind.FromClause
                    Return TryCreateSpanForFromClause(DirectCast(node, FromClauseSyntax), position)

                Case Else
                    Dim executableStatement = TryCast(node, ExecutableStatementSyntax)
                    If executableStatement IsNot Nothing Then
                        Return CreateSpan(node)
                    End If

                    Dim expression = TryCast(node, ExpressionSyntax)
                    If expression IsNot Nothing Then
                        Return TryCreateSpanForExpression(expression)
                    End If

                    Return Nothing
            End Select
        End Function

        Private Function CreateSpanForMethodBase(methodBase As MethodBaseSyntax) As TextSpan
            If methodBase.Modifiers.Count = 0 Then
                Return TextSpan.FromBounds(methodBase.DeclarationKeyword.SpanStart, methodBase.Span.End)
            End If

            Return TextSpan.FromBounds(methodBase.Modifiers.First().SpanStart, methodBase.Span.End)
        End Function

        Private Function TryCreateSpanForPropertyStatement(node As PropertyStatementSyntax) As TextSpan?
            If node.Parent.IsKind(SyntaxKind.PropertyBlock) Then
                ' not an auto-property:
                Return Nothing
            End If

            If node.Initializer IsNot Nothing Then
                Return TextSpan.FromBounds(node.Identifier.Span.Start, node.Initializer.Span.End)
            End If

            If node.AsClause IsNot Nothing AndAlso node.AsClause.IsKind(SyntaxKind.AsNewClause) Then
                Return TextSpan.FromBounds(node.Identifier.Span.Start, node.AsClause.Span.End)
            End If

            Return Nothing
        End Function

        Private Function TryCreateSpanForVariableDeclaration(modifiers As SyntaxTokenList, declarators As SeparatedSyntaxList(Of VariableDeclaratorSyntax), position As Integer) As TextSpan?
            If declarators.Count = 0 Then
                Return Nothing
            End If

            If modifiers.Any(SyntaxKind.ConstKeyword) Then
                Return New TextSpan()
            End If

            Dim name = FindClosestNameWithInitializer(declarators, position)
            If name Is Nothing Then
                Return New TextSpan()
            End If

            If name.ArrayBounds IsNot Nothing OrElse DirectCast(name.Parent, VariableDeclaratorSyntax).Names.Count > 1 Then
                Return CreateSpan(name)
            Else
                Return CreateSpan(name.Parent)
            End If
        End Function

        Private Function FindClosestNameWithInitializer(declarators As SeparatedSyntaxList(Of VariableDeclaratorSyntax), position As Integer) As ModifiedIdentifierSyntax
            Return FindClosestNode(declarators, position,
                Function(declarator)
                    If declarator.HasInitializer Then
                        Return declarator.Names(GetItemIndexByPosition(declarator.Names, position))
                    End If

                    Return FindClosestNode(declarator.Names, position, Function(idf)
                                                                           Return If(idf.ArrayBounds IsNot Nothing, idf, Nothing)
                                                                       End Function)
                End Function)
        End Function

        Private Function FindClosestNode(Of TListNode As SyntaxNode, TResult As SyntaxNode)(nodes As SeparatedSyntaxList(Of TListNode), position As Integer, predicate As Func(Of TListNode, TResult)) As TResult
            Dim d = GetItemIndexByPosition(nodes, position)

            Dim i = 0
            Do
                Dim left = d - i
                Dim right = d + i

                If left < 0 AndAlso right >= nodes.Count Then
                    Return Nothing
                End If

                If left >= 0 Then
                    Dim result = predicate(nodes(left))
                    If result IsNot Nothing Then
                        Return result
                    End If
                End If

                If right < nodes.Count Then
                    Dim result = predicate(nodes(right))
                    If result IsNot Nothing Then
                        Return result
                    End If
                End If

                i += 1
            Loop
        End Function

        Private Function GetItemIndexByPosition(Of TNode As SyntaxNode)(list As SeparatedSyntaxList(Of TNode), position As Integer) As Integer
            For i = list.SeparatorCount - 1 To 0 Step -1
                If position > list.GetSeparator(i).SpanStart Then
                    Return i + 1
                End If
            Next

            Return 0
        End Function

        Private Function TryCreateSpanForFromClause(fromClause As FromClauseSyntax, position As Integer) As TextSpan?
            Dim query = DirectCast(fromClause.Parent, QueryExpressionSyntax)

            ' If it's not the first from clause, then you can set the breakpoint on the first
            ' variable.
            If query.Clauses.First() IsNot fromClause AndAlso fromClause.Variables.Any() Then
                Return TryCreateSpanForNode(fromClause.Variables.First(), position)
            End If

            ' If it is the first from clause, you can only set the breakpoint on the second or
            ' higher variable.
            If query.Clauses.First() Is fromClause AndAlso fromClause.Variables.Count > 1 Then
                Return TryCreateSpanForNode(fromClause.Variables(1), position)
            End If

            Return Nothing
        End Function

        Private Function TryCreateSpanForFunctionAggregation(functionAggregation As FunctionAggregationSyntax) As TextSpan?
            If functionAggregation.Argument IsNot Nothing Then
                Return CreateSpan(functionAggregation.Argument)
            End If

            Return Nothing
        End Function

        Private Function TryCreateSpanForOrderByClause(orderByClause As OrderByClauseSyntax, position As Integer) As TextSpan?
            If orderByClause.Orderings.Any() Then
                Return TryCreateSpanForNode(orderByClause.Orderings.First(), position)
            End If

            Return Nothing
        End Function

        Private Function TryCreateSpanForOrderingSyntax(orderingSyntax As OrderingSyntax) As TextSpan?
            Return CreateSpan(orderingSyntax.Expression)
        End Function

        Private Function TryCreateSpanForPartitionWhileClauseSyntax(partitionWhileClause As PartitionWhileClauseSyntax) As TextSpan?
            Return CreateSpan(partitionWhileClause.Condition)
        End Function

        Private Function TryCreateSpanForCollectionRangeVariable(collectionRangeVariable As CollectionRangeVariableSyntax) As TextSpan?
            If collectionRangeVariable.Parent.Kind = SyntaxKind.FromClause Then
                Dim fromClause = DirectCast(collectionRangeVariable.Parent, FromClauseSyntax)
                Dim query = DirectCast(fromClause.Parent, QueryExpressionSyntax)

                ' We can break on this expression if we're not the first clause in a
                ' query, or if the range variable it not the first range variable in the
                ' list.
                If query.Clauses.First() IsNot fromClause OrElse fromClause.Variables.IndexOf(collectionRangeVariable) <> 0 Then
                    Return CreateSpan(collectionRangeVariable.Expression)
                End If
            End If

            Return Nothing
        End Function

        Private Function TryCreateSpanForWhereClause(clause As WhereClauseSyntax) As TextSpan?
            Return CreateSpan(clause.Condition)
        End Function

        Private Function TryCreateSpanForGroupByClause(clause As GroupByClauseSyntax, position As Integer) As TextSpan?
            If position < clause.ByKeyword.SpanStart Then
                If clause.Items.Count = 1 Then
                    Return CreateSpan(clause.Items.Single.Expression)
                End If

                Return TryCreateSpan(clause.Items)
            End If

            If clause.Keys.Count = 0 Then
                Return Nothing
            End If

            If position >= clause.Keys.First.SpanStart AndAlso position < clause.IntoKeyword.SpanStart Then
                If clause.Keys.Count = 1 Then
                    Return CreateSpan(clause.Keys.Single.Expression)
                End If

                Return TryCreateSpan(clause.Keys)
            End If

            Return TextSpan.FromBounds(clause.Keys.First.SpanStart, clause.Span.End)
        End Function

        Private Function TryCreateSpanForSelectClause(clause As SelectClauseSyntax) As TextSpan?
            If clause.Variables.Count = 1 Then
                Return CreateSpan(clause.Variables.Single.Expression)
            End If

            Return TryCreateSpan(clause.Variables)
        End Function

        Private Function TryCreateSpanForLetClause(clause As LetClauseSyntax, position As Integer) As TextSpan?
            Return clause.Variables(GetItemIndexByPosition(clause.Variables, position)).Expression.Span
        End Function

        Private Function TryCreateSpanForExpression(expression As ExpressionSyntax) As TextSpan?
            If IsBreakableExpression(expression) Then
                Return CreateSpan(expression)
            End If

            Return Nothing
        End Function

        Private Function IsBreakableExpression(expression As ExpressionSyntax) As Boolean
            If expression Is Nothing OrElse expression.Parent Is Nothing Then
                Return False
            End If

            Select Case expression.Parent.Kind
                Case SyntaxKind.JoinCondition
                    Dim joinCondition = DirectCast(expression.Parent, JoinConditionSyntax)
                    Return expression Is joinCondition.Left OrElse expression Is joinCondition.Right

                Case SyntaxKind.SingleLineFunctionLambdaExpression
                    Dim lambda = DirectCast(expression.Parent, SingleLineLambdaExpressionSyntax)
                    Return expression Is lambda.Body
            End Select

            Return False
        End Function
    End Module
End Namespace
