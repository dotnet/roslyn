' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.OnErrorStatements
    ''' <summary>
    ''' Recommends "Error" after "On" in a "On Error" statement.
    ''' </summary>
    Friend Class ErrorKeywordRecommender
        Inherits AbstractKeywordRecommender

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As IEnumerable(Of RecommendedKeyword)
            Dim targetToken = context.TargetToken

            If targetToken.IsKind(SyntaxKind.OnKeyword) AndAlso IsOnErrorStatement(targetToken.Parent) Then
                Return {New RecommendedKeyword("Error Resume Next", VBFeaturesResources.OnErrorResumeNextKeywordToolTip),
                        New RecommendedKeyword("Error GoTo", VBFeaturesResources.OnErrorGotoKeywordToolTip)}
            End If

            ' The Error statement (i.e. "Error 11" to raise an error)
            If context.IsMultiLineStatementContext OrElse context.IsSingleLineStatementContext Then
                Return SpecializedCollections.SingletonEnumerable(New RecommendedKeyword("Error",
                                                                                         VBFeaturesResources.ErrorKeywordToolTip))
            End If

            Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
        End Function
    End Class
End Namespace
