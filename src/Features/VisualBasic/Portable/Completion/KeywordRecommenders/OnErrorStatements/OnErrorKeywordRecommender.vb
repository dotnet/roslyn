' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.OnErrorStatements
    ''' <summary>
    ''' Recommends "On Error Resume Next" and "On Error Goto" in a statement context.
    ''' </summary>
    Friend Class OnErrorKeywordRecommender
        Inherits AbstractKeywordRecommender

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As IEnumerable(Of RecommendedKeyword)
            If context.IsMultiLineStatementContext AndAlso Not context.IsInLambda Then
                Return {New RecommendedKeyword("On Error Resume Next", VBFeaturesResources.OnErrorResumeNextKeywordToolTip),
                        New RecommendedKeyword("On Error GoTo", VBFeaturesResources.OnErrorGotoKeywordToolTip)}
            Else
                Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
            End If
        End Function
    End Class
End Namespace
