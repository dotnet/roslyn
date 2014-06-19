

Namespace Roslyn.Compilers.VisualBasic

    Partial Class BoundQueryClauseBase

        Public Overrides ReadOnly Property Span As TextSpan
            Get
                Dim result As TextSpan = MyBase.Span

                ' May need to adjust the span to make sure RegionDataFlowAnalysis
                ' is able to accurately track position within the region.
                Dim parent As SyntaxNode = Me.Syntax.Parent

                If parent IsNot Nothing Then

                    Dim startAdjuster As SyntaxNode = Nothing

                    If Me.Syntax.Kind = SyntaxKind.FunctionAggregation Then
                        ' In order to make region flow analysis work, we need to use begining of AggregateClauseSyntax
                        ' as the start for the only item in the [Into] clause. The reason is that the group expression
                        ' is inlined into this expression and its syntax node preceeds the [Into] clause.
                        If parent.Kind = SyntaxKind.AggregationRangeVariable Then
                            parent = parent.Parent

                            If parent IsNot Nothing AndAlso parent.Kind = SyntaxKind.AggregateClause AndAlso
                               DirectCast(parent, AggregateClauseSyntax).AggregationVariables.Count = 1 Then
                                Debug.Assert(Me.Syntax.Parent Is DirectCast(parent, AggregateClauseSyntax).AggregationVariables(0))
                                startAdjuster = parent
                            End If
                        End If
                    Else
                        Select Case parent.Kind
                            Case SyntaxKind.QueryExpression, SyntaxKind.AggregateClause
                                If TypeOf Me.Syntax Is QueryClauseSyntax Then
                                    ' Span should begin at the beginning of the QueryExpression.
                                    startAdjuster = parent
                                End If

                            Case SyntaxKind.LetClause
                                If Me.Syntax.Kind = SyntaxKind.ExpressionRangeVariable Then
                                    parent = parent.Parent

                                    If parent IsNot Nothing AndAlso
                                       (parent.Kind = SyntaxKind.QueryExpression OrElse parent.Kind = SyntaxKind.AggregateClause) Then
                                        ' Span should begin at the beginning of the QueryExpression.
                                        startAdjuster = parent
                                    End If
                                End If

                            Case SyntaxKind.FromClause
                                If Me.Syntax.Kind = SyntaxKind.CollectionRangeVariable Then
                                    parent = parent.Parent

                                    If parent IsNot Nothing AndAlso
                                       (parent.Kind = SyntaxKind.QueryExpression OrElse parent.Kind = SyntaxKind.AggregateClause) Then
                                        ' Span should begin at the beginning of the QueryExpression.
                                        startAdjuster = parent
                                    End If
                                End If

                            Case SyntaxKind.JoinClause, SyntaxKind.GroupJoinClause
                                If Me.Syntax.Kind = SyntaxKind.JoinClause OrElse
                                   Me.Syntax.Kind = SyntaxKind.GroupJoinClause Then
                                    ' Span should begin at the beginning of the parent Join.
                                    startAdjuster = parent
                                End If
                        End Select
                    End If

                    If startAdjuster IsNot Nothing Then

                        ' Span should begin at the beginning of the startAdjuster.
                        Dim startSpan As TextSpan = startAdjuster.Span

                        If startSpan.Start < result.Start Then
                            Return TextSpan.FromBounds(startSpan.Start, result.End)
                        End If
                    End If
                End If

                Return result
            End Get
        End Property

    End Class


    ''' <summary>
    ''' Adding in this file because this is a temporary solution, pending flow analysis change from spans to nodes.
    ''' This file should be deleted then. 
    ''' </summary>
    Partial Class BoundRangeVariableAssignment

        Public Overrides ReadOnly Property Span As TextSpan
            Get
                Dim result As TextSpan = MyBase.Span

                If Me.Syntax.Kind = SyntaxKind.AggregationRangeVariable Then
                    Dim item = DirectCast(Me.Syntax, AggregationRangeVariableSyntax)

                    ' In order to make region flow analysis work, we need to use span of AggregateClauseSyntax
                    ' as a syntax node for the only item in the [Into] clause. The reason is that the group expression
                    ' is inlined into the selector and its syntax node preceeds the [Into] clause.
                    If item.Parent.Kind = SyntaxKind.AggregateClause AndAlso
                       DirectCast(item.Parent, AggregateClauseSyntax).AggregationVariables.Count = 1 Then
                        Debug.Assert(item Is DirectCast(item.Parent, AggregateClauseSyntax).AggregationVariables(0))
                        result = item.Parent.Span
                    End If
                End If

                Return result
            End Get
        End Property

    End Class
End Namespace


