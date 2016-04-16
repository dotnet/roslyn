' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Declarations

    Friend Class CovarianceModifiersKeywordRecommender
        Inherits AbstractKeywordRecommender

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As IEnumerable(Of RecommendedKeyword)
            If context.FollowsEndOfStatement Then
                Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
            End If

            Dim targetToken = context.TargetToken

            ' No matter what, these can only happen after an Of or a comma
            If Not targetToken.IsKind(SyntaxKind.OfKeyword, SyntaxKind.CommaToken) Then
                Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
            End If

            Dim parent = targetToken.Parent
            If parent Is Nothing Then
                Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
            End If

            Dim covarianceKeywords = {New RecommendedKeyword("In", VBFeaturesResources.CovarianceInKeywordToolTip),
                                      New RecommendedKeyword("Out", VBFeaturesResources.CovarianceOutKeywordToolTip)}

            If parent.IsChildNode(Of DelegateStatementSyntax)(Function(declaration) declaration.TypeParameterList) Then
                Return covarianceKeywords
            ElseIf parent.IsChildNode(Of TypeStatementSyntax)(Function(declaration) declaration.TypeParameterList) Then
                If parent.GetAncestor(Of TypeStatementSyntax)().IsKind(SyntaxKind.InterfaceStatement) Then
                    Return covarianceKeywords
                End If
            End If

            Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
        End Function
    End Class
End Namespace
