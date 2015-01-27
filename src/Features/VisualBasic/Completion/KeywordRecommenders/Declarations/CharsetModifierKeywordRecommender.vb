' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Declarations
    ''' <summary>
    ''' Recommends one of the charset modifiers after a "Declare" keyword
    ''' </summary>
    Friend Class CharsetModifierKeywordRecommender
        Inherits AbstractKeywordRecommender

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As IEnumerable(Of RecommendedKeyword)
            If context.FollowsEndOfStatement Then
                Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
            End If

            Dim targetToken = context.TargetToken

            If targetToken.IsChildToken(Of DeclareStatementSyntax)(Function(externalMethodDeclaration) externalMethodDeclaration.DeclareKeyword) Then
                Return {New RecommendedKeyword("Ansi", VBFeaturesResources.AnsiKeywordToolTip),
                        New RecommendedKeyword("Unicode", VBFeaturesResources.UnicodeKeywordToolTip),
                        New RecommendedKeyword("Auto", VBFeaturesResources.AutoKeywordToolTip)}
            Else
                Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
            End If
        End Function
    End Class
End Namespace
