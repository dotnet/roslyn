' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Expressions
    ''' <summary>
    ''' Recommends the "AddressOf" keyword.
    ''' </summary>
    Friend Class AddressOfKeywordRecommender
        Inherits AbstractKeywordRecommender

        Private Shared ReadOnly s_keywords As ImmutableArray(Of RecommendedKeyword) =
            ImmutableArray.Create(New RecommendedKeyword("AddressOf", VBFeaturesResources.Creates_a_delegate_procedure_instance_that_references_the_specified_procedure_AddressOf_procedureName))

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, CancellationToken As CancellationToken) As ImmutableArray(Of RecommendedKeyword)
            If context.FollowsEndOfStatement Then
                Return ImmutableArray(Of RecommendedKeyword).Empty
            End If

            If context.IsDelegateCreationContext() Then
                Return s_keywords
            End If

            If context.IsAnyExpressionContext AndAlso Not context.TargetToken.Parent.IsKind(SyntaxKind.AddressOfExpression) Then
                Return s_keywords
            End If

            Return ImmutableArray(Of RecommendedKeyword).Empty
        End Function
    End Class
End Namespace
