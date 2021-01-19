' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.OptionStatements
    ''' <summary>
    ''' Recommends the "On" and "Off" options that appear after Option Explicit.
    ''' </summary>
    Friend Class ExplicitOptionsRecommender
        Inherits AbstractKeywordRecommender

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As ImmutableArray(Of RecommendedKeyword)
            If context.FollowsEndOfStatement Then
                Return ImmutableArray(Of RecommendedKeyword).Empty
            End If

            If context.TargetToken.IsKind(SyntaxKind.ExplicitKeyword) Then
                Return ImmutableArray.Create(
                    New RecommendedKeyword("On", VBFeaturesResources.Turns_a_compiler_option_on),
                    New RecommendedKeyword("Off", VBFeaturesResources.Turns_a_compiler_option_off))
            Else
                Return ImmutableArray(Of RecommendedKeyword).Empty
            End If
        End Function
    End Class
End Namespace
