' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.PreprocessorDirectives
    ''' <summary>
    ''' Recommends the "#!" preprocessor directive
    ''' </summary>
    Friend Class ShebangDirectiveKeywordRecommender
        Inherits AbstractKeywordRecommender

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As IEnumerable(Of RecommendedKeyword)
            Dim tree = context.SyntaxTree
            Dim previousToken = tree.FindTokenOnLeftOfPosition(context.Position, cancellationToken, includeDirectives:=True)
            Dim afterFirstHash = previousToken.IsKind(SyntaxKind.HashToken) AndAlso previousToken.SpanStart = 0 AndAlso Not previousToken.HasTrailingTrivia
            If tree.IsScript AndAlso (context.Position = 0 OrElse afterFirstHash) Then
                Return SpecializedCollections.SingletonEnumerable(New RecommendedKeyword("#!", VBFeaturesResources.ShebangKeywordToolTip))
            End If

            Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
        End Function
    End Class
End Namespace
