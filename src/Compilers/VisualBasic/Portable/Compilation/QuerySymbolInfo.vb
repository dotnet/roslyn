' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    Public Structure CollectionRangeVariableSymbolInfo
        ''' <summary>
        ''' Optional AsQueryable/AsEnumerable/Cast(Of Object) method used 
        ''' to "convert" <see cref="CollectionRangeVariableSyntax.Expression"/> to queryable
        ''' collection.
        ''' </summary>
        Public ReadOnly Property ToQueryableCollectionConversion As SymbolInfo

        ''' <summary>
        ''' Optional Select method to handle AsClause.
        ''' </summary>
        Public ReadOnly Property AsClauseConversion As SymbolInfo

        ''' <summary>
        ''' SelectMany method for <see cref="CollectionRangeVariableSyntax"/>, which is not the first
        ''' <see cref="CollectionRangeVariableSyntax"/> in a <see cref="QueryExpressionSyntax"/>, and is not the first 
        ''' <see cref="CollectionRangeVariableSyntax"/> in <see cref="AggregateClauseSyntax"/>.
        ''' </summary>
        Public ReadOnly Property SelectMany As SymbolInfo

        Friend Shared ReadOnly None As New CollectionRangeVariableSymbolInfo(SymbolInfo.None, SymbolInfo.None, SymbolInfo.None)

        Friend Sub New(
            toQueryableCollectionConversion As SymbolInfo,
            asClauseConversion As SymbolInfo,
            selectMany As SymbolInfo
        )
            Me.ToQueryableCollectionConversion = toQueryableCollectionConversion
            Me.AsClauseConversion = asClauseConversion
            Me.SelectMany = selectMany
        End Sub
    End Structure

    Public Structure AggregateClauseSymbolInfo
        ''' <summary>
        ''' The first of the two optional Select methods associated with <see cref="AggregateClauseSyntax"/>.
        ''' </summary>
        Public ReadOnly Property Select1 As SymbolInfo

        ''' <summary>
        ''' The second of the two optional Select methods associated with <see cref="AggregateClauseSyntax"/>.
        ''' </summary>
        Public ReadOnly Property Select2 As SymbolInfo

        Friend Sub New(select1 As SymbolInfo)
            Me.Select1 = select1
            Me.Select2 = SymbolInfo.None
        End Sub

        Friend Sub New(select1 As SymbolInfo, select2 As SymbolInfo)
            Me.Select1 = select1
            Me.Select2 = select2
        End Sub
    End Structure

    Partial Friend Class VBSemanticModel

        ''' <summary>
        ''' Returns information about methods associated with CollectionRangeVariableSyntax.
        ''' </summary>
        Public Function GetCollectionRangeVariableSymbolInfo(
            variableSyntax As CollectionRangeVariableSyntax,
            Optional cancellationToken As CancellationToken = Nothing
        ) As CollectionRangeVariableSymbolInfo
            If variableSyntax Is Nothing Then
                Throw New ArgumentNullException(NameOf(variableSyntax))
            End If
            If Not IsInTree(variableSyntax) Then
                Throw New ArgumentException(VBResources.VariableSyntaxNotWithinSyntaxTree)
            End If

            Return GetCollectionRangeVariableSymbolInfoWorker(variableSyntax, cancellationToken)
        End Function

        Friend MustOverride Function GetCollectionRangeVariableSymbolInfoWorker(node As CollectionRangeVariableSyntax, Optional cancellationToken As CancellationToken = Nothing) As CollectionRangeVariableSymbolInfo

        ''' <summary>
        ''' Returns information about methods associated with AggregateClauseSyntax.
        ''' </summary>
        Public Function GetAggregateClauseSymbolInfo(
            aggregateSyntax As AggregateClauseSyntax,
            Optional cancellationToken As CancellationToken = Nothing
        ) As AggregateClauseSymbolInfo
            If aggregateSyntax Is Nothing Then
                Throw New ArgumentNullException(NameOf(aggregateSyntax))
            End If
            If Not IsInTree(aggregateSyntax) Then
                Throw New ArgumentException(VBResources.AggregateSyntaxNotWithinSyntaxTree)
            End If

            ' Stand-alone Aggregate does not use Select methods.
            If aggregateSyntax.Parent Is Nothing OrElse
               (aggregateSyntax.Parent.Kind = SyntaxKind.QueryExpression AndAlso
                DirectCast(aggregateSyntax.Parent, QueryExpressionSyntax).Clauses.FirstOrDefault Is aggregateSyntax) Then
                Return New AggregateClauseSymbolInfo(SymbolInfo.None)
            End If

            Return GetAggregateClauseSymbolInfoWorker(aggregateSyntax, cancellationToken)
        End Function

        Friend MustOverride Function GetAggregateClauseSymbolInfoWorker(node As AggregateClauseSyntax, Optional cancellationToken As CancellationToken = Nothing) As AggregateClauseSymbolInfo

        ''' <summary>
        ''' DistinctClauseSyntax -       Returns Distinct method associated with DistinctClauseSyntax.
        ''' 
        ''' WhereClauseSyntax -          Returns Where method associated with WhereClauseSyntax.
        ''' 
        ''' PartitionWhileClauseSyntax - Returns TakeWhile/SkipWhile method associated with PartitionWhileClauseSyntax.
        ''' 
        ''' PartitionClauseSyntax -      Returns Take/Skip method associated with PartitionClauseSyntax.
        ''' 
        ''' GroupByClauseSyntax -        Returns GroupBy method associated with GroupByClauseSyntax.
        ''' 
        ''' JoinClauseSyntax -           Returns Join/GroupJoin method associated with JoinClauseSyntax/GroupJoinClauseSyntax.
        ''' 
        ''' SelectClauseSyntax -         Returns Select method associated with SelectClauseSyntax, if needed.
        ''' 
        ''' FromClauseSyntax -           Returns Select method associated with FromClauseSyntax, which has only one 
        '''                              CollectionRangeVariableSyntax and is the only query clause within 
        '''                              QueryExpressionSyntax. NotNeeded SymbolInfo otherwise. 
        '''                              The method call is injected by the compiler to make sure that query is translated to at 
        '''                              least one method call. 
        ''' 
        ''' LetClauseSyntax -            NotNeeded SymbolInfo.
        ''' 
        ''' OrderByClauseSyntax -        NotNeeded SymbolInfo.
        ''' 
        ''' AggregateClauseSyntax -      Empty SymbolInfo. GetAggregateClauseInfo should be used instead.
        ''' </summary>
        Public Shadows Function GetSymbolInfo(
            clauseSyntax As QueryClauseSyntax,
            Optional cancellationToken As CancellationToken = Nothing
        ) As SymbolInfo
            CheckSyntaxNode(clauseSyntax)

            If CanGetSemanticInfo(clauseSyntax) Then
                Select Case clauseSyntax.Kind
                    Case SyntaxKind.LetClause, SyntaxKind.OrderByClause
                        Return SymbolInfo.None

                    Case SyntaxKind.AggregateClause
                        Return SymbolInfo.None
                End Select

                Return GetQueryClauseSymbolInfo(clauseSyntax, cancellationToken)
            Else
                Return SymbolInfo.None
            End If
        End Function

        Friend MustOverride Function GetQueryClauseSymbolInfo(node As QueryClauseSyntax, Optional cancellationToken As CancellationToken = Nothing) As SymbolInfo

        ''' <summary>
        ''' Returns Select method associated with ExpressionRangeVariableSyntax within a LetClauseSyntax, if needed.
        ''' NotNeeded SymbolInfo otherwise.
        ''' </summary>
        Public Shadows Function GetSymbolInfo(
            variableSyntax As ExpressionRangeVariableSyntax,
            Optional cancellationToken As CancellationToken = Nothing
        ) As SymbolInfo
            CheckSyntaxNode(variableSyntax)

            If CanGetSemanticInfo(variableSyntax) Then
                If variableSyntax.Parent Is Nothing OrElse variableSyntax.Parent.Kind <> SyntaxKind.LetClause Then
                    Return SymbolInfo.None
                End If

                Return GetLetClauseSymbolInfo(variableSyntax, cancellationToken)
            Else
                Return SymbolInfo.None
            End If
        End Function

        Friend MustOverride Function GetLetClauseSymbolInfo(node As ExpressionRangeVariableSyntax, Optional cancellationToken As CancellationToken = Nothing) As SymbolInfo

        ''' <summary>
        ''' Returns aggregate function associated with FunctionAggregationSyntax.
        ''' </summary>
        Public Shadows Function GetSymbolInfo(
            functionSyntax As FunctionAggregationSyntax,
            Optional cancellationToken As CancellationToken = Nothing
        ) As SymbolInfo
            CheckSyntaxNode(functionSyntax)

            If CanGetSemanticInfo(functionSyntax) Then
                If Not IsInTree(functionSyntax) Then
                    Throw New ArgumentException(VBResources.FunctionSyntaxNotWithinSyntaxTree)
                End If

                Return GetSymbolInfo(DirectCast(functionSyntax, ExpressionSyntax), cancellationToken)
            Else
                Return SymbolInfo.None
            End If
        End Function

        ''' <summary>
        ''' Returns OrderBy/OrderByDescending/ThenBy/ThenByDescending method associated with OrderingSyntax.
        ''' </summary>
        Public Shadows Function GetSymbolInfo(
            orderingSyntax As OrderingSyntax,
            Optional cancellationToken As CancellationToken = Nothing
        ) As SymbolInfo
            CheckSyntaxNode(orderingSyntax)

            If CanGetSemanticInfo(orderingSyntax) Then
                Return GetOrderingSymbolInfo(orderingSyntax, cancellationToken)
            Else
                Return SymbolInfo.None
            End If
        End Function

        Friend MustOverride Function GetOrderingSymbolInfo(node As OrderingSyntax, Optional cancellationToken As CancellationToken = Nothing) As SymbolInfo
    End Class

End Namespace
