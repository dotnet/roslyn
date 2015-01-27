' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Queries
    ''' <summary>
    ''' Recommends the "Ascending" and "Descending" contextual keywords in a Order By clause.
    ''' </summary>
    Friend Class AscendingDescendingKeywordRecommender
        Inherits AbstractKeywordRecommender

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As IEnumerable(Of RecommendedKeyword)
            If context.SyntaxTree.IsFollowingCompleteExpression(Of OrderingSyntax)(
                    context.Position, context.TargetToken, Function(orderingSyntax) orderingSyntax.Expression, cancellationToken) Then
                Return {New RecommendedKeyword("Ascending", VBFeaturesResources.AscendingQueryKeywordToolTip),
                        New RecommendedKeyword("Descending", VBFeaturesResources.DescendingQueryKeywordToolTip)}
            End If

            Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
        End Function
    End Class
End Namespace
