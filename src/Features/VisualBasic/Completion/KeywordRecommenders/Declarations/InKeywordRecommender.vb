' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As IEnumerable(Of RecommendedKeyword)
            If context.FollowsEndOfStatement Then
                Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
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
                Return SpecializedCollections.SingletonEnumerable(New RecommendedKeyword("In", VBFeaturesResources.InForEachKeywordToolTip))
            End If

            ' From element |
            ' Group Join element |
            If targetToken.IsFromIdentifierNode(Of CollectionRangeVariableSyntax)(Function(rangeVariable) rangeVariable.Identifier) OrElse
               IsAfterCompleteAsClause(Of CollectionRangeVariableSyntax)(context, Function(rangeVariable) rangeVariable.AsClause, cancellationToken) Then
                Return SpecializedCollections.SingletonEnumerable(New RecommendedKeyword("In", VBFeaturesResources.InQueryKeywordToolTip))
            End If

            Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
        End Function

        Private Function IsAfterCompleteAsClause(Of T As {SyntaxNode})(
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
