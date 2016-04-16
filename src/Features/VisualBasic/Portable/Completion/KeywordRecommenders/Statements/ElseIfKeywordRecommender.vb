' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Statements
    ''' <summary>
    ''' Recommends the "ElseIf" keyword for the statement context
    ''' </summary>
    Friend Class ElseIfKeywordRecommender
        Inherits AbstractKeywordRecommender

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As IEnumerable(Of RecommendedKeyword)
            If context.IsSingleLineStatementContext AndAlso
               context.IsInStatementBlockOfKind(SyntaxKind.MultiLineIfBlock, SyntaxKind.ElseIfBlock) AndAlso
               Not context.IsInStatementBlockOfKind(SyntaxKind.ElseBlock) Then

                Return SpecializedCollections.SingletonEnumerable(New RecommendedKeyword("ElseIf", VBFeaturesResources.ElseIfKeywordToolTip))
            End If

            Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
        End Function
    End Class
End Namespace
