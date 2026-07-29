' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Option Strict Off

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CSharp
Imports Microsoft.CodeAnalysis.CSharp.SyntaxFactory
Imports Microsoft.CodeAnalysis.VisualBasic
Imports CS = Microsoft.CodeAnalysis.CSharp
Imports VB = Microsoft.CodeAnalysis.VisualBasic

Namespace VisualBasicToCSharpConverter

    Partial Public Class Converter

        Partial Private Class NodeConvertingVisitor

            Public Class QueryClauseConvertingVisitor
                Inherits VisualBasicSyntaxVisitor(Of Object)

                Private ReadOnly Parent As NodeConvertingVisitor

                Private IsFirstAfterSelect As Boolean
                Private InitialClause As CS.Syntax.FromClauseSyntax
                Private Clauses As New List(Of CS.Syntax.QueryClauseSyntax)()
                Private Expression As CS.Syntax.ExpressionSyntax
                Private RangeVariablesInScope As New List(Of String)()
                Private SelectOrGroupClause As CS.Syntax.SelectOrGroupClauseSyntax
                Private Continuation As CS.Syntax.QueryContinuationSyntax

                Public Sub New(parent As NodeConvertingVisitor)
                    Me.Parent = parent
                End Sub

                Public Overrides Function VisitFromClause(node As VB.Syntax.FromClauseSyntax) As Object

                    ' TODO: Implement call to .Cast or .Select with cast if type of As Clause doesn't match element type of source.
                    For Each crv In node.Variables
                        Dim clause = FromClause(Parent.VisitIdentifier(crv.Identifier.Identifier), Parent.Visit(crv.Expression)).WithType(Parent.DeriveType(crv))
                        If (InitialClause Is Nothing) Then
                            InitialClause = clause
                        Else
                            Clauses.Add(clause)
                        End If

                        RangeVariablesInScope.Add(crv.Identifier.Identifier.ValueText)
                    Next

                    Return Nothing
                End Function

                Public Overrides Function VisitLetClause(node As VB.Syntax.LetClauseSyntax) As Object

                    For Each erv In node.Variables
                        Clauses.Add(LetClause(Parent.VisitIdentifier(erv.NameEquals.Identifier.Identifier), Parent.Visit(erv.Expression)))

                        RangeVariablesInScope.Add(erv.NameEquals.Identifier.Identifier.ValueText)
                    Next

                    Return Nothing
                End Function

                Public Overrides Function VisitAggregateClause(node As VB.Syntax.AggregateClauseSyntax) As Object

                    Return WhereClause(MissingToken(CS.SyntaxKind.WhereKeyword), Parent.NotImplementedExpression(node))

                    Throw New NotImplementedException(node.ToString())
                End Function

                Public Overrides Function VisitDistinctClause(node As VB.Syntax.DistinctClauseSyntax) As Object
                    Throw New InvalidOperationException()
                End Function

                Public Overrides Function VisitWhereClause(node As VB.Syntax.WhereClauseSyntax) As Object

                    Clauses.Add(WhereClause(Parent.Visit(node.Condition)))

                    Return Nothing
                End Function

                ' Take While, Skip While
                Public Overrides Function VisitPartitionWhileClause(node As VB.Syntax.PartitionWhileClauseSyntax) As Object
                    Return WhereClause(MissingToken(CS.SyntaxKind.WhereKeyword), Parent.NotImplementedExpression(node))

                    Throw New NotImplementedException(node.ToString())
                End Function

                ' Take, Skip
                Public Overrides Function VisitPartitionClause(node As VB.Syntax.PartitionClauseSyntax) As Object
                    Return WhereClause(MissingToken(CS.SyntaxKind.WhereKeyword), Parent.NotImplementedExpression(node))

                    Throw New NotImplementedException(node.ToString())
                End Function

                Public Overrides Function VisitGroupByClause(node As VB.Syntax.GroupByClauseSyntax) As Object
                    Return WhereClause(MissingToken(CS.SyntaxKind.WhereKeyword), Parent.NotImplementedExpression(node))

                    Throw New NotImplementedException(node.ToString())

                End Function

                Public Overrides Function VisitSimpleJoinClause(node As VB.Syntax.SimpleJoinClauseSyntax) As Object

                    If node.AdditionalJoins.Count > 0 Then Return WhereClause(MissingToken(CS.SyntaxKind.WhereKeyword), Parent.NotImplementedExpression(node)) 'Throw New NotImplementedException("Joins with additional nested joins.")
                    If node.JoinConditions.Count > 1 Then Return WhereClause(MissingToken(CS.SyntaxKind.WhereKeyword), Parent.NotImplementedExpression(node)) ' Throw New NotImplementedException("Joins with multiple conditions.")

                    Clauses.Add(JoinClause(
                                    Parent.VisitIdentifier(node.JoinedVariables(0).Identifier.Identifier),
                                    Parent.Visit(node.JoinedVariables(0).Expression),
                                    Parent.Visit(node.JoinConditions(0).Left),
                                    Parent.Visit(node.JoinConditions(0).Right)
                                ).WithType(Parent.DeriveType(node.JoinedVariables(0)))
                            )

                    RangeVariablesInScope.Add(node.JoinedVariables(0).Identifier.Identifier.Value)

                    Return Nothing
                End Function

                Public Overrides Function VisitGroupJoinClause(node As VB.Syntax.GroupJoinClauseSyntax) As Object
                    Return WhereClause(MissingToken(CS.SyntaxKind.WhereKeyword), Parent.NotImplementedExpression(node))

                    Throw New NotImplementedException(node.ToString())
                End Function

                Public Overrides Function VisitOrderByClause(node As VB.Syntax.OrderByClauseSyntax) As Object

                    Clauses.Add(OrderByClause(SeparatedList(VisitOrderings(node.Orderings))))

                    Return Nothing
                End Function

                Protected Shadows Function VisitOrdering(node As VB.Syntax.OrderingSyntax) As CS.Syntax.OrderingSyntax

                    If node.IsKind(VB.SyntaxKind.AscendingOrdering) Then
                        Return Ordering(CS.SyntaxKind.AscendingOrdering, Parent.Visit(node.Expression))
                    Else
                        Return Ordering(CS.SyntaxKind.DescendingOrdering, Parent.Visit(node.Expression))
                    End If

                End Function

                Protected Function VisitOrderings(nodes As IEnumerable(Of VB.Syntax.OrderingSyntax)) As IEnumerable(Of CS.Syntax.OrderingSyntax)

                    Return From node In nodes Select VisitOrdering(node)

                End Function


                Public Overrides Function VisitSelectClause(node As VB.Syntax.SelectClauseSyntax) As Object

                    Dim variables As New List(Of CS.Syntax.ExpressionSyntax)()

                    RangeVariablesInScope.Clear()
                    If node.Variables.Count = 1 Then

                        SelectOrGroupClause = SelectClause(Parent.Visit(node.Variables(0).Expression))

                        RangeVariablesInScope.Add(DeriveRangeVariableName(node.Variables(0)))
                    Else

                        For Each v In node.Variables

                            If v.NameEquals IsNot Nothing Then
                                variables.Add(AssignmentExpression(CS.SyntaxKind.SimpleAssignmentExpression, IdentifierName(Parent.VisitIdentifier(v.NameEquals.Identifier.Identifier)), Parent.Visit(v.Expression)))
                            Else
                                variables.Add(Parent.Visit(v.Expression))
                            End If

                            RangeVariablesInScope.Add(DeriveRangeVariableName(v))
                        Next

                        SelectOrGroupClause = SelectClause(
                                                  AnonymousObjectCreationExpression(
                                                      SeparatedList(From variable In variables Select AnonymousObjectMemberDeclarator(variable))
                                                  )
                                              )
                    End If

                    IsFirstAfterSelect = True

                    Return Nothing
                End Function

                Public Overrides Function VisitQueryExpression(node As VB.Syntax.QueryExpressionSyntax) As Object

                    Dim clauses = node.Clauses

                    Dim aggregate = TryCast(clauses.First, VB.Syntax.AggregateClauseSyntax)

                    If aggregate IsNot Nothing Then

                        Return Parent.NotImplementedExpression(node)

                        Throw New NotImplementedException(node.ToString())

                        clauses = aggregate.AdditionalQueryOperators

                    Else

                        For Each c In clauses

                            If c.IsKind(VB.SyntaxKind.DistinctClause) Then

                                EndQuery()

                                Expression = InvocationExpression(
                                                 MemberAccessExpression(
                                                     CS.SyntaxKind.SimpleMemberAccessExpression,
                                                     ParenthesizedExpression(Expression),
                                                     IdentifierName("Distinct")
                                                 ),
                                                 ArgumentList()
                                             )

                                Continue For

                            ElseIf IsFirstAfterSelect Then

                                EndQuery()

                                BringRangeVariablesIntoScope()

                                IsFirstAfterSelect = False

                            End If

                            Visit(c)

                        Next

                        EndQuery()
                    End If

                    Return Expression
                End Function

                Protected Function DeriveRangeVariableName(variable As VB.Syntax.ExpressionRangeVariableSyntax) As String

                    If variable.NameEquals IsNot Nothing Then
                        Return variable.NameEquals.Identifier.Identifier.ValueText
                    End If

                    Return Parent.DeriveName(variable.Expression)

                End Function

                Private Sub EndQuery()

                    ' This means the query terminated in an aggregate.
                    If InitialClause Is Nothing And Clauses.Count = 0 Then Return

                    ' If this query omitted the Select clause, synthesize it.
                    If Not IsFirstAfterSelect Then

                        If RangeVariablesInScope.Count = 1 Then

                            SelectOrGroupClause = SelectClause(IdentifierName(RangeVariablesInScope(0)))

                        Else
                            SelectOrGroupClause = SelectClause(
                                                      AnonymousObjectCreationExpression(
                                                          SeparatedList(From n In RangeVariablesInScope
                                                                        Select AnonymousObjectMemberDeclarator(IdentifierName(n)))
                                                      )
                                                  )
                        End If

                        IsFirstAfterSelect = True
                    End If

                    Expression = QueryExpression(InitialClause, QueryBody(List(Clauses), SelectOrGroupClause, Continuation))

                    Clauses.Clear()
                    InitialClause = Nothing
                    SelectOrGroupClause = Nothing
                    IsFirstAfterSelect = False
                    Continuation = Nothing
                End Sub

                Private Sub BringRangeVariablesIntoScope()

                    If RangeVariablesInScope.Count = 1 Then

                        Clauses.Add(FromClause(Identifier(RangeVariablesInScope(0)), Expression))

                    Else
                        Clauses.Add(FromClause(Identifier("_"), Expression))

                        For Each v In RangeVariablesInScope
                            Clauses.Add(LetClause(Identifier(v), MemberAccessExpression(CS.SyntaxKind.SimpleMemberAccessExpression, IdentifierName("_"), IdentifierName(v))))
                        Next
                    End If

                    Expression = Nothing
                End Sub

            End Class

        End Class

    End Class

End Namespace
