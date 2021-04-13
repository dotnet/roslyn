' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.PreprocessorDirectives
    ''' <summary>
    ''' Recommends the "#End If" preprocessor directive
    ''' </summary>
    Friend Class EndIfDirectiveKeywordRecommender
        Inherits AbstractKeywordRecommender

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As ImmutableArray(Of RecommendedKeyword)
            If context.IsPreprocessorEndDirectiveKeywordContext AndAlso HasMatchingIfDirective(context, cancellationToken) Then

                Return ImmutableArray.Create(New RecommendedKeyword("If", VBFeaturesResources.Terminates_the_definition_of_an_SharpIf_block))
            End If

            If context.IsPreprocessorStartContext Then
                Dim innermostKind = context.SyntaxTree.GetInnermostIfPreprocessorKind(context.Position, cancellationToken)

                If innermostKind.HasValue Then
                    Return ImmutableArray.Create(New RecommendedKeyword("#End If", VBFeaturesResources.Terminates_the_definition_of_an_SharpIf_block))
                End If
            End If

            Return ImmutableArray(Of RecommendedKeyword).Empty
        End Function

        Private Shared Function HasMatchingIfDirective(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As Boolean
            Dim innermostKind = context.SyntaxTree.GetInnermostIfPreprocessorKind(context.Position, cancellationToken)

            Return innermostKind.HasValue
        End Function
    End Class
End Namespace
