' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.PreprocessorDirectives
    ''' <summary>
    ''' Recommends the "#End Region" directive
    ''' </summary>
    Friend Class EndRegionDirectiveKeywordRecommender
        Inherits AbstractKeywordRecommender

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As IEnumerable(Of RecommendedKeyword)
            If context.IsPreprocessorEndDirectiveKeywordContext AndAlso
               HasUnmatchedRegionDirective(context, cancellationToken) Then

                Return SpecializedCollections.SingletonEnumerable(New RecommendedKeyword("Region", VBFeaturesResources.EndRegionKeywordToolTip))
            End If

            If context.IsPreprocessorStartContext Then
                Dim directives = context.SyntaxTree.GetStartDirectives(cancellationToken)

                If HasUnmatchedRegionDirective(context, cancellationToken) Then
                    Return SpecializedCollections.SingletonEnumerable(New RecommendedKeyword("#End Region", VBFeaturesResources.EndRegionKeywordToolTip))
                End If
            End If

            Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
        End Function

        Private Function HasUnmatchedRegionDirective(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As Boolean
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
