' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.OptionStatements
    ''' <summary>
    ''' Recommends the "Option" keyword
    ''' </summary>
    Friend Class OptionKeywordRecommender
        Inherits AbstractKeywordRecommender

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As IEnumerable(Of RecommendedKeyword)
            If context.IsPreProcessorDirectiveContext Then
                Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
            End If

            Dim targetToken = context.TargetToken

            ' If we have no left token, then we're at the start of the file
            If targetToken.Kind = SyntaxKind.None Then
                Return SpecializedCollections.SingletonEnumerable(New RecommendedKeyword("Option", VBFeaturesResources.OptionKeywordToolTip))
            End If

            ' Show if after an earlier option statement
            If context.IsAfterStatementOfKind(SyntaxKind.OptionStatement) Then
                Return SpecializedCollections.SingletonEnumerable(New RecommendedKeyword("Option", VBFeaturesResources.OptionKeywordToolTip))
            End If

            Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
        End Function
    End Class
End Namespace
