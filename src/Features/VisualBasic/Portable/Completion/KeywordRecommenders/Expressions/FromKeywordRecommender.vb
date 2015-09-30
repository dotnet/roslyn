' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Expressions
    ''' <summary>
    ''' Recommends the "From" keyword when used in a New syntax (such as New foo From)
    ''' </summary>
    Friend Class FromKeywordRecommender
        Inherits AbstractKeywordRecommender

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As IEnumerable(Of RecommendedKeyword)
            If context.FollowsEndOfStatement Then
                Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
            End If

            Dim targetToken = context.TargetToken

            If targetToken.IsFollowingCompleteAsNewClause() OrElse
               targetToken.IsFollowingCompleteObjectCreation() Then

                Dim objectCreation = targetToken.GetAncestor(Of ObjectCreationExpressionSyntax)()
                Dim type = TryCast(context.SemanticModel.GetSymbolInfo(objectCreation.Type, cancellationToken).Symbol, ITypeSymbol)
                Dim enclosingSymbol = context.SemanticModel.GetEnclosingNamedTypeOrAssembly(context.Position, cancellationToken)
                If type IsNot Nothing AndAlso type.CanSupportCollectionInitializer(enclosingSymbol) Then
                    Return SpecializedCollections.SingletonEnumerable(New RecommendedKeyword("From", VBFeaturesResources.FromCollectionInitializerKeywordToolTip))
                End If
            End If

            Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
        End Function
    End Class
End Namespace
