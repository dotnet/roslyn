' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.Utilities.IntrinsicOperators

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.EventHandling
    ''' <summary>
    ''' Recommends the "AddHandler" keyword.
    ''' </summary>
    Friend Class AddHandlerKeywordRecommender
        Inherits AbstractKeywordRecommender

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As IEnumerable(Of RecommendedKeyword)
            If context.IsSingleLineStatementContext OrElse context.CanDeclareCustomEventAccessor(SyntaxKind.AddHandlerAccessorBlock) Then
                Return SpecializedCollections.SingletonEnumerable(
                                CreateRecommendedKeywordForIntrinsicOperator(SyntaxKind.AddHandlerKeyword,
                                                                             VBFeaturesResources.AddhandlerStatement,
                                                                             Glyph.Keyword,
                                                                             New AddHandlerStatementDocumentation()))
            Else
                Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
            End If
        End Function
    End Class
End Namespace
