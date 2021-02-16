' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
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

        Private Shared ReadOnly s_keywords As ImmutableArray(Of RecommendedKeyword) =
            ImmutableArray.Create(New RecommendedKeyword("Handles", VBFeaturesResources.Declares_that_a_procedure_handles_a_specified_event))

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As ImmutableArray(Of RecommendedKeyword)
            If context.FollowsEndOfStatement Then
                Return ImmutableArray(Of RecommendedKeyword).Empty
            End If

            If context.IsFollowingParameterListOrAsClauseOfMethodDeclaration() Then
                Dim targetToken = context.TargetToken
                Dim typeBlock = targetToken.GetAncestor(Of TypeBlockSyntax)()

                If typeBlock Is Nothing OrElse Not typeBlock.IsKind(SyntaxKind.ClassBlock, SyntaxKind.ModuleBlock) Then
                    Return ImmutableArray(Of RecommendedKeyword).Empty
                End If

                Dim methodDeclaration = targetToken.GetAncestor(Of MethodStatementSyntax)()
                If methodDeclaration Is Nothing OrElse methodDeclaration.Modifiers.Any(SyntaxKind.IteratorKeyword) Then
                    Return ImmutableArray(Of RecommendedKeyword).Empty
                End If

                Return s_keywords
            End If

            Return ImmutableArray(Of RecommendedKeyword).Empty
        End Function
    End Class
End Namespace
