' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.EventHandling
    ''' <summary>
    ''' Recommends the "Handles" keyword.
    ''' </summary>
    Friend Class HandlesKeywordRecommender
        Inherits AbstractKeywordRecommender

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As IEnumerable(Of RecommendedKeyword)
            If context.FollowsEndOfStatement Then
                Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
            End If

            If context.IsFollowingParameterListOrAsClauseOfMethodDeclaration() Then
                Dim targetToken = context.TargetToken
                Dim typeBlock = targetToken.GetAncestor(Of TypeBlockSyntax)()

                If typeBlock Is Nothing OrElse Not typeBlock.IsKind(SyntaxKind.ClassBlock, SyntaxKind.ModuleBlock) Then
                    Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
                End If

                Dim methodDeclaration = targetToken.GetAncestor(Of MethodStatementSyntax)()
                If methodDeclaration Is Nothing OrElse methodDeclaration.Modifiers.Any(SyntaxKind.IteratorKeyword) Then
                    Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
                End If

                Return SpecializedCollections.SingletonEnumerable(New RecommendedKeyword("Handles", VBFeaturesResources.HandlesKeywordToolTip))
            End If

            Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
        End Function
    End Class
End Namespace
