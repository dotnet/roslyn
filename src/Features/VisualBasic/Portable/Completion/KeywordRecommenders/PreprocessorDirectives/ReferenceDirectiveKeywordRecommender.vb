' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.PreprocessorDirectives
    ''' <summary>
    ''' Recommends the "#R" preprocessor directive
    ''' </summary>
    Friend Class ReferenceDirectiveKeywordRecommender
        Inherits AbstractKeywordRecommender

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As IEnumerable(Of RecommendedKeyword)
            If context.IsPreprocessorStartContext AndAlso Not context.SyntaxTree.IsEnumMemberNameContext(context) Then
                Return SpecializedCollections.SingletonEnumerable(New RecommendedKeyword("#R", VBFeaturesResources.ReferenceKeywordTooltip))
            End If

            Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
        End Function
    End Class
End Namespace
