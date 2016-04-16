' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Declarations
    ''' <summary>
    ''' Recommends the "Function" and "Sub" keywords in external method declarations.
    ''' </summary>
    Friend Class ExternalSubFunctionKeywordRecommender
        Inherits AbstractKeywordRecommender

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As IEnumerable(Of RecommendedKeyword)
            If context.FollowsEndOfStatement Then
                Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
            End If

            Dim targetToken = context.TargetToken
            If targetToken.IsKind(SyntaxKind.DeclareKeyword, SyntaxKind.AnsiKeyword, SyntaxKind.UnicodeKeyword, SyntaxKind.AutoKeyword) AndAlso
               targetToken.GetAncestor(Of DeclareStatementSyntax)() IsNot Nothing Then
                Return {New RecommendedKeyword("Function", VBFeaturesResources.ExternFunctionKeywordToolTip),
                        New RecommendedKeyword("Sub", VBFeaturesResources.ExternSubKeywordToolTip)}
            Else
                Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
            End If
        End Function
    End Class
End Namespace
