' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Declarations
    ''' <summary>
    ''' Recommends the "In" keyword in all types of declarations.
    ''' </summary>
    Friend Class InKeywordRecommender
        Inherits AbstractKeywordRecommender

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As ImmutableArray(Of RecommendedKeyword)
            If context.FollowsEndOfStatement Then
                Return ImmutableArray(Of RecommendedKeyword).Empty
            End If

            Dim targetToken = context.TargetToken

            Dim getForEachLoopAsOpt = Function(forEachStatement As ForEachStatementSyntax) As SimpleAsClauseSyntax
                                          ' TODO: make this API less ugly in the parser
                                          Dim variableDeclarator = TryCast(forEachStatement.ControlVariable, VariableDeclaratorSyntax)
                                          If variableDeclarator IsNot Nothing Then
                                              ' TODO: improve this
                                              Return DirectCast(variableDeclarator.AsClause, SimpleAsClauseSyntax)
                                          Else
                                              Return Nothing
                                          End If
                                      End Function

            ' For Each x |
            ' TODO: figure out if this is the parse tree not acting correctly here. Why is this a SyntaxNonTerminal?
            If targetToken.IsFromIdentifierNode(Of ForEachStatementSyntax)(Function(forEachStatement) forEachStatement.ControlVariable) OrElse
               IsAfterCompleteAsClause(Of ForEachStatementSyntax)(context, getForEachLoopAsOpt, cancellationToken) Then
                Return ImmutableArray.Create(New RecommendedKeyword("In", VBFeaturesResources.Specifies_the_group_that_the_loop_variable_in_a_For_Each_statement_is_to_traverse))
            End If

            ' From element |
            ' Group Join element |
            If targetToken.IsFromIdentifierNode(Of CollectionRangeVariableSyntax)(Function(rangeVariable) rangeVariable.Identifier) OrElse
               IsAfterCompleteAsClause(Of CollectionRangeVariableSyntax)(context, Function(rangeVariable) rangeVariable.AsClause, cancellationToken) Then
                Return ImmutableArray.Create(New RecommendedKeyword("In", VBFeaturesResources.Specifies_the_group_that_the_range_variable_is_to_traverse_in_a_query))
            End If

            Return ImmutableArray(Of RecommendedKeyword).Empty
        End Function

        Private Shared Function IsAfterCompleteAsClause(Of T As {SyntaxNode})(
                context As VisualBasicSyntaxContext, childGetter As Func(Of T, SimpleAsClauseSyntax), cancellationToken As CancellationToken) As Boolean

            Dim targetToken = context.TargetToken
            Dim ancestor = targetToken.GetAncestor(Of T)()

            If ancestor IsNot Nothing AndAlso childGetter(ancestor) IsNot Nothing Then
                Return context.SyntaxTree.IsFollowingCompleteExpression(Of SimpleAsClauseSyntax)(
                    context.Position, targetToken, Function(asClause) asClause.Type, cancellationToken)
            Else
                Return False
            End If
        End Function
    End Class
End Namespace
