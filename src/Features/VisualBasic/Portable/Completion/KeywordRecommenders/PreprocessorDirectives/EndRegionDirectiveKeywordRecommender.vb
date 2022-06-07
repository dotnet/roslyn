' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.PreprocessorDirectives
    ''' <summary>
    ''' Recommends the "#End Region" directive
    ''' </summary>
    Friend Class EndRegionDirectiveKeywordRecommender
        Inherits AbstractKeywordRecommender

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As ImmutableArray(Of RecommendedKeyword)
            If context.IsPreprocessorEndDirectiveKeywordContext AndAlso
               HasUnmatchedRegionDirective(context, cancellationToken) Then

                Return ImmutableArray.Create(New RecommendedKeyword("Region", VBFeaturesResources.Terminates_a_SharpRegion_block))
            End If

            If context.IsPreprocessorStartContext Then
                Dim directives = context.SyntaxTree.GetStartDirectives(cancellationToken)

                If HasUnmatchedRegionDirective(context, cancellationToken) Then
                    Return ImmutableArray.Create(New RecommendedKeyword("#End Region", VBFeaturesResources.Terminates_a_SharpRegion_block))
                End If
            End If

            Return ImmutableArray(Of RecommendedKeyword).Empty
        End Function

        Private Shared Function HasUnmatchedRegionDirective(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As Boolean
            Dim directives = context.SyntaxTree.GetStartDirectives(cancellationToken)

            For Each directive In directives
                If directive.Kind = SyntaxKind.RegionDirectiveTrivia AndAlso directive.Span.End <= context.Position Then
                    If directive.GetMatchingStartOrEndDirective(cancellationToken) Is Nothing Then
                        Return True
                    End If
                End If
            Next

            Return False
        End Function

    End Class
End Namespace
