' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Queries
    ''' <summary>
    ''' Recommends the While keyword after a Skip/Take query
    ''' </summary>
    Friend Class WhileKeywordRecommender
        Inherits AbstractKeywordRecommender

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As IEnumerable(Of RecommendedKeyword)
            Dim targetToken = context.TargetToken

            ' We may get two different types, depending on whether the user has already typed the While or not.
            If targetToken.IsChildToken(Of PartitionClauseSyntax)(Function(partitionQuery) partitionQuery.SkipOrTakeKeyword) OrElse
               targetToken.IsChildToken(Of PartitionWhileClauseSyntax)(Function(partitionWhileQuery) partitionWhileQuery.SkipOrTakeKeyword) Then
                Return SpecializedCollections.SingletonEnumerable(New RecommendedKeyword("While", VBFeaturesResources.WhileQueryKeywordToolTip))
            End If

            Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
        End Function
    End Class
End Namespace
