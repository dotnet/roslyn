' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.PreprocessorDirectives
    ''' <summary>
    ''' Recommends the "#End If" preprocessor directive
    ''' </summary>
    Friend Class EndIfDirectiveKeywordRecommender
        Inherits AbstractKeywordRecommender

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As IEnumerable(Of RecommendedKeyword)
            If context.IsPreprocessorEndDirectiveKeywordContext AndAlso HasMatchingIfDirective(context, cancellationToken) Then

                Return SpecializedCollections.SingletonEnumerable(New RecommendedKeyword("If", VBFeaturesResources.EndIfCCKeywordToolTip))
            End If

            If context.IsPreprocessorStartContext Then
                Dim innermostKind = context.SyntaxTree.GetInnermostIfPreprocessorKind(context.Position, cancellationToken)

                If innermostKind.HasValue Then
                    Return SpecializedCollections.SingletonEnumerable(New RecommendedKeyword("#End If", VBFeaturesResources.EndIfCCKeywordToolTip))
                End If
            End If

            Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
        End Function

        Private Function HasMatchingIfDirective(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As Boolean
            Dim innermostKind = context.SyntaxTree.GetInnermostIfPreprocessorKind(context.Position, cancellationToken)

            Return innermostKind.HasValue
        End Function
    End Class
End Namespace
