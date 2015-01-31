' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.OnErrorStatements
    ''' <summary>
    ''' Recommends "Next" after "On Error Resume" or after the "Resume" statement
    ''' </summary>
    Friend Class NextKeywordRecommender
        Inherits AbstractKeywordRecommender

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As IEnumerable(Of RecommendedKeyword)
            If context.FollowsEndOfStatement Then
                Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
            End If

            Dim targetToken = context.TargetToken
            If targetToken.IsKind(SyntaxKind.ResumeKeyword) AndAlso Not context.IsInLambda AndAlso
               targetToken.Parent.IsKind(SyntaxKind.OnErrorResumeNextStatement, SyntaxKind.ResumeStatement, SyntaxKind.ResumeNextStatement) Then
                Return SpecializedCollections.SingletonEnumerable(New RecommendedKeyword("Next",
                                                                                         VBFeaturesResources.OnErrorResumeNextKeywordToolTip))
            Else
                Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
            End If
        End Function
    End Class
End Namespace
