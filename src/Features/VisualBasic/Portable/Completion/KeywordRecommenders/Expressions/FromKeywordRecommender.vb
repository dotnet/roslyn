' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Expressions
    ''' <summary>
    ''' Recommends the "From" keyword when used in a New syntax (such as New goo From)
    ''' </summary>
    Friend Class FromKeywordRecommender
        Inherits AbstractKeywordRecommender

        Private Shared ReadOnly s_keywords As ImmutableArray(Of RecommendedKeyword) =
            ImmutableArray.Create(New RecommendedKeyword("From", VBFeaturesResources.Identifies_a_list_of_values_as_a_collection_initializer))

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As ImmutableArray(Of RecommendedKeyword)
            If context.FollowsEndOfStatement Then
                Return ImmutableArray(Of RecommendedKeyword).Empty
            End If

            Dim targetToken = context.TargetToken

            If targetToken.IsFollowingCompleteAsNewClause() OrElse
               targetToken.IsFollowingCompleteObjectCreation() Then

                Dim objectCreation = targetToken.GetAncestor(Of ObjectCreationExpressionSyntax)()
                Dim type = TryCast(context.SemanticModel.GetSymbolInfo(objectCreation.Type, cancellationToken).Symbol, ITypeSymbol)
                Dim enclosingSymbol = context.SemanticModel.GetEnclosingNamedTypeOrAssembly(context.Position, cancellationToken)
                If type IsNot Nothing AndAlso type.CanSupportCollectionInitializer(enclosingSymbol) Then
                    Return s_keywords
                End If
            End If

            Return ImmutableArray(Of RecommendedKeyword).Empty
        End Function
    End Class
End Namespace
